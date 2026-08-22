using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.Rendering;
using UnityEditor.Tilemaps;
#endif
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;

public class GameManager : MonoBehaviour
{
    public event Action OnNewTurn;  //Fire an event when player star a new turn. To tell all factories to +1 turn and survays.
    public event Action OnEnemyTurn;  //Fire an event when enemy turn starts. To tell mining outpost to attack ships in orbit.

    public static GameManager Instance { get; private set; }

    public GameObject FogOfWarDisplay;
    public bool activateFogOfWar = true;

    public List<Unit> unitsList = new List<Unit>();     //All units. Sets in the inspector.
    public List<Unit> playerUnits = new List<Unit>();   //Player controled units. Sets in the inspector
    public List<Unit> enemyUnits = new List<Unit>();    //Enemy list. Sets in the inspector.

    public bool playerTurn;
    private int remainingEnemies;   //used to determine if all enemy have moved/attacked before switching back to player turn.

    public int currentTurnIndex;    //what player start therer turn.

    public List<BluePrints> playerBluePrints = new List<BluePrints>(); 

    public TMP_Text turnNumberText;
    public int turnNumber = 0;

    public GameObject MotherShip;   //Used for exsampel in a mining component to know where to unload ore.
    public Unit motherShip_Unit_Script;

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

    void Start()
    {
        //Get mothership Unit script.
        motherShip_Unit_Script = MotherShip.GetComponent<Unit>();

        if (activateFogOfWar) FogOfWarDisplay.SetActive(true);

        //Hide UI
        UIManager.Instance.stackViewRectTransform.gameObject.SetActive(false);

        //Setup player and enemy lists based on unitsList.
        SortPlayerAndEnemyList();

        // Read unit stats from itemdefinitions and apply them to the units.
        //ModuleManager.Instance.SetUpShipStats();
        foreach (var playerUnit in playerUnits)
        {
            ModuleManager.Instance.SetUpShipStats(playerUnit);
        }

        //SetUp Enemy ship modules
        ModuleManager.Instance.SetUpEnemyShipModules();

        // existing initialization...
        //Kick off first player turn (delayed one frame so other singletons finish Start)
        StartCoroutine(DelayedStartPlayerTurn());
    }

    private IEnumerator DelayedStartPlayerTurn()
    {
        // wait one frame (or yield return null twice if necessary)
        yield return null;
        yield return StartCoroutine(StartPlayerTurn());
    }

    public IEnumerator StartPlayerTurn()
    {
        OnNewTurn?.Invoke(); // Fire event to tell factories to +1 turn.

        playerTurn = true;

        // ✅ BLOCK INPUT FIRST
        GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);

        //Apply all module changes for the turne.
        ModuleManager.Instance.ApplyModuleChangesForTurn();

        // Calc energy consumption this lastturn.
        EnergyManager.Instance.CalculateEnergyConsumption();

        //Remove energy from all unit storage and turn off modules if unit dont have energy.
        EnergyManager.Instance.ApplyEnergyCostAndTurnOffModules();

        //Increment list
        currentTurnIndex++;
        if (currentTurnIndex > playerUnits.Count - 1) { currentTurnIndex = 0; }

        //Select unit
        if (playerUnits.Count > 0 && currentTurnIndex >= 0 && currentTurnIndex < playerUnits.Count)
        {
            SelectionService.Instance.SetSelectedUnit(playerUnits[currentTurnIndex]);
        }
        else
        {
            Debug.LogWarning("StartPlayerTurn: no player units available.");

            SelectionService.Instance.ClearSelection();
        }

        //Send message to player
        MessageSystemManager.Instance.CreateMessage("Unit " + playerUnits[currentTurnIndex].unitName + " turne started with "
            + (SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange - SelectionService.Instance.SelectedUnit.movedThisTurn) + " moves left!"
            , null, HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnits[currentTurnIndex].gameObject.transform.position), Color.green);

        //Reset UI
        UIManager.Instance.ResetUI();

        //Set selecting state
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Move Camera
        CameraManager.Instance.CenterOnSelectedObject();

        //Show selected unit (guard)
        if (playerUnits.Count > 0 && currentTurnIndex >= 0 && currentTurnIndex < playerUnits.Count)
            UIManager.Instance.ShowSelectedUnitView(playerUnits[currentTurnIndex]);

        /*
        //AutoRun miner if on mission
        if (SelectionService.Instance.SelectedUnit.miningComponent != null)
        {
            if (SelectionService.Instance.SelectedUnit.miningComponent.HasMission)
            {
                SelectionService.Instance.SelectedUnit.miningComponent.OnTurn();
            }
        }
        */

        // AutoRun all routes — wait briefly for RouteExecutor to initialize, then call it or skip with a warning.
        int waitFrames = 0;
        const int maxWaitFrames = 30; // ~0.5s at 60fps, tune as needed
        while (RouteExecutor.Instance == null && waitFrames < maxWaitFrames)
        {
            yield return null;
            waitFrames++;
        }

        if (RouteExecutor.Instance != null)
        {
            yield return StartCoroutine(RouteExecutor.Instance.ExecuteAllPlayerRoutes(playerUnits));
        }
        else
        {
            Debug.LogWarning("RouteExecutor.Instance is null in StartPlayerTurn — skipping auto-run routes. Ensure a RouteExecutor exists in the scene and initializes early.");
        }

        //UIManager.Instance.UpdateRouteButton(playerUnits[currentTurnIndex]);

        /*
        //Set the route button (guard indices)
        if (playerUnits.Count > 0 && currentTurnIndex >= 0 && currentTurnIndex < playerUnits.Count)
            UIManager.Instance.UpdateRouteButton(playerUnits[currentTurnIndex]);
        else
            UIManager.Instance.UpdateRouteButton(null);
        */
        /*
        //AutoRun all routes
        yield return StartCoroutine(RouteExecutor.Instance.ExecuteAllPlayerRoutes(playerUnits));
        //yield return StartCoroutine(RouteManager.Instance.ExecuteAllRoutes());  

        //Set the route button.
        UIManager.Instance.UpdateRouteButton(playerUnits[currentTurnIndex]);
        */
    }

    public void SortPlayerAndEnemyList()
    {
        //Clear lists
        playerUnits.Clear();
        enemyUnits.Clear();

        //populate unit lists
        for (int i = 0; i < unitsList.Count; i++)
        {
            if (unitsList[i].isPlayerControlled)
            {
                playerUnits.Add(GameManager.Instance.unitsList[i]);
            }
            else
            {
                enemyUnits.Add(GameManager.Instance.unitsList[i]);
            }
        }
    }

    //When UI button pressed.
    public void EndOfTurneBtPressed()
    {
        //Call Event to tell miningoutpost to attack ships in orbit.
        OnEnemyTurn?.Invoke();

        //Start next turne
        playerTurn = false;

        //Clear previous path
        HexPathClickControllerPointTop_LineStrip.Instance.ClearPath();
        HexHighlighter.Instance.HideHighlight();

        //Reset Selected
        SelectionService.Instance.ClearSelection();

        //HideUI
        UIManager.Instance.HideAllUI();

        //Set Enemy input state
        GameStateMachine.Instance.SetState(GameplayStateId.EnemyTurn);

        //Reset all player units movedThisTurn. And hasmined
        for (int i = 0; i < playerUnits.Count; i++)
        {
            playerUnits[i].movedThisTurn = 0;
            playerUnits[i].haveAttackedThisTurn = false;
            playerUnits[i].hasMinedThisTurn = false;
        }

        string mainMessage =
            "ENEMY TURN";
        string subMessage = "";
        MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
        this.transform.position, Color.white);

        StartCoroutine(TakeTurn());
    }

    //Called when player click "End Turne" button. Make shure that enemy execute there turne in order and not all att once.
    public IEnumerator TakeTurn()
    {
        remainingEnemies = enemyUnits.Count;

        // Make a safe copy so modifications don’t break the loop. the EnenyUnits list can be modified during the loop if an enemy dies and is removed from the list, which would cause issues if we were iterating directly over it.
        var enemiesThisTurn = new List<Unit>(enemyUnits);


        foreach (var enemy in enemiesThisTurn)
        {
            if (enemy == null) continue;
            Debug.Log("Enemy turn for: " + enemy.unitName);
            yield return StartCoroutine(enemy.DoEnemyTurn(OnEnemyFinished));
        }

        //Reset all enemy movedThisTurn
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            enemyUnits[i].movedThisTurn = 0;
        }
        Debug.Log("All enemy have made there move/attack for the turne!");

        //Start Player turn
        StartCoroutine(StartPlayerTurn());
    }

    //Called from AttacState. This is a workaound becuse TryToAttack Can not be called directly from attackstage when attachstage do not inheret from monobehavior.
    public void PlayerTryToAttack(Unit player, GameObject target)
    {
        StartCoroutine(AttackManager.Instance.TryToAttack(player, target));
    }

    //Called from Ienumerator enemy.DoEnemyTurn(unit script) when enemy have finished there move/attack for the turne.
    private void OnEnemyFinished()
    {
        remainingEnemies--;

        if (remainingEnemies <= 0)
        {
            EndEnemyTurn();
        }
    }

    //Called when ALL enemy have made there move/attack for the turne.
    private void EndEnemyTurn()
    {
        Debug.Log("Enemy turn finished!");
        // Switch back to player turn

        //Set Enemy input state
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Clear previous path
        HexPathClickControllerPointTop_LineStrip.Instance.ClearPath();
        HexHighlighter.Instance.HideHighlight();

        //Reset all enemy movment this turn and attack
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            enemyUnits[i].movedThisTurn = 0;
            enemyUnits[i].haveAttackedThisTurn = false;
        }

        //Reset Selected
        SelectionService.Instance.ClearSelection();

        //HideUI
        UIManager.Instance.ResetUI();

        //New Turn
        turnNumber++;
        turnNumberText.text = $"Turn nr: {turnNumber}";

        //Start player turne
        StartCoroutine(StartPlayerTurn());
    }

    public List<Unit> GetPlayerUnitsInOrbit(Vector3 OrbitPos)
    {
        List<Unit> unitsInOrbitList = new List<Unit>();

        // Get all player ship in orbit.
        foreach (var playerUnit in playerUnits)
        {
            //Check if player unit is in asteroid orbit
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);
            Vector2Int OrbitPosHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(OrbitPos);
            if (playerUnitCurrentHexPos == OrbitPosHexPos)
            {
                unitsInOrbitList.Add(playerUnit);
            }
        }
        
        return unitsInOrbitList;
    }

    //Used to get how meny missiles, soldiers and engineers in orbit around asteroid.
    public int GetAmountOfItemsInOrbit(GameObject objectTocheckAgainst, ItemDefinition itemToGet)
    {
        //Get objectPos hexpos
        Vector2Int objectCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(objectTocheckAgainst.transform.position);

        //Reset nr of missiles in orbit
        int nrOfItemsInOrbit = 0;

        //Get all playerUnit pos
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            Vector2Int playerUnitCurrentHexPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(playerUnit.transform.position);

            if (playerUnitCurrentHexPos == objectCurrentHexPos)
            {
                if (playerUnit.unitInventory.HasItem(itemToGet, 1))
                {
                    nrOfItemsInOrbit = nrOfItemsInOrbit + playerUnit.unitInventory.GetItemCount(itemToGet);
                }
            }
        }
        return nrOfItemsInOrbit;
    }
}