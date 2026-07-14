using System;

[System.Serializable]
public class InventoryItemAmount
{
    public ItemDefinition item;
    public int amount;

    public InventoryItemAmount(ItemDefinition item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}
