using UnityEngine;

public class EnemyState : IGameState
{
    private readonly GameStateMachine _fsm;

    public EnemyState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.activeState.text = "Active state: Enemy State";
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
