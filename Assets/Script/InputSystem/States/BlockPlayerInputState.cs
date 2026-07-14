using UnityEngine;

/// <summary>
/// Used to lock player input for exsampel during unit movment or enemy turn.
/// </summary>
public class BlockPlayerInputState : IGameState
{
    private readonly GameStateMachine _fsm;

    public BlockPlayerInputState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        UIManager.Instance.BlockPlayerInputPanel.SetActive(true);

        UIManager.Instance.activeState.text = "Active state: BlockPlayerInput State";
    }
    // Exit State.
    public void Exit() 
    {
        UIManager.Instance.BlockPlayerInputPanel.SetActive(false);
    }

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
