using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventory", menuName = "Inventory/InventoryFactory")]
public class InventoryFactory : ScriptableObject
{
    public List<ItemDefinition> bluePrints = new List<ItemDefinition>();

    /*
    public bool HasBluePrint(Item item)
    {
        bool found = false;
        foreach (var slot in bluePrints)
        {
            if (slot.item == item)
                found = true;
        }
        return found;
    }

    
    public void AddItem(Item item, int amount)
    {
        // Check if item already exists
        InventorySlot slot = items.Find(i => i.item == item);
        if (slot != null)
        {
            slot.amount += amount;
        }
        else
        {
            items.Add(new InventorySlot(item, amount));
        }
    }

    public void RemoveItem(Item item, int amount)
    {
        InventorySlot slot = items.Find(i => i.item == item);
        if (slot != null)
        {
            slot.amount -= amount;
            if (slot.amount <= 0)
                items.Remove(slot);
        }
    }

    public void SaveInventory(string filePath)
    {
        InventorySaveData data = new InventorySaveData();
        foreach (var slot in items)
        {
            data.itemNames.Add(slot.item.itemName);
            data.itemAmounts.Add(slot.amount);
        }

        string json = JsonUtility.ToJson(data, true);
        System.IO.File.WriteAllText(filePath, json);
    }

    public void LoadInventory(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return;

        string json = System.IO.File.ReadAllText(filePath);
        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        items.Clear();

        for (int i = 0; i < data.itemNames.Count; i++)
        {
            Item foundItem = Resources.Load<Item>("Items/" + data.itemNames[i]);
            if (foundItem != null)
                items.Add(new InventorySlot(foundItem, data.itemAmounts[i]));
        }
    }*/
}


