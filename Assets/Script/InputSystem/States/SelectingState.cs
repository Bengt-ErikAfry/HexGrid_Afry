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
        Debug.Log("OnTap called.");
        if (CameraManager.Instance.isMovingCamera) return;

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // (1) Hit everything along the ray. You can restrict with a layerMask if you have an "Enemy" layer.
        // Example: LayerMask enemyMask = LayerMask.GetMask("Enemy");
        // RaycastHit[] hits = Physics.RaycastAll(ray, 500f, enemyMask, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            // clear selection
            SelectionService.Instance.ClearSelection();

            //Clear highlighted tiles
            //TileManager.Instance.ClearHighlightedTiles(); NO NEED. HIGH UNDE SCREEN UNHIED ALL OTHER TILES

            //HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);
            //Debug.Log("after SelectState invoke" + GameStateMachine.Instance.Current);

            

            //Show hex under mouse position
            // Convert tap -> world -> axial (point-top)
            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
            //var hexClicked = HexMath.WorldToAxial_PointTop(worldPos, HexGridLinesBaker.Instance.hexSize);

            Debug.Log("SelectState OnTap at screenPos " + screenPos + " and Grid " + TileManager.Instance.GetTileCoordinateFromWorldPosition(worldPos) + " and WorldPos " + worldPos);

            // select unit via selection service
            SelectionService.Instance.SetSelectedHex(TileManager.Instance.GetTileCoordinateFromWorldPosition(worldPos));

            //Highlight tile under mouse position
            TileManager.Instance.HighlightTileUnderScreenPosition(screenPos);

            return; 
        }

        // Sort closest to farthest — useful for resolving the clicked hex
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // (2) Determine the clicked hex using the closest hit point (or use your own method if you have one)
        Vector3 clickPoint = hits[0].point;
        var clickedHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(clickPoint); // <-- swap to your actual hex system

        Debug.Log($"{hits[0].collider.gameObject.name} parent={hits[0].collider.transform.parent?.name} root={hits[0].collider.transform.root.name}");

        // (3) Map each hit to its root Unit, then filter to units inside the same clicked hex
        List<Unit> unitsInClickedHex = hits
            .Select(h => h.collider.transform.GetComponent<Unit>())
            .Where(u => u != null)
            .Distinct() // prevent duplicates if a unit has multiple colliders
            .Where(u => HexGridLinesBaker.Instance.GetGridPosFromWorldPos(u.transform.position) == clickedHex)
            .ToList();

        // (4) Fall back for cases where none of the Units reported in the hex (e.g., you hit ground first):
        if (unitsInClickedHex.Count == 0)
        {
            var firstUnit = hits.Select(h => h.collider.transform.GetComponent<Unit>())
                                .FirstOrDefault(u => u != null);
            if (firstUnit != null)
                unitsInClickedHex.Add(firstUnit);
        }

        // (5) Use result
        if (unitsInClickedHex.Count == 0)
        {
            SelectionService.Instance.ClearSelection();

            HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);

            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
            var hexClicked = HexMath.WorldToAxial_PointTop(worldPos, HexGridLinesBaker.Instance.hexSize);

            // select unit via selection service
            SelectionService.Instance.SetSelectedHex(hexClicked);
        }
        else if (unitsInClickedHex.Count == 1)
        {
            var unit = unitsInClickedHex[0];

            //Highlight hex under selected unit
            HexHighlighter.Instance.HighlightHexUnderWorldPosition(unit.transform.position);

            // select unit via selection service
            SelectionService.Instance.SetSelectedUnit(unit);

            //Show Route if exsisting
            if (unit.routeComponent != null)
            {
                if (unit.routeComponent.routeActions.Count > 0)
                {
                    HexGridManager.Instance.CalculateRoutePath(unit);
                }
            }

            Debug.Log($"SelectAsteroid mode selected and {unit.unitName} is clicked.");

            /*      ----Remove when new mining system is ok
            // start mining mission using selected unit
            var sel = SelectionService.Instance.SelectedUnit;
            if (sel != null && sel.miningComponent != null)
            {
                sel.miningComponent.StartMiningMission(unit.asteroidFieldComponent);
                sel.miningComponent.OnTurn();
            }*/
        }
        else
        {
            UIManager.Instance.ShowStackView(unitsInClickedHex, screenPos);

            var defaultUnit = unitsInClickedHex
                .OrderBy(u => Vector3.SqrMagnitude(u.transform.position - clickPoint))
                .First();

            // do not auto-set selection here; stack view will let player pick
        }
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
