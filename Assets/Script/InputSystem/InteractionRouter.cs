using UnityEngine;

// This component listens to input events from InputReader and forwards them to the current game state in GameStateMachine.
// It should be attached to a central game object (e.g., GameManager) that persists across scenes.
public class InteractionRouter : MonoBehaviour
{
    [SerializeField] Camera mainCam;

    public void Start()
    {
        //OnEnabel seams sometime to be called before InputReader.Awake. So InputReader are null.
        InputReader.Instance.OnTap += HandleTap;
        InputReader.Instance.OnDrag += HandleDrag;
        InputReader.Instance.OnPinch += HandlePinch;
    }
    void OnEnable()
    {
        //OnEnabel seams sometime to be called before InputReader.Awake. So InputReader are null.
        /*
        InputReader.Instance.OnTap += HandleTap;
        InputReader.Instance.OnDrag += HandleDrag;
        InputReader.Instance.OnPinch += HandlePinch;
        */
    }
    void OnDisable()
    {
        if (InputReader.Instance == null) return;
        InputReader.Instance.OnTap -= HandleTap;
        InputReader.Instance.OnDrag -= HandleDrag;
        InputReader.Instance.OnPinch -= HandlePinch;
    }

    void HandleTap(Vector2 screenPos) => GameStateMachine.Instance.Current.OnTap(screenPos);
    void HandleDrag(Vector2 delta) => GameStateMachine.Instance.Current.OnDrag(delta);
    void HandlePinch(float amount) => GameStateMachine.Instance.Current.OnPinch(amount);
}