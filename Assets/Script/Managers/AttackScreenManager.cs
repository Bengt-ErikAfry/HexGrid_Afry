using DG.Tweening;
using JetBrains.Annotations;
using System;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackScreenManager : MonoBehaviour
{
    /*
    public static AttackScreenManager Instance { get; private set; }

    public RectTransform bombardmentOptionsPanel;
    public RectTransform attackMiningOutpostPanel;

    public Button bombardFromSpace;
    public Button sendInSoldiersBT;
    public Button repairBT;

    public Button leftDoorBT;
    public Button rightDoorBT;
    public Button defenceBT;
    public Button commandCenterBT;
    public Button storrageBT;
    public Button drillBT;
    public Button powerCoreBT;

    public Image leftDoorImage;
    public Image rightDoorImage;
    public Image defenceImage;
    public Image commandcenterImage;
    public Image storageImage;
    public Image drillImage;
    public Image powerCoreImage;

    public TextMeshProUGUI leftDoorText;
    public TextMeshProUGUI rightDoorText;
    public TextMeshProUGUI defenceText;
    public TextMeshProUGUI commandcenterText;
    public TextMeshProUGUI storageText;
    public TextMeshProUGUI drillText;
    public TextMeshProUGUI powerCoreText;

    private ItemDefinition missile_Item;
    private ItemDefinition solider_Item;
    private ItemDefinition engineer_Item;

    public GameObject MissileMissedTargetBT;

    public bool isSendingSoldeir_Mode = false;
    public bool isReparing_Mode = false;

    [Header("Bombardment panel")]
    public TMP_Text aliensLeftTezxt;

    public TextMeshProUGUI chansDefence_Text;
    public TextMeshProUGUI chansComand_Text;
    public TextMeshProUGUI chansLeftDoor_Text;
    public TextMeshProUGUI chansRightDoor_Text;
    public TextMeshProUGUI chaneStorage_Text;
    public TextMeshProUGUI chansDrill_Text;
    public TextMeshProUGUI chansPowerCore_Text;
    public TextMeshProUGUI chansMiss_Text;

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

    public void Start()
    {
        missile_Item = Resources.Load<ItemDefinition>("Weapon/Missile_Bombardment");
        solider_Item = Resources.Load<ItemDefinition>("Units/SoldierCrew");
        engineer_Item = Resources.Load<ItemDefinition>("Units/EngineerCrew");
        if(missile_Item == null)
        {
            Debug.LogError("Failed to load missile_Item from Resourse folder");
        }
        if (solider_Item == null)
        {
            Debug.LogError("Failed to load solider_Item from Resourse folder");
        }
        if (engineer_Item == null)
        {
            Debug.LogError("Failed to load engineer_Item from Resourse folder");
        }
    }

    public void BombardFromSpace_buttonPressed()
    {
        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;
        UpdateBombardmentPanel();
        Show(bombardmentOptionsPanel);
    }
    public void AttackMiningOutpost_buttonPressed()
    {
        Show(attackMiningOutpostPanel);
    }
    public void Close_ButtonPressed()
    {
        Hide(attackMiningOutpostPanel);
        AsteroidManager.Instance.ResetSlotInfo();
    }
    public void Hide(RectTransform panelToHide)
    {
        panelToHide.DOAnchorPos(new Vector2(0, 2000), 0.5f).SetEase(Ease.OutBack);
        panelToHide.gameObject.SetActive(false);
    }
    public void Show(RectTransform panelToShow)
    {
        panelToShow.gameObject.SetActive(true);
        panelToShow.DOAnchorPos(new Vector2(0, 0), 0.5f).SetEase(Ease.InBack);
        UpdateActionButtons();
    }
    public bool ShipInOrbitHaveItem(ItemDefinition itemToCheck)
    { 
        //Get all player units
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);
            if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
            {
                //Check if player unit have deployableMiningKit
                if (playerUnit.unitInventory.HasItem(itemToCheck, 1))
                {
                    //Get amount of deployableMiningKit in unit inventory
                    return true;
                }
            }
        }
        return false;
    }
    public Inventory GetInventoryFromShipInOrbitWithItem(ItemDefinition itemToCheck)
    {
        //Get all player units
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);
            if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
            {
                //Check if player unit have deployableMiningKit
                if (playerUnit.unitInventory.HasItem(itemToCheck, 1))
                {
                    //Get amount of deployableMiningKit in unit inventory
                    return playerUnit.unitInventory;
                }
            }
        }
        return null;
    }
    public int GetAmountOfItemsInAllShipInOrbit(ItemDefinition item)
    {
        int totalAmount = 0;
        //Get all player units
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);
            if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
            {
                //Check if player unit have deployableMiningKit
                if (playerUnit.unitInventory.HasItem(item, 1))
                {
                    //Get amount of deployableMiningKit in unit inventory
                    totalAmount = totalAmount + playerUnit.unitInventory.GetItemCount(item);
                }
            }
        }
        return totalAmount;
    }
    public void SendInSoliderBt_Pressed()
    {
        isSendingSoldeir_Mode = true;
        isReparing_Mode = false;
        UpdateActionButtons();
    }
    public void ReparingBt_Pressed()
    {
        isSendingSoldeir_Mode = false;
        isReparing_Mode = true;
        UpdateActionButtons();
    }
    public void BombardmentPanel_Close_buttonPressed()
    {
         Hide(bombardmentOptionsPanel);
    }
    public void BombardmentPanel_FireBomb_buttonPressed()
    {
        FireBomb();
    }
    public void LeftDoorBT_Pressed()
    {
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_LeftDoor = CalculateAliensSurvivors(surfaceSlotData_script.aliens_LeftDoor);
            Debug.Log("Left Door Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_LeftDoor);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_LeftDoor - surfaceSlotData_script.health_LeftDoor)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_LeftDoor += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_LeftDoor - surfaceSlotData_script.health_LeftDoor);
                surfaceSlotData_script.health_LeftDoor = surfaceSlotData_script.maxHealth_LeftDoor;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void RightDoorBT_Pressed()
    {       
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_RightDoor = CalculateAliensSurvivors(surfaceSlotData_script.aliens_RightDoor);
            Debug.Log("Right Door Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_RightDoor);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_RightDoor - surfaceSlotData_script.health_RightDoor)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_RightDoor += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_RightDoor - surfaceSlotData_script.health_RightDoor);
                surfaceSlotData_script.health_RightDoor = surfaceSlotData_script.maxHealth_RightDoor;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void StorageBT_Pressed()
    {        
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_Storage = CalculateAliensSurvivors(surfaceSlotData_script.aliens_Storage);
            Debug.Log("Storage Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_Storage);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_Storage - surfaceSlotData_script.health_Storage)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_Storage += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_Storage - surfaceSlotData_script.health_Storage);
                surfaceSlotData_script.health_Storage = surfaceSlotData_script.maxHealth_Storage;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void CommandCenterBT_Pressed()
    {        
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_CommandCenter = CalculateAliensSurvivors(surfaceSlotData_script.aliens_CommandCenter);
            Debug.Log("Command Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_CommandCenter);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_RightDoor - surfaceSlotData_script.health_RightDoor)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_RightDoor += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_RightDoor - surfaceSlotData_script.health_RightDoor);
                surfaceSlotData_script.health_RightDoor = surfaceSlotData_script.maxHealth_RightDoor;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void DefenceBT_Pressed()
    {        
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_Defences = CalculateAliensSurvivors(surfaceSlotData_script.aliens_Defences);
            Debug.Log("Defend Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_Defences);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_Defence - surfaceSlotData_script.health_Defence)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_Defence += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_Defence - surfaceSlotData_script.health_Defence);
                surfaceSlotData_script.health_Defence = surfaceSlotData_script.maxHealth_Defence;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void DrillBT_Pressed()
    {        
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_Drill = CalculateAliensSurvivors(surfaceSlotData_script.aliens_Drill);
            Debug.Log("Drill Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_Drill);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_Drill - surfaceSlotData_script.health_Drill)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_Drill += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_Drill - surfaceSlotData_script.health_Drill);
                surfaceSlotData_script.health_Drill = surfaceSlotData_script.maxHealth_Drill;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }
    public void PowerCoreBT_Pressed()
    {       
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (isSendingSoldeir_Mode)
        {
            surfaceSlotData_script.aliens_PowerCore = CalculateAliensSurvivors(surfaceSlotData_script.aliens_PowerCore);
            Debug.Log("PowerCore Pressed - Sending Soldiers Mode:" + isSendingSoldeir_Mode + " alien remaning:" + surfaceSlotData_script.aliens_PowerCore);
        }
        else if (isReparing_Mode)
        {
            Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(engineer_Item);
            int nrPlayerEngineers = playerInventory.GetItemCount(engineer_Item);

            if (nrPlayerEngineers < surfaceSlotData_script.maxHealth_PowerCore - surfaceSlotData_script.health_PowerCore)
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, nrPlayerEngineers);
                surfaceSlotData_script.health_PowerCore += nrPlayerEngineers;
            }
            else
            {
                playerInventory.RemoveItem(new ItemInstance() { ItemDefinition = engineer_Item }, surfaceSlotData_script.maxHealth_PowerCore - surfaceSlotData_script.health_PowerCore);
                surfaceSlotData_script.health_PowerCore = surfaceSlotData_script.maxHealth_PowerCore;
            }
        }

        isSendingSoldeir_Mode = false;
        isReparing_Mode = false;

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;

        UpdateActionButtons();
    }  
    public void UpdateActionButtons()
    {
        // Get surface slot data
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        if (surfaceSlotData_script.isPlayerTurnOver)
        {
            //Turn Over

            //DeActivate buttons
            bombardFromSpace.interactable = false;
            sendInSoldiersBT.interactable = false;
            repairBT.interactable = false;

            //DeActivate part buttons
            leftDoorBT.interactable = false;
            rightDoorBT.interactable = false;
            storrageBT.interactable = false;
            commandCenterBT.interactable = false;
            defenceBT.interactable = false;
            drillBT.interactable = false;
            powerCoreBT.interactable = false;
        }
        else
        {
            //New Turn

            //Activate buttons
            bombardFromSpace.interactable = ShipInOrbitHaveItem(missile_Item);
            sendInSoldiersBT.interactable = ShipInOrbitHaveItem(solider_Item);
            repairBT.interactable = ShipInOrbitHaveItem(engineer_Item);

            if (isSendingSoldeir_Mode)
            {
                leftDoorBT.interactable = true;
                rightDoorBT.interactable = true;

                storrageBT.interactable = surfaceSlotData_script.aliens_LeftDoor == 0 || surfaceSlotData_script.aliens_RightDoor == 0;

                commandCenterBT.interactable = (surfaceSlotData_script.aliens_LeftDoor == 0 && surfaceSlotData_script.aliens_Storage == 0) ||
                    (surfaceSlotData_script.aliens_RightDoor == 0 && surfaceSlotData_script.aliens_Storage == 0);

                defenceBT.interactable = (surfaceSlotData_script.aliens_LeftDoor == 0 && surfaceSlotData_script.aliens_Storage == 0 && surfaceSlotData_script.aliens_CommandCenter == 0) ||
                    (surfaceSlotData_script.aliens_RightDoor == 0 && surfaceSlotData_script.aliens_Storage == 0 && surfaceSlotData_script.aliens_CommandCenter == 0);

                drillBT.interactable = (surfaceSlotData_script.aliens_LeftDoor == 0 && surfaceSlotData_script.aliens_Storage == 0) ||
                    (surfaceSlotData_script.aliens_RightDoor == 0 && surfaceSlotData_script.aliens_Storage == 0);

                powerCoreBT.interactable = (surfaceSlotData_script.aliens_LeftDoor == 0 && surfaceSlotData_script.aliens_Storage == 0) ||
                    (surfaceSlotData_script.aliens_RightDoor == 0 && surfaceSlotData_script.aliens_Storage == 0);
            }
            else if (isReparing_Mode)
            {
                leftDoorBT.interactable = false;
                rightDoorBT.interactable = false;
                storrageBT.interactable = false;
                commandCenterBT.interactable = false;
                defenceBT.interactable = false;
                drillBT.interactable = false;
                powerCoreBT.interactable = false;

                leftDoorBT.interactable = surfaceSlotData_script.health_LeftDoor < surfaceSlotData_script.maxHealth_LeftDoor;
                rightDoorBT.interactable = surfaceSlotData_script.health_RightDoor < surfaceSlotData_script.maxHealth_RightDoor;
                storrageBT.interactable = surfaceSlotData_script.health_Storage < surfaceSlotData_script.maxHealth_Storage;
                commandCenterBT.interactable = surfaceSlotData_script.health_CommandCenter < surfaceSlotData_script.maxHealth_CommandCenter;
                defenceBT.interactable = surfaceSlotData_script.health_Defence < surfaceSlotData_script.maxHealth_Defence;
                drillBT.interactable = surfaceSlotData_script.health_Drill < surfaceSlotData_script.maxHealth_Drill;
                powerCoreBT.interactable = surfaceSlotData_script.health_PowerCore < surfaceSlotData_script.maxHealth_PowerCore;
            }
            else
            {
                leftDoorBT.interactable = false;
                rightDoorBT.interactable = false;
                storrageBT.interactable = false;
                commandCenterBT.interactable = false;
                defenceBT.interactable = false;
                drillBT.interactable = false;
                powerCoreBT.interactable = false;
            }
        }

        //Get amount of missiles in orbit.
        bombardFromSpace.GetComponentInChildren<TextMeshProUGUI>().text = "Bombard from space. Nr Misiles left: " +
        GameManager.Instance.GetAmountOfItemsInOrbit(AsteroidManager.Instance.selectedAsteroidComp.gameObject, missile_Item);

        sendInSoldiersBT.GetComponentInChildren<TextMeshProUGUI>().text = "Send in Soldiers. " +
        GameManager.Instance.GetAmountOfItemsInOrbit(AsteroidManager.Instance.selectedAsteroidComp.gameObject, solider_Item);

        repairBT.GetComponentInChildren<TextMeshProUGUI>().text = "Repair with engineer. " +
        GameManager.Instance.GetAmountOfItemsInOrbit(AsteroidManager.Instance.selectedAsteroidComp.gameObject, engineer_Item);

        //Parts health
        leftDoorText.text = "Left door Healt " + surfaceSlotData_script.health_LeftDoor.ToString() + "/" + surfaceSlotData_script.maxHealth_LeftDoor.ToString();
        rightDoorText.text = "Right door: " + surfaceSlotData_script.health_RightDoor.ToString() + "/" + surfaceSlotData_script.maxHealth_RightDoor.ToString();
        storageText.text = "Storage: " + surfaceSlotData_script.health_Storage.ToString() + "/" + surfaceSlotData_script.maxHealth_Storage.ToString();
        defenceText.text = "Defence: " + surfaceSlotData_script.health_Defence.ToString() + "/" + surfaceSlotData_script.maxHealth_Defence.ToString();
        commandcenterText.text = "Command Center: " + surfaceSlotData_script.health_CommandCenter.ToString() + "/" + surfaceSlotData_script.maxHealth_CommandCenter.ToString();
        drillText.text = "Drill: " + surfaceSlotData_script.health_Drill.ToString() + "/" + surfaceSlotData_script.maxHealth_Drill.ToString();
        powerCoreText.text = "Power Core: " + surfaceSlotData_script.health_PowerCore.ToString() + "/" + surfaceSlotData_script.maxHealth_PowerCore.ToString();

        //Check if MiningOutpost are taken over
        if (surfaceSlotData_script.aliens_CommandCenter == 0 && surfaceSlotData_script.aliens_Defences == 0 && surfaceSlotData_script.aliens_LeftDoor == 0 &&
            surfaceSlotData_script.aliens_RightDoor == 0 && surfaceSlotData_script.aliens_Drill == 0 && surfaceSlotData_script.aliens_PowerCore == 0 && surfaceSlotData_script.aliens_Storage == 0)
        {
            //No Aliens
            if (surfaceSlotData_script.health_CommandCenter == surfaceSlotData_script.maxHealth_CommandCenter && surfaceSlotData_script.health_Defence == surfaceSlotData_script.maxHealth_Defence &&
                surfaceSlotData_script.health_LeftDoor == surfaceSlotData_script.maxHealth_LeftDoor && surfaceSlotData_script.health_RightDoor == surfaceSlotData_script.maxHealth_RightDoor &&
                surfaceSlotData_script.health_Drill == surfaceSlotData_script.maxHealth_Drill && surfaceSlotData_script.health_PowerCore == surfaceSlotData_script.maxHealth_PowerCore && surfaceSlotData_script.health_Storage == surfaceSlotData_script.maxHealth_Storage)
            {
                //Health full
                Debug.Log("Mining outpost taken over");
                MessageBoxManager.Instance.ShowMessage("Mining outpost taken over");

                //Convert to player mining outpost
                AsteroidManager.Instance.CreatePlayerOutpost(AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>());
            }
        }
    }
    public void UpdateBombardmentPanel()
    {
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        //Calculate total Aliens;
        int totalAliens = surfaceSlotData_script.aliens_CommandCenter + surfaceSlotData_script.aliens_Defences + surfaceSlotData_script.aliens_LeftDoor +
            surfaceSlotData_script.aliens_RightDoor + surfaceSlotData_script.aliens_Drill + surfaceSlotData_script.aliens_PowerCore + surfaceSlotData_script.aliens_Storage;
        
        aliensLeftTezxt.text = totalAliens.ToString();

        //Calculate chans to hit each part
        //TODO: make this more dynamic. So it will automaticly update if the design of the outpost change. For example if a new part is added or removed.

        //Set text
        chansDefence_Text.text = surfaceSlotData_script.chanseToHit_Defence.ToString() + "%";
        chansComand_Text.text = surfaceSlotData_script.chanseToHit_ComandCenter.ToString() + "%";
        chansLeftDoor_Text.text = surfaceSlotData_script.chanseToHit_LeftDoor.ToString() + "%";
        chansRightDoor_Text.text = surfaceSlotData_script.chanseToHit_RightDoor.ToString() + "%";
        chaneStorage_Text.text = surfaceSlotData_script.chanseToHit_Storage.ToString() + "%";
        chansDrill_Text.text = surfaceSlotData_script.chanseToHit_Drill.ToString() + "%";
        chansPowerCore_Text.text = surfaceSlotData_script.chanseToHit_PowerCore.ToString() + "%";
        chansMiss_Text.text = "45%";
    }
    public void CloseMissileMissedTargetBT()
    {
        MissileMissedTargetBT.SetActive(false);
    }
    public void FireBomb()
    {
        Debug.Log("FireBomb method called.");

        //Get random ship in orbit with missile in inventory and remove one missile from inventory
        Inventory shipInventory = GetInventoryFromShipInOrbitWithItem(missile_Item);
        if (shipInventory != null)
        {
            ItemInstance tempItemInstance = new ItemInstance();
            tempItemInstance.ItemDefinition = missile_Item;
            shipInventory.RemoveItem(tempItemInstance, 1);
        }

        //CAL damage
        SurfaceSlotData surfaceSlotData_script = AsteroidManager.Instance.selectedSurfaceSlotGO.GetComponent<SurfaceSlot>().surfaceSlotData;

        float damageModifier = UnityEngine.Random.Range(1, 100);
        int damage = Mathf.RoundToInt(missile_Item.damage * (damageModifier / 100));

        int Result = UnityEngine.Random.Range(1, 100);
        if (Result <= surfaceSlotData_script.chanseToHit_Defence)
        {
            surfaceSlotData_script.health_Defence -= damage;
            if(surfaceSlotData_script.health_Defence < 0)
            {
                surfaceSlotData_script.health_Defence = 0;
            }

            surfaceSlotData_script.aliens_Defences -= damage;
            if(surfaceSlotData_script.aliens_Defences < 0)
            {
                surfaceSlotData_script.aliens_Defences = 0;
            }

            Debug.Log($"Bomb hit Defence {surfaceSlotData_script.health_Defence} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence && 
            Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter)
        {
            surfaceSlotData_script.health_CommandCenter -= damage;
            if (surfaceSlotData_script.health_CommandCenter < 0)
            {
                surfaceSlotData_script.health_CommandCenter = 0;
            }

            surfaceSlotData_script.aliens_CommandCenter -= damage;
            if (surfaceSlotData_script.aliens_CommandCenter < 0)
            {
                surfaceSlotData_script.aliens_CommandCenter = 0;
            }

            Debug.Log($"Bomb hit Command {surfaceSlotData_script.aliens_CommandCenter} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter &&
            Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor)
        {
            surfaceSlotData_script.health_LeftDoor -= damage;
            if (surfaceSlotData_script.health_LeftDoor < 0)
            {
                surfaceSlotData_script.health_LeftDoor = 0;
            }

            surfaceSlotData_script.aliens_LeftDoor -= damage;
            if (surfaceSlotData_script.aliens_LeftDoor < 0)
            {
                surfaceSlotData_script.aliens_LeftDoor = 0;
            }

            Debug.Log($"Bomb hit Left Door {surfaceSlotData_script.aliens_LeftDoor} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor &&
            Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor)
        {
            surfaceSlotData_script.health_RightDoor -= damage;
            if (surfaceSlotData_script.health_RightDoor < 0)
            {
                surfaceSlotData_script.health_RightDoor = 0;
            }

            surfaceSlotData_script.aliens_RightDoor -= damage;
            if (surfaceSlotData_script.aliens_RightDoor < 0)
            {
                surfaceSlotData_script.aliens_RightDoor = 0;
            }

            Debug.Log($"Bomb hit Right Door {surfaceSlotData_script.aliens_RightDoor} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor &&
            Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor + surfaceSlotData_script.chanseToHit_Storage)
        {
            surfaceSlotData_script.health_Storage -= damage;
            if (surfaceSlotData_script.health_Storage < 0)
            {
                surfaceSlotData_script.health_Storage = 0;
            }

            surfaceSlotData_script.aliens_Storage -= damage;
            if (surfaceSlotData_script.aliens_Storage < 0)
            {
                surfaceSlotData_script.aliens_Storage = 0;
            }

            Debug.Log($"Bomb hit Storage {surfaceSlotData_script.aliens_Storage} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor + surfaceSlotData_script.chanseToHit_Storage
            && Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor + surfaceSlotData_script.chanseToHit_Storage + surfaceSlotData_script.chanseToHit_Drill)
        {
            surfaceSlotData_script.health_Drill -= damage;
            if (surfaceSlotData_script.health_Drill < 0)
            {
                surfaceSlotData_script.health_Drill = 0;
            }

            surfaceSlotData_script.aliens_Drill -= damage;
            if (surfaceSlotData_script.aliens_Drill < 0)
            {
                surfaceSlotData_script.aliens_Drill = 0;
            }

            Debug.Log($"Bomb hit Drill {surfaceSlotData_script.aliens_Drill} aliens left. Damage {damage}");
        }
        else if (Result > surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor + surfaceSlotData_script.chanseToHit_Storage + surfaceSlotData_script.chanseToHit_Drill &&
            Result <= surfaceSlotData_script.chanseToHit_Defence + surfaceSlotData_script.chanseToHit_ComandCenter + surfaceSlotData_script.chanseToHit_LeftDoor + surfaceSlotData_script.chanseToHit_RightDoor + surfaceSlotData_script.chanseToHit_Storage + surfaceSlotData_script.chanseToHit_Drill + surfaceSlotData_script.chanseToHit_PowerCore)
        {
            surfaceSlotData_script.health_PowerCore -= damage;
            if (surfaceSlotData_script.health_PowerCore < 0)
            {
                surfaceSlotData_script.health_PowerCore = 0;
            }

            surfaceSlotData_script.aliens_PowerCore -= damage;
            if (surfaceSlotData_script.aliens_PowerCore < 0)
            {
                surfaceSlotData_script.aliens_PowerCore = 0;
            }

            Debug.Log($"Bomb hit PowerCore {surfaceSlotData_script.aliens_PowerCore} aliens left. Damage {damage}");
        }
        else
        {
            Debug.Log($"Bomb miss");
        }

        UpdateActionButtons();
        Hide(bombardmentOptionsPanel);

        //Set component bool to true to end player turn.
        surfaceSlotData_script.isPlayerTurnOver = true;
    }
    public int CalculateAliensSurvivors(int nrAlienDefenders)
    {
        Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(solider_Item);
        int nrPlayerSoldiers = playerInventory.GetItemCount(solider_Item);
        int nrPlayerSoldiers_survivers = 0;
        int nrAlienDefenders_survivers = 0;

        if (nrPlayerSoldiers == nrAlienDefenders)
        {
            //All Die exsept random one.
            int Result = UnityEngine.Random.Range(1, 2);
            if (Result == 1)
            {
                nrPlayerSoldiers_survivers = 1;
                nrAlienDefenders_survivers = 0;

                MessageBoxManager.Instance.ShowMessage($"VICTORY! Soldiers lost: {nrPlayerSoldiers - nrPlayerSoldiers_survivers}/{nrPlayerSoldiers}, Alien killed: {nrAlienDefenders- nrAlienDefenders_survivers}/{nrAlienDefenders}");
            }
            else
            {
                nrPlayerSoldiers_survivers = 0;
                nrAlienDefenders_survivers = 1;

                MessageBoxManager.Instance.ShowMessage($"LOSE! Soldiers lost: {nrPlayerSoldiers - nrPlayerSoldiers_survivers}/{nrPlayerSoldiers}, Alien killed: {nrAlienDefenders - nrAlienDefenders_survivers}/{nrAlienDefenders}");
            }
        }
        else if (nrPlayerSoldiers > nrAlienDefenders)
        {
            // use "Lanchester’s Square Law" to calculate nr of survival.
            nrPlayerSoldiers_survivers = (int)Math.Floor(Math.Sqrt(nrPlayerSoldiers * nrPlayerSoldiers - nrAlienDefenders * nrAlienDefenders));
            nrAlienDefenders_survivers = 0;

            MessageBoxManager.Instance.ShowMessage($"VICTORY! Soldiers lost: {nrPlayerSoldiers - nrPlayerSoldiers_survivers}/{nrPlayerSoldiers}, Alien killed: {nrAlienDefenders - nrAlienDefenders_survivers}/{nrAlienDefenders}");
        }
        else if (nrAlienDefenders > nrPlayerSoldiers)
        {
            // use "Lanchester’s Square Law" to calculate nr of survival.
            nrAlienDefenders_survivers = (int)Math.Floor(Math.Sqrt(nrAlienDefenders * nrAlienDefenders - nrPlayerSoldiers * nrPlayerSoldiers));
            nrPlayerSoldiers_survivers = 0;

            MessageBoxManager.Instance.ShowMessage($"LOSE! Soldiers lost: {nrPlayerSoldiers - nrPlayerSoldiers_survivers}/{nrPlayerSoldiers}, Alien killed: {nrAlienDefenders - nrAlienDefenders_survivers}/{nrAlienDefenders}");
        }

        //if Solders are 0 the slot will be removed from the inventory. This is handeled by the "ReplaceAmountItem" function
        ItemInstance tempItemInstance = new ItemInstance();
        tempItemInstance.ItemDefinition = solider_Item;

        ItemDefinition soliderItem = Resources.Load<ItemDefinition>("Units/SoldierCrew");
        if(soliderItem == null) {Debug.LogError("Failed to load soliderItem from Resourse folder"); }

        int newSoldierAmount = playerInventory.GetItemCount(soliderItem) - nrPlayerSoldiers_survivers;

        playerInventory.RemoveItem(tempItemInstance, newSoldierAmount);
        return (int)nrAlienDefenders_survivers;
    }*/
}
