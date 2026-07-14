using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab; // Prefab for inventory slots
    [SerializeField] private Transform contentParent; // The ScrollView content

    private readonly List<GameObject> activeSlots = new();
    public bool showOre = true;
    public bool showCraftable = true;
    public bool showTools = true;
    public bool showRocks = true;
    public bool showUnits = false;
    public bool showShips = false;
    public bool showModule = false;

    public void RefreshUI(Inventory unitInventory)
    {
        // Clear old slots
        foreach (var slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();
        
        foreach (var InventorySlot in unitInventory.itemInstance)
        {
            Debug.Log($"inventory item nem {InventorySlot.item.ItemDefinition.itemName}");
            if ((InventorySlot.item.ItemDefinition.itemType == ItemType.Ore && showOre) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Craftable && showCraftable) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Tool && showTools) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Rocks && showRocks) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Units && showUnits) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Ship && showShips) ||
                (InventorySlot.item.ItemDefinition.itemType == ItemType.Module && showModule))
            {
                GameObject newSlot = Instantiate(slotPrefab, contentParent);
                InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();
                Debug.Log(InventorySlot.item.ItemDefinition.itemName + InventorySlot.amount);
                slotUI.SetSlot(InventorySlot.item.ItemDefinition.itemName, InventorySlot.amount);
                activeSlots.Add(newSlot);
            }
        }
    }
}


