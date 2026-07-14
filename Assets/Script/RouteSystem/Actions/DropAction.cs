using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

[Serializable]
public class DropAction : RouteAction
{
    public Inventory dropOfInventory;
    public ItemDefinition itemDefinition;
    public int itemQuantity;
    public bool allAvalibuleItems;
    public bool waitForAvalibuleItems;

    // For drop we assume owner.unitInventory is the source and owner.orbitingAround (if any) has an Inventory target
    public override IEnumerator Perform(Unit owner, Action<ActionResult> callback)
    {
        if (owner == null || dropOfInventory == null || itemDefinition == null)
        {
            Debug.Log($"DropAction: Invalid parameters. Owner: {owner}, DropInventory: {dropOfInventory}, ItemDefinition: {itemDefinition}");
            callback(ActionResult.Failed);
            yield break;
        }

        // Ensure same hex as owner
        Vector2 ownerHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(owner.transform.position);
        Vector2 inventoryHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(dropOfInventory.transform.position);
        if (ownerHex != inventoryHex)
        {
            Debug.Log($"DropAction: Owner {owner.unitName} is not on the same hex as the drop inventory. Owner hex: {ownerHex}, Inventory hex: {inventoryHex}");
            callback(ActionResult.Failed);
            yield break;
        }

        int available = owner.unitInventory.GetItemCount(itemDefinition);
        if (available <= 0)
        {
            Debug.Log($"DropAction: No available items of type {itemDefinition.name} in inventory {owner.unitInventory.name}. Available: {available}");
            callback(waitForAvalibuleItems ? ActionResult.Retry : ActionResult.Failed);
            yield break;
        }

        int toTake = allAvalibuleItems ? available : Mathf.Min(itemQuantity, available);
        if (toTake <= 0)
        {
            Debug.Log($"PickupAction: No items to take. Requested: {itemQuantity}, Available: {available}, All available flag: {allAvalibuleItems}");
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
            yield break;
        }

        // Transfer via InventoryService for clarity
        Debug.Log($"owner {owner.unitName} unitInventory{owner.unitInventory} dropOfInventory {dropOfInventory} item {itemDefinition.name} toDrop{toTake}");

        int moved = 0;
        var slot = owner.unitInventory.FindFirstInstance(itemDefinition);
        if (slot == null)
        {
            Debug.Log($"PickupAction: No item instance of type {itemDefinition.name} found in inventory {dropOfInventory.name}. Cannot pick up.");
            // nothing to pick up
        }
        else
        {
            // slot.item is the ItemInstance to clone/copy from; slot.amount shows available quantity
            moved = Inventory.TransferItemInstance(owner.unitInventory, dropOfInventory, slot.item, toTake);
        }

        if (moved > 0)
        {
            Debug.Log($"{owner.unitName} dropped {moved} of item {itemDefinition.name} to inventory {dropOfInventory.name}");
            callback(ActionResult.Completed);
            MessageSystemManager.Instance.CreateMessage($"{owner.unitName} picked up {moved} {itemDefinition.name}", "", owner.transform.position, Color.green);
        }
        else
        {
            Debug.Log($"{owner.unitName} Failed to pick up item {itemDefinition.name} from inventory {dropOfInventory.name}. Retry next turn");
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
        }
        yield break;
    }
}