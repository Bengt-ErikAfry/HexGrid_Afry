using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot_Cargo : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;
    public Button pickUpBT;
    public int slotID;

    public void SetSlot(string name, int amount, int slotID)
    {

        if (name != "")
        {
            nameText.text = name;
            amountText.text = amount.ToString();
            this.slotID = slotID;
        }
        else
        {
            nameText.text = "";
            amountText.text = "";
        }
    }

    public void OnPickUpButtonPressed()
    {
        //cargoHualerPanelManager.Instance.PickUpItemToCargoHualer(slotID);
    }
}
