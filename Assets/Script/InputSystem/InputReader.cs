using System;
#if UNITY_EDITOR
using UnityEditor.Timeline.Actions;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Read the player input and call an event in the interface. All script that inherete from the interface will subscribe to the event and do the corresponding action. 
/// All actions are described in the class that subscribe to the event. 
/// </summary>
public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }

    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDrag;     // world/camera drag
    public event Action<float> OnPinch;    // + for zoom in, - for zoom out
    public event Action<Vector2> OnTouchDown;
    public event Action<Vector2> OnTouchUp;

    [SerializeField] float tapThreshold = 15f; // pixels
    [SerializeField] float dragSensitivity = 0.01f;
    [SerializeField] float pinchZoomSpeed = 0.02f;

    private bool isPanning;
    private bool tapCanceled;
    private Vector2 startPos, lastPos;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        EnhancedTouchSupport.Enable();
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() => EnhancedTouchSupport.Disable();


    void Update()
    {
        var touches = Touch.activeTouches;
        if (touches.Count == 0)
        {
            isPanning = false;
            tapCanceled = false;
            return;
        }

        if (touches.Count == 2)
        {
            tapCanceled = true;
            HandlePinch(touches[0], touches[1]);
            return;
        }

        var touch = touches[0];
        switch (touch.phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                startPos = lastPos = touch.screenPosition;
                tapCanceled = false;
                isPanning = false;
                OnTouchDown?.Invoke(touch.screenPosition);
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:
                float dist = Vector2.Distance(startPos, touch.screenPosition);
                if (dist > tapThreshold)
                {
                    tapCanceled = true;
                    isPanning = true;
                    Vector2 delta = touch.screenPosition - lastPos;
                    lastPos = touch.screenPosition;
                    OnDrag?.Invoke(delta);
                }
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:
                OnTouchUp?.Invoke(touch.screenPosition);

                // For mobile, pass finger index if you want stricter UI check:
                // if (EventSystem.current.IsPointerOverGameObject(touch.finger.index)) { return; }
                if (!tapCanceled && !IsOverUI())
                {
                    Debug.Log("before invoke" + GameStateMachine.Instance.Current);
                    OnTap?.Invoke(touch.screenPosition);
                }

                isPanning = false;
                tapCanceled = false;
                break;
        }
    }

    private void HandlePinch(Touch t0, Touch t1)
    {
        // Previous and current distance to get pinch delta
        float prevDistance = ((t0.screenPosition - t0.delta) - (t1.screenPosition - t1.delta)).magnitude;
        float currDistance = (t0.screenPosition - t1.screenPosition).magnitude;
        float delta = (currDistance - prevDistance) * pinchZoomSpeed;
        OnPinch?.Invoke(delta);
    }

    private bool IsOverUI()
    {
        if (EventSystem.current == null) return false;
        // For mouse: IsPointerOverGameObject()
        // For touch (EnhancedTouch), you can also use the finger index overload if needed.
        return EventSystem.current.IsPointerOverGameObject();
    }
}
