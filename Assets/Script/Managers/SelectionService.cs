using System;
using UnityEngine;

public class SelectionService : MonoBehaviour
{
    public static SelectionService Instance { get; private set; }

    // Serialized backing field — visible in the Inspector and updated at runtime
    [SerializeField, Tooltip("Current selected unit (for debug/inspection).")]
    private Unit selectedUnitField;

    // Public read-only property for other code
    public Unit SelectedUnit => selectedUnitField;

    // Fired whenever selection changes (SelectedUnit may be null)
    public event Action<Unit> OnSelectionChanged;
    public event Action<Vector3> OnHexClicked;

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

    // Set selection to a Unit
    public void SetSelectedUnit(Unit unit)
    {
        if (SelectedUnit == unit) return;
        selectedUnitField = unit;               // update serialized field so Inspector shows it
        OnSelectionChanged?.Invoke(selectedUnitField);
    }

    public void SetSelectedHex(Vector3 worldPos)
    {
        // update serialized field so Inspector shows it
        OnHexClicked?.Invoke(worldPos);
    }

    // Set selection by GameObject (convenience)
    public void SetSelectedGameObject(GameObject go)
    {
        SetSelectedUnit(go == null ? null : go.GetComponent<Unit>());
    }

    // Clear selection
    public void ClearSelection() => SetSelectedUnit(null);
}