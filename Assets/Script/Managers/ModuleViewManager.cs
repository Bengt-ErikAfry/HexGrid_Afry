using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;
using static ShipLayoutExporter;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
using static UnityEngine.GraphicsBuffer;
//using static UnityEngine.Rendering.DebugUI;


public class ModuleViewManager : MonoBehaviour
{
    public static ModuleViewManager Instance { get; private set; }

    [Header("UI")]
    public RectTransform shipImage;          // RectTransform of the ship sprite/image
    public RectTransform bubblesParent;      // Where bubbles will be instantiated
    public GameObject ModuleView_Slot_Prefab;    // Your UI prefab

    public Button boardingButton;
    public TMP_Text shootWithoutAimButtonText;
    public TMP_Text avalibulePowerUpBoosts_Text;

    //[SerializeField]
    public List<ModuleView_Slot> slotList = new List<ModuleView_Slot>();   // List of modules TO TAKE DAMAGE.

    [SerializeField]
    public List<GameObject> slotGOList = new List<GameObject>();   // List of modules TO TAKE DAMAGE.

    [SerializeField]
    public List<ItemInstance> temp_moduleRuntimeDataList = new List<ItemInstance>();   // List of modules TO TAKE DAMAGE. 

    [Header("Data")]
    public TextAsset layoutJson;             // Load per shipId, or via addressable/resources

    [Header("ModelView UI")]
    public TMP_Text nameText;
    public TMP_Text statusText;
    public TMP_Text moduleInfoText;
    public TMP_Text nrOfSoldiersText;
    public TMP_Text minmaxText;
    public Slider nrOfSoldiersSlider;
    public int attackerBoardingPartySize;
    public TMP_Text modulePowerUpBoostText;
    public TMP_Text moduleViewInfoText;
    public Toggle[] toggles; // 5 toggles

    public GameObject modifyModuleView;
    public GameObject attackView;

    public int slotSelected_index;
    public ItemInstance runtimeDataToShow;
    public bool cangedByCode;

    private float energyCostAmount; //Only used to create UI all modules have a energy cost and a wear.
    private float wearBostAmount;    
    private LayoutDTO layout;
    

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

    private void Start()
    {
        /*
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i; // preserve loop index
            toggles[i].onValueChanged.AddListener((isOn) =>
            {
                OnToggleClicked(index); // level = index + 1
            });
        }*/
    }

    public void ShowForUnit(Unit unit)
    {
        if (unit == null) return;

        //Clear all lists
        slotList.Clear();
        foreach (var slotGamObject in slotGOList)
        {
            if (slotGamObject != null)
            {
                Destroy(slotGamObject);
            }
        }
        slotGOList.Clear();

        //Set background image
        if (unit.unitSprite != null)
        {
            shipImage.GetComponent<Image>().sprite = unit.unitSprite;
        }
        else
        {
            Debug.LogWarning($"Unit {unit.unitName} has no sprite assigned.");
        }

        //Set Layout
        if(ShipClassesManager.Instance.GetShipLayoutJson(unit.shipRuntimeData.shipType) == null)
        {
            Debug.LogWarning($"Unit {unit.unitName} has no layout JSON assigned.");
            return;
        }
        layoutJson = ShipClassesManager.Instance.GetShipLayoutJson(unit.shipRuntimeData.shipType);
        layout = JsonUtility.FromJson<LayoutDTO>(layoutJson.text);

        //Place out all slots
        if (layout != null)
        {
            for (int i = 0; i < layout.slots.Length; i++)
            {
                //Instatiate slot prefab
                GameObject slotGO = Instantiate(ModuleView_Slot_Prefab, bubblesParent);

                //Add it to slot list to delete later
                slotGOList.Add(slotGO);

                //Set slot position from layout file
                float xSlotPos = Mathf.Lerp(0f, shipImage.rect.width, layout.slots[i].pos[0]);
                float ySlotPos = Mathf.Lerp(shipImage.rect.height, 0f, layout.slots[i].pos[1]);
                var slotRT = slotGO.GetComponent<RectTransform>();
                slotRT.anchoredPosition = new Vector2(xSlotPos, -ySlotPos);

                //Get slot script
                ModuleView_Slot moduleView_Slot_script = slotGO.GetComponent<ModuleView_Slot>();

                //Populate slotlist
                slotList.Add(moduleView_Slot_script);

                //Clear current slot.
                layout.slots[i].acceptedModuleTypes.Clear();

                //Copy acceptedmodules from file to slot
                foreach (var acceptedModuleType in layout.slots[i].acceptedModuleTypes)
                {
                    moduleView_Slot_script.acceptedModuleTypes.Add(acceptedModuleType);
                }

                //Set slot index
                moduleView_Slot_script.slotIndex = i;

                // Set content: real module or empty placeholder
                if (unit.moduleRuntimeList[i].ItemDefinition != null)
                {
                    //Slot populated with module
                    moduleView_Slot_script.Set(unit.moduleRuntimeList[i]);
                }
                else
                {
                    //No module in slot
                    moduleView_Slot_script.SetEmptySlot();
                }

                //Toggles interactiv
                if (unit.isPlayerControlled)
                {
                    if (unit.moduleRuntimeList[i].ItemDefinition != null)
                    {
                        //Module in slot, show toggle.
                        moduleView_Slot_script.toggleParentGO.SetActive(true);
                    }
                    else
                    {
                        //Empty slot, no module to modify.
                        moduleView_Slot_script.toggleParentGO.SetActive(false);
                    }
                    moduleView_Slot_script.soliderGarding.gameObject.SetActive(true);
                    moduleView_Slot_script.soliderGarding.text = $"Soldiers: {unit.moduleRuntimeList[i].nextTurnSolidersGarding}";
                }
                else
                {
                    moduleView_Slot_script.toggleParentGO.SetActive(false);
                    moduleView_Slot_script.soliderGarding.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            //No layout file
            Debug.LogWarning($"Layout file missing for this ship");
        }

        //Show all Module View UI
        ToggleModuleViewTextObjects_Visability(false,false);

        //Avalibule powerupboosts ON SHOW there are also an text change when toggles are updated.
        avalibulePowerUpBoosts_Text.text = $"Avalibule Power-Up Boosts: {unit.shipRuntimeData.nextTurnAvalibulePowerUpBoost}";

        //Check if player are aming or modifying modules.
        if (GameStateMachine.Instance.CurrentId == GameplayStateId.Attacking)
        {
            //Show Chanse to hit if not aming.
            float totalHitChanse = 0f;
            int nrOfWeapons = 0;
            foreach (var module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
            {
                //Check if module slot are empty.
                if (module.ItemDefinition != null)
                {
                    if (module.isOnline && module.ItemDefinition.itemType == ItemType.Module && module.ItemDefinition.moduleType == ModuleType.Weapon)
                    {
                        /*
                        //RangeModifier 0-1
                        float rangeModifier = InputManager.Instance.selectedObject_Unit_Script.gunnerHitChance *
                                              AttackManager.Instance.CalculateRangeModifier(InputManager.Instance.selectedObject_Unit_Script, module);
                        */

                //get result from ChanseToHitTarget with all modifiers.
                var resultChanseTohitTarget = AttackManager.Instance.ChanseToHitTarget(
                            SelectionService.Instance.SelectedUnit,   //Attacking unit
                            module, //Weapon to fire
                            SelectionService.Instance.SelectedUnit.isAmingForModule,  //isaming
                            SelectionService.Instance.SelectedUnit.moduleToHit);  //selected module to hit

                        totalHitChanse += resultChanseTohitTarget.chanseToHitTarget;

                        nrOfWeapons++;
                    }
                }
            }
            float avrageHitChanse = totalHitChanse/nrOfWeapons;
            shootWithoutAimButtonText.text = $"Shoot without aiming: {Mathf.RoundToInt(avrageHitChanse)}%";
        }

        //Check if boarding capability
        boardingButton.interactable = false;
        attackerBoardingPartySize = 0;
        foreach (ItemInstance module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
        {
            if (module.ItemDefinition != null)
            {
                if (module.isOnline && module.ItemDefinition.moduleType == ModuleType.BoardingDock && module.currentSolidersGarding > 0)
                {
                    boardingButton.interactable = true;
                    attackerBoardingPartySize += module.nextTurnSolidersGarding;
                    break;
                }
            }
        }

        //Set Boarding button text
        boardingButton.GetComponentInChildren<TMP_Text>().text = $"Boarding avalibule Soldiers:{attackerBoardingPartySize}";
    }

    public void ShowInforForModule(ItemInstance moduleToShow, int selectedModuleSlot_index)
    {
        
        //Show UI
        ToggleModuleViewTextObjects_Visability(true,false);

        //Save selectedSlot
        slotSelected_index = selectedModuleSlot_index;
        runtimeDataToShow= moduleToShow;

        //Header
        nameText.text = moduleToShow.ItemDefinition.itemName;

        //Content
        //All modules have energy cost and wear.
        energyCostAmount = Mathf.RoundToInt(((moduleToShow.ItemDefinition.energyCost * moduleToShow.ItemDefinition.energyConsumtion_Boost) - moduleToShow.ItemDefinition.energyCost) * (moduleToShow.nextTurnPowerUpboost - 1));
        moduleToShow.nextTurnEnergyCostValue = Mathf.RoundToInt(moduleToShow.ItemDefinition.energyCost + energyCostAmount);

        wearBostAmount = Mathf.RoundToInt(((moduleToShow.ItemDefinition.wear * moduleToShow.ItemDefinition.wearModifier_Boost) - moduleToShow.ItemDefinition.wear) * (moduleToShow.nextTurnPowerUpboost - 1));
        moduleToShow.nextTurnWear = Mathf.RoundToInt(moduleToShow.ItemDefinition.wear + wearBostAmount);
        
        //Same for the text
        if (moduleToShow.nextTurnPowerUpboost == 0)
        {
            energyCostAmount = 0; moduleToShow.nextTurnEnergyCostValue = 0;
            wearBostAmount = 0; moduleToShow.nextTurnWear = 0;
        }

        //Create new moduleModEnterys
        switch (moduleToShow.ItemDefinition.moduleType)
        {
            case ModuleType.Weapon:

                float damageBostAmount = (((moduleToShow.ItemDefinition.damage * moduleToShow.ItemDefinition.damage_Boost) - moduleToShow.ItemDefinition.damage) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnDamage = Mathf.RoundToInt(moduleToShow.ItemDefinition.damage + damageBostAmount);

                float rangeBostAmount = (((moduleToShow.ItemDefinition.range * moduleToShow.ItemDefinition.range_Boost) - moduleToShow.ItemDefinition.range) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnRange = Mathf.RoundToInt(moduleToShow.ItemDefinition.range + rangeBostAmount);

                if (moduleToShow.nextTurnPowerUpboost == 0)
                {
                    damageBostAmount = 0; moduleToShow.nextTurnDamage = 0;
                    rangeBostAmount = 0; moduleToShow.nextTurnRange = 0;
                }

                moduleInfoText.text =
                    $"Damage                       {moduleToShow.nextTurnDamage}(<color=green>{damageBostAmount}</color>)\n" +
                    $"Range                           {moduleToShow.nextTurnRange}(<color=green>{rangeBostAmount}</color>)\n" +
                    $"Energy cost                   {moduleToShow.nextTurnEnergyCostValue}(<color=green>{energyCostAmount}</color>)\n" +
                    $"Wear                             {moduleToShow.nextTurnWear}(<color=green>{wearBostAmount}</color>)\n"
                    ;
                break;

            case ModuleType.MiningLaser:

                float miningSpeedBostAmount = Mathf.RoundToInt(((moduleToShow.ItemDefinition.miningSpeed * moduleToShow.ItemDefinition.miningSpeed_Boost) - moduleToShow.ItemDefinition.miningSpeed) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnMiningSpeed = Mathf.RoundToInt(moduleToShow.ItemDefinition.miningSpeed + miningSpeedBostAmount);
                
                if (moduleToShow.nextTurnPowerUpboost == 0)
                {
                    miningSpeedBostAmount = 0; moduleToShow.nextTurnMiningSpeed = 0;
                }

                moduleInfoText.text =
                    $"Mining Speed                       {moduleToShow.nextTurnMiningSpeed}(<color=green>{miningSpeedBostAmount}</color>)\n" +
                    $"Energy cost                   {moduleToShow.nextTurnEnergyCostValue}(<color=green>{energyCostAmount}</color>)\n" +
                    $"Wear                             {moduleToShow.nextTurnWear}(<color=green>{wearBostAmount}</color>)\n"
                    ;
                break;

            case ModuleType.Engine:

                float movmentRangeBostAmount = (((moduleToShow.ItemDefinition.movmentRange * moduleToShow.ItemDefinition.movmentRange_Boost) - moduleToShow.ItemDefinition.movmentRange) * (moduleToShow.nextTurnPowerUpboost - 1));
                SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnMovmentRange = Mathf.RoundToInt(SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnMovmentRange + movmentRangeBostAmount);

                if (moduleToShow.nextTurnPowerUpboost == 0)
                {
                    movmentRangeBostAmount = 0; SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnMovmentRange = 0;
                }
                moduleInfoText.text =
                    $"Movment Range                 {SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnMovmentRange}(<color=green>{movmentRangeBostAmount}</color>)\n" +
                    $"Energy cost                   {moduleToShow.nextTurnEnergyCostValue}(<color=green>{energyCostAmount}</color>)\n" +
                    $"Wear                             {moduleToShow.nextTurnWear}(<color=green>{wearBostAmount}</color>)\n"
                    ;
                break;

            case ModuleType.Hull:

                float healthBoostAmount = (((moduleToShow.currentMaxHealth * moduleToShow.ItemDefinition.health_Boost) - moduleToShow.currentMaxHealth) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnHealth = Mathf.RoundToInt(moduleToShow.currentHealth + healthBoostAmount);
                moduleToShow.nextTurnMaxHealth = Mathf.RoundToInt(moduleToShow.currentMaxHealth + healthBoostAmount);
                wearBostAmount = wearBostAmount * 2; //Hull gets more wear from powerupboosts than other modules.

                if (moduleToShow.nextTurnPowerUpboost == 0)
                {
                    healthBoostAmount = 0; moduleToShow.nextTurnHealth = 0;
                } 

                moduleInfoText.text =
                    $"Health                        {moduleToShow.nextTurnHealth}(<color=green>{healthBoostAmount}</color>)\n" +
                    $"Max Health                        {moduleToShow.nextTurnMaxHealth}(<color=green>{healthBoostAmount}</color>)\n" +
                    $"Energy cost                   {moduleToShow.nextTurnEnergyCostValue}(<color=green>{energyCostAmount}</color>)\n" +
                    $"Wear                             {moduleToShow.nextTurnWear}(<color=green>{wearBostAmount}</color>)\n"
                    ;
                break;

            case ModuleType.Reactor:

                float extraEnergyBostAmount = ((moduleToShow.ItemDefinition.energyStartPowerBoost * moduleToShow.ItemDefinition.extraStartPowerBoost_Boost) - moduleToShow.ItemDefinition.energyStartPowerBoost) * (moduleToShow.nextTurnPowerUpboost - 1);
                SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost =
                    Mathf.RoundToInt(moduleToShow.ItemDefinition.energyStartPowerBoost + extraEnergyBostAmount);

                float energyProductionBoostAmount = (((moduleToShow.ItemDefinition.energyProduction * moduleToShow.ItemDefinition.energyProduction_Boost) - moduleToShow.ItemDefinition.energyProduction) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnEnergyProduction = Mathf.RoundToInt(moduleToShow.ItemDefinition.energyProduction + energyProductionBoostAmount);

                float energyMaxStorageBoostAmount = (((moduleToShow.ItemDefinition.energyMaxStorage * moduleToShow.ItemDefinition.energyStorage_Boost) - moduleToShow.ItemDefinition.energyMaxStorage) * (moduleToShow.nextTurnPowerUpboost - 1));
                moduleToShow.nextTurnEnergyMaxStorage = Mathf.RoundToInt(moduleToShow.ItemDefinition.energyMaxStorage + energyMaxStorageBoostAmount);

                if (moduleToShow.nextTurnPowerUpboost == 0)
                {
                    extraEnergyBostAmount = 0; SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost = 0;
                    energyProductionBoostAmount = 0; moduleToShow.nextTurnEnergyProduction = 0; SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnEnergyProduction = 0;
                    energyMaxStorageBoostAmount = 0; moduleToShow.nextTurnEnergyMaxStorage = 0; SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnEnergyMaxStorage = 0;

                    //Turn off all modules.
                    foreach (var unit in GameManager.Instance.playerUnits)
                    {
                        foreach (var module in unit.moduleRuntimeList)
                        {
                            module.isOnline = false;
                            module.nextTurnPowerUpboost = 0;
                        }
                    }
                }

                //Update ModuleView
                foreach (var slot in slotList)
                {
                    slot.SetToggleAmount();
                    slot.UpdateModuleStatus();
                }

                moduleInfoText.text =
                    $"Start Power-Ups                 {moduleToShow.nextTurnEnergyStartPowerBoost}(<color=green>{extraEnergyBostAmount}</color>)\n" +
                    $"Energy Cost                   {moduleToShow.nextTurnEnergyCostValue}(<color=green>{energyCostAmount}</color>)\n" +
                    $"Energy Production                   {moduleToShow.nextTurnEnergyProduction}(<color=green>{energyProductionBoostAmount}</color>)\n" +
                    $"Energy Max Storage                   {moduleToShow.nextTurnEnergyMaxStorage}(<color=green>{energyMaxStorageBoostAmount}</color>)\n" +
                    $"Wear                             {moduleToShow.nextTurnWear}(<color=green>{wearBostAmount}</color>)\n"
                    ;
                break;

            case ModuleType.BoardingDock:

 
                break;

            default:
                Debug.LogError($"ModuleType {moduleToShow.ItemDefinition.moduleType} not handled in ShowInforForModule");
                break;
        }
        
        //Set Status. Same for all modules
        SetModuleStatusText(moduleToShow);

        //Show nr of soldiers
        minmaxText.text = $"0                 {SelectionService.Instance.SelectedUnit.unitInventory.GetItemCount(Resources.Load<ItemDefinition>("Units/SoldierCrew"))}";
        nrOfSoldiersText.text = $"Nr of soldiers: {moduleToShow.nextTurnSolidersGarding}";

        //Toggles
        //cangedByCode = true;    //To prevent toggle.onvaluechanged to fire when toggle are setup at start.
        UpdatePowerUpBoostToggles();
        //cangedByCode = false;

        //Update Slider
        UpdateSliderValue(selectedModuleSlot_index);
    }

    public void ShowInforForEmptyModule(int selectedModuleSlot_index)
    {
        //Show UI
        ToggleModuleViewTextObjects_Visability(true,true);

        //Save selectedSlot
        slotSelected_index = selectedModuleSlot_index;

        //Header
        nameText.text = "Empty";

        //Show nr of soldiers
        minmaxText.text = $"0                 {SelectionService.Instance.SelectedUnit.unitInventory.GetItemCount(Resources.Load<ItemDefinition>("Units/SoldierCrew"))}";
        nrOfSoldiersText.text = $"Nr of soldiers: {SelectionService.Instance.SelectedUnit.moduleRuntimeList[selectedModuleSlot_index].nextTurnSolidersGarding}";

        //Toggles
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].SetIsOnWithoutNotify(false); //Set without notify to prevent onValueChanged to fire when setup toggles for empty slot.
        }

        //Update Slider
        UpdateSliderValue(selectedModuleSlot_index);
    }
    public void UpdateSliderValue(int selectedModuleSlot_index)
    {
        //Set soldier slider
        int nrOfSoldiersIninventory = SelectionService.Instance.SelectedUnit.unitInventory.GetItemCount(Resources.Load<ItemDefinition>("Units/SoldierCrew"));
        nrOfSoldiersSlider.maxValue = nrOfSoldiersIninventory;

        float newSlideValue = nrOfSoldiersIninventory * (float)SelectionService.Instance.SelectedUnit.moduleRuntimeList[selectedModuleSlot_index].nextTurnSolidersGarding / nrOfSoldiersIninventory;
        nrOfSoldiersSlider.SetValueWithoutNotify(newSlideValue);
    }

    public void AttackWithoutAming_ButtonPressed()
    {
        //hide module view
        InfoScreenManager.Instance.ModuleView_GameObject.gameObject.SetActive(false);

        //Set moduleToHit
        SelectionService.Instance.SelectedUnit.moduleToHit = GetRandomModule();

        if (SelectionService.Instance.SelectedUnit.moduleToHit != null)
        {
            //attack
            SelectionService.Instance.SelectedUnit.isAmingForModule = false;
            StartCoroutine(AttackManager.Instance.TryToAttack(SelectionService.Instance.SelectedUnit, SelectionService.Instance.SelectedUnit.target_Unit_Script.gameObject));
        }
        else
        {
            string mainMessage =
                $"ATTACK FAIL:{SelectionService.Instance.SelectedUnit.unitName} try to attack {SelectionService.Instance.SelectedUnit.target_Unit_Script.unitName} but target have no modules";
            string subMessage = "";

            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, SelectionService.Instance.SelectedUnit.transform.position, Color.gray);
        }
    }

    public ItemInstance GetRandomModule()
    {
        List<ItemInstance> possibleModulesToHit = new List<ItemInstance>();
        foreach (var module in SelectionService.Instance.SelectedUnit.target_Unit_Script.moduleRuntimeList)
        {
            if (module.ItemDefinition != null)
            {
                possibleModulesToHit.Add(module);
            }
        }
        if (possibleModulesToHit.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibleModulesToHit.Count);
            return possibleModulesToHit[randomIndex];
        }
        else
        {
            Debug.LogWarning("No modules to hit.");
            return null;
        }
    }

    public void SetModuleStatusText(ItemInstance module)
    {
        //Module status
        if (module.isBroken)
        {
            statusText.text = "Module Status: <color=orange>Broken</color>";
        }
        else if (module.isDestroyd)
        {
            statusText.text = "Module Status: <color=red>Destroyed</color>";
        }
        else if (module.nextTurnPowerUpboost > 0)
        {
            statusText.text = "Module Status: <color=green>Online.</color>";
        }
        else
        {
            statusText.text = "Module Status: <color=red>Offline.</color>";
        }
    }

    public void ToggleModuleViewTextObjects_Visability(bool isShowing, bool isEmpty)
    {
        nameText.gameObject.SetActive(isShowing);
        statusText.gameObject.SetActive(isShowing);
        moduleInfoText.gameObject.SetActive(isShowing);
        nrOfSoldiersText.gameObject.SetActive(isShowing);
        if (isEmpty)
        {
            statusText.text = "Module Status: Empty";
            modulePowerUpBoostText.gameObject.SetActive(false);
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].gameObject.SetActive(false);
            }
            moduleInfoText.gameObject.SetActive(false);
        }
        else
        { 
            modulePowerUpBoostText.gameObject.SetActive(isShowing);
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].gameObject.SetActive(isShowing);
            }
        }
    }

    public void UpdatePowerUpBoostToggles()
    {
        //Debug.Log($"UpdagePowerUpBoostsToggle Module power: {runtimeDataToShow.nextTurnPowerUpboost}");
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].SetIsOnWithoutNotify( i < runtimeDataToShow.nextTurnPowerUpboost);   //Do not call the onValueChanged.
        }

        //Avalibule powerupboosts
        avalibulePowerUpBoosts_Text.text = $"Avalibule Power-Up Boosts: {SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost}";
    }

    public void ApplyModuleWearAndEnergyCost()
    {
        foreach (var unit in GameManager.Instance.playerUnits)
        {
            foreach (var module in unit.moduleRuntimeList)
            {
                //Take damage from Wear
                module.currentHealth = module.currentHealth - module.currentWear;

                //Update the module status if health is 0 or below.
                module.TakeDamage(module.currentWear);

                //Energy cost
                unit.shipRuntimeData.currentEnergyMaxStorage = unit.shipRuntimeData.currentEnergyMaxStorage - module.currentEnergyCostValue;
                //Debug.Log($"ApplyModuleWearAndEnergyCost for unit {unit.unitName} module {module.ItemDefinition.itemName}: drain energy {module.currentEnergyCostValue}");
            }

            if (unit.shipRuntimeData.currentEnergyMaxStorage <= 0)
            {
                //Not enufe energy to keep module online. It goes offline and energy storage is set to 0.
                unit.shipRuntimeData.currentEnergyMaxStorage = 0;

                foreach (var module in unit.moduleRuntimeList)
                {
                    module.isOnline = false;
                }

                //Send message to player that module is offline.
                string mainMessage =
                "Out of energy" + unit.unitName;
                string subMessage = $"";
                MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
                unit.transform.position, Color.white);
            }
        }
    }

    public void AddEnergyToUnit()
    {

    }

    public void OnToggleClicked(int clickedToggleIndex)
    {
        Debug.Log("OnToggleClicked " + clickedToggleIndex);
        if (toggles[clickedToggleIndex].isOn)
        {
            // Clicked a empty toggle
            if (SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost > 0 || runtimeDataToShow.ItemDefinition.moduleType == ModuleType.Reactor)    //Reactor is added to let player restart ship if reactor is offline.
            {
                //Add Module PowerupUpBoost
                runtimeDataToShow.nextTurnPowerUpboost++;
                SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost--;
            }
            else
            {
                //No mor powerboosts.
                Debug.Log("No more powerboosts avalibule");
            }
        }
        else
        {
            //Clicked a checked toggle
            //Remove Module powerup
            runtimeDataToShow.nextTurnPowerUpboost--;
            SelectionService.Instance.SelectedUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost++;
        }

        //Toggles
        UpdatePowerUpBoostToggles();

        //Update Model Info view
        ShowInforForModule(runtimeDataToShow, slotSelected_index);

        //Update current module SLOT toggles.
        slotList[slotSelected_index].SetToggleAmount();

        //Update current module SLOT status.
        slotList[slotSelected_index].UpdateModuleStatus();
    }

    public void OnSliderChanged()
    {
        ItemInstance selectedModule = SelectionService.Instance.SelectedUnit.moduleRuntimeList[slotSelected_index]; // Just to make code cleaner

        int newValue = Mathf.RoundToInt(nrOfSoldiersSlider.value);
        int difference = newValue - selectedModule.nextTurnSolidersGarding;
        int totalAmountSoldierInInventory = SelectionService.Instance.SelectedUnit.unitInventory.GetItemCount(Resources.Load<ItemDefinition>("Units/SoldierCrew"));

        int totGardingSoldeiers = 0;
        for (int i = 0; i < SelectionService.Instance.SelectedUnit.moduleRuntimeList.Count; i++)
        {
            totGardingSoldeiers += SelectionService.Instance.SelectedUnit.moduleRuntimeList[i].nextTurnSolidersGarding;
        }

        int availableSoldiers = totalAmountSoldierInInventory - totGardingSoldeiers;

        if (difference > 0)
        {
            // Player wants to ADD soldiers
            if (availableSoldiers >= difference)
            {
                selectedModule.nextTurnSolidersGarding += difference;
            }
            else
            {
                // Not enough soldiers → clamp
                selectedModule.nextTurnSolidersGarding += availableSoldiers;
            }
        }
        else if (difference < 0)
        {
            // Player wants to REMOVE soldiers
            selectedModule.nextTurnSolidersGarding += difference;        // difference is negative
        }

        // Update slider to actual value (important!)
        nrOfSoldiersSlider.value = selectedModule.nextTurnSolidersGarding;

        //Update Sodlier Text with new value
        nrOfSoldiersText.text = $"Nr of soldiers: {selectedModule.nextTurnSolidersGarding}";

        //Update ModuleView slot with new soldier guarding value.
        slotList[slotSelected_index].soliderGarding.text = $"Soldiers: {selectedModule.nextTurnSolidersGarding}";
    }

    public void Boarding_ButtonPressed()
    {
        GameStateMachine.Instance.SetState(GameplayStateId.Boarding);
    }
}