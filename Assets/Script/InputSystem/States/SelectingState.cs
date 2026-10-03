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

        //Clear selection
        SelectionService.Instance.ClearSelection();

        //Clear highlighted tiles
        TileManager.Instance.ClearHighlightedTiles(); 

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // 1) Get physics hits along ray (3D colliders)
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Send ray to a plane at z=0 to get the world position of the tap. This is useful for 2D overlap checks or other logic.
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter))
        {
            Debug.LogWarning("Tap ray missed the gameplay plane.");
            return;
        }
        Vector3 worldPos = ray.GetPoint(enter);

        Debug.Log($"Tap at screenPos {screenPos}, worldPos {worldPos}, hits={hits.Length}");

        //Get the hex tile that was clicked on. IF no hex tile was clicked, hexClicked will be null and hexcoord will be (0,0)
        HexGrid_TilePrefab hexClicked = TileManager.Instance.GetTileFromWorldPosition(worldPos);
        Vector2Int hexcoord = Vector2Int.zero;
        if (hexClicked != null)
        {
            //Tile was clicked, get the hex coordinates
            hexcoord = new Vector2Int(hexClicked.tileIndexCol, hexClicked.tileIndexRow);
            Debug.Log("SelectState OnTap at TILE in screenPos " + screenPos + " and Grid " + hexcoord + " and WorldPos " + worldPos);

            // select unit via selection service
            SelectionService.Instance.SetSelectedHex(worldPos);

            //Highlight tile under mouse position
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
        }
        else
        {
            //No tile was clicked, hexcoord will be (0,0)
            Debug.Log("SelectState OnTap at NO TILE in screenPos " + screenPos + " and Grid " + hexcoord + " and WorldPos " + worldPos);
        }

        //Start check for units.
        if(hits.Length > 0)
        {
            // Sort closest to farthest — useful for resolving the clicked hex
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // (2) Determine the clicked hex using the closest hit point (or use your own method if you have one)
            Vector3 clickPoint = hits[0].point;

            Debug.Log($"{hits[0].collider.gameObject.name} parent={hits[0].collider.transform.parent?.name} root={hits[0].collider.transform.root.name}");

            // (3) Map each hit to its root Unit, then filter to units inside the same clicked hex
            var unitsInClickedHex = new List<Unit>();
            foreach (var h in hits)
            {
                
                var u = h.collider.transform.GetComponent<Unit>();
                if (u == null) continue;
                unitsInClickedHex.Add(u);
            }
            unitsInClickedHex = unitsInClickedHex.Distinct().ToList();

            // (4) Fall back for cases where none of the Units reported in the hex (e.g., you hit ground first):
            if (unitsInClickedHex.Count == 0)
            {
                var firstUnit = hits.Select(h => h.collider.transform.GetComponent<Unit>())
                                    .FirstOrDefault(u => u != null);
                if (firstUnit != null)
                    unitsInClickedHex.Add(firstUnit);
            }
            else if (unitsInClickedHex.Count == 1)
            {
                var unit = unitsInClickedHex[0];

                // select unit via selection service
                SelectionService.Instance.SetSelectedUnit(unit);

                //Show Route if exsiting
                if (unit.routeComponent != null)
                {
                    if (unit.routeComponent.routeActions.Count > 0)
                    {
                        HexGridManager.Instance.CalculateRoutePath(unit);
                    }
                }

                Debug.Log($"SelectAsteroid mode selected and {unit.unitName} is clicked.");
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
