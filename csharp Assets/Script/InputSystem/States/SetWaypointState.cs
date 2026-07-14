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

        // Use UIManager selection as the authoritative selected unit for gameplay
        Unit ownerUnit = UIManager.Instance.unit_script;
        if (ownerUnit == null)
        {
            Debug.LogWarning("No unit selected. Cannot add waypoint to route.");
            return;
        }

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);
            Debug.Log("after SetWaypointState invoke" + GameStateMachine.Instance.Current);

            Debug.Log("SetWaypointState OnTap at screenPos " + screenPos + " and Grid " + HexGridLinesBaker.Instance.GetGridPosFromWorldPos(HexHighlighter.Instance.highlightGO.transform.position) + " and WorldPos " + HexHighlighter.Instance.highlightGO.transform.position);

            UIManager.Instance.HideSelectedUnitView();

            // Pass the owner unit explicitly
            RouteManager.Instance.AddWaypointToRoute(ownerUnit, HexHighlighter.Instance.highlightGO.transform.position);
            InfoScreenManager.Instance.ShowInfoScreen();
            InfoScreenManager.Instance.ShowRoute();
            RouteManager.Instance.UpdateRouteViewFor(ownerUnit);

            return;
        }
        
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 clickPoint = hits[0].point;
        var clickedHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(clickPoint);

        List<Unit> unitsInClickedHex = hits
            .Select(h => h.collider.transform.root.GetComponent<Unit>())
            .Where(u => u != null)
            .Distinct()
            .Where(u => HexGridLinesBaker.Instance.GetGridPosFromWorldPos(u.transform.position) == clickedHex)
            .ToList();

        if (unitsInClickedHex.Count == 0)
        {
            // Add waypoint to owner's route at world click point
            RouteManager.Instance.AddWaypointToRoute(ownerUnit, clickPoint);
        }
        else if (unitsInClickedHex.Count == 1)
        {
            var targetUnit = unitsInClickedHex[0];
            // Add waypoint that points to an object (owner -> target)
            RouteManager.Instance.AddWaypointToObjectRoute(ownerUnit, targetUnit.gameObject);
        }
        else
        {
            UIManager.Instance.ShowStackView(unitsInClickedHex, screenPos);

            var defaultUnit = unitsInClickedHex
                .OrderBy(u => Vector3.SqrMagnitude(u.transform.position - clickPoint))
                .First();

            RouteManager.Instance.AddWaypointToObjectRoute(ownerUnit, defaultUnit.gameObject);
        }

        HexHighlighter.Instance.HighlightHexUnderScreenPosition(screenPos);

        Debug.Log("after SetWaypointState invoke" + GameStateMachine.Instance.Current);
        
        InfoScreenManager.Instance.ShowInfoScreen();
        InfoScreenManager.Instance.ShowRoute();
        RouteManager.Instance.UpdateRouteViewFor(ownerUnit);
    }

    public void OnDrag(Vector2 delta)
    {
        if (UIManager.Instance.stackViewRectTransform.gameObject.activeSelf == true)
        {
            return;
        }
        else
        {
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