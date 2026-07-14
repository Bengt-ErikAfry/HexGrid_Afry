using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
using UnityEngine;
using UnityEngine.UI;

using static ShipLayoutExporter;

public class HangerManager : MonoBehaviour
{
    public static HangerManager Instance { get; private set; }
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

    [Header("General Referenses")]
    public Unit unitScript; //DO NOT SET in the inspector.
    public RectTransform MessageBox;
    public TMP_Text MessageBoxText;
    public TMP_InputField shipNameInputField;

    [Header("ModuleList")]
    public GameObject hangerModuleSlot_Prefab;
    public Transform content_ModuleList;
    public List<ItemInstance> moduleList = new List<ItemInstance>();    //modules in inventory

    [Header("ModuleViewList")] 
    public GameObject slot_moduleListView_Prefab;
    public Transform content_ModuleViewList;
    public List<GameObject> moduleSlotList = new List<GameObject>();        //Slots in the module list to the right
    public GameObject hullSlot;

    public List<ShipSlotUI> currentBuildSlotList = new List<ShipSlotUI>();    //The list of modules to be palced by the layout file
    public ItemInstance hullModuleInstance; //The hull module that is placed in the first slot. This is used to set up the hanger view and to check compatibily with other modules.

    public Image hangerViewBackgroundImage;
    private RectTransform hangerViewBackgroundImageRecttransform;

    public TextAsset layoutJson;             // Load per shipId, or via addressable/resources
    private LayoutDTO layoutData;


    public void ShowHangerView()
    {
        //Debug.Log("Hanger Manager called");
        //Create shorter ref to player unit script
        unitScript = SelectionService.Instance.SelectedUnit;

        //Load module list
        LoadModuleList();
    }
    public void LoadModuleList()
    {
        if (moduleSlotList.Count > 0)
        {
            foreach (var moduleGO in moduleSlotList)
            {
                Destroy(moduleGO);
            }
        }

        //Make sure are empty
        moduleList.Clear();
        moduleSlotList.Clear();

        //Debug.Log($"Unit inventory count: {unitScript.unitInventory.items.Count}");
        for (int i = 0; i < unitScript.unitInventory.itemInstance.Count; i++)
        {
            //Foreatch Item in inventory
            //Debug.Log($"LoadModuleListCalled for item{unitScript.unitInventory.items[i].item.itemName}");

            //Check if not a module. If not:break
            if (unitScript.unitInventory.itemInstance[i].item.ItemDefinition.itemType != ItemType.Module) continue;

            //Instatiate module in list.
            GameObject newHangerSlot = Instantiate(hangerModuleSlot_Prefab, content_ModuleList);
            InventoryItemUI slotUI = newHangerSlot.GetComponent<InventoryItemUI>();
            slotUI.SetupSlot(unitScript.unitInventory.itemInstance[i].item);

            //Add to moduleList
            moduleList.Add(unitScript.unitInventory.itemInstance[i].item);

            //Add Slot to slot list
            moduleSlotList.Add(newHangerSlot);
        }
    }
    //Used when player have shoused a hull and gay aout all modules not comatible with that hull.
    public void UpdateModuleListColor(ItemInstance hullModule)
    {
        foreach (var slot in moduleSlotList)
        {
            if (slot.GetComponent<InventoryItemUI>().moduleInSlot.shipType == hullModule.shipType)
            {
                slot.GetComponent<Image>().color = Color.white;
            }
            else
            { 
                slot.GetComponent<Image>().color = Color.gray;
            }
        }
    }

    public void ResetModuleColor()
    {
        foreach (var slot in moduleSlotList)
        {
            slot.GetComponent<Image>().color = Color.white;
        }
    }

    //This is only for the first HULL module.
    public void HullModuleSetup(ItemInstance itemInstance)
    {
        //Set hanger view background image

        hangerViewBackgroundImage.sprite = ShipClassesManager.Instance.GetShipLayoutBackground(itemInstance.shipType);
        hangerViewBackgroundImageRecttransform = hangerViewBackgroundImage.GetComponent<RectTransform>();
        //Load and set module Layout

        //Set Layout
        layoutJson = ShipClassesManager.Instance.GetShipLayoutJson(itemInstance.shipType);
        layoutData = JsonUtility.FromJson<LayoutDTO>(layoutJson.text);

        //Go thrue the layout data and populae emplySlots with data.
        if (layoutData != null)
        {
            //All slots exsept hull is already hiden from start.

            //Only show slots in layoutData
            Debug.Log($"Layout data read. layoutData.slots.Length: {layoutData.slots.Length}");
            for (int i = 0; i < layoutData.slots.Length; i++)
            {
                if (layoutData.slots[i].acceptedModuleTypes[0] == ModuleType.Hull)
                {
                    //Hull Slot

                    hullModuleInstance = itemInstance;
                    hullSlot.gameObject.SetActive(true);
                    ShipSlotUI hullSlotUI = hullSlot.GetComponent<ShipSlotUI>();
                    hullSlotUI.slotNameText.text = itemInstance.ItemDefinition.itemName;
                    hullSlotUI.slotStatusText.text = $"Status: OffLine";
                    hullSlotUI.slotHealthText.text = $"Health: {itemInstance.currentHealth.ToString()}/{itemInstance.currentMaxHealth.ToString()}";
                    hullSlotUI.slotImage.color = ColorManager.Instance.moduleOffline;

                    //Updatecolor on module list to show only compatible modules.
                    UpdateModuleListColor(itemInstance);

                    //Update Slot autosize
                    LayoutRebuilder.ForceRebuildLayoutImmediate(hullSlot.GetComponent<RectTransform>());    // Nedded to update the TMP_Text. This is not done automaticly as when changing text in the inspector.

                    currentBuildSlotList.Add(hullSlotUI);//Used later for checking that all is okay to build ship.
                }
                else
                {
                    //Other Slots

                    //Instatiate slot
                    GameObject newSlot = Instantiate(slot_moduleListView_Prefab, content_ModuleViewList.transform);

                    newSlot.gameObject.SetActive(true);
                    ShipSlotUI newSlotUI = newSlot.GetComponent<ShipSlotUI>();
                    newSlotUI.slotNameText.text = "Empty slot for";
                    newSlotUI.acceptedModuleTypes.Clear();
                    string statusText = "";
                    newSlotUI.acceptedItemType = ItemType.Module;
                    foreach (var acceptedModule in layoutData.slots[i].acceptedModuleTypes)
                    {
                        statusText += acceptedModule.ToString() +"/ ";
                        newSlotUI.acceptedModuleTypes.Add(acceptedModule);
                    }
                    newSlotUI.acceptedModuleClass = hullModuleInstance.shipType;
                    newSlotUI.slotStatusText.text = statusText;
                    newSlotUI.slotHealthText.text = "";
                    newSlotUI.slotImage.color = ColorManager.Instance.moduleOffline;

                    //Position the slot
                    float xSlotPos = Mathf.Lerp(0f, hangerViewBackgroundImageRecttransform.rect.width, layoutData.slots[i].pos[0]);
                    float ySlotPos = Mathf.Lerp(hangerViewBackgroundImageRecttransform.rect.height, 0f, layoutData.slots[i].pos[1]);
                    var slotRT = newSlot.GetComponent<RectTransform>();
                    slotRT.anchoredPosition = new Vector2(xSlotPos, -ySlotPos);

                    //Update Slot autosize
                    LayoutRebuilder.ForceRebuildLayoutImmediate(newSlot.GetComponent<RectTransform>());    // Nedded to update the TMP_Text. This is not done automaticly as when changing text in the inspector.

                    currentBuildSlotList.Add(newSlotUI);//Used later for checking that all is okay to build ship.
                }
            }
        }
        else
        {
            //No layout file
            Debug.LogWarning($"Layout file missing for this ship");
        }
    }

    public void LaunchNewShip()
    {
        //Check if ship have hull Module.
        if (currentBuildSlotList[0].CurrentItem.ItemDefinition == null)
        {
            Debug.LogWarning($"Can not launch ship without hull module");
            //Move message box to center of screen and show it.
            MessageBox.offsetMax = new Vector2(0, 0);
            MessageBox.offsetMin = new Vector2(0, 0);
            MessageBoxText.text = "Can not launch ship without HULL module";
            return;
        }

        //Check for engines
        bool havEngineModule = false;
        for (int i = 0; i < currentBuildSlotList.Count; i++)
        {
            if (currentBuildSlotList[i].CurrentItem.ItemDefinition != null)
            {
                if (currentBuildSlotList[i].CurrentItem.ItemDefinition.moduleType == ModuleType.Engine)
                {
                    //Have engine
                    havEngineModule = true;
                    break;
                }
            }
        }
        if (!havEngineModule)
        {
            Debug.LogWarning($"Can not launch ship without engine module");
            //Move message box to center of screen and show it.
            MessageBox.offsetMax = new Vector2(0, 0);
            MessageBox.offsetMin = new Vector2(0, 0);
            MessageBoxText.text = "Can not launch ship without ENGINE module";
            return;
        }

        //Check for reactor
        bool haveReactorModule = false;
        for (int i = 0; i < currentBuildSlotList.Count; i++)
        {
            if (currentBuildSlotList[i].CurrentItem.ItemDefinition != null)
            {
                if (currentBuildSlotList[i].CurrentItem.ItemDefinition.moduleType == ModuleType.Reactor)
                {
                    //Have engine
                    haveReactorModule = true;
                    break;
                }
            }
        }
        if (!haveReactorModule)
        {
            Debug.LogWarning($"Can not launch ship without reactor module");
            //Move message box to center of screen and show it.
            MessageBox.offsetMax = new Vector2(0, 0);
            MessageBox.offsetMin = new Vector2(0, 0);
            MessageBoxText.text = "Can not launch ship without REACTOR module";
            return;
        }

        if(shipNameInputField.text == "") 
        {
            Debug.LogWarning($"Can not launch ship without a NAME");
            //Move message box to center of screen and show it.
            MessageBox.offsetMax = new Vector2(0, 0);
            MessageBox.offsetMin = new Vector2(0, 0);
            MessageBoxText.text = "Can not launch ship without a Name";
            return;
        }

        //Check if slot have a assemblyResultItem.
        if (currentBuildSlotList[0] == null)
        {
            Debug.LogWarning("currentBuildSlotList[0] are null");
            return;
        }
        if (currentBuildSlotList[0].CurrentItem == null)
        {
            Debug.Log("currentBuildSlotList[0].CurrentItem are null");
            return;
        }
        if (currentBuildSlotList[0].CurrentItem.ItemDefinition == null)
        {
            Debug.Log("currentBuildSlotList[0].CurrentItem.ItemDefinition are null");
            return;
        }
        if (ShipClassesManager.Instance.GetAssemblyResultItem(currentBuildSlotList[0].CurrentItem.shipType) == null)
        {
            Debug.Log("emtyModuleSlotList[0].CurrentItem.ItemDefinition.assemblyResultItem are null");
            return;
        }
        Debug.Log($"LunchShip {ShipClassesManager.Instance.GetAssemblyResultItem(currentBuildSlotList[0].CurrentItem.shipType).itemName}");
        
        //Create New ship
        CreateNewShip();

        //Close UI
        InfoScreenManager.Instance.HideAll();
        InfoScreenManager.Instance.HideInfoScreen();

        //Move camera to selected unit.
        CameraManager.Instance.CenterOnSelectedObject();
    }

    public void CreateNewShip()
    {
        //Check if we have a AssemblyResultPrefab to instatiate.
        if(currentBuildSlotList[0] == null)
        {
            Debug.LogWarning("emtyModuleSlotList[0] are null");
            return;
        }
        if(currentBuildSlotList[0].CurrentItem == null)
        {
            Debug.Log("emtyModuleSlotList[0].CurrentItem are null");
            return;
        }
        if (currentBuildSlotList[0].CurrentItem.ItemDefinition == null)
        {
            Debug.Log("emtyModuleSlotList[0].CurrentItem.ItemDefinition are null");
            return;
        }
        if (ShipClassesManager.Instance.GetAssemblyResultPrefab(currentBuildSlotList[0].CurrentItem.shipType) == null)
        {
            Debug.Log("emtyModuleSlotList[0].CurrentItem.ItemDefinition.assemblyResultPrefab are null");
            return;
        }

        //Create new ship.
        GameObject newShipGO = Instantiate(ShipClassesManager.Instance.GetAssemblyResultPrefab(currentBuildSlotList[0].CurrentItem.shipType));
        newShipGO.transform.position = SelectionService.Instance.SelectedUnit.transform.position;

        //Get unit script
        Unit newShipUnit_Script = newShipGO.GetComponent<Unit>();

        //SetUp Variabels for new ship.
        newShipUnit_Script.unitName = shipNameInputField.text;

        //Add ship to gamemaker unit list to make it part of player turn
        GameManager.Instance.unitsList.Add(newShipUnit_Script);
        GameManager.Instance.playerUnits.Add(newShipUnit_Script);

        //Add Modules to ship.
        foreach (var module in currentBuildSlotList)
        {
            var item = module?.CurrentItem;
            newShipUnit_Script.moduleRuntimeList.Add(item);
        }

        //Setup all Module values
        ModuleManager.Instance.SetUpShipStats(newShipUnit_Script);

        //Calculate movment range.
        newShipUnit_Script.shipRuntimeData.currentMovmentRange = GetAllModuleValues(ModuleType.Engine, item => item.currentRange);

        //Calculate detectionRange
        newShipUnit_Script.detectionRange = GetAllModuleValues(ModuleType.Engine, item => item.currentRange);

        newShipUnit_Script.isPlayerControlled = true;

        //Rename the go
        newShipGO.name = shipNameInputField.text;

        //Set Item type
        if (currentBuildSlotList[0].CurrentItem.shipType == ShipType.Stelite)
        {
            newShipUnit_Script.unitType = Unit.UnitType.Satelite;
        }
        else
        {
            newShipUnit_Script.unitType = Unit.UnitType.Ship;
        }

        //Sen message about new ship tp player
        string mainMessage = $"BUILD RAPPORT {unitScript.unitName} produced {newShipUnit_Script.unitName}";
        string subMessage = "";
        MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
        unitScript.transform.position, Color.purple);
    }

    public int GetAllModuleValues(ModuleType moduleTypeToCheck, System.Func<ItemInstance, int> valueSelector)
    {
        int total = 0;
        foreach (var slot in currentBuildSlotList)
        {
            var item = slot?.CurrentItem;
            if (item?.ItemDefinition == null) continue;
            if (item.ItemDefinition.moduleType != moduleTypeToCheck) continue;
            total += valueSelector(item);
        }
        return total;
    }

    public void CloseMessageBox()
    {
        MessageBox.offsetMax = new Vector2(6000, 0);
        MessageBox.offsetMin = new Vector2(6000, 0);
    }
}
