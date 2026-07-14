using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class MineAction : RouteAction
{
    // Target asteroid to mine from and item to produce
    public AsteroidFieldComponent targetAsteroidField;
    public ItemDefinition itemToMine;

    // If true the action will wait (Retry) when it cannot complete (e.g. not on same hex or no storage).
    // If false the action will fail immediately in those cases.
    public bool waitIfCannotComplete = true;

    public override IEnumerator Perform(Unit owner, Action<ActionResult> callback)
    {
        if (owner == null || targetAsteroidField == null || itemToMine == null)
        {
            callback(ActionResult.Failed);
            yield break;
        }

        // Ensure owner is on the same hex as the asteroid field
        var ownerHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(owner.transform.position);
        var asteroidHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(targetAsteroidField.transform.position);
        if (ownerHex != asteroidHex)
        {
            // Not at the asteroid yet — retry later (or fail based on flag)
            callback(waitIfCannotComplete ? ActionResult.Retry : ActionResult.Failed);
            yield break;
        }

        // If asteroid is depleted, treat as completed (nothing to mine)
        if (targetAsteroidField.totalAmount <= 0)
        {
            MessageSystemManager.Instance.CreateMessage($"{owner.unitName} found {itemToMine.itemName} depleted.", "", owner.transform.position, Color.yellow);
            callback(ActionResult.Completed);
            yield break;
        }

        // Compute mining capability from owner's modules (sum currentMiningSpeed of online mining lasers)
        int totalMinePerTurn = 0;
        foreach (var module in owner.moduleRuntimeList)
        {
            if (module?.ItemDefinition == null) continue;
            // ModuleType enum and module types assumed from existing codebase
            if (module.ItemDefinition.itemType == ItemType.Module && module.ItemDefinition.moduleType == ModuleType.MiningLaser && module.isOnline)
            {
                totalMinePerTurn += module.currentMiningSpeed;
            }
        }

        if (totalMinePerTurn <= 0)
        {
            // No mining capability — fail the action
            MessageSystemManager.Instance.CreateMessage($"{owner.unitName} has no active mining lasers.", "", owner.transform.position, Color.yellow);
            callback(ActionResult.Failed);
            yield break;
        }

        // Determine how much can actually be mined (respect asteroid remaining)
        int desired = Mathf.Min(totalMinePerTurn, targetAsteroidField.totalAmount);
        if (desired <= 0)
        {
            callback(ActionResult.Completed);
            yield break;
        }

        // Try to add to owner's inventory
        ItemInstance minedPrototype = new ItemInstance { ItemDefinition = itemToMine };

        // Prefer TryAddItem (will respect stacking/non-stacking and free slots)
        bool canAdd = owner.unitInventory.TryAddItem(minedPrototype, desired);

        if (!canAdd)
        {
            // If cannot add any, either wait (Retry) or fail
            MessageSystemManager.Instance.CreateMessage($"{owner.unitName} has no space to store mined {itemToMine.itemName}.", "", owner.transform.position, Color.yellow);
            callback(waitIfCannotComplete ? ActionResult.Retry : ActionResult.Failed);
            yield break;
        }

        // If TryAddItem succeeded it either merged into an existing stack or created new slot(s).
        // Deduct mined amount from asteroid and mark unit as having mined.
        targetAsteroidField.totalAmount -= desired;
        owner.hasMinedThisTurn = true;

        MessageSystemManager.Instance.CreateMessage($"{owner.unitName} mined {desired} {itemToMine.itemName}.", "", owner.transform.position, Color.green);

        callback(ActionResult.Completed);
        yield break;
    }
}