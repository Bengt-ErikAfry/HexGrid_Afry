using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

// What hapens when player are in selecting state:
public class SelectingState : IGameState
{
    private readonly GameStateMachine _fsm;

    public SelectingState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter() 
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.activeState.text = "Active state: Selecting State";
        }
    }
    // Exit State.
    public void Exit() { }

    public void OnTap(Vector2 screenPos)
    {
        //Reset collections and highlights when player taps on the screen, so that player can select new unit or asteroid.
        SelectionService.Instance.ClearSelection();
        TileManager.Instance.ClearHighlightedTiles();
        UIManager.Instance.HideStackView();

        Debug.Log("OnTap called.");
        if (CameraManager.Instance.isMovingCamera) return;

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // 1) Get physics hits along ray (3D colliders) — keep for debug/fallback
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Send ray to a plane at z=0 to get the world position of the tap.
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter))
        {
            Debug.LogWarning("Tap ray missed the gameplay plane.");
            return;
        }
        Vector3 worldPos = ray.GetPoint(enter);

        Debug.Log($"Tap at screenPos {screenPos}, worldPos {worldPos}, hits={hits.Length}");

        // Get the hex tile that was clicked on.
        HexGrid_TilePrefab hexClicked = TileManager.Instance.GetTileFromWorldPosition(worldPos);
        if (hexClicked == null)
        {
            // 1) player clicked outside tilegrid

            Debug.Log("SelectState OnTap: clicked outside tile grid -> cleared selection/highlights.");
            return;
        }

        // Compute authoritative axial coordinate for the clicked world position.
        Vector2Int clickedCoord = TileManager.Instance.WorldToAxial(worldPos);
        Debug.Log($"SelectState OnTap at TILE coord {clickedCoord} and WorldPos {worldPos}");

        // Find units located on the same hex (use GameManager authoritative list).
        var unitsInClickedHex = new List<Unit>();
        if (GameManager.Instance != null && GameManager.Instance.unitsList != null)
        {
            unitsInClickedHex = GameManager.Instance.unitsList
                .Where(u => u != null && TileManager.Instance.WorldToAxial(u.transform.position) == clickedCoord)
                .Distinct()
                .ToList();
        }

        // Find asteroids located on the same hex.
        var asteroidsInClickedHex = UnityEngine.Object.FindObjectsOfType<AsteroidComponent>()
            .Where(a => a != null && TileManager.Instance.WorldToAxial(a.transform.position) == clickedCoord)
            .ToList();

        // Case 2: empty tile (no asteroid and no unit)
        if (unitsInClickedHex.Count == 0 && asteroidsInClickedHex.Count == 0)
        {
            SelectionService.Instance.SetSelectedHex(worldPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log("SelectState OnTap: empty tile -> selected hex and highlighted.");
            return;
        }

        // Case 5 or multiple units: tile with multiple units OR asteroid + one or more units -> show stack
        if (unitsInClickedHex.Count > 1 || (unitsInClickedHex.Count >= 1 && asteroidsInClickedHex.Count >= 1))
        {
            UIManager.Instance.ShowStackView(unitsInClickedHex, asteroidsInClickedHex, screenPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log("SelectState OnTap: multiple units or units+asteroid -> showing stack view.");
            return;
        }

        // Case 3: single unit (no asteroid)
        if (unitsInClickedHex.Count == 1 && asteroidsInClickedHex.Count == 0)
        {
            var unit = unitsInClickedHex[0];
            SelectionService.Instance.SetSelectedUnit(unit);

            // Show Route if existing
            if (unit.routeComponent != null && unit.routeComponent.routeActions.Count > 0)
            {
                HexGridManager.Instance.CalculateRoutePath(unit);
            }

            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log($"SelectState OnTap: single unit '{unit.unitName}' selected.");
            return;
        }

        // Case 4: tile with asteroid (and no units)
        if (unitsInClickedHex.Count == 0 && asteroidsInClickedHex.Count >= 1)
        {
            // If there are multiple asteroids (unlikely) pick the first
            var asteroid = asteroidsInClickedHex[0];
            SelectionService.Instance.SetSelectedAsteroid(asteroid);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log($"SelectState OnTap: asteroid '{asteroid.asteroidID}' selected.");
            return;
        }

        // Fallback: highlight tile
        TileManager.Instance.HighlightTileWorldPosition(worldPos);
    }

    public void OnDrag(Vector2 delta)
    {
        //Return if player is interacting with UI, so camera does not move when player is interacting with UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (UIManager.Instance.stackViewRectTransform.gameObject.activeSelf == true) return;
        var move = new Vector3(-delta.x * 0.01f, -delta.y * 0.01f, 0);
        Camera.main.transform.Translate(move, Space.World);
    }

    public void OnPinch(float amount)
    {
        Camera.main.orthographicSize = Mathf.Clamp(
            Camera.main.orthographicSize - amount,
            CameraManager.Instance.minZoom,
            CameraManager.Instance.maxZoom);
    }

    public void Tick(float dt) { }
}
