
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.LightTransport;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class MiningComponent : MonoBehaviour
{

    // 2️⃣ Current state (DECLARED HERE)
    [SerializeField] private MinerState currentState = MinerState.Idle;

    //Use this to see if Miner are on a auto mission or not
    public bool HasMission => currentMission != null;
    public bool IsIdle => currentState == MinerState.Idle;


    // 3️⃣ Current mission (optional but recommended)
    private MiningMission currentMission;

    public ItemDefinition itemToMine;

    // References
    [SerializeField] private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    public void StartMiningMission(AsteroidFieldComponent asteroidFieldComponent)
    {
        currentMission = new MiningMission { targetAsteroidField = asteroidFieldComponent };
        currentState = MinerState.MovingToAsteroid;
    }

    public void CancelMission()
    {
        currentMission = null;
        currentState = MinerState.Idle;
    }



    // Turn-based entry point
    public void OnTurn()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case MinerState.Idle:
                // Do nothing / wait for orders
                break;

            case MinerState.MovingToAsteroid:
                HandleMoveToAsteroid();
                break;

            case MinerState.Mining:
                HandleMining();
                break;

            case MinerState.MovingToMothership:
                HandleReturnToMothership();
                break;
        }
    }

    void HandleMoveToAsteroid()
    {
        Vector2Int minerCurrentPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(this.transform.position);
        Vector2Int asteroidFieldCurrentPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(currentMission.targetAsteroidField.transform.position);

        Debug.Log($"Miner: {this.gameObject.name} HandelMoveToAsteroid called at pos {minerCurrentPos} asteroid pos {asteroidFieldCurrentPos}");

        if (minerCurrentPos == asteroidFieldCurrentPos)
        {
            //We are there
            currentState = MinerState.Mining;
        }
        else if(SelectionService.Instance.SelectedUnit.movedThisTurn < SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange)
        {
            //Can move

            //Set path
            HexGridManager.Instance.CalculatePath(currentMission.targetAsteroidField.transform.position);

            //Lock player input when unit moves
            GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);

            //Move Player
            Debug.Log("ExecuteMoveButtonPressed: " + SelectionService.Instance.SelectedUnit.gameObject.name);
            //StartCoroutine(MovementManager.Instance.MoveUnitAlongPath(SelectionService.Instance.SelectedUnit, HexPathClickControllerPointTop_LineStrip.Instance.LastPath));
        }
        else
        {
            //No more moves left this turn.
        }
    }

    void HandleMining()
    {
        Debug.Log($"Miner: {this.gameObject.name} HandleMining called");
        if(SelectionService.Instance.SelectedUnit.hasMinedThisTurn) { return; }

        if (currentMission.targetAsteroidField.totalAmount <= 0) 
        {
            //no ore left
            string mainMessage =
            SelectionService.Instance.SelectedUnit.unitName + " try to min. Asteroid field depleted";
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, SelectionService.Instance.SelectedUnit.transform.position, Color.yellow);
            currentState = MinerState.Idle;
            return;
        }

        if (SelectionService.Instance.SelectedUnit.unitInventory.itemInstance.Count >=
            SelectionService.Instance.SelectedUnit.unitInventory.maxItems)
        {
            //no storageSpaceLeft
            foreach (var item in SelectionService.Instance.SelectedUnit.unitInventory.itemInstance)
            {
                if(item.item.ItemDefinition.itemType == ItemType.Rocks)
                {
                    //Full inventory have rocks. Return to mothership.
                    currentState = MinerState.MovingToMothership;
                    return;
                }
                else
                {
                    //No free space in inventory to mine.
                    string mainMessage =
                    SelectionService.Instance.SelectedUnit.unitName + " is trying to Mine asteroid field but no free space in inventory";
                    string subMessage = "";
                    MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, SelectionService.Instance.SelectedUnit.transform.position, Color.yellow);
                    currentState = MinerState.Idle;
                    return;
                }
            }
        }
        else
        {
            //Mine
            //Get mined amount per turn.
            int totalMinePerTurn = 0;
            foreach (var module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
            {
                if (module.ItemDefinition != null)
                {
                    if (module.ItemDefinition.itemType == ItemType.Module && module.ItemDefinition.moduleType == ModuleType.MiningLaser)
                    {
                        totalMinePerTurn += module.currentMiningSpeed;
                    }
                }
            }

            //Remove minde item
            currentMission.targetAsteroidField.totalAmount = currentMission.targetAsteroidField.totalAmount - totalMinePerTurn;

            //Add to inventory
            ItemInstance minedItem = new ItemInstance();
            minedItem.ItemDefinition = itemToMine;
            SelectionService.Instance.SelectedUnit.unitInventory.AddItem(minedItem, totalMinePerTurn);

            //To prevent more mining this turn
            SelectionService.Instance.SelectedUnit.hasMinedThisTurn = true;

            //Spawn mining floating text above unit
            FloatingTextPoolManager.Instance.Spawn($"Mined +{totalMinePerTurn}", SelectionService.Instance.SelectedUnit.transform.position, Color.red, 9);
        }
    }


    void HandleReturnToMothership()
    {
        Debug.Log($"Miner: {this.gameObject.name} HandleReturnToMothership called");

        Vector2Int minerCurrentPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(this.transform.position);
        Vector2Int motherShipCurrentPos = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(GameManager.Instance.MotherShip.transform.position);

        Debug.Log($"Miner: {this.gameObject.name} moves to motehrship minerCurrentPos {minerCurrentPos} motherShipCurrentPos {motherShipCurrentPos}");

        if (minerCurrentPos == motherShipCurrentPos)
        {
            //We are there
            //Check mothership inventory
            if(GameManager.Instance.motherShip_Unit_Script.unitInventory.itemInstance.Count >= GameManager.Instance.motherShip_Unit_Script.unitInventory.maxItems)
            {
                string mainMessage =
                SelectionService.Instance.SelectedUnit.unitName + " is trying to UNLOAD to MotherSHip but no free space in inventory";
                string subMessage = "";
                MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, SelectionService.Instance.SelectedUnit.transform.position, Color.yellow);
                currentState = MinerState.Idle;
                return;
            }

            //Remove from miner
            int amountOfRocksToUnload = SelectionService.Instance.SelectedUnit.unitInventory.GetItemCount(itemToMine);
            ItemInstance itemToremove = new ItemInstance();
            itemToremove.ItemDefinition = itemToMine;
            SelectionService.Instance.SelectedUnit.unitInventory.RemoveItem(itemToremove, amountOfRocksToUnload);

            //Add rocks
            GameManager.Instance.motherShip_Unit_Script.unitInventory.AddItem(itemToremove, amountOfRocksToUnload);

            //Restart Loop
            currentState = MinerState.MovingToAsteroid;
        }
        else if (SelectionService.Instance.SelectedUnit.movedThisTurn < SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange)
        {
            //Can move

            //Set path
            HexGridManager.Instance.CalculatePath(GameManager.Instance.MotherShip.transform.position);

            //Lock player input when unit moves
            GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);

            //Move Player
            Debug.Log("ExecuteMoveButtonPressed: " + SelectionService.Instance.SelectedUnit.gameObject.name);
            //StartCoroutine(MovementManager.Instance.MoveUnitAlongPath(SelectionService.Instance.SelectedUnit, HexPathClickControllerPointTop_LineStrip.Instance.LastPath));
        }
        else
        {
            //No more moves left this turn.
        }
        /*
        MoveToward(mothership.position);

        if (IsAt(mothership))
        {
            inventory.Unload();
            currentState = MinerState.MovingToAsteroid;
        }
        */
    }

    // 1️⃣ State definition
    enum MinerState
    {
        Idle,
        MovingToAsteroid,
        Mining,
        MovingToMothership
    }
}


class MiningMission
{
    public AsteroidFieldComponent targetAsteroidField;
}

