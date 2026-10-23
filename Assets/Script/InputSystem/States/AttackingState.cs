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

        // Send ray to a plane at z=0 to get the world position of the tap.
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter))
        {
            Debug.LogWarning("Tap ray missed the gameplay plane.");
            return;
        }
        Vector3 worldPos = ray.GetPoint(enter);

        // Physics hits (used for units/asteroids)
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Get the hex tile that was clicked on.
        HexGrid_TilePrefab hexClicked = TileManager.Instance.GetTileFromWorldPosition(worldPos);
        if (hexClicked == null)
        {
            // Clicked outside tile grid -> cancel attack and return to selecting
            if (SelectionService.Instance.SelectedUnit != null)
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;

            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            TileManager.Instance.ClearHighlightedTiles();
            Debug.Log("AttackingState OnTap: clicked outside tile grid -> cancelling attack and returning to Selecting.");
            return;
        }

        // Authoritative clicked coord
        Vector2Int clickedCoord = TileManager.Instance.WorldToAxial(worldPos);

        // Find units on that hex using GameManager authoritative list
        var unitsInClickedHex = new List<Unit>();
        if (GameManager.Instance != null && GameManager.Instance.unitsList != null)
        {
            unitsInClickedHex = GameManager.Instance.unitsList
                .Where(u => u != null && TileManager.Instance.WorldToAxial(u.transform.position) == clickedCoord)
                .Distinct()
                .ToList();
        }

        // Find asteroids on that hex
        var asteroidsInClickedHex = UnityEngine.Object.FindObjectsOfType<AsteroidComponent>()
            .Where(a => a != null && TileManager.Instance.WorldToAxial(a.transform.position) == clickedCoord)
            .ToList();

        // Case: empty tile (no asteroid and no unit) -> cancel attack and switch to selecting (and highlight tile)
        if (unitsInClickedHex.Count == 0 && asteroidsInClickedHex.Count == 0)
        {
            if (SelectionService.Instance.SelectedUnit != null)
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;

            SelectionService.Instance.SetSelectedHex(worldPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            Debug.Log("AttackingState OnTap: empty tile -> cancelling attack, selecting hex and returning to Selecting.");
            return;
        }

        // Case: multiple units OR asteroid + units -> show stack view
        if (unitsInClickedHex.Count > 1 || (unitsInClickedHex.Count >= 1 && asteroidsInClickedHex.Count >= 1))
        {
            UIManager.Instance.ShowStackView(unitsInClickedHex, asteroidsInClickedHex, screenPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log("AttackingState OnTap: multiple units or units+asteroid -> showing stack view.");
            return;
        }

        // Case: single unit (no asteroid)
        if (unitsInClickedHex.Count == 1 && asteroidsInClickedHex.Count == 0)
        {
            var unit = unitsInClickedHex[0];

            // Don't target yourself
            if (unit == SelectionService.Instance.SelectedUnit)
            {
                if (SelectionService.Instance.SelectedUnit != null)
                    SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
                GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
                Debug.Log("AttackingState OnTap: clicked on self -> cancelling attack and returning to Selecting.");
                return;
            }

            int rangeToTarget = AttackManager.Instance.GetRangeToTarget(SelectionService.Instance.SelectedUnit.gameObject, unit.gameObject);
            Debug.Log($"AttackingState: rangeToTarget={rangeToTarget}, largestRange={largestRange}");

            if (rangeToTarget <= largestRange)
            {
                // Target in range -> set target and proceed (attack flow handled elsewhere)
                SelectionService.Instance.SelectedUnit.target_Unit_Script = unit;
                TileManager.Instance.HighlightTileWorldPosition(worldPos);
                Debug.Log($"AttackingState OnTap: target '{unit.unitName}' selected and is in range.");

                // Show modules/info
                InfoScreenManager.Instance.ShowModules();
            }
            else
            {
                // Target out of range -> give feedback and cancel attack
                Debug.Log("AttackingState: Target is out of range, cancelling attack.");
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
                GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            }
            return;
        }

        // Case: asteroid (no units)
        if (unitsInClickedHex.Count == 0 && asteroidsInClickedHex.Count >= 1)
        {
            var asteroid = asteroidsInClickedHex[0];
            SelectionService.Instance.SetSelectedAsteroid(asteroid);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            Debug.Log($"AttackingState OnTap: asteroid '{asteroid.asteroidJsonFileName}' selected -> switching to Selecting.");
            return;
        }

        // Fallback: highlight tile and cancel attack
        if (SelectionService.Instance.SelectedUnit != null)
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
        TileManager.Instance.HighlightTileWorldPosition(worldPos);
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
        Debug.Log("AttackingState OnTap: fallback -> cancelling attack and returning to Selecting.");




        /*
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
        */
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

