using UnityEngine;

// Start is called once before the first execution of Update after the MonoBehaviour is created
public class CinematicState : IGameState
{
    private readonly GameStateMachine _fsm;

    public CinematicState(GameStateMachine fsm) => _fsm = fsm;

    public void Enter()
    {
        // Show UI hints, ghost path, etc.
        CameraManager.Instance.MoveCameraTo(SelectionService.Instance.SelectedUnit.gameObject.transform.position, 1f, false);
    }

    public void Exit()
    {
        // Hide hints/cleanup if needed
    }

    public void OnTap(Vector2 screenPos)
    {
        Debug.Log("CinematicState: Tap ignored");
    }

    public void OnDrag(Vector2 delta)
    {
        Debug.Log("CinematicState: Tap ignored");
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
