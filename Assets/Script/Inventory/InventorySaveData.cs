using System.Collections.Generic;

[System.Serializable]
public class InventorySaveData
{
    public List<string> itemNames = new();
    public List<int> itemAmounts = new();
}
