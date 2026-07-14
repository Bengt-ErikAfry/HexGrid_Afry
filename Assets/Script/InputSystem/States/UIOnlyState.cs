using UnityEngine;

public class UIOnlyState : IGameState
{
    private readonly GameStateMachine _fsm;

    public UIOnlyState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: UIonly State";
    }
    // Exit State.
    public void Exit() { }

    public void OnTap(Vector2 screenPos)
    {
         
    }

    public void OnDrag(Vector2 delta)
    {
       
    }

    public void OnPinch(float amount)
    {
        
    }

    public void Tick(float dt) { }
}

