
using NUnit.Framework.Interfaces;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using static UnityEditor.Progress;
using Unity.Android.Gradle.Manifest;
using static UnityEngine.Analytics.IAnalytic;

public class ShipSlotUI : MonoBehaviour, IDropHandler
{
    public ItemType acceptedItemType;
    public List<ModuleType> acceptedModuleTypes = new List<ModuleType>();
    public ShipType acceptedModuleClass;
    public ItemInstance CurrentItem;
    public int slotIndex;
    public TMP_Text slotNameText;
    public TMP_Text slotHealthText;
    public TMP_Text slotStatusText;
    public Image slotImage;

    public void OnDrop(PointerEventData eventData)
    {
        //Debug.Log($"OnDrop called {DragContext.CurrentItem}");
        var item = DragContext.CurrentItem;
        if (item == null)
        {
            Debug.LogWarning("Item == null");
            return;
        }

        //Debug.Log($"Try to see if candrop: {item.ItemDefinition.itemName}");
        if (!CanAccept(item))
        {
            Debug.LogWarning($"Can not accept {item.ItemDefinition.itemType} class{item.shipType}");
            return;
        }

        //Debug.Log($"Call place for item {item.ItemDefinition.itemName}");
        PlaceItem(item);
    }

    bool CanAccept(ItemInstance data)
    {
        Debug.LogWarning($"data:{data} itemtype:{data.ItemDefinition.itemType} moduletype:{data.ItemDefinition.moduleType}");
        if (data.ItemDefinition.moduleType != ModuleType.Hull)
        {
            return data.ItemDefinition.itemType == acceptedItemType &&
            acceptedModuleTypes.Contains(data.ItemDefinition.moduleType) && CurrentItem != null &&
            data.shipType == HangerManager.Instance.hullModuleInstance.shipType;
        }
        else
        {
            return data.ItemDefinition.itemType == acceptedItemType &&
            acceptedModuleTypes.Contains(data.ItemDefinition.moduleType) && CurrentItem != null;
        }
    }
    //Called every time something is droped on a slot but also fist when player drop on hull to set up layoutData.
    void PlaceItem(ItemInstance item)
    {
        CurrentItem = item;

        //item.transform.SetParent(transform);
        //item.transform.localPosition = Vector3.zero;

        SelectionService.Instance.SelectedUnit.unitInventory.RemoveItem(item, 1);
        HangerManager.Instance.LoadModuleList();

        if (item.ItemDefinition.itemType == ItemType.Module && item.ItemDefinition.moduleType == ModuleType.Hull) 
        {
            //Debug.Log($"HullModuleSetup called from ShipSlot{slotIndex} for item {item.ItemDefinition.itemName}");
            HangerManager.Instance.HullModuleSetup(item);

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());    // Nedded to update the TMP_Text. This is not done automaticly as when changing text in the inspector.
        }
        else
        {
            //Drop on correct slot
            slotNameText.text = item.ItemDefinition.itemName;
            slotStatusText.text = $"Status: OffLine";
            slotHealthText.text = $"Health: {item.currentHealth.ToString()}/{item.currentMaxHealth.ToString()}";
            slotImage.color = ColorManager.Instance.moduleOffline;

            LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());    // Nedded to update the TMP_Text. This is not done automaticly as when changing text in the inspector.
        }
    }
}

