using System.Collections.Generic;
using UnityEngine;

public class InventoryFactoryUI : MonoBehaviour
{

    public GameObject slotBlueprint; // Prefab for Blueprint list slot
    public GameObject slotReqiredAmountPrefab; //Ordinary slot prefab sor exsampel storage. This is used to show how many resorces are needed.
    public Transform content_blueprintList; // The ScrollView content for blueprint list
    public Transform content_resorceList; // The ScrollView content for resorce list

    private readonly List<GameObject> activeBlueprintSlots = new();
    private readonly List<GameObject> activeResourceSlots = new();

    public void RefreshUI(List<BluePrints> blueprints, Inventory inventory)
    {
        DestroyAllBlueptrintSlots();
        DestroyAllResorceSlots();

        if (SelectionService.Instance.SelectedUnit.factoryComponent == null) { Debug.LogError("FactoryComponent is null!"); return; }

        //Create Blueprints
        for (int i = 0; i < blueprints.Count; i++)
        {
            Color slotColor = Color.green;

            for (int u = 0; u < blueprints[i].ItemDefinition.requiredItems.Count; u++)
            {
                if (!inventory.HasItem(blueprints[i].ItemDefinition.requiredItems[u].item, blueprints[i].ItemDefinition.requiredItems[u].amount))
                {
                    slotColor = Color.red;
                    break;
                }
            }

            GameObject newSlot = Instantiate(slotBlueprint, content_blueprintList);
            InventoryFactorySlotUI slotUI = newSlot.GetComponent<InventoryFactorySlotUI>();
            if (slotUI == null) { Debug.LogError("InventoryFactorySlotUI is null!"); return; }

            slotUI.SetSlot(blueprints[i].ItemDefinition.itemName, blueprints[i].ItemDefinition, slotColor, this);
            activeBlueprintSlots.Add(newSlot);
        }

        FactoryManager.Instance.UpDateFactoryProductionUI();
    }

    //Show Blueprints NOT reqItems
    public void ShowRequierdItems(ItemDefinition itemToShowReqFor, Inventory inventory)
    {
        DestroyAllResorceSlots();

        Color slotColor = Color.green;
        bool haveAllResorce = true;

        for (int i = 0; i < itemToShowReqFor.requiredItems.Count; i++)
        {
            if (!inventory.HasItem(itemToShowReqFor.requiredItems[i].item, itemToShowReqFor.requiredItems[i].amount))
            {
                slotColor = Color.red;
                haveAllResorce = false;
            }

            GameObject newSlot = Instantiate(slotReqiredAmountPrefab, content_resorceList);
            InventoryRequiredAmountSlotUI slotResorceUI = newSlot.GetComponent<InventoryRequiredAmountSlotUI>();

            slotResorceUI.SetSlot(
                itemToShowReqFor.requiredItems[i].item.itemName, 
                itemToShowReqFor.requiredItems[i].amount, 
                inventory.GetItemCount(itemToShowReqFor.requiredItems[i].item),
                slotColor);
            activeResourceSlots.Add(newSlot);
        }
    }

    public void DestroyAllBlueptrintSlots()
    {
        // Clear old slots
        foreach (var slot in activeBlueprintSlots)
        {
            Destroy(slot);
        }
        activeBlueprintSlots.Clear();
    }

    public void DestroyAllResorceSlots()
    {
        // Clear old slots
        foreach (var slot in activeResourceSlots)
        {
            Destroy(slot);
        }
        activeResourceSlots.Clear();
    }
}

