
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModuleView_Slot : MonoBehaviour
{
    public List<ItemType> acceptedItemTypes = new List<ItemType>();   //Are set by the Json file when moduleView is loaded.
    public List<ModuleType> acceptedModuleTypes = new List<ModuleType>();   //Are set by the Json file when moduleView is loaded.
    public int slotIndex;   //index nr in moduleRuntimeList on the unit. Are set by the Json file when moduleView is loaded.

    //public ItemInstance moduleRuntimeData_atThisSlot;

    public TMP_Text nameText;
    public TMP_Text healthText;
    public TMP_Text statusText;
    public Image backGroundImage;

    public GameObject chansToHitGO;
    public TMP_Text chanseToHitText;
    public GameObject toggleParentGO;
    public Toggle[] toggles; // 5 toggles

    public TMP_Text soliderGarding;

    public int finalChanseToHit;

    public void Set(ItemInstance module)
    {
        nameText.text = module.ItemDefinition.itemName;
        healthText.text = module.currentHealth.ToString() + "/" + module.currentMaxHealth;
        UpdateModuleStatus();

        if(GameStateMachine.Instance.CurrentId == GameplayStateId.Attacking && !SelectionService.Instance.SelectedUnit.target_Unit_Script.isPlayerControlled)
        {
            // Clicked Enemy unit to attack.
            toggleParentGO.SetActive(false);
            chansToHitGO.SetActive(true);

            float totalChanseToHit = 0;
            int nrOfWeapons = 0;
            foreach (var weaponModule in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
            {
                if (weaponModule.ItemDefinition != null)
                { 
                    if (weaponModule.isOnline && weaponModule.ItemDefinition.itemType == ItemType.Module && weaponModule.ItemDefinition.moduleType == ModuleType.Weapon)
                    {
                        /*
                        //RangeModifier 0-100%
                        float rangeModifier = InputManager.Instance.selectedObject_Unit_Script.gunnerHitChance *
                                              AttackManager.Instance.CalculateRangeModifier(InputManager.Instance.selectedObject_Unit_Script, weaponModule);

                        //AmingModulePenelty 0-25%
                        float amingForModulePenalty = AttackManager.Instance.ModuleSizeComparison(moduleRuntimeData_atThisSlot);

                        //Hit Chance.... chanseToHitTarget = gunner - range penelty - aming penelty
                        totalChanseToHit += InputManager.Instance.selectedObject_Unit_Script.gunnerHitChance -
                                            rangeModifier -
                                            amingForModulePenalty;

                        Debug.Log($"SlotIndex {slotIndex} rangeModifier: {rangeModifier} Aim: {amingForModulePenalty} for module {moduleRuntimeData_atThisSlot.ItemDefinition.itemName} shot with weapon {weaponModule.ItemDefinition.name}");
                        */

                        //get result from ChanseToHitTarget with all modifiers.
                        var resultChanseTohitTarget = AttackManager.Instance.ChanseToHitTarget(
                            SelectionService.Instance.SelectedUnit,   //Attacking unit
                            weaponModule, //Weapon to fire
                            true,  //isaming
                            module);  //selected module to hit

                        totalChanseToHit += resultChanseTohitTarget.chanseToHitTarget;

                        nrOfWeapons++;
                    }
                }
            }

            chanseToHitText.text = $"{Mathf.RoundToInt(totalChanseToHit / nrOfWeapons)}%";
    }
        else
        {
            //Clicked player unit to see status.
            SetToggleAmount();
            toggleParentGO.SetActive(true);
            chansToHitGO.SetActive(false);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());    // Nedded to update the TMP_Text. This is not done automaticly as when changing text in the inspector.
    }

    // New: mark slot as empty and update UI
    public void SetEmptySlot()
    {
        //moduleRuntimeData_atThisSlot = null;
        nameText.text = "no Module  ";
        healthText.text = "";
        backGroundImage.color = ColorManager.Instance.moduleOffline;
        statusText.text = "";
        BlankOutAllToggles();
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
    }

    public void ModelView_Slot_Pressed()
    {
        //Debug.Log("Pressed slot with index " + slotIndex);
        if (GameStateMachine.Instance.CurrentId == GameplayStateId.Selecting)
        {
            if (SelectionService.Instance.SelectedUnit.isPlayerControlled)
            { 
                //Player Unit clicked

                //Selecting mode
                if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].ItemDefinition == null)
                {
                    //Clicked emty slot.
                    //Debug.Log($"Clicked empty slot at {slotIndex}");
                    ModuleViewManager.Instance.ShowInforForEmptyModule(slotIndex);
                }
                else
                {
                    //Clicked a module.
                    //Debug.Log($"Clicked module {SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].ItemDefinition.itemName} at {slotIndex}");
                    ModuleViewManager.Instance.ShowInforForModule(SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex], slotIndex);
                }
            }
            else
            {
                Debug.Log("Can not edit enemy modules");
            }
        }
        else if (GameStateMachine.Instance.CurrentId == GameplayStateId.Attacking)
        {
            //Attacking mode
            SelectionService.Instance.SelectedUnit.moduleToHit = SelectionService.Instance.SelectedUnit.target_Unit_Script.moduleRuntimeList[slotIndex];
            SelectionService.Instance.SelectedUnit.isAmingForModule = true;

            //Hide ModuleView
            InfoScreenManager.Instance.ModuleView_GameObject.offsetMax = new Vector2(6000, 0);
            InfoScreenManager.Instance.ModuleView_GameObject.offsetMin = new Vector2(6000, 0);

            Debug.Log($"Trying to attack {SelectionService.Instance.SelectedUnit.target_Unit_Script.moduleRuntimeList[slotIndex].ItemDefinition.itemName} at {slotIndex}");

            StartCoroutine(AttackManager.Instance.TryToAttack(SelectionService.Instance.SelectedUnit,
                SelectionService.Instance.SelectedUnit.target_Unit_Script.gameObject));
        }
        else if (GameStateMachine.Instance.CurrentId == GameplayStateId.Boarding)
        {
            //Hide ModuleView
            InfoScreenManager.Instance.ModuleView_GameObject.offsetMax = new Vector2(6000, 0);
            InfoScreenManager.Instance.ModuleView_GameObject.offsetMin = new Vector2(6000, 0);

            //Boarding mode
            BoardingAttack();
        }
        else
        {
            Debug.LogWarning($"Pressed module slot but not in a state that can handle it. Current state is {GameStateMachine.Instance.CurrentId}");
        }
    }

    public void BoardingAttack()
    {
        AttackManager.Instance.BoardingAttack(SelectionService.Instance.SelectedUnit, SelectionService.Instance.SelectedUnit.target_Unit_Script, slotIndex);
    }

    public void BlankOutAllToggles()
    {
        foreach (Toggle toggle in toggles)
        {
            toggle.interactable = false;
            toggle.isOn = false;
        }
    }

    public void SetToggleAmount()
    {
        if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex] != null)
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].isOn = i < SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].nextTurnPowerUpboost;
            }
        }
        else
        {
            Debug.LogWarning($"moduleRuntimeData_atThisSlot are empty but called for on slotIndex {slotIndex}");
        }
}

    public void UpdateModuleStatus()
    {

        //Module status
        if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex] != null)
        {
            if(SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].ItemDefinition == null)
            {
                //Empty slot
                return; 
            }

            if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].isBroken)
            {
                statusText.text = "<color=orange>Broken</color>";
                backGroundImage.color = ColorManager.Instance.moduleBroken;
                return;
            }
            else if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].isDestroyd)
            {
                statusText.text = "<color=red>Destroyed</color>";
                backGroundImage.color = ColorManager.Instance.moduleDestroyd;
                return;
            }
            else
            {
                if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].nextTurnPowerUpboost < 1)
                {
                    statusText.text = "Module Status: <color=red>Offline.</color>";
                    backGroundImage.color = ColorManager.Instance.moduleOffline;
                    return;
                }
                else
                {
                    if (SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotIndex].isOnline == false)
                    {
                        statusText.text = "Module Status: <color=red>Offline.</color>";
                        backGroundImage.color = ColorManager.Instance.moduleOffline;
                        return;
                    }
                    else
                    {
                        statusText.text = "Module Status: <color=green>Online.</color>";
                        backGroundImage.color = ColorManager.Instance.moduleOnline;
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"moduleRuntimeData_atThisSlot are empty but called for on slotIndex {slotIndex}");
        }
    }
}

