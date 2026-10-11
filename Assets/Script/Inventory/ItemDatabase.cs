using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Database of all items in RESOURCE folder. 
/// Do not populate in inspector.
/// Used for runtime access to item definitions by ID.
/// Used in save/load and inventory management.
/// When saving/loading savemanager only save itemid and then look in this itemdatabase for data. 
/// Health and other runtime properties are saved in ItemInstanceSaveData.
/// 
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    // optional: populate in inspector or leave empty to load from Resources
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    private Dictionary<string, ItemDefinition> byId;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If inspector list is empty, load all ItemDefinition from Resources once
        if (allItems == null || allItems.Count == 0)
        {
            var loaded = Resources.LoadAll<ItemDefinition>("ItemDefinitions");
            allItems = loaded.ToList();
        }

        byId = new Dictionary<string, ItemDefinition>(allItems.Count);
        foreach (var def in allItems)
        {
            if (def == null || string.IsNullOrEmpty(def.itemId)) continue;
            if (!byId.ContainsKey(def.itemId)) byId.Add(def.itemId, def);
        }
    }
    public List<ItemDefinition> GetAllItemDefinitions() => allItems;

    public ItemDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        byId.TryGetValue(id, out var def);
        return def;
    }
}
