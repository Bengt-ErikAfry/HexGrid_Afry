using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class PickupAction : RouteAction
{
    public Inventory pickupInventory;
    public ItemDefinition itemDefinition;
    public int itemQuantity;
    public bool allAvalibuleItems;

    public override IEnumerator Perform(Unit owner, Action<ActionResult> callback)
    {
        if (owner == null || pickupInventory == null || itemDefinition == null)
        {
            Debug.Log($"PickupAction: Invalid parameters. Owner: {owner}, PickupInventory: {pickupInventory}, ItemDefinition: {itemDefinition}");
            callback(ActionResult.Failed);
            yield break;
        }

        // Ensure same hex as owner
        Vector2 ownerHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(owner.transform.position);
        Vector2 inventoryHex = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(pickupInventory.transform.position);
        if (ownerHex != inventoryHex)
        {
            Debug.Log($"PickupAction: Owner {owner.unitName} is not on the same hex as the pickup inventory. Owner hex: {ownerHex}, Inventory hex: {inventoryHex}");
            callback(ActionResult.Failed);
            yield break;
        }

        int available = pickupInventory.GetItemCount(itemDefinition);
        if (available <= 0)
        {
            Debug.Log($"PickupAction: No available items of type {itemDefinition.name} in inventory {pickupInventory.name}. Available: {available}");
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
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
        int moved = 0;
        var slot = pickupInventory.FindFirstInstance(itemDefinition);
        if (slot == null)
        {
            Debug.Log($"PickupAction: No item instance found for {itemDefinition.name} in inventory {pickupInventory.name}. This should not happen since available count was > 0.");
            // nothing to pick up
        }
        else
        {
            // slot.item is the ItemInstance to clone/copy from; slot.amount shows available quantity
            moved = Inventory.TransferItemInstance(pickupInventory, owner.unitInventory, slot.item, toTake);
        }

        if (moved > 0)
        {
            callback(ActionResult.Completed);
            MessageSystemManager.Instance.CreateMessage($"{owner.unitName} picked up {moved} {itemDefinition.name}", "", owner.transform.position, Color.green);
        }
        else
        {
            Debug.Log($"{owner.unitName} Failed to pick up item {itemDefinition.name} from inventory {pickupInventory.name}. Retry next turn");
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
        }
        yield break;
    }
}