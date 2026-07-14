using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FactoryManager : MonoBehaviour
{
    public static FactoryManager Instance { get; private set; }

    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemBuildTimeText;
    public TMP_Text itemcompleteText;
    public Slider itemCompleteSlider;
    public Button startProducingButton;
    public TMP_Text factoryStatusText;

    public ItemInstance itemSelectedToShowData;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //Show ReqItems
    public void ItemSelectedToBuild(ItemInstance itemInstance)
    {
        itemNameText.text = itemInstance.ItemDefinition.itemName;
        itemSelectedToShowData = itemInstance;
        itemDescriptionText.text = itemInstance.ItemDefinition.description;
        itemBuildTimeText.text = $"Build Time: {itemInstance.ItemDefinition.craftingTime} turn.";

        if (SelectionService.Instance.SelectedUnit == null) { Debug.LogError("SelectedUnit is null!"); return; }
        if (SelectionService.Instance.SelectedUnit.factoryComponent == null) { Debug.LogError("factoryComponent is null!"); return; }
        float completePros = SelectionService.Instance.SelectedUnit.factoryComponent.completePercentage;

        itemcompleteText.text = $"{Mathf.RoundToInt(completePros * 100)}% turn {Mathf.RoundToInt(itemInstance.ItemDefinition.craftingTime * completePros)}/{itemInstance.ItemDefinition.craftingTime}";
        itemCompleteSlider.value = completePros;

        //Set button off if still producing or on if nothing is producing
        if (startProducingButton == null) { Debug.LogError(" is null!"); return; }

        if(HaveRequeredItem(itemInstance, SelectionService.Instance.SelectedUnit.unitInventory))
        {
            //Debug.Log("Have all items");
            startProducingButton.interactable = true;
        }
        else
        {
            //Debug.Log("Do NOT have all items");
            startProducingButton.interactable = false;
        }
        
    }

    public void ResetBuilderMenu()
    {
        itemNameText.text = "Select a Blueprint to Build.";
        itemSelectedToShowData = null;
        itemDescriptionText.text = "";
        itemBuildTimeText.text = $"Build Time: 0 turn.";
        itemcompleteText.text = $"0% turn 0/0";
        itemCompleteSlider.value = 0;

        factoryStatusText.text = "Factory Status: idl.";
        startProducingButton.interactable = false;
    }

    public void ShowWhatIsBuilding()
    {
        ItemInstance itemToBuild = SelectionService.Instance.SelectedUnit.factoryComponent.itemToProduce;

        itemNameText.text = itemToBuild.ItemDefinition.itemName;
        itemSelectedToShowData = itemToBuild;
        itemDescriptionText.text = itemToBuild.ItemDefinition.description;
        itemBuildTimeText.text = $"Build Time: {itemToBuild.ItemDefinition.craftingTime} turn.";

        if (SelectionService.Instance.SelectedUnit == null) { Debug.LogError("SelectedUnit is null!"); return; }
        if (SelectionService.Instance.SelectedUnit.factoryComponent == null) { Debug.LogError("factoryComponent is null!"); return; }
        float completePros = SelectionService.Instance.SelectedUnit.factoryComponent.completePercentage;

        itemcompleteText.text = $"{Mathf.RoundToInt(completePros * 100)}% turn {Mathf.RoundToInt(itemToBuild.ItemDefinition.craftingTime * completePros)}/{itemToBuild.ItemDefinition.craftingTime}";
        itemCompleteSlider.value = completePros;

        //Set button off if still producing or on if nothing is producing
        if (startProducingButton == null) { Debug.LogError(" is null!"); return; }
        startProducingButton.interactable = false;
        factoryStatusText.text = $"Factory Status: Building {itemToBuild.ItemDefinition.itemName}";
    }

    public void StartProducing_Pressed()
    {
        SelectionService.Instance.SelectedUnit.factoryComponent.StartProduction(itemSelectedToShowData);
        startProducingButton.interactable = false;
        factoryStatusText.text = $"Factory Status: Building {SelectionService.Instance.SelectedUnit.factoryComponent.itemToProduce.ItemDefinition.itemName}";

        //Remove Item
        for (int i = 0; i < itemSelectedToShowData.ItemDefinition.requiredItems.Count; i++)
        {
            ItemInstance newItemInstance = new ItemInstance();
            newItemInstance.ItemDefinition = itemSelectedToShowData.ItemDefinition.requiredItems[i].item;
            SelectionService.Instance.SelectedUnit.unitInventory.RemoveItem(newItemInstance, itemSelectedToShowData.ItemDefinition.requiredItems[i].amount);
        }

        //Update the UI now that you have removed one item.
        InfoScreenManager.Instance.ShowFactory();
    }

    public void UpDateFactoryProductionUI()
    {
        if (SelectionService.Instance.SelectedUnit.factoryComponent == null) { Debug.LogError("FactoryComponent is null!"); return; }

        if (SelectionService.Instance.SelectedUnit.factoryComponent.itemToProduce.ItemDefinition == null)
        {
            ResetBuilderMenu();
        }
        else
        {
            ShowWhatIsBuilding();
        }
    }

    public bool HaveRequeredItem(ItemInstance itemToBuild, Inventory inventoryToCheck)
    {
        bool isHavingAllItems = true;
        for (int i = 0; i < itemToBuild.ItemDefinition.requiredItems.Count; i++)
        {
            if (!inventoryToCheck.HasItem(itemToBuild.ItemDefinition.requiredItems[i].item, itemToBuild.ItemDefinition.requiredItems[i].amount))
            {
                isHavingAllItems = false; 
                break;
            }
        }

        return isHavingAllItems;
    }
}
