using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRequiredAmountSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;
    public Image backGroundImage;

    public void SetSlot(string name, int amount, int requiredAmount, Color backGroundColor)
    {

        if (name != "")
        {
            nameText.text = name;
            amountText.text = requiredAmount.ToString() + "/" + amount.ToString();
            backGroundImage.color = backGroundColor;
        }
        else
        {
            nameText.text = "";
            amountText.text = "";
            backGroundImage.color = Color.white;
        }
    }
}
