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

        //Show movment range.
        HexHighlighter.Instance.HighlightRangeUnderScreenPosition(
            SelectionService.Instance.SelectedUnit.transform.position,
            SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange - SelectionService.Instance.SelectedUnit.movedThisTurn,
            HexHighlighter.Instance.movmentRange_Mat);
    }

    public void Exit()
    {
        // Hide hints/cleanup if needed

        //Toggle the buttons
        UIManager.Instance.ResetUI();
    }

    public void OnTap(Vector2 screenPos)
    {
        var selected = SelectionService.Instance.SelectedUnit;
        if (selected == null)
        {
            // No unit? Go back to selecting
            _fsm.SetState(GameplayStateId.Selecting);
            return;
        }

        // Hide range
        HexHighlighter.Instance.ClearRangeHighlights();

        // Set or extend path
        if (MiningUIManager.Instance.MinabelObject_View.activeSelf)
        {
            Debug.Log("PlaningPathState are calling for a path calculation becuse TILE CLICKED");
            HexPathClickControllerPointTop_LineStrip.Instance.HandleTapMiningObjectView(screenPos);
        }
        else
        {
            Debug.Log("PlaningPathState are calling for a path calculation becuse HEX CLICKED");
            HexPathClickControllerPointTop_LineStrip.Instance.HandleTapUnified(screenPos);
            //HexPathClickControllerPointTop_LineStrip.Instance.HandleTap(screenPos);
        }

        // Block execute if no moves left
        if (selected.movedThisTurn >= selected.shipRuntimeData.currentMovmentRange)
        {
            //No more moves left
            UIManager.Instance.HideUnitMovmentControlls();
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