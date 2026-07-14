using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;

    public void SetSlot(string name, int amount)
    {

        if (name != "")
        {
            nameText.text = name;
            amountText.text = amount.ToString();
        }
        else
        {
            nameText.text = "";
            amountText.text = "";
        }
    }
}
