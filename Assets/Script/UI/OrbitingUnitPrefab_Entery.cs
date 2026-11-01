using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbitingUnitPrefab_Entery : MonoBehaviour
{
    public string enteryUnitId;
    public Unit enteryUnitScript;
    public Image icon;
    public TMP_Text nametext;

    private Outline outline;

    private void Start()
    {
        outline = GetComponent<Outline>();
    }

    public void SetUnit(Unit unit)
    {
        enteryUnitId = unit.unitId;
        enteryUnitScript = unit;

        if (unit == null || unit.gameObject == null)
        {
            nametext.text = "Missing Unit";
            if (icon != null) icon.sprite = null;
            return;
        }

        nametext.text = unit.unitName;
        if (ShipClassesManager.Instance != null)
            icon.sprite = ShipClassesManager.Instance.GetShipIcon(unit.shipType);
    }

    public void OnClick()
    {
        if (enteryUnitScript != null)
        {
            if (enteryUnitScript.unitType == UnitType.Satelite)
            {
                Debug.Log($"OrbitingUnitPrefab_Entery: {enteryUnitScript.unitName} is clicked.");

                //Set placemnet state
                GameStateMachine.Instance.SetState(GameplayStateId.PlaceOrbitalUnit);

                //Tell AsteroidManager what unit to place
                AsteroidManager.Instance.unitScriptToPlace = enteryUnitScript;

                SelectionService.Instance.SetSelectedUnit(enteryUnitScript);
                UIManager.Instance.HideAllUI();
            }
            else if (enteryUnitScript.unitType == UnitType.Ship)
            {
                Debug.Log($"Shit {enteryUnitScript.unitName} clicked");
                SelectionService.Instance.SetSelectedUnit(enteryUnitScript);
                UIManager.Instance.HideAllUI();
            }
        }
        else
        {
            Debug.LogWarning("OrbitingUnitPrefab_Entery: No unit assigned to this entry.");
        }

        // The old. Not used anymore. Unit gameobject are now not destroyed when asteroid view open.
        /*
        if (enteryUnitId != null)
        {
            SaveManager.UnitSaveData unitSaveData = SaveManager.Instance.GetUnitSaveDataById(enteryUnitId, GameManager.Instance.gameViewUnitsSaveFile);

            if (unitSaveData.unitType == UnitType.Satelite)
            {
                Debug.Log($"OrbitingUnitPrefab_Entery: {enteryUnitId} is clicked.");

                //Set placemnet state
                GameStateMachine.Instance.SetState(GameplayStateId.PlaceOrbitalUnit);

                //Tell AsteroidManager what unit to place
                AsteroidManager.Instance.unitToPlaceUnitId = enteryUnitId;
            }
            else if (unitSaveData.unitType == UnitType.Ship)
            {
                Debug.Log($"Shit {unitSaveData.unitName} clicked");
                //SelectionService.Instance.SetSelectedUnit(unitSaveData);
            }
        }
        else
        {
            Debug.LogWarning("OrbitingUnitPrefab_Entery: No unit assigned to this entry.");
        }*/
    }
}
