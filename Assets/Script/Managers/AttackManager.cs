using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
using static UnityEngine.GraphicsBuffer;

public class AttackManager : MonoBehaviour
{
    public static AttackManager Instance { get; private set; }
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

    public IEnumerator TryToAttack(Unit attackingUnit, GameObject target)
    {
        //Check if player have alredy attacked this turn
        if (SelectionService.Instance.SelectedUnit.haveAttackedThisTurn)
        {
            string mainMessage =
            "ATTACK Alredy attacked" + attackingUnit.unitName;
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
            attackingUnit.transform.position, Color.gray);

            //Reset state to selecting
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            yield break;
        }

        // Check if there are weapons available
        if (!attackingUnit.moduleRuntimeList.Any(h => h.ItemDefinition != null && h.ItemDefinition.moduleType == ModuleType.Weapon))
        {
            string mainMessage =
            "ATTACK Ship do not have any weapons" + attackingUnit.unitName;
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
            attackingUnit.transform.position, Color.gray);

            //Reset state to selecting
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            yield break;
        }

        // Get total energy cost and longset range of all weapons
        float totalEnergyCost = 0f;
        float longestRange = 0f;
        foreach (var module in attackingUnit.moduleRuntimeList)
        {
            if (module.ItemDefinition != null && module.ItemDefinition.moduleType == ModuleType.Weapon)
            {
                totalEnergyCost += module.currentEnergyCostValue;
                if (module.currentRange > longestRange) { longestRange = module.currentRange; }
            }
        }

        // See if you have enufe energy to fire all weapons
        if (attackingUnit.shipRuntimeData.energyStorage < totalEnergyCost) 
        {
            Debug.Log("Not enufe energy to attack. Current energy: " + attackingUnit.shipRuntimeData.energyStorage + " Energi cost: " + totalEnergyCost);

            string mainMessage =
            "ATTACK No energy to attack" + attackingUnit.unitName + "Current energy: " + attackingUnit.shipRuntimeData.energyStorage +
            " Energi cost: " + totalEnergyCost;
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
            attackingUnit.transform.position, Color.gray);

            //Reset state to selecting
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            yield break;
        }

        // check if target is in range for all weapons
        int distance = HexGridLinesBaker.Instance.AxialDistance(target.transform.position, attackingUnit.transform.position);

        if (longestRange < distance)
        {
            string mainMessage =
            "ATTACK Enemy to far away" + attackingUnit.unitName + "Current Range: " + attackingUnit.shipRuntimeData.currentMovmentRange +
            " distance: " + distance;
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
            attackingUnit.transform.position, Color.gray);

            //Reset state to selecting
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

            yield break;
        }

        //Attack with all weapons that are in range
        foreach (var module in attackingUnit.moduleRuntimeList)
        {
            if (module.ItemDefinition != null)
            {
                if (module.ItemDefinition.itemType == ItemType.Module && module.ItemDefinition.moduleType == ModuleType.Weapon)
                {
                    // Fire the weapon

                    // Consume energy
                    attackingUnit.shipRuntimeData.currentEnergyMaxStorage -= module.currentEnergyCostValue;

                    //Show shoting animation
                    attackingUnit.laserBeam_Script.Fire(attackingUnit.transform.position, target.transform.position);

                    //Get result
                    var resultChanseTohitTarget = ChanseToHitTarget(attackingUnit,
                                                                    module,
                                                                    SelectionService.Instance.SelectedUnit.isAmingForModule,
                                                                    SelectionService.Instance.SelectedUnit.moduleToHit);

                    //ROLE
                    float roll = (int)UnityEngine.Random.Range(0f, 100f);

                    if (roll <= resultChanseTohitTarget.chanseToHitCritical)
                    {
                        ///Sen message about critical hit
                        string mainMessage =
                            $"ATTACK:{attackingUnit.unitName} > {attackingUnit.target_Unit_Script.unitName} " +
                            $"roll {roll:F0}/{resultChanseTohitTarget.chanseToHitCritical:F0}% Critical Hit! {module.currentDamage} damage on {attackingUnit.moduleToHit.ItemDefinition.itemName}" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $" module: {attackingUnit.moduleToHit.ItemDefinition.itemName}" : "");
                        string subMessage =
                            $"Gunner Critical hit chance: {attackingUnit.gunnerCriticalHitChance:F0}%\n" +
                            $"Range modifier: -{resultChanseTohitTarget.rangeModifierCriticalInt}%\n" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $"Aiming For Module Penalty: -{resultChanseTohitTarget.amingForModulePenaltyCriticalInt}%" : "");

                        MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, target.transform.position, Color.purple);

                        // Apply critical damage to target here
                        attackingUnit.moduleToHit.TakeDamage(Mathf.RoundToInt(module.currentDamage * 2)); // Critical hit does double damage
                        attackingUnit.moduleToHit.isBroken = true;
                        attackingUnit.moduleToHit.isOnline = false; // Critical hit also takes module offline

                        //Spawn miss/hit info
                        float randomPos = UnityEngine.Random.Range(-0.2f, 0.2f);
                        Vector3 spanPos = new Vector3(target.transform.position.x + randomPos, target.transform.position.y + randomPos, target.transform.position.z);
                        FloatingTextPoolManager.Instance.Spawn("Crit -" + module.currentDamage * 2, spanPos, Color.purple, 8, 6.0f);
                    }
                    else if (roll <= resultChanseTohitTarget.chanseToHitTarget)
                    {
                        string mainMessage =
                            $"ATTACK:{attackingUnit.unitName} > {attackingUnit.target_Unit_Script.unitName} " +
                            $"roll {roll:F0}/{resultChanseTohitTarget.chanseToHitTarget:F0}% Hit! {module.currentDamage} damage on {attackingUnit.moduleToHit.ItemDefinition.itemName}" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $" module: {attackingUnit.moduleToHit.ItemDefinition.itemName}" : "");
                        string subMessage =
                            $"Gunner hit chance: {attackingUnit.gunnerHitChance:F0}%\n" +
                            $"Range modifier: -{resultChanseTohitTarget.rangeModifierInt}%\n" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $"Aiming For Module Penalty: -{resultChanseTohitTarget.amingForModulePeneltyInt}%" : "");

                        MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, target.transform.position, Color.red);

                        // Apply damage to target here
                        attackingUnit.moduleToHit.TakeDamage(Mathf.RoundToInt(module.currentDamage));

                        //Spawn miss hit info
                        float randomPos = UnityEngine.Random.Range(-0.2f, 0.2f);
                        Vector3 spanPos = new Vector3(target.transform.position.x + randomPos, target.transform.position.y + randomPos, target.transform.position.z);
                        FloatingTextPoolManager.Instance.Spawn("Hit -" + module.currentDamage, spanPos, Color.red, 6, 4.0f);
                    }
                    else
                    {
                        string mainMessage =
                            $"ATTACK:{attackingUnit.unitName} > {attackingUnit.target_Unit_Script.unitName} " +
                            $"roll {roll:F0}/{resultChanseTohitTarget.chanseToHitTarget:F0}% MISS! on" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $" module: {attackingUnit.moduleToHit.ItemDefinition.itemName}" : "");
                        string subMessage =
                            $"Gunner hit chance: {attackingUnit.gunnerHitChance:F0}%\n" +
                            $"Range modifier: -{resultChanseTohitTarget.rangeModifierInt}%\n" +
                            (SelectionService.Instance.SelectedUnit.isAmingForModule ? $"Aiming For Module Penalty: -{resultChanseTohitTarget.amingForModulePeneltyInt:F0}%" : "");

                        MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, target.transform.position, Color.red);

                        //Spawn miss hit info
                        float randomPos = UnityEngine.Random.Range(-0.2f, 0.2f);
                        Vector3 spanPos = new Vector3(target.transform.position.x + randomPos, target.transform.position.y + randomPos, target.transform.position.z);
                        FloatingTextPoolManager.Instance.Spawn("Miss", spanPos, Color.white, 3, 2.0f);
                    }


                    //Puase between shots
                    yield return new WaitForSeconds(1f);
                }
            }
        }
        //Player can only attack once per turn
        SelectionService.Instance.SelectedUnit.haveAttackedThisTurn = true;

        //Hide attack controlls
        UIManager.Instance.HideUnitAttackControlls();

        //Reset state to selecting
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
    }

    public (int chanseToHitTarget, int chanseToHitCritical, int amingForModulePeneltyInt, int amingForModulePenaltyCriticalInt, int rangeModifierInt
        , int rangeModifierCriticalInt) 
        ChanseToHitTarget(Unit attackingUnit, ItemInstance weponToFire, bool isAmingForModule, ItemInstance moduleAmingFor)
    {
        //RangeModifier 0-100% High is bad. 0% is point blank. 100% is target atmax weapon range.
        float rangeModifierRaw = CalculateRangeModifier(attackingUnit, weponToFire);

        //RangeModifierInt is how mutch of gunnersHitChanse. 0% is no penelty. 100% is 50 %Penalty
        int rangeModifierInt = Mathf.RoundToInt(attackingUnit.gunnerHitChance * (rangeModifierRaw / 200f));

        //RangeModifierInt is how mutch of gunnersHitCriticalChanse. 0% is no penelty. 100% is 50 %Penalty
        int rangeModifierCriticalInt = Mathf.RoundToInt(attackingUnit.gunnerCriticalHitChance * (rangeModifierRaw / 200f));

        //Check if aiming for module and get aming for module penelty if true.
        int chanseToHitTarget = 0;
        int chanseToHitCritical = 0;
        float amingForModulePenaltyRaw = 0;
        int amingForModulePenaltyInt = 0;
        int amingForModulePenaltyCriticalInt = 0;

        if (isAmingForModule)
        {
            //AmingModulePenelty 0-99% High is bad.0% is not aming for module or ship only have 1 module. 99% is aiming for small module that have 1% of ship size.
            amingForModulePenaltyRaw = ModuleSizeComparison(moduleAmingFor);

            amingForModulePenaltyInt = Mathf.RoundToInt(attackingUnit.gunnerHitChance * (amingForModulePenaltyRaw / 200f));

            amingForModulePenaltyCriticalInt = Mathf.RoundToInt(SelectionService.Instance.SelectedUnit.gunnerCriticalHitChance * (amingForModulePenaltyRaw / 200f));
        }

        //Hit Chance.
        chanseToHitTarget =
                                    attackingUnit.gunnerHitChance -
                                    rangeModifierInt -
                                    amingForModulePenaltyInt;
        chanseToHitCritical =
                                    attackingUnit.gunnerCriticalHitChance -
                                    rangeModifierCriticalInt -
                                    amingForModulePenaltyCriticalInt;

        return (chanseToHitTarget, chanseToHitCritical, amingForModulePenaltyInt, amingForModulePenaltyCriticalInt, rangeModifierInt, rangeModifierCriticalInt);
    }

    public float CalculateRangeModifier(Unit attackingUnit, ItemInstance weaponToFire)
    {
        //Get distance to target
        float distanceToTarget = HexGridLinesBaker.Instance.AxialDistance(attackingUnit.target_Unit_Script.transform.position, attackingUnit.transform.position);

        //Range modifier 0-100% High is bad.
        //Range modifier is based on how far target is in weapon range. If target is at max range, range modifier is 100%. If target is point blank, range modifier is 0%.
        float rangeModifier = (distanceToTarget / weaponToFire.currentRange) * 100;
        return rangeModifier;
    }

    public float ModuleSizeComparison(ItemInstance moduleToCompare)
    {
        //Calculate all module total size
        float totalModuleSize = 0;
        foreach (var modules in SelectionService.Instance.SelectedUnit.target_Unit_Script.moduleRuntimeList)
        {
            if (modules.ItemDefinition != null)
            {
                totalModuleSize += modules.ItemDefinition.size;
            }
        }

        //Cal how big module are of total
        float thismodulePartOfShipSize = -1;
        if (moduleToCompare.ItemDefinition != null)
        {
            thismodulePartOfShipSize = moduleToCompare.ItemDefinition.size / totalModuleSize; //0-1
        }

        return thismodulePartOfShipSize * 100;    //*100 to get procent.
    }

    public void BoardingAttack(Unit attacking_Unit_Script, Unit target_Unit_Script, int target_Module_Slot_Index)
    {
        // calculate losses for both sides based on their crew and boarding strength
        int start_attackingBoardingPartySize = ModuleViewManager.Instance.attackerBoardingPartySize;
        int start_defenderBoardingPartySize = target_Unit_Script.moduleRuntimeList[target_Module_Slot_Index].currentSolidersGarding;

        var battlecalculation = CalculateBattleLosses(start_attackingBoardingPartySize, start_defenderBoardingPartySize);

        //Apply losses to target.
        target_Unit_Script.moduleRuntimeList[target_Module_Slot_Index].currentSolidersGarding = battlecalculation.survivor_Defender;
        target_Unit_Script.moduleRuntimeList[target_Module_Slot_Index].nextTurnSolidersGarding = battlecalculation.survivor_Defender;

        //Apply losses to attacker.
        int totalLosses = start_attackingBoardingPartySize - battlecalculation.survivor_Attacker;

        for (int i = 0; i < attacking_Unit_Script.moduleRuntimeList.Count; i++)
        {
            var module = attacking_Unit_Script.moduleRuntimeList[i];

            if (module.ItemDefinition != null &&
                module.ItemDefinition.moduleType == ModuleType.BoardingDock)
            {
                int soldiers = module.currentSolidersGarding;

                if (soldiers >= totalLosses)
                {
                    // This module can absorb all remaining damage
                    module.currentSolidersGarding -= totalLosses;
                    module.nextTurnSolidersGarding -= totalLosses;
                    break;
                }
                else
                {
                    // This module is emptied, spill over
                    totalLosses -= soldiers;
                    module.currentSolidersGarding = 0;
                    module.nextTurnSolidersGarding = 0;
                }
            }
        }

        //Player can only attack once per turn
        SelectionService.Instance.SelectedUnit.haveAttackedThisTurn = true;

        //Hide attack controlls
        UIManager.Instance.HideUnitAttackControlls();

        //Reset state to selecting
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Show results in UI
        if (battlecalculation.survivor_Attacker == 0)
        {
            //Attacker lost all soldiers
            MessageBoxManager.Instance.ShowMessage($"You LOST all soldiers!!! \n Losses: {start_attackingBoardingPartySize - battlecalculation.survivor_Attacker}/{start_attackingBoardingPartySize} \n killed: {start_defenderBoardingPartySize - battlecalculation.survivor_Defender}/{start_defenderBoardingPartySize}");

            string mainMessage =
                $"ATTACK lost:{attacking_Unit_Script.unitName} > {attacking_Unit_Script.target_Unit_Script.unitName} " +
                $"Boarded {target_Unit_Script.moduleRuntimeList[target_Module_Slot_Index].ItemDefinition.itemName}";
            string subMessage =
                $"Losses: {start_attackingBoardingPartySize - battlecalculation.survivor_Attacker}/{start_attackingBoardingPartySize}\n" +
                $"Killed: {start_defenderBoardingPartySize - battlecalculation.survivor_Defender}/{start_defenderBoardingPartySize})";

            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, attacking_Unit_Script.transform.position, Color.red);
        }
        else if(battlecalculation.survivor_Defender == 0)
        {
            //Defender lost all soldiers
            MessageBoxManager.Instance.ShowMessage($"You WIN. \n Losses: {start_attackingBoardingPartySize - battlecalculation.survivor_Attacker}/{start_attackingBoardingPartySize} \n killed: {start_defenderBoardingPartySize - battlecalculation.survivor_Defender}/{start_defenderBoardingPartySize}");

            string mainMessage =
                $"ATTACK WIN:{attacking_Unit_Script.unitName} > {attacking_Unit_Script.target_Unit_Script.unitName} " +
                $"Boarded {target_Unit_Script.moduleRuntimeList[target_Module_Slot_Index].ItemDefinition.itemName}";
            string subMessage =
                $"Losses: {start_attackingBoardingPartySize - battlecalculation.survivor_Attacker}/{start_attackingBoardingPartySize}\n" +
                $"Killed: {start_defenderBoardingPartySize - battlecalculation.survivor_Defender}/{start_defenderBoardingPartySize})";

            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, attacking_Unit_Script.transform.position, Color.red);
        }
    }

    public (int survivor_Attacker, int survivor_Defender) CalculateBattleLosses(int amountAttacker, int amountDefender)
    {
        //Inventory playerInventory = GetInventoryFromShipInOrbitWithItem(solider_Item);
        //int nrPlayerSoldiers = playerInventory.GetItemCount(solider_Item);
        int survivor_Attacker = 0;
        int survivor_Defender = 0;

        if (amountAttacker == amountDefender)
        {
            //All Die exsept random one.
            int Result = UnityEngine.Random.Range(1, 2);
            if (Result == 1)
            {
                survivor_Attacker = 1;
                survivor_Defender = 0;
            }
            else
            {
                survivor_Attacker = 0;
                survivor_Defender = 1;
            }
        }
        else if (amountAttacker > amountDefender)
        {
            // use "Lanchester’s Square Law" to calculate nr of survival.
            survivor_Attacker = (int)Math.Floor(Math.Sqrt(amountAttacker * amountAttacker - amountDefender * amountDefender));
            survivor_Defender = 0;
        }
        else if (amountAttacker < amountDefender)
        {
            // use "Lanchester’s Square Law" to calculate nr of survival.
            survivor_Defender = (int)Math.Floor(Math.Sqrt(amountDefender * amountDefender - amountAttacker * amountAttacker));
            survivor_Attacker = 0;
        }

        return (survivor_Attacker, survivor_Defender);
    }
}
