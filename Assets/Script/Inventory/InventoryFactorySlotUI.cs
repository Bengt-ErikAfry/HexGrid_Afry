using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryFactorySlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image backgroundImage;
    public ItemDefinition item;
    public InventoryFactoryUI inventoryFactorySlot_Script;

    public void SetSlot(string name, ItemDefinition item, Color bgColor, InventoryFactoryUI InventoryFactoryUI_Script)
    {
        inventoryFactorySlot_Script = InventoryFactoryUI_Script;
        if (name != "")
        {
            nameText.text = name;
            this.item = item;
            backgroundImage.color = bgColor;
        }
        else
        {
            nameText.text = "";
            this.item = null;
            backgroundImage.color = Color.lightGray;
        }
    }

    public void InventoryFactorySlot_Pressed()
    {
        if (SelectionService.Instance.SelectedUnit.factoryComponent.itemToProduce.ItemDefinition == null)
        {
            inventoryFactorySlot_Script.ShowRequierdItems(item, SelectionService.Instance.SelectedUnit.unitInventory);
            ItemInstance newItemInstance = new ItemInstance();
            newItemInstance.ItemDefinition = item;

            FactoryManager.Instance.ItemSelectedToBuild(newItemInstance);
        }
        else
        {
            Debug.Log("Still producing");
        }
    }
}
