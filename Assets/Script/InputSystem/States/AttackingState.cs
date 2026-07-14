using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class AttackingState : IGameState
{
    private readonly GameStateMachine _fsm;

    public AttackingState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: Attacking State";

        //Get largest weapon range.
        int largestRange = 0;
        foreach (var module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
        {
            //Chech if module are NOT empty
            if (module.ItemDefinition != null)
            {
                if (module.ItemDefinition.moduleType == ModuleType.Weapon && module.isOnline && module.currentRange > largestRange)
                    largestRange = module.currentRange;
            }
        }

        HexHighlighter.Instance.HighlightRangeUnderScreenPosition(
            SelectionService.Instance.SelectedUnit.transform.position,
            largestRange,
            HexHighlighter.Instance.weaponRange_Mat);
    }
    // Exit State.
    public void Exit() 
    {
        //Remove Range Highlight
        HexHighlighter.Instance.ClearRangeHighlights();
    }

    public void OnTap(Vector2 screenPos)
    {

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
            //Remove targets if exsist
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);
            Debug.Log("after SelectState invoke" + GameStateMachine.Instance.Current);
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
            .Select(h => h.collider.transform.root.GetComponent<Unit>())
            .Where(u => u != null)
            .Distinct() // prevent duplicates if a unit has multiple colliders
            .Where(u => HexGridLinesBaker.Instance.GetGridPosFromWorldPos(u.transform.position) == clickedHex)
            .ToList();

        // (4) Fall back for cases where none of the Units reported in the hex (e.g., you hit ground first):
        // Optionally, if you also want to include the one you directly hit even if hex-mapping fails:
        if (unitsInClickedHex.Count == 0)
        {
            var firstUnit = hits.Select(h => h.collider.transform.root.GetComponent<Unit>())
                                .FirstOrDefault(u => u != null);
            if (firstUnit != null)
                unitsInClickedHex.Add(firstUnit);
        }

        // (5) Use result
        if (unitsInClickedHex.Count == 0)
        {
            //No units in hex → deselect target
            SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
            return;
        }
        else if (unitsInClickedHex.Count == 1)
        {
            //Dont hit your self
            if (unitsInClickedHex[0] == SelectionService.Instance.SelectedUnit)
            {
                SelectionService.Instance.SelectedUnit.target_Unit_Script = null;
                GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
                return;
            }
            else
            { 
                //Trying to attack other then your self

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
            // Multiple in same hex → show a selection UI to the player

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

        Debug.Log("after SelectState invoke" + GameStateMachine.Instance.Current);

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

