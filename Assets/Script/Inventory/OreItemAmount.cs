using System;

[System.Serializable]
public class OreItemAmount
{
    public ItemDefinition item;
    public float amount;

    public OreItemAmount(ItemDefinition item, float amount)
    {
        this.item = item;
        this.amount = amount;
    }
}
