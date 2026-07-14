using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHangerSlotUI : MonoBehaviour
{
    public Image itemIconImage;
    public TMP_Text itemName;
    public ItemDefinition moduleInSlot;

    public void SetupSlot(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null) 
        {
            Debug.LogWarning("SetupSlot wass called but without item");
            return; 
        }

        //Set up icon
        itemIconImage.sprite = itemDefinition.icon;

        //Setup name
        itemName.text = itemDefinition.name;

        //Save item for later
        moduleInSlot = itemDefinition;
    }

    public void OnClickingSlot()
    {
        if (moduleInSlot != null) { Debug.Log("Slot Missing item"); }

        Debug.Log("Clicked Slot");
    }
}
