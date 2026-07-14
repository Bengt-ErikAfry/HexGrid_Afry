using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;                    // New Input System
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;     // Enhanced Touch

/// <summary>
/// Tap (New Input System) to toggle a colored filled hex at the tapped cell.
/// - POINT-TOP hex layout only (no orientation toggle)
/// - No Tilemap required; works with a baked grid-lines mesh.
/// - hexSize must match the size (center->corner) used to bake your grid lines.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("SelectedObject")]
    //public GameObject selectedObject;
    //public Unit selectedObject_Unit_Script;

    public bool isPlacingMiningOutpost; //True when player want to place a mining outpost.

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}