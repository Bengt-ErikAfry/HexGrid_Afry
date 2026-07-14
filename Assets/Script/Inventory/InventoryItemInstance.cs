[System.Serializable]
public class InventoryItemInstance
{
    public ItemInstance item;
    public int amount;

    public InventoryItemInstance(ItemInstance item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public InventoryItemInstance Clone()
    {
        return new InventoryItemInstance(item.Clone(), amount);
    }
}
