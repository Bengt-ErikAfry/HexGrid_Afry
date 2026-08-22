using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;
using Random = UnityEngine.Random;

public class AsteroidComponent : MonoBehaviour
{
    public int amountOfProducingMiningOutpost;
    public int amountOfStoredRocks;
    public float amountOfAvalibuleOre;
    public int nrOfSatelitesInOrbit;
    public int nrOfSatelitesOnPlanet;
    public Sprite miningOutpostSprite;

    public bool SurveyingPlanet;
    public int nrOfTilesSurvayed = -1;

    public int deployableMiningKitInOrbit;

    public int AbandonMiningOutpostDefenceDamage = 10;

    [Header("Instantiate Grid variabels")]
    public int initNrOfAbandonMiningStations;
    //public float chanseTheMineIsAbandon;
    public float totalAmountOfOre;
    public float minOrePerSlot;
    public float maxOrePerSlot;

    [SerializeField]
    //public List<TileData> surfaceSlotDataList = new List<TileData>();    //A list of current slotDATA. Accessed by AsteroidManager when survaying planet. Updated when placing mining outpost and when mining ore.

    [Header("Ore Concentrations")]
    public int gridSizeX;
    public int gridSizeY;
    public List<Sprite> ore_Map = new List<Sprite>();
    [Range(0f, 100f)]
    public float yield;
    //[Range(0f, 100f)]
    //public float chanseToFindAbandonMiningOutpost;

    [Header("Material Concentrations")]
    [Range(0f, 100f)]
    public float ironConcentration;
    [Range(0f, 100f)]
    public float copperConcentration;
    [Range(0f, 100f)]
    public float siliconeConcentration;

    // Track whether we've subscribed so we don't subscribe/unsubscribe incorrectly.
    private bool isSubscribed = false;

    [Header("For Debuging only. DO NOT SET in inspector")]
    public Inventory inventory;
    public List<Unit> playerUnitsInOrbit = new List<Unit>();

    public void Start()
    {
        TrySubscribe();

        //Get reference
        inventory = GetComponent<Inventory>();

        //Create Slotlist so that AstroidManager can access them when survaying planet.
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                //TileData surfaceSlotData = new TileData();

                //surfaceSlotData.slotIndexCol = y;
                //surfaceSlotData.slotIndexRow = x;
                //surfaceSlotDataList.Add(surfaceSlotData);
            }
        }

        ////Populate random slots with mines

        int totalCells = gridSizeX * gridSizeY;
        HashSet<int> mines = new HashSet<int>();

        // Pick unique random indices
        while (mines.Count < initNrOfAbandonMiningStations)
        {
            mines.Add(Random.Range(0, totalCells));
        }

        // Place mines
        foreach (int index in mines)
        {
            //surfaceSlotDataList[index].hasMiningOutpost = true;
            //surfaceSlotDataList[index].hasAbandonMiningOutpost = true;
            /*
            float value = UnityEngine.Random.Range(0f, 100f);
            if (value <= chanseTheMineIsAbandon)
            {
                surfaceSlotDataList[index].hasAbandonMiningOutpost = true;
            }
            else
            {
                surfaceSlotDataList[index].hasAbandonMiningOutpost = false;
            }*/
        }

        //Set ore amount for each slot
        GenerateOreDistribution();
    }

    public void GenerateOreDistribution()
    {
        //oreList.Clear();

        float remainingOre = totalAmountOfOre;

        // First pass: assign minimum guaranteed values
        /*for (int i = 0; i < surfaceSlotDataList.Count; i++)
        {
            float value = Mathf.Min(minOrePerSlot, remainingOre);
            surfaceSlotDataList[i].oreAmount = value;
            remainingOre -= value;
        }*/
        /*

        // Second pass: randomly distribute remaining ore
        int index = 0;
        while (remainingOre > 0)
        {
            float maxAdd = Mathf.Min(maxOrePerSlot - surfaceSlotDataList[index].oreAmount, remainingOre);

            if (maxAdd > 0)
            {
                float randomAdd = Random.Range(0, maxAdd + 1);
                surfaceSlotDataList[index].oreAmount += randomAdd;
                remainingOre -= randomAdd;
            }

            index = (index + 1) % surfaceSlotDataList.Count;
        }*/
    }

    public void StartSurvay()
    {
        if (nrOfSatelitesOnPlanet > 0)
            SurveyingPlanet = true;
    }

    void OnEnable()
    {
        // If component is re-enabled at runtime after Start, ensure subscription exists.
        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnNewTurn += PlayerNewTurn;
            GameManager.Instance.OnEnemyTurn += EnemyNewTurn; // Subscribe to enemy turn as well if needed
            isSubscribed = true;
        }
    }

    void OnDisable()
    {
        if (isSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnNewTurn -= PlayerNewTurn;
            GameManager.Instance.OnEnemyTurn -= EnemyNewTurn;
            isSubscribed = false;
        }
    }

    void TrySubscribe()
    {
        // Start() runs after all Awake() calls, so GameManager.Instance should be initialized.
        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnNewTurn += PlayerNewTurn;
            GameManager.Instance.OnEnemyTurn += EnemyNewTurn;
            isSubscribed = true;
        }
    }

    public void EnemyNewTurn()
    {
        playerUnitsInOrbit.Clear();

        playerUnitsInOrbit = GameManager.Instance.GetPlayerUnitsInOrbit(this.transform.position);
        /*
        // Get all player ship in orbit.
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int asteroidCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(this.transform.position);
            if (playerUnitCurrentHexPos == asteroidCurrentHexPos)
            {
                playerUnitsInOrbit.Add(playerUnit);
            }
        }*/

        //get random.
        /*if (playerUnitsInOrbit.Count > 0)
        {
            for (int i = 0; i < surfaceSlotDataList.Count; i++)
            {
                if (surfaceSlotDataList[i].hasAbandonMiningOutpost && surfaceSlotDataList[i].isSurveyed && surfaceSlotDataList[i].aliens_Defences > 0 && surfaceSlotDataList[i].health_Defence > 0)
                {
                    int randomIndex = Random.Range(0, playerUnitsInOrbit.Count);
                    Unit selectedUnit = playerUnitsInOrbit[randomIndex];

                    //Attack.
                    if (selectedUnit != null)
                    {
                        //Damage player ship
                        int damageAmount = Random.Range(1, AbandonMiningOutpostDefenceDamage);

                        if (selectedUnit.moduleRuntimeList.Count > 0)
                        {
                            int indexToHit = Random.Range(0, selectedUnit.moduleRuntimeList.Count);
                            selectedUnit.moduleRuntimeList[indexToHit].TakeDamage(damageAmount);
                            MessageSystemManager.Instance.CreateMessage($"Ship {selectedUnit.unitName} was damaged {selectedUnit.moduleRuntimeList[indexToHit].ItemDefinition.itemName} by an abandoned mining outpost for {damageAmount} damage!", null, this.transform.position, Color.red);
                        }
                    }
                }
            }
        }*/
    }
    public void PlayerNewTurn()
    {
        //Reset player Turn Over on Mining Outposts
        /*for (int u = 0; u < surfaceSlotDataList.Count; u++)
        {
            surfaceSlotDataList[u].isPlayerTurnOver = false;
        }

        //Survay planet
        if (SurveyingPlanet)
        {
            for (int i = 0; i < nrOfSatelitesOnPlanet; i++)
            {
                if (nrOfTilesSurvayed < surfaceSlotDataList.Count)
                {
                    //Set tile as survayed
                    surfaceSlotDataList[nrOfTilesSurvayed].isSurveyed = true;

                    nrOfTilesSurvayed++;
                }
                else
                {
                    Debug.Log("Survay of Planet Completed");
                    SurveyingPlanet = false;
                }
            }
        }

        //Produce if Have Outpost and room in inventory.
        if (amountOfProducingMiningOutpost > 0)
        {
            for (int i = 0; i < surfaceSlotDataList.Count; i++)
            {
                TileData surfaceSlotData = surfaceSlotDataList[i];
                if (surfaceSlotData.hasMiningOutpost && !surfaceSlotData.hasAbandonMiningOutpost && surfaceSlotData.oreAmount > 1 && inventory.maxItems > inventory.itemInstance.Count)
                {
                    //Have room
                    //Remove ore from slot
                    surfaceSlotData.oreAmount--;
                    totalAmountOfOre--;

                    //ADD ore to storage
                    ItemDefinition rockItem = Resources.Load<ItemDefinition>("Ore/Ore_Rock");
                    ItemInstance rockItemitemInstance = new ItemInstance();
                    rockItemitemInstance.ItemDefinition = rockItem;
                    if (rockItem != null)
                    {
                        inventory.AddItem(rockItemitemInstance, 1);
                    }
                    amountOfStoredRocks++;            
                }
            }
        }*/
    }
}