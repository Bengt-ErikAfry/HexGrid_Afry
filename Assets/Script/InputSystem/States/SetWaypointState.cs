using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetWaypointState : IGameState
{
    private readonly GameStateMachine _fsm;

    public SetWaypointState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: Set Waypoint State";
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
            //InputManager.Instance.selectedObject = null;
            //InputManager.Instance.selectedObject_Unit_Script = null;

            HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);
            Debug.Log("after SetWaypointState invoke" + GameStateMachine.Instance.Current);

            //Debug where clicked
            Debug.Log("SetWaypointState OnTap at screenPos " + screenPos + " and Grid " + HexGridLinesBaker.Instance.GetGridPosFromWorldPos(HexHighlighter.Instance.highlightGO.transform.position) + " and WorldPos " + HexHighlighter.Instance.highlightGO.transform.position);

            UIManager.Instance.HideSelectedUnitView();

            RouteManager.Instance.AddWaypointToRoute(SelectionService.Instance.SelectedUnit, HexHighlighter.Instance.highlightGO.transform.position);
            InfoScreenManager.Instance.ShowInfoScreen();
            InfoScreenManager.Instance.ShowRoute();
            RouteManager.Instance.UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);

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
            //InputManager.Instance.selectedObject = null;
            //InputManager.Instance.selectedObject_Unit_Script = null;
            RouteManager.Instance.AddWaypointToRoute(SelectionService.Instance.SelectedUnit, clickPoint);
        }
        else if (unitsInClickedHex.Count == 1)
        {
            var unit = unitsInClickedHex[0];
            //InputManager.Instance.selectedObject = unit.gameObject;
            //InputManager.Instance.selectedObject_Unit_Script = unit;
            RouteManager.Instance.AddWaypointToObjectRoute(SelectionService.Instance.SelectedUnit, unit.gameObject);
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

            RouteManager.Instance.AddWaypointToObjectRoute(SelectionService.Instance.SelectedUnit, defaultUnit.gameObject);
            //InputManager.Instance.selectedObject = defaultUnit.gameObject;
            //InputManager.Instance.selectedObject_Unit_Script = defaultUnit;
        }

        // Highlight selected tile (keep your current behavior)
        HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);

        Debug.Log("after SetWaypointState invoke" + GameStateMachine.Instance.Current);

        /*
        //Show/Hide UI buttons
        if (InputManager.Instance.selectedObject_Unit_Script.isPlayerControlled)
        {
            //Player unit selected

            UIManager.Instance.ResetUI();
            if (InputManager.Instance.selectedObject_Unit_Script.movedThisTurn >= InputManager.Instance.selectedObject_Unit_Script.shipRuntimeData.currentMovmentRange)
            {
                UIManager.Instance.HideUnitMovmentControlls();
            }
            if (InputManager.Instance.selectedObject_Unit_Script.haveAttackedThisTurn)
            {
                UIManager.Instance.HideUnitAttackControlls();
            }
        }
        else
        {
            //Enemy unit selected

            UIManager.Instance.HideUnitMovmentControlls();
            UIManager.Instance.HideUnitAttackControlls();
        }
        */
        /*
        //Show asteroidview if asteroid selected
        if (InputManager.Instance.selectedObject_Unit_Script.unitType == Unit.UnitType.Asteroid)
        {
            UIManager.Instance.ShowAsteroidView();
            AsteroidManager.Instance.ShowAsteroidView();
        }
        else
        {
            //Show selected unit view
            if (InputManager.Instance.selectedObject_Unit_Script != null) UIManager.Instance.ShowSelectedUnitView();
        }*/

        //Debug where clicked
        Debug.Log("SetWaypointState OnTap at screenPos " + screenPos + " and Grid " + HexGridLinesBaker.Instance.GetGridPosFromWorldPos(HexHighlighter.Instance.highlightGO.transform.position) + " and WorldPos " + HexHighlighter.Instance.highlightGO.transform.position);
        
        InfoScreenManager.Instance.ShowInfoScreen();
        InfoScreenManager.Instance.ShowRoute();
        RouteManager.Instance.UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
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
