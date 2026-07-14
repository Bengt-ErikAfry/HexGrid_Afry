using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

//[CreateAssetMenu(fileName = "NewInventory", menuName = "Inventory/Inventory")]
public class Inventory : MonoBehaviour
{
    public int maxItems;
    public bool singelItemsOnly = false;


    //public List<InventoryItemAmount> items = new List<InventoryItemAmount>();
    //public List<ItemInstance> instances = new List<ItemInstance>();
    public List<InventoryItemInstance> itemInstance = new List<InventoryItemInstance>();

    private void Awake()
    {
        UpdateInventoryItemStats();
    }
    public void UpdateInventoryItemStats()
    {
        foreach (var inventoryItem in itemInstance)
        {
            inventoryItem.item.currentHealth = inventoryItem.item.ItemDefinition.maxHealth;
            inventoryItem.item.currentMaxHealth = inventoryItem.item.ItemDefinition.maxHealth;
            inventoryItem.item.currentPowerUpBoost = inventoryItem.item.ItemDefinition.energyStartPowerBoost;
            inventoryItem.item.currentDamage = inventoryItem.item.ItemDefinition.damage;
            inventoryItem.item.currentRange = inventoryItem.item.ItemDefinition.range;
            inventoryItem.item.currentEnergyCostValue = inventoryItem.item.ItemDefinition.energyCost;
            inventoryItem.item.currentEnergyStartPowerBoost = inventoryItem.item.ItemDefinition.energyStartPowerBoost;
            inventoryItem.item.currentEnergyProduction = inventoryItem.item.ItemDefinition.energyProduction;
            inventoryItem.item.currentEnergyMaxStorage = inventoryItem.item.ItemDefinition.energyMaxStorage;
            inventoryItem.item.currentWear = inventoryItem.item.ItemDefinition.energyProduction;

            inventoryItem.item.isOnline = false;
            inventoryItem.item.isBroken = false;
            inventoryItem.item.isDestroyd = false;
        }
    }
    public bool HaveItemOfShipType(ShipType shipType)
    {
        foreach (var slot in itemInstance)
        {
            if (slot.item.ItemDefinition.itemType == ItemType.Ship && slot.item.shipType == shipType)
                return true;
        }
        return false;
    }
    public int GetItemCountOfShipType(ShipType shipType)
    {
        int count = 0;
        foreach (var slot in itemInstance)
        {
            if (slot.item.ItemDefinition.itemType == ItemType.Ship && slot.item.shipType == shipType)
                count += slot.amount;
        }
        return count;
    }
    public void RemoveItemOfShipType(ShipType shipType, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            InventoryItemInstance slot = itemInstance.Find(i => i.item.ItemDefinition.itemType == ItemType.Ship && i.item.shipType == shipType);
            if (slot != null)
            {
                slot.amount -= 1;
                if (slot.amount <= 0)
                    itemInstance.Remove(slot);
            }
        }
    }
    public int GetItemCount(ItemDefinition item)
    {
        int count = 0;
        foreach (var slot in itemInstance)
        {
            if (slot.item.ItemDefinition == item)
                count += slot.amount;
        }
        return count;
    }
    public bool HasItem(ItemDefinition item, int amount)
    {
        int currentAmount = 0;
        foreach (var slot in itemInstance)
        {
            //Debug.Log("Checking slot item: " + slot.item.itemName + " against " + item.itemName);
            if (slot.item.ItemDefinition == item)
                currentAmount += slot.amount;
        }
        return currentAmount >= amount;
    }
    public bool AddItemIfNotFull(ItemDefinition item, int amount, int maxCapacity)
    {
        int currentAmount = 0;
        foreach (var slot in itemInstance)
        {
            if (slot.item.ItemDefinition == item)
                currentAmount += slot.amount;
        }
        if (currentAmount + amount <= maxCapacity || maxCapacity==0)
        {
            Debug.Log("AddingIfNotFull item: " + item.itemName + " Amount: " + amount);
            ItemInstance newItemInstance = new ItemInstance();
            newItemInstance.ItemDefinition = item;
            AddItem(newItemInstance, amount);
            return true;
        }
        else
        {
            Debug.Log("Cannot add item: " + item.itemName + ". Exceeds max capacity. " + maxCapacity);
            return false;
        }
    }
    public static int TransferItemInstance(Inventory from, Inventory to, ItemInstance instance, int amount)
    {
        if (from == null || to == null || instance == null || amount <= 0) return 0;

        // Find a source slot that holds this instance (prefer reference match)
        InventoryItemInstance sourceSlot = from.itemInstance.Find(s => s.item == instance);
        if (sourceSlot == null)
        {
            // fallback: find any slot with same definition
            sourceSlot = from.itemInstance.Find(s => s.item.ItemDefinition == instance.ItemDefinition);
        }

        if (sourceSlot == null) return 0;

        int available = sourceSlot.amount;
        int wanted = Mathf.Min(amount, available);
        if (wanted <= 0) return 0;

        // To preserve runtime fields we clone the ItemInstance(s) and add them to destination.
        // This preserves per-instance runtime variables but consumes slots.
        // For simplicity we treat instance-transfer as requiring free slots (one slot per transferred unit)
        // — if you want stacking of identical runtime instances, adjust logic accordingly.
        int freeSlots = to.FreeSlots();
        int canMove = Mathf.Min(wanted, freeSlots);

        if (canMove <= 0)
        {
            // If the item is stackable and destination has an existing stack AND you accept losing instance-specific data,
            // you could instead merge into the existing stack. This implementation prefers preserving runtime data.
            return 0;
        }

        // Perform the transfer: add cloned instances to 'to', then remove from 'from'
        for (int i = 0; i < canMove; i++)
        {
            var clone = instance.Clone(); // preserves runtime fields
            to.itemInstance.Add(new InventoryItemInstance(clone, 1));
        }

        // Remove from source (by definition); this will decrement amounts and remove empty slots
        from.RemoveItem(instance, canMove);

        return canMove;
    }
    public InventoryItemInstance FindFirstInstance(ItemDefinition def)
    {
        if (def == null) return null;
        return itemInstance.Find(slot => slot.item != null
                                         && slot.item.ItemDefinition == def
                                         && slot.amount > 0);
    }
    public List<InventoryItemInstance> GetInventoryItemInstanceOfType(ItemDefinition def)
    {
        List<InventoryItemInstance> result = new List<InventoryItemInstance>();

        foreach (var item in itemInstance)
        {
            if (item.item.ItemDefinition == def)
                result.Add(item);
        }

        return result;
    }
    public InventoryItemInstance FindStack(ItemDefinition def)
    {
        foreach (var slot in itemInstance)
        {
            if (slot.item.ItemDefinition == def && def.itemCanStack)
            {
                return slot;
            }
        }

        return null;
    }
    public bool HasFreeSlot()
    {
        return itemInstance.Count < maxItems;
    }
    public int FreeSlots()
    {
        return maxItems - itemInstance.Count;
    }
    public void AddNewStack(ItemInstance sourceItem, int amount)
    {
        InventoryItemInstance newStack =
            new InventoryItemInstance(sourceItem.Clone(), amount);

        itemInstance.Add(newStack);
    }
    public void AddItemInstance(InventoryItemInstance source)
    {
        InventoryItemInstance copy = source.Clone(); // ✅ uses both clone levels
        itemInstance.Add(copy);
    }
    bool TryAddStackable(ItemInstance item, int amount)
    {
        var stack = FindStack(item.ItemDefinition);

        if (stack != null)
        {
            stack.amount += amount;
            return true;
        }

        if (!HasFreeSlot())
            return false;

        AddNewStack(item, amount);
        return true;
    }
    bool TryAddNonStackable(ItemInstance item, int amount)
    {
        if (FreeSlots() < amount)
        {
            Debug.Log($"Not enufe free slotts({FreeSlots()}) for transfering({amount})");
            return false;
        }

        for (int i = 0; i < amount; i++)
        {
            InventoryItemInstance newSlot =
                new InventoryItemInstance(item.Clone(), 1);

            itemInstance.Add(newSlot);
        }
        Debug.Log($"Have enufe freespace({FreeSlots()}) for amount({amount})");

        return true;
    }
    public bool TryAddItem(ItemInstance item, int amount)
    {
        if (item.ItemDefinition.itemCanStack)
        {
            Debug.Log("Item can stack");
            return TryAddStackable(item, amount);
        }
        else
        {
            Debug.Log("Item can NOT stack");
            return TryAddNonStackable(item, amount);
        }
    }
    //NEW ItemInstance
    public void AddItem(ItemInstance item, int amount)
    {
        // Check if item already exists
        if (item == null)
        {
            Debug.LogWarning("Trying to add a null item to the inventory.");
            return;
        }
        else
        {
            Debug.Log("Item is valid: " + item.ItemDefinition.itemName);

            if (!singelItemsOnly)
            { 
                InventoryItemInstance slot = itemInstance.Find(i => i.item.ItemDefinition == item.ItemDefinition);
                if (slot != null)
                {
                    slot.amount += amount;
                }
                else
                {
                    itemInstance.Add(new InventoryItemInstance(item, amount));
                }
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    itemInstance.Add(new InventoryItemInstance(item, 1));
                }
            }
        }
    }
    public void RemoveItem(ItemInstance item, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            InventoryItemInstance slot = itemInstance.Find(i => i.item.ItemDefinition == item.ItemDefinition);
            if (slot != null)
            {
                slot.amount -= 1;
                if (slot.amount <= 0)
                    itemInstance.Remove(slot);
            }
        }
    }
    public void ReplaceAmountItem(ItemInstance item, int newAmount)
    {
        // Check if item already exists
        if (item == null)
        {
            Debug.LogWarning("Trying to add a null item to the inventory.");
            return;
        }
        else
        {
            Debug.Log("Item is valid: " + item.ItemDefinition.itemName);

            InventoryItemInstance slot = itemInstance.Find(i => i.item == item);
            if (slot != null)
            {
                if (newAmount <= 0)
                {
                    itemInstance.Remove(slot);
                }
                else
                {
                    slot.amount = newAmount;
                }
            }
            else
            {
                itemInstance.Add(new InventoryItemInstance(item, newAmount));
            }
        }
    }
    public void SaveInventory(string filePath)
    {
        InventorySaveData data = new InventorySaveData();
        foreach (var slot in itemInstance)
        {
            data.itemNames.Add(slot.item.ItemDefinition.itemName);
            data.itemAmounts.Add(slot.amount);
            //TODO: Add more variabels...
        }

        string json = JsonUtility.ToJson(data, true);
        System.IO.File.WriteAllText(filePath, json);
    }
    public void LoadInventory(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return;

        string json = System.IO.File.ReadAllText(filePath);
        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        itemInstance.Clear();

        for (int i = 0; i < data.itemNames.Count; i++)
        {
            ItemDefinition foundItem = Resources.Load<ItemDefinition>("Items/" + data.itemNames[i]);
            if (foundItem != null)
            {
                ItemInstance newItemInstnace = new ItemInstance();
                newItemInstnace.ItemDefinition = foundItem;
                //TODO: add more variabels....
                itemInstance.Add(new InventoryItemInstance(newItemInstnace, data.itemAmounts[i]));
            }
        }
    }
}