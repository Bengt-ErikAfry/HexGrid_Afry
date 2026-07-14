using System.Linq;
using System.Reflection;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    public static ModuleManager Instance { get; private set; }
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

    //This is made so that it can be run bothe at game startup for all ships and when player build a new ship in a hanger.
    public void SetUpShipStats(Unit playerUnit)
    {
        //Populate all player units next turn variabels
        if (playerUnit.shipRuntimeData.itemDefinition != null)
        {
            //Power bost
            foreach (var module in playerUnit.moduleRuntimeList)
            {
                if (module.ItemDefinition != null)  //Check if module slot is empty.
                {
                    if (module.ItemDefinition.moduleType == ModuleType.Reactor)
                    {
                        playerUnit.shipRuntimeData.currentAvalibulePowerUpBoost += module.ItemDefinition.energyStartPowerBoost;
                        playerUnit.shipRuntimeData.currentEnergyProduction = module.ItemDefinition.energyProduction;
                        playerUnit.shipRuntimeData.currentEnergyMaxStorage = module.ItemDefinition.energyMaxStorage;
                    }

                    if (module.ItemDefinition.moduleType == ModuleType.Engine)
                    {
                        playerUnit.shipRuntimeData.currentMovmentRange += module.ItemDefinition.movmentRange;
                    }
                }
            }

            playerUnit.shipRuntimeData.nextTurnAvalibulePowerUpBoost = playerUnit.shipRuntimeData.currentAvalibulePowerUpBoost;
            playerUnit.shipRuntimeData.nextTurnMovmentRange = playerUnit.shipRuntimeData.currentMovmentRange;
            playerUnit.shipRuntimeData.nextTurnEnergyProduction = playerUnit.shipRuntimeData.currentEnergyProduction;
            playerUnit.shipRuntimeData.nextTurnEnergyMaxStorage = playerUnit.shipRuntimeData.currentEnergyMaxStorage;

            playerUnit.shipRuntimeData.energyConsumptionThisTurn = 0;
            playerUnit.shipRuntimeData.energyStorage = playerUnit.shipRuntimeData.currentEnergyMaxStorage;

            foreach (var module in playerUnit.moduleRuntimeList)
            {
                if (module.ItemDefinition != null)  //If module slot is empty.
                {
                    //Module Runting
                    module.currentPowerUpBoost = 1; //Always start with 1, becuse the module is online and can be used.
                    module.currentHealth = module.ItemDefinition.maxHealth;
                    module.currentMaxHealth = module.ItemDefinition.maxHealth;
                    module.currentDamage = module.ItemDefinition.damage;
                    module.currentRange = module.ItemDefinition.range;
                    module.currentEnergyCostValue = module.ItemDefinition.energyCost;
                    module.currentWear = module.ItemDefinition.wear;
                    module.currentEnergyStartPowerBoost = module.ItemDefinition.energyStartPowerBoost;
                    module.currentEnergyProduction = module.ItemDefinition.energyProduction;
                    module.currentEnergyMaxStorage = module.ItemDefinition.energyMaxStorage;
                    module.currentStorageSpace = module.ItemDefinition.storageSpace;
                    module.currentMiningSpeed = module.ItemDefinition.miningSpeed;

                    module.nextTurnPowerUpboost = 1;
                    module.nextTurnHealth = module.ItemDefinition.maxHealth;
                    module.nextTurnMaxHealth = module.ItemDefinition.maxHealth;
                    module.nextTurnDamage = module.ItemDefinition.damage;
                    module.nextTurnRange = module.ItemDefinition.range;
                    module.nextTurnEnergyCostValue = module.ItemDefinition.energyCost;
                    module.nextTurnWear = module.ItemDefinition.wear;
                    module.nextTurnEnergyStartPowerBoost = module.ItemDefinition.energyStartPowerBoost;
                    module.nextTurnEnergyProduction = module.ItemDefinition.energyProduction;
                    module.nextTurnEnergyMaxStorage = module.ItemDefinition.energyMaxStorage;
                    module.nextTurnStorageSpace = module.ItemDefinition.storageSpace;
                    module.nextTurnMiningSpeed = module.ItemDefinition.miningSpeed;

                    module.isOnline = true;
                    module.isBroken = false;
                    module.isDestroyd = false;
                }
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"ItemDefinition missing for ShipRuntimeData for Unit {playerUnit.unitName}");
        }
    }

    public void SetUpEnemyShipModules()
    { 
        foreach (var EnemyUnit in GameManager.Instance.enemyUnits)
        {


            if (EnemyUnit.shipRuntimeData.itemDefinition != null)
            {
                //Power bost
                foreach (var module in EnemyUnit.moduleRuntimeList)
                {
                    //Check if slot is empty
                    if (module.ItemDefinition != null)  
                    {
                        if (module.ItemDefinition.moduleType == ModuleType.Engine)
                        {
                            EnemyUnit.shipRuntimeData.currentMovmentRange += module.ItemDefinition.movmentRange;
                        }

                        //Module Runting
                        module.currentPowerUpBoost = 1; //Always start with 1, becuse the module is online and can be used.
                        module.currentHealth = module.ItemDefinition.maxHealth;
                        module.currentMaxHealth = module.ItemDefinition.maxHealth;
                        module.currentDamage = module.ItemDefinition.damage;
                        module.currentRange = module.ItemDefinition.range;
                        module.currentStorageSpace = module.ItemDefinition.storageSpace;

                        module.nextTurnPowerUpboost = 1;
                        module.nextTurnHealth = module.ItemDefinition.maxHealth;
                        module.nextTurnMaxHealth = module.ItemDefinition.maxHealth;
                        module.nextTurnDamage = module.ItemDefinition.damage;
                        module.nextTurnRange = module.ItemDefinition.range;
                        module.nextTurnStorageSpace = module.ItemDefinition.storageSpace;
                        module.nextTurnMiningSpeed = module.ItemDefinition.miningSpeed;

                        module.isOnline = true;
                        module.isBroken = false;
                        module.isDestroyd = false;
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"ItemDefinition missing for ShipRuntimeData for Unit {EnemyUnit.unitName}");
            }
        }
    }

    public void ApplyModuleChangesForTurn()
    {
        // Apply all module changes made this turn.
        for (int u = 0; u<GameManager.Instance.playerUnits.Count; u++)
        {
            //Ship Stats
            GameManager.Instance.playerUnits[u].shipRuntimeData.currentAvalibulePowerUpBoost = GameManager.Instance.playerUnits[u].shipRuntimeData.nextTurnAvalibulePowerUpBoost;
            GameManager.Instance.playerUnits[u].shipRuntimeData.currentMovmentRange = GameManager.Instance.playerUnits[u].shipRuntimeData.nextTurnMovmentRange;
            GameManager.Instance.playerUnits[u].shipRuntimeData.currentEnergyProduction = GameManager.Instance.playerUnits[u].shipRuntimeData.nextTurnEnergyProduction;
            GameManager.Instance.playerUnits[u].shipRuntimeData.currentEnergyMaxStorage = GameManager.Instance.playerUnits[u].shipRuntimeData.nextTurnEnergyMaxStorage;

            foreach (var module in GameManager.Instance.playerUnits[u].moduleRuntimeList)
            {
                //Module stats.
                module.currentPowerUpBoost = module.nextTurnPowerUpboost;
                module.currentHealth = module.nextTurnHealth;
                module.currentMaxHealth = module.nextTurnMaxHealth;
                module.currentDamage = module.nextTurnDamage;
                module.currentRange = module.nextTurnRange;
                module.currentEnergyCostValue = module.nextTurnEnergyCostValue;
                module.currentWear = module.nextTurnWear;
                module.currentEnergyProduction = module.nextTurnEnergyProduction;
                module.currentEnergyMaxStorage = module.nextTurnEnergyMaxStorage;
                module.currentStorageSpace = module.nextTurnStorageSpace;
                module.currentMiningSpeed = module.nextTurnMiningSpeed;
                module.currentSolidersGarding = module.nextTurnSolidersGarding;
            }

        }
    }

    public bool CheckIfUnitHaveModule(Unit unit, ModuleType moduleTypeToCheck)
    {
        foreach (var module in unit.moduleRuntimeList)
        {
            if (module.ItemDefinition.moduleType == moduleTypeToCheck)
            {
                return true;
            }
        }
        return false;
    }
}
