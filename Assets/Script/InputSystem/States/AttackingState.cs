using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class AttackingState : IGameState
{
    private readonly GameStateMachine _fsm;
    public int largestRange = 0;

    public AttackingState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: Attacking State";

        //Get largest weapon range.
        largestRange = 0;
        foreach (var module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
        {
            //Chech if module are NOT empty
            if (module.ItemDefinition != null)
            {
                if (module.ItemDefinition.moduleType == ModuleType.Weapon && module.isOnline && module.currentRange > largestRange)
                    largestRange = module.currentRange;
            }
        }

        // Use TileManager to compute reachable tiles and highlight them (do not use HexHighlighter)
        var startTile = TileManager.Instance?.GetTileFromWorldPosition(SelectionService.Instance.SelectedUnit.transform.position);
        if (startTile == null) return;

        var startCoord = new Vector2Int(startTile.tileIndexCol, startTile.tileIndexRow);
        var reachable = TileManager.Instance.GetReachableTiles(startCoord, largestRange);
        TileManager.Instance.HighlightCoords(reachable, ColorManager.Instance.hex_AttackRange);

        /*
        HexHighlighter.Instance.HighlightRangeUnderScreenPosition(
            SelectionService.Instance.SelectedUnit.transform.position,
            largestRange,
            HexHighlighter.Instance.weaponRange_Mat);
        */
    }
    // Exit State.
    public void Exit() 
    {
        //Remove Range Highlight
        //HexHighlighter.Instance.ClearRangeHighlights();
        TileManager.Instance?.ClearHighlights();
    }

    public void OnTap(Vector2 screenPos)
    {
        Debug.Log("AttackingState OnTap at " + screenPos);
        if (CameraManager.Instance.isMovingCamera) return;

        // Remove Range Highlight
        TileManager.Instance?.ClearHighlights();

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // (1) Hit everything along the ray. You can restrict with a layerMask if you have an "Enemy" layer.
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);

        // If no physics hits, highlight the tile under pointer (if any) and deselect
        if (hits.Length == 0)
        {
            Debug.Log("AttackingState: No hits, deselecting target.");

            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            // Determine world point on gameplay plane and highlight single tile
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                var tile = TileManager.Instance?.GetTileFromWorldPosition(worldPos);
                if (tile != null)
                {
                    var coord = new Vector2Int(tile.tileIndexCol, tile.tileIndexRow);
                    TileManager.Instance.HighlightCoords(new[] { coord }, ColorManager.Instance.hex_Select_Empty);
                }
            }

            Debug.Log("after AttackingState invoke" + GameStateMachine.Instance.Current);
            return;
        }

        // Sort closest to farthest — useful for resolving the clicked tile
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Use closest hit point to find clicked tile
        Vector3 clickPoint = hits[0].point;
        var clickedTile = TileManager.Instance.GetTileFromWorldPosition(clickPoint);
        if (clickedTile == null)
        {
            Debug.Log("AttackingState: clicked tile not found, deselecting target.");
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            return;
        }
        var clickedCoord = new Vector2Int(clickedTile.tileIndexCol, clickedTile.tileIndexRow);

        // Map each hit to its root Unit, then filter to units inside the same clicked tile
        List<Unit> unitsInClickedTile = hits
            .Select(h => h.collider.transform.GetComponent<Unit>())
            .Where(u => u != null)
            .Distinct()
            .Where(u =>
            {
                var t = TileManager.Instance.GetTileFromWorldPosition(u.transform.position);
                return t != null && new Vector2Int(t.tileIndexCol, t.tileIndexRow) == clickedCoord;
            })
            .ToList();

        // Fall back: pick first unit hit if none matched by tile
        if (unitsInClickedTile.Count == 0)
        {
            var firstUnit = hits.Select(h => h.collider.transform.GetComponent<Unit>())
                                .FirstOrDefault(u => u != null);
            if (firstUnit != null) unitsInClickedTile.Add(firstUnit);
        }

        // Use result
        if (unitsInClickedTile.Count == 0)
        {
            Debug.Log("AttackingState: No units in clicked tile, deselecting target.");
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            return;
        }
        else if (unitsInClickedTile.Count == 1)
        {
            Debug.Log("AttackingState: One unit in clicked tile " + unitsInClickedTile[0].unitName + " in tile.");

            // Don't hit yourself
            if (unitsInClickedTile[0] == SelectionService.Instance.SelectedUnit)
            {
                Debug.Log("AttackingState: Clicked on self, deselecting target.");
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
                GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
                return;
            }
            else
            {
                int rangeToTarget = AttackManager.Instance.GetRangeToTarget(SelectionService.Instance.SelectedUnit.gameObject, unitsInClickedTile[0].gameObject);
                Debug.Log("AttackingState: Checking if target is in range. Largest weapon range: " + largestRange + ", Range to target: " + rangeToTarget);
                if (rangeToTarget < largestRange)
                {
                    Debug.Log("AttackingState: Target is in range, proceeding to attack.");
                }
                else
                {
                    Debug.Log("AttackingState: Target is out of range, cannot attack.");
                    // Optionally, you could provide feedback to the player here.
                    SelectionService.Instance.SetSelectedUnit(unitsInClickedTile[0]);
                    GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
                    TileManager.Instance.HighlightTileWorldPosition(unitsInClickedTile[0].transform.position);
                    if(!unitsInClickedTile[0].isPlayerControlled) UIManager.Instance.HideAllUI();
                    return;
                }

                Debug.Log("AttackingState: Trying to attack " + unitsInClickedTile[0].unitName);

                // Single unit in tile → select it and immediately try to attack
                var unit = unitsInClickedTile[0];

                // Set player target
                SelectionService.Instance.SelectedUnit.target_Unit_Script = unit;

                // Show modules/info
                InfoScreenManager.Instance.ShowModules();
            }
        }
        else
        {
            Debug.Log("AttackingState: Multiple units in clicked tile, showing selection UI.");

            // Show StackView for units in tile
            UIManager.Instance.ShowStackView(unitsInClickedTile, screenPos);

            // Optionally set a default (e.g., the closest one)
            var defaultUnit = unitsInClickedTile
                .OrderBy(u => Vector3.SqrMagnitude(u.transform.position - clickPoint))
                .First();

            // Set a default target so the player can see info about it in the UI
            SelectionService.Instance.SelectedUnit.target_Unit_Script = defaultUnit;
        }
        




        /*
        Debug.Log("AttackingState OnTap at " + screenPos);
        if (CameraManager.Instance.isMovingCamera) return;

        //Remove Range Highlight
        HexHighlighter.Instance.ClearRangeHighlights();

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // (1) Hit everything along the ray. You can restrict with a layerMask if you have an "Enemy" layer.
        // Example: LayerMask enemyMask = LayerMask.GetMask("Enemy");
        // RaycastHit[] hits = Physics.RaycastAll(ray, 500f, enemyMask, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            Debug.Log("AttackingState: No hits, deselecting target.");

            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);
            Debug.Log("after AttackingState invoke" + GameStateMachine.Instance.Current);
            return;
        }

        // Sort closest to farthest — useful for resolving the clicked hex
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // (2) Determine the clicked hex using the closest hit point (or use your own method if you have one)
        // If your HexHighlighter exposes a WorldToHex, prefer that for consistency.
        // Here I assume you have a method like HexGrid.Instance.WorldToHex(Vector3 worldPos)
        Vector3 clickPoint = hits[0].point;
        var clickedHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(clickPoint); // <-- swap to your actual hex system

        // (3) Map each hit to its root Unit, then filter to units inside the same clicked hex
        List<Unit> unitsInClickedHex = hits
            .Select(h => h.collider.transform.GetComponent<Unit>())
            .Where(u => u != null)
            .Distinct() // prevent duplicates if a unit has multiple colliders
            .Where(u => HexGridLinesBaker.Instance.GetGridPosFromWorldPos(u.transform.position) == clickedHex)
            .ToList();

        // (4) Fall back for cases where none of the Units reported in the hex (e.g., you hit ground first):
        // Optionally, if you also want to include the one you directly hit even if hex-mapping fails:
        
        /*if (unitsInClickedHex.Count == 0)
        {
            var firstUnit = hits.Select(h => h.collider.transform.root.GetComponent<Unit>())
                                .FirstOrDefault(u => u != null);
            if (firstUnit != null)
                unitsInClickedHex.Add(firstUnit);
        }*/
        /*
        // (5) Use result
        if (unitsInClickedHex.Count == 0)
        {
            Debug.Log("AttackingState: No units in clicked hex, deselecting target.");

            //No units in hex → deselect target
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            return;
        }
        else if (unitsInClickedHex.Count == 1)
        {
            Debug.Log("AttackingState: One unit in clicked hex " + unitsInClickedHex[0].unitName + " in hex.");

            //Dont hit your self
            if (unitsInClickedHex[0] == SelectionService.Instance.SelectedUnit)
            {
                Debug.Log("AttackingState: Clicked on self, deselecting target.");
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
                GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
                return;
            }
            else
            { 
                Debug.Log("AttackingState: Trying to attack " + unitsInClickedHex[0].unitName);

                // Single unit in hex → select it and immediately try to attack
                var unit = unitsInClickedHex[0];

                //Set player target
                SelectionService.Instance.SelectedUnit.target_Unit_Script = unit;

                // You can immediately attack here if that's the desired flow:
                InfoScreenManager.Instance.ShowModules();
            }
        }
        else
        {
            Debug.Log("AttackingState: Multiple units in clicked hex, showing selection UI.");

            //Set Enemy input state
            //GameStateMachine.Instance.SetState(GameplayStateId.UIOnly);

            //Show StackView
            UIManager.Instance.ShowStackView(unitsInClickedHex, screenPos);

            // Optionally set a default (e.g., the closest one)
            var defaultUnit = unitsInClickedHex
                .OrderBy(u => Vector3.SqrMagnitude(u.transform.position - clickPoint))
                .First();

            //Set a defult target so the player can see info about it in the UI, but they can still change their mind by clicking the stack view
            SelectionService.Instance.SelectedUnit.target_Unit_Script = defaultUnit;
        }

        Debug.Log("after AttackingState invoke" + GameStateMachine.Instance.Current);

    }*/
    }

    public void OnDrag(Vector2 delta)
    {
        if (UIManager.Instance.stackViewRectTransform.gameObject.activeSelf == true)
        {
            //Don't allow camera movement if stack view is open
            return;
        }
        else
        {
            // Allow 1-finger pan in selecting state if you want
            var move = new Vector3(-delta.x * 0.01f, -delta.y * 0.01f, 0);
            Camera.main.transform.Translate(move, Space.World);
        }
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

