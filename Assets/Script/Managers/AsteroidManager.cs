using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;


public class AsteroidManager : MonoBehaviour
{
    /*public TMP_Text satelitesOnPlanet;
    public TMP_Text miningOutpostOnPlanet;
    public TMP_Text producingRocksAmount;
    public TMP_Text storageRocksAmount;
    public TMP_Text descoveredAmountOfOreText;
    public GameObject selectedSurfaceSlotGO;
    public Button attackButton;
    public TMP_Text avalibuleSatelitesInOrbit;
    public TMP_Text avalibuleDeployableKitInOrbit;
    public TMP_Text slotContent;
    public TMP_Text slotDescriptionText;

    public GameObject miningOutpostPrefab;

    public AsteroidComponent selectedAsteroidComp;

    public List<GameObject> gridSlotsList = new List<GameObject>(); //The GO to show Component info.

    [Header("Ore Concentrations")]
    public RectTransform surfaceSlotsParent; // Parent panel (with RectTransform)
    public GameObject surfaceSquaresSlottsPrefab; // Your UI prefab (with RectTransform)
    private float squareSize = 128f; // width and height in pixels (UI coordinates

    public bool isPlacingMiningOutpost;

    public static AsteroidManager Instance { get; private set; }

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

    public void ShowAsteroidView()
    {
        selectedAsteroidComp = SelectionService.Instance.SelectedUnit.asteroidComponent;

        //Reset slot Info
        ResetSlotInfo();

        //Reset Slots GO
        foreach (Transform slotGO in surfaceSlotsParent)
        {
            Destroy(slotGO.gameObject);
        }

        //Create or show grid
        Debug.Log("Spawn grid");
        SpawnGrid();

        //Update grid data
        UpdateAllSlots();

        //Update asteroid stats.
        UpdateAsteroidStats(selectedAsteroidComp);
    }
    void SpawnGrid()
    {
        // Calculate total size of the grid in pixels
        float totalWidth = selectedAsteroidComp.gridSizeX * squareSize;
        float totalHeight = selectedAsteroidComp.gridSizeY * squareSize;

        // Calculate starting position (top-left corner of the grid relative to center)
        // Since UI y-axis goes up, start y is half height up, start x is half width left
        Vector2 startPos = new Vector2(-totalWidth / 2 + squareSize / 2, totalHeight / 2 - squareSize / 2);

        gridSlotsList.Clear();

        for (int y = 0; y < selectedAsteroidComp.gridSizeY; y++)
        {
            for (int x = 0; x < selectedAsteroidComp.gridSizeX; x++)
            {
                // Calculate position for each square relative to center of the canvas
                Vector2 pos = new Vector2(startPos.x + x * squareSize, startPos.y - y * squareSize);

                // Instantiate and set parent to this GameObject (make sure this is under Canvas)
                GameObject go = Instantiate(surfaceSquaresSlottsPrefab, surfaceSlotsParent);

                // Set localPosition of the RectTransform
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.localPosition = pos;

                // Optional: reset scale and rotation (if needed)
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                gridSlotsList.Add(go);

                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SurfaceSlotClicked(go);
                });
            }
        }
    }
    public void UpdateAllSlots()
    {
        for (int i = 0; i < gridSlotsList.Count; i++)
        {
            //Get slot script
            SurfaceSlot surfaceSlotScript = gridSlotsList[i].GetComponent<SurfaceSlot>();

            //Set reference to slot data so when slot is clicked We can get the data from the slot.
            surfaceSlotScript.surfaceSlotData = selectedAsteroidComp.surfaceSlotDataList[i];

            //Chech if survayed.
            if (selectedAsteroidComp.surfaceSlotDataList[i].isSurveyed)
            {
                gridSlotsList[i].GetComponent<Image>().sprite = selectedAsteroidComp.ore_Map[i];

                //Check if has abandon mining outpost
                if (selectedAsteroidComp.surfaceSlotDataList[i].hasMiningOutpost)
                {
                    gridSlotsList[i].transform.GetChild(2).GetComponent<Image>().sprite = selectedAsteroidComp.miningOutpostSprite;
                    if (selectedAsteroidComp.surfaceSlotDataList[i].hasAbandonMiningOutpost)
                    {
                        gridSlotsList[i].transform.GetChild(2).GetComponent<Image>().color = Color.red;
                    }

                    surfaceSlotScript.MiningOutpostGO.SetActive(true);
                }
            }
        }
    }
    public void SurfaceSlotClicked(GameObject surfaceSlot_Clicked)
    {
        selectedSurfaceSlotGO = surfaceSlot_Clicked;   //used later for when exsample bombarding from space
        SurfaceSlot surfaceSlotScript = surfaceSlot_Clicked.GetComponent<SurfaceSlot>();
        SurfaceSlotData surfaceSlotData = surfaceSlotScript.surfaceSlotData;
        //AsteroidComponent asteroidComponent = SelectionService.Instance.SelectedUnit.asteroidComponent;
        Inventory inventory = SelectionService.Instance.SelectedUnit.unitInventory;

        if (isPlacingMiningOutpost && surfaceSlotData.isSurveyed && !surfaceSlotData.hasMiningOutpost)
        {
            Debug.Log("Placing Mining Outposts on " + this.gameObject.name +
                        " indexRow:" + surfaceSlotData.slotIndexRow +
                        " indexCol:" + surfaceSlotData.slotIndexRow +
                        " isSurveyed:" + surfaceSlotData.isSurveyed +
                        " hasMiningOutpost:" + surfaceSlotData.hasMiningOutpost);

            Debug.Log("Mining Outpost placed!");
            CreatePlayerOutpost(surfaceSlotScript);
            /*
            surfaceSlotData.hasMiningOutpost = true;
            surfaceSlotScript.MiningOutpostGO.GetComponent<Image>().sprite = selectedAsteroidComp.miningOutpostSprite;
            surfaceSlotScript.MiningOutpostGO.SetActive(true);

            selectedAsteroidComp.amountOfProducingMiningOutpost++;
            selectedAsteroidComp.amountOfAvalibuleOre = selectedAsteroidComp.amountOfAvalibuleOre + surfaceSlotData.oreAmount;
            inventory.maxItems = inventory.maxItems + 3;
            */
    /*
            InputManager.Instance.isPlacingMiningOutpost = false;

            Debug.Log($"deployableMiningKitInOrbit: {selectedAsteroidComp.deployableMiningKitInOrbit}");

            //Remove from deployabel kit in orbit
            RemoveOneDeployableMiningOutpostKitFromUnitsInOrbit();
            selectedAsteroidComp.deployableMiningKitInOrbit--;


            Debug.Log($"deployableMiningKitInOrbit: {selectedAsteroidComp.deployableMiningKitInOrbit}");

            UpdateAsteroidStats(selectedAsteroidComp);

            isPlacingMiningOutpost=false;
        }
        else
        {
            ShowSurfaceSlotInformation(surfaceSlotData);
        }
    }
    public void CreatePlayerOutpost(SurfaceSlot surfaceSlotToAdd)
    {
        surfaceSlotToAdd.surfaceSlotData.hasMiningOutpost = true;
        surfaceSlotToAdd.MiningOutpostGO.GetComponent<Image>().sprite = selectedAsteroidComp.miningOutpostSprite;
        surfaceSlotToAdd.MiningOutpostGO.GetComponent<Image>().color = Color.white;
        surfaceSlotToAdd.MiningOutpostGO.SetActive(true);

        selectedAsteroidComp.amountOfProducingMiningOutpost++;
        selectedAsteroidComp.amountOfAvalibuleOre = selectedAsteroidComp.amountOfAvalibuleOre + surfaceSlotToAdd.surfaceSlotData.oreAmount;
        SelectionService.Instance.SelectedUnit.unitInventory.maxItems = SelectionService.Instance.SelectedUnit.unitInventory.maxItems + 3;
    }
    public void UpdateAsteroidStats(AsteroidComponent astroidComp)
    {
        Debug.Log("Updating Asteroid stats UI");
        satelitesOnPlanet.text = $"Satelites on planet: {astroidComp.nrOfSatelitesOnPlanet}";
        producingRocksAmount.text = $"Producing: Rocks:{astroidComp.amountOfProducingMiningOutpost}/turn";
        storageRocksAmount.text = $"Storage: Rocks:{astroidComp.amountOfStoredRocks}/{astroidComp.amountOfProducingMiningOutpost * 2}";
        descoveredAmountOfOreText.text = $"Descovered amount of ore: {astroidComp.amountOfAvalibuleOre}";
        SetAmountOfOrbitingSatelites();
        avalibuleSatelitesInOrbit.text = $"Avalibule Survay satelites in Orbit: {astroidComp.nrOfSatelitesInOrbit}";

        //// Mining outpost button text
        //Check if deployableMiningKit in orbit
        astroidComp.deployableMiningKitInOrbit = GetDeployableMiningKitInOrbit();
        avalibuleDeployableKitInOrbit.text = $"Avalibule Deployable Mining Outpost Kit in orbit: {astroidComp.deployableMiningKitInOrbit}";
    }
    public int GetDeployableMiningKitInOrbit()
    {
        //Reset count
        int amountOfKits = 0;

        //Get all player units
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);
            if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
            {
                //Check if player unit have deployableMiningKit
                if (playerUnit.unitInventory.HaveItemOfShipType(ShipType.DeployableMingingOutpostKit)) 
                {
                    //Get amount of deployableMiningKit in unit inventory
                    amountOfKits = amountOfKits + playerUnit.unitInventory.GetItemCountOfShipType(ShipType.DeployableMingingOutpostKit);
                }
            }
        }
        return amountOfKits;
    }
    public void RemoveOneDeployableMiningOutpostKitFromUnitsInOrbit()
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
                if (playerUnit.unitInventory.HaveItemOfShipType(ShipType.DeployableMingingOutpostKit))
                {
                    //Remove one deployableMiningKit from unit inventory
                    playerUnit.unitInventory.RemoveItemOfShipType(ShipType.DeployableMingingOutpostKit, 1);
                    break; // Exit the loop after removing one kit
                }
            }
        }
    }
    public void ShowSurfaceSlotInformation(SurfaceSlotData surfaceSlotData)
    {
        if(surfaceSlotData.isSurveyed)
        {
            if (surfaceSlotData.hasMiningOutpost)
            {
                if (surfaceSlotData.hasAbandonMiningOutpost)
                {
                    slotContent.text = $"This slot has a abandon mining outpost.";
                    slotDescriptionText.text = "Click Attack To attack";
                    if (surfaceSlotData.isPlayerTurnOver)
                    {
                        attackButton.interactable = false;
                    }
                    else
                    {
                        attackButton.interactable = true;
                    }
                }
                else
                {
                    slotContent.text = $"This slot has a mining outpost. It produces {surfaceSlotData.oreAmount} ore per turn.";
                    slotDescriptionText.text = $"Slot Row:{surfaceSlotData.slotIndexRow} Col:{surfaceSlotData.slotIndexCol}\n" +
                                                $"Ore Amount:{surfaceSlotData.oreAmount}\n" +
                                                $"Has Mining Outpost:{surfaceSlotData.hasMiningOutpost}\n" +
                                                $"Is Survayed:{surfaceSlotData.isSurveyed}";
                    attackButton.interactable = false;
                }
            }
            else
            {
                slotContent.text = $"This slot is survayed. It has {surfaceSlotData.oreAmount} amount of ore.";
                slotDescriptionText.text = $"Slot Row:{surfaceSlotData.slotIndexRow} Col:{surfaceSlotData.slotIndexCol}\n" +
                                            $"Ore Amount:{surfaceSlotData.oreAmount}\n" +
                                            $"Has Mining Outpost:{surfaceSlotData.hasMiningOutpost}\n" +
                                            $"Is Survayed:{surfaceSlotData.isSurveyed}";
                attackButton.interactable = false;
            }
        }
        else
        {
            slotContent.text = "This slot is not survayed.";
            slotDescriptionText.text = "";
            attackButton.interactable = false;
        }
    }
    public void ResetSlotInfo()
    {
        selectedSurfaceSlotGO = null;
        slotContent.text = "Select a square.";
        attackButton.interactable = false;
        slotDescriptionText.text = "";
    }
    public void SetAmountOfOrbitingSatelites()
    {
        //Get asteroid hexpos
        Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);

        //Reset nr of satelites
        SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesInOrbit = 0;

        //Get all playerUnit pos
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            if(playerUnit.unitType == Unit.UnitType.Satelite)
            {
                Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);

                if(playerUnitCurrentHexPos == asteroidCurrentHexPos)
                {
                    SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesInOrbit++;
                }
            }
        }
    }
    public void StartSurvayButtonPressed()
    {
        if (SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesInOrbit != 0)
        {
            //Add satelite
            SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesOnPlanet += SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesInOrbit;

            // Remove from orbit
            SelectionService.Instance.SelectedUnit.asteroidComponent.nrOfSatelitesInOrbit = 0;

            //Remove Satelite GO. Need a temp list becuse i can not remove objects from the playerUnits list while iterating through it.
            List<Unit> tempplayerUnitList = new List<Unit>();
            tempplayerUnitList = GameManager.Instance.playerUnits;

            for (int i = 0; i < tempplayerUnitList.Count; i++)
            {
                if (tempplayerUnitList[i].unitType == Unit.UnitType.Satelite)
                {
                    Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(tempplayerUnitList[i].transform.position);
                    Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(SelectionService.Instance.SelectedUnit.transform.position);

                    if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
                    {
                        Debug.Log($"Removing satelite {GameManager.Instance.playerUnits[i].unitName}");
                        GameManager.Instance.playerUnits[i].RemoveUnitFromPlay();
                    }
                }
            }
        }

        //Start survay
        SelectionService.Instance.SelectedUnit.asteroidComponent.StartSurvay();

        //Update UI
        UpdateAsteroidStats(SelectionService.Instance.SelectedUnit.asteroidComponent);
    }
    public void PlaceMouningOutpostButtonPressed()
    {
        isPlacingMiningOutpost = true;
    }*/
}
