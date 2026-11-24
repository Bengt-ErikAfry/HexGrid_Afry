using DG.Tweening.Core.Easing;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbitingUnitPrefab_Entery : MonoBehaviour
{
    public string enteryUnitId;
    public Unit enteryUnitScript;
    public Image icon;
    public TMP_Text nametext;
    public Unit sourceShip;

    private Outline outline;

    private void Start()
    {
        outline = GetComponent<Outline>();
    }

    public void SetUnit(Unit unit)
    {
        if (unit == null || unit.gameObject == null)
        {
            enteryUnitId = null;
            enteryUnitScript = null;
            if (nametext != null) nametext.text = "Missing Unit";
            if (icon != null) icon.sprite = null;
            return;
        }

        enteryUnitId = unit.unitId;
        enteryUnitScript = unit;
        if (nametext != null) nametext.text = unit.unitName;

        if (icon != null)
        {
            icon.sprite = unit.shipRuntimeData != null && unit.shipRuntimeData.itemDefinition != null
                ? unit.shipRuntimeData.itemDefinition.icon
                : null;
        }
    }

    public void SetSourceShip(Unit ship)
    {
        sourceShip = ship;
    }

    public void OnClick()
    {
        //set state to selecting orbital unit to place
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //enteryUnitScript.gameObject.SetActive(true);

        if (enteryUnitScript != null)
        {
            // Remove previously shown mining units
            if (enteryUnitScript.unitType != UnitType.Building)
            {
                foreach (var go in GameManager.Instance.spawnedInventoryUnits)
                {
                    Destroy(go);
                }
                GameManager.Instance.spawnedInventoryUnits.Clear();
            }
            /*
            //If Player click the same unit that is already expanded, collapse it and return
            if (GameManager.Instance.expandedUnit == enteryUnitScript)
            {
                // If the same unit was clicked again, just collapse it
                GameManager.Instance.spawnedInventoryUnits.Clear();
                GameManager.Instance.expandedUnit= null;
                AsteroidManager.Instance.UpdateUnitInOrbitView();
                return;
            }*/

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
                AsteroidManager.Instance.itemSourceShip = sourceShip;

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
                AsteroidManager.Instance.itemSourceShip = sourceShip;

                SelectionService.Instance.SetSelectedUnit(enteryUnitScript);
                UIManager.Instance.HideAllUI();
            }
            else if (enteryUnitScript.unitType == UnitType.Ship)
            {
                Unit inventoryShip = enteryUnitScript;
                if (inventoryShip.unitInventory == null)
                {
                    Debug.LogWarning($"OrbitingUnitPrefab_Entery: Ship '{inventoryShip.unitName}' has no inventory.");
                    return;
                }

                foreach (var go in GameManager.Instance.spawnedExpandedEntery)
                    if (go != null) Destroy(go);
                GameManager.Instance.spawnedExpandedEntery.Clear();

                foreach (var go in GameManager.Instance.spawnedInventoryUnits)
                    if (go != null) Destroy(go);
                GameManager.Instance.spawnedInventoryUnits.Clear();

                GameManager.Instance.expandedUnit = inventoryShip;
                SelectionService.Instance.SetSelectedUnit(inventoryShip);

                int insertIndex = transform.GetSiblingIndex() + 1;
                foreach (InventoryItemInstance inventorySlot in inventoryShip.unitInventory.itemInstance)
                {
                    if (inventorySlot == null || inventorySlot.item == null || inventorySlot.item.ItemDefinition == null)
                        continue;

                    ItemDefinition itemDefinition = inventorySlot.item.ItemDefinition;
                    if (itemDefinition.craftingResult == null)
                        continue;

                    Unit deployableTemplate = itemDefinition.craftingResult.GetComponent<Unit>();
                    if (deployableTemplate == null ||
                        (deployableTemplate.unitType != UnitType.Satelite && deployableTemplate.unitType != UnitType.Building))
                        continue;

                    for (int i = 0; i < inventorySlot.amount; i++)
                    {
                        if (OrbitViewManager.Instance.OrbitalInventortyEntery_Prefab == null)
                        {
                            Debug.LogError("OrbitViewManager: OrbitalInventortyEntery_Prefab is not assigned.");
                            return;
                        }

                        GameObject entryObject = Instantiate(
                            OrbitViewManager.Instance.OrbitalInventortyEntery_Prefab,
                            OrbitViewManager.Instance.unitInOrbitListContent);
                        entryObject.transform.SetSiblingIndex(insertIndex++);
                        GameManager.Instance.spawnedExpandedEntery.Add(entryObject);

                        GameObject deployableObject = Instantiate(itemDefinition.craftingResult);
                        deployableObject.SetActive(false);
                        GameManager.Instance.spawnedInventoryUnits.Add(deployableObject);

                        Unit deployableUnit = deployableObject.GetComponent<Unit>();
                        if (deployableUnit == null)
                            continue;

                        deployableUnit.unitName = itemDefinition.itemName;
                        deployableUnit.shipRuntimeData = new ShipRuntimeData { itemDefinition = itemDefinition };
                        if (deployableUnit.grafics != null)
                        {
                            SpriteRenderer renderer = deployableUnit.grafics.GetComponent<SpriteRenderer>();
                            if (renderer != null) renderer.sprite = itemDefinition.icon;
                        }

                        OrbitingUnitPrefab_Entery inventoryEntry =
                            entryObject.GetComponent<OrbitingUnitPrefab_Entery>();
                        if (inventoryEntry == null)
                        {
                            Debug.LogError("OrbitingUnitPrefab_Entery component not found on inventory entry prefab.");
                            continue;
                        }

                        inventoryEntry.SetUnit(deployableUnit);
                        inventoryEntry.SetSourceShip(inventoryShip);
                    }
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
