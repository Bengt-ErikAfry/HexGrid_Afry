using TMPro;
using UnityEngine;

public class InventorySlottHangerUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;
    public ItemInstance item;

    public void SetSlot(string name, int amount, ItemInstance Item)
    {

        if (name != "")
        {
            nameText.text = name;
            amountText.text = amount.ToString();
            this.item = Item;
        }
        else
        {
            nameText.text = "";
            amountText.text = "";
            this.item = null;
        }
    }

    public void SlotPressed()
    {
        //ShipInfoPanelManager.Instance.SelectDeployItem(item);
    }
}
