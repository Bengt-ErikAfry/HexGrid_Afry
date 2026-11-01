using DG.Tweening.Core.Easing;
using NUnit.Framework;
using System.Collections.Generic;
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
        icon.sprite = unit.shipRuntimeData.itemDefinition.icon;
        //icon.sprite = ShipClassesManager.Instance.GetShipIcon(unit.shipType);
    }

    public void OnClick()
    {
        //set state to selecting orbital unit to place
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        if (enteryUnitScript != null)
        {
            if (enteryUnitScript.unitType == UnitType.Satelite)
            {
                // Remove previously shown mining units
                foreach (var go in GameManager.Instance.spawnedInventoryUnits)
                {
                    Destroy(go);
                }
                GameManager.Instance.spawnedInventoryUnits.Clear();

                Debug.Log($"OrbitingUnitPrefab_Entery: {enteryUnitScript.unitName} is clicked.");

                //Set placemnet state
                GameStateMachine.Instance.SetState(GameplayStateId.PlaceOrbitalUnit);

                //Tell AsteroidManager what unit to place
                AsteroidManager.Instance.unitScriptToPlace = enteryUnitScript;

                SelectionService.Instance.SetSelectedUnit(enteryUnitScript);
                UIManager.Instance.HideAllUI();
            }
            else if(enteryUnitScript.unitType == UnitType.Building)
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

                // If the same unit was clicked again, just collapse it
                if (GameManager.Instance.expandedUnit == enteryUnitScript)
                {
                    GameManager.Instance.expandedUnit = null;
                    return;
                }

                GameManager.Instance.expandedUnit = enteryUnitScript;

                int insertIndex = this.transform.GetSiblingIndex() + 1;

                List<InventoryItemInstance> miningUnitsList = enteryUnitScript.unitInventory.GetItemInstanceListOfItemType(ItemType.MiningOutpost);

                foreach (var miningUnit in miningUnitsList)
                {
                    if (miningUnit == null || miningUnit.item == null || miningUnit.item.ItemDefinition == null)
                    {
                        continue;
                    }

                    GameObject go = Instantiate(AsteroidManager.Instance.unitInOrbitInventoryUnitPrefab, AsteroidManager.Instance.unitInOrbitListContent);
                    go.transform.SetSiblingIndex(insertIndex);
                    GameManager.Instance.spawnedInventoryUnits.Add(go);
                    insertIndex++;

                    if (miningUnit.item.ItemDefinition.craftingResult == null)
                        Debug.LogWarning($"OrbitingUnitPrefab_Entery: The crafting result for {miningUnit.item.ItemDefinition.itemName} is null. Please check the item definition.");

                    GameObject unitGo = Instantiate(miningUnit.item.ItemDefinition.craftingResult, AsteroidManager.Instance.unitInOrbitListContent);
                    GameManager.Instance.spawnedInventoryUnits.Add(unitGo);
                    Unit newUnitScript = unitGo.GetComponent<Unit>();
                    newUnitScript.unitName = miningUnit.item.ItemDefinition.itemName;
                    newUnitScript.shipRuntimeData = new ShipRuntimeData
                    {
                        itemDefinition = miningUnit.item.ItemDefinition
                    };
                    AsteroidManager.Instance.unitScriptToPlace = enteryUnitScript;
                    AsteroidManager.Instance.itemSourceShip = enteryUnitScript;       // add this field to Unit

                    go.GetComponent<OrbitingUnitPrefab_Entery>().SetUnit(newUnitScript);
                }
            }
            else
            {
                Debug.LogWarning($"OrbitingUnitPrefab_Entery: {enteryUnitScript.unitName} has an unhandled unit type: {enteryUnitScript.unitType}");
            }
        }
        else
        {
            Debug.LogWarning("OrbitingUnitPrefab_Entery: No unit assigned to this entry.");
        }
    }

}
