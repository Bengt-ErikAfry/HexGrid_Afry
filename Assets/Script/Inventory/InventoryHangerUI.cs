using System.Collections.Generic;
using UnityEngine;

public class InventoryHangerUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab; // Prefab for inventory slots
    [SerializeField] private Transform contentParent; // The ScrollView content

    private readonly List<GameObject> activeSlots = new();

    public void RefreshUI(Inventory ships_inventory)
    {
        // Clear old slots
        foreach (var slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();
        
        for (int i = 0; i < ships_inventory.itemInstance.Count; i++)
        {
            if (ships_inventory.itemInstance[i].item.ItemDefinition.itemType == ItemType.Ship)
            {
                GameObject newSlot = Instantiate(slotPrefab, contentParent);
                InventorySlottHangerUI slotUI = newSlot.GetComponent<InventorySlottHangerUI>();

                slotUI.SetSlot(ships_inventory.itemInstance[i].item.ItemDefinition.itemName, ships_inventory.itemInstance[i].amount, (ItemInstance)ships_inventory.itemInstance[i].item);
                activeSlots.Add(newSlot);
            }
        }
    }
}
