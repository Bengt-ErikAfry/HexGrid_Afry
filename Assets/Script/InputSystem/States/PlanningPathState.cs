using System.Collections.Generic;
using UnityEngine;

public class PlanningPathState : IGameState
{
    private readonly GameStateMachine _fsm;

    public PlanningPathState(GameStateMachine fsm) => _fsm = fsm;

    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: PlanningPath State";

        // Show UI hints, ghost path, etc.
        MessageSystemManager.Instance.CollapseIfExpanded();

        //Toggle the buttons
        UIManager.Instance.EnterPathPlaningMode();

        // Ensure TileManager has up-to-date lookup (safe to call; TileManager may already auto-build)
        TileManager.Instance?.BuildTileLookup();

        // Show movement range using the new tile-based system (do not use HexHighlighter)
        var selected = SelectionService.Instance.SelectedUnit;
        if (selected == null)
        {
            Debug.LogWarning("PlanningPathState.Enter: no selected unit");
            return;
        }

        int remainingMoves = selected.shipRuntimeData.currentMovmentRange - selected.movedThisTurn;
        if (remainingMoves <= 0)
        {
            // nothing to highlight
            return;
        }

        // Find the tile under the selected unit
        var tile = TileManager.Instance?.GetTileFromWorldPosition(selected.transform.position);
        if (tile == null)
        {
            Debug.LogWarning("PlanningPathState.Enter: could not find tile for selected unit position");
            return;
        }

        var startCoord = new Vector2Int(tile.tileIndexCol, tile.tileIndexRow);

        // Compute reachable tile coords and highlight them via TileManager
        var reachable = TileManager.Instance.GetReachableTiles(startCoord, remainingMoves);
        TileManager.Instance.HighlightCoords(reachable);
    }

    public void Exit()
    {
        // Hide hints/cleanup if needed
        TileManager.Instance?.ClearHighlights();

        //Toggle the buttons
        UIManager.Instance.ResetUI();
    }

    public void OnTap(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Ray -> plane intersection (gameplay plane z=0)
        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter)) return;
        Vector3 worldPos = ray.GetPoint(enter);

        // Find target tile under pointer
        var targetTile = TileManager.Instance?.GetTileFromWorldPosition(worldPos);
        if (targetTile == null)
        {
            // nothing to path to
            TileManager.Instance?.ClearPath();
            SelectionService.Instance.SelectedUnit.currentMovePath = new List<Vector2Int>();
            return;
        }
        Vector2Int target = new Vector2Int(targetTile.tileIndexCol, targetTile.tileIndexRow);

        // Get selected unit and its start tile
        var selected = SelectionService.Instance.SelectedUnit;
        if (selected == null) return;
        var startTile = TileManager.Instance?.GetTileFromWorldPosition(selected.transform.position);
        if (startTile == null) { Debug.LogWarning("Start tile not found for selected unit."); return; }
        Vector2Int start = new Vector2Int(startTile.tileIndexCol, startTile.tileIndexRow);

        // Compute path (tile-based A*)
        var path = TileManager.Instance.FindPath(start, target);

        // Visualize path and markers
        TileManager.Instance.DrawLineStrip(path);
        TileManager.Instance.PlaceTurnMarkers(path, selected.shipRuntimeData.currentMovmentRange);

        // Save path on unit for MovementManager
        selected.currentMovePath = path ?? new List<Vector2Int>();

        // Optionally show movement range (tile-based)
        int remainingMoves = selected.shipRuntimeData.currentMovmentRange - selected.movedThisTurn;
        if (remainingMoves > 0)
        {
            var reachable = TileManager.Instance.GetReachableTiles(start, remainingMoves);
            TileManager.Instance.HighlightCoords(reachable);
        }
        Debug.Log("after PlanningPathState invoke" + GameStateMachine.Instance.Current);
    }

    public void OnDrag(Vector2 delta)
    {
        // While planning, you can either block camera panning
        // or allow it; pick one. Here: allow.
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