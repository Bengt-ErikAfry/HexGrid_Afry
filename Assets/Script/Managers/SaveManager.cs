using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public GameObject unitPrefab; // Assign the Unit prefab in the Inspector
    public Transform unitFolderForAllUnits; // Assign a parent transform in the Inspector to hold all units

    public static SaveManager Instance { get; private set; }

    [Tooltip("Relative filename under Application.persistentDataPath")]
    public string defaultFileName = "units_save.json";

    private readonly List<Unit> hiddenGameViewUnits = new List<Unit>();
    private readonly List<AsteroidComponent> hiddenGameViewAsteroids = new List<AsteroidComponent>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void HideGameViewObjects()
    {
        //Destroy all units.
        foreach (var unit in GameManager.Instance.unitsList)
        {
            if (unit == null) continue;
            // ADDED: Prevent hidden units from contributing to fog of war.
                TileManager.Instance.UnregisterUnit(unit);
            Destroy(unit.gameObject);
        }

        //Destroy all asteroids.
        foreach (var asteroid in GameManager.Instance.asteroidsList)
        {
            if (asteroid == null) continue;
            Destroy(asteroid.gameObject);
        }

        // Only have active units in list.
        GameManager.Instance.unitsList.Clear();
        GameManager.Instance.playerUnits.Clear();
        GameManager.Instance.enemyUnits.Clear();

        Debug.Log("Game-view units and asteroids hidden.");
    }

    public void LoadAsteroidViewObject(AsteroidComponent asteroid)
    {
        if (asteroid == null)
        {
            Debug.LogWarning(
                "LoadAsteroidViewObject: AsteroidComponent is null.");
            return;
        }

        // An empty filename means this asteroid has not been entered before.
        if (string.IsNullOrWhiteSpace(asteroid.unitsOnSurfaceJsonFileName))
        {
            Debug.Log(
                $"LoadAsteroidViewObject: No saved units for asteroid " +
                $"{asteroid.asteroidName}. First visit.");
            return;
        }

        // SaveAllUnits writes to persistentDataPath, so load from there too.
        string path = Path.Combine(
            Application.persistentDataPath,
            asteroid.unitsOnSurfaceJsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"LoadAsteroidViewObject: File not found: {path}");
            return;
        }

        UnitsSaveWrapper wrapper;

        try
        {
            string json = File.ReadAllText(path);
            wrapper = JsonUtility.FromJson<UnitsSaveWrapper>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"LoadAsteroidViewObject: Could not read {path}. " +
                exception.Message);
            return;
        }

        if (wrapper == null || wrapper.units == null)
        {
            Debug.LogWarning(
                $"LoadAsteroidViewObject: No unit data found in {path}");
            return;
        }

        // Used later to reconnect target_Unit_Script references.
        var loadedUnitsById = new Dictionary<string, Unit>();

        foreach (var savedUnit in wrapper.units)
        {
            if (savedUnit == null || string.IsNullOrWhiteSpace(savedUnit.unitId))
                continue;

            // Find the already existing Unit object by its unitId.
            Unit runtimeUnit = GameManager.Instance.unitsList
                .FirstOrDefault(unit =>
                    unit != null &&
                    unit.unitId == savedUnit.unitId);

            if (runtimeUnit == null)
            {
                if (unitPrefab == null)
                {
                    Debug.LogWarning(
                        $"LoadAsteroidViewObject: No runtime Unit found for " +
                        $"unitId {savedUnit.unitId} and no unitPrefab assigned.");
                    continue;
                }

                GameObject spawned = Instantiate(unitPrefab, unitFolderForAllUnits);
                runtimeUnit = spawned.GetComponent<Unit>();

                if (runtimeUnit == null)
                {
                    Debug.LogWarning(
                        $"LoadAsteroidViewObject: Spawned prefab for " +
                        $"unitId {savedUnit.unitId} does not contain a Unit component.");
                    Destroy(spawned);
                    continue;
                }

                runtimeUnit.unitId = savedUnit.unitId;
                runtimeUnit.unitName = savedUnit.unitName;
                runtimeUnit.unitType = savedUnit.unitType;
                runtimeUnit.shipType = savedUnit.shipType;
                runtimeUnit.isPlayerControlled = savedUnit.isPlayerControlled;

                if (!GameManager.Instance.unitsList.Contains(runtimeUnit))
                    GameManager.Instance.unitsList.Add(runtimeUnit);

                if (runtimeUnit.isPlayerControlled && !GameManager.Instance.playerUnits.Contains(runtimeUnit))
                {
                    GameManager.Instance.playerUnits.Add(runtimeUnit);
                }
                else if (!runtimeUnit.isPlayerControlled && !GameManager.Instance.enemyUnits.Contains(runtimeUnit))
                {
                    GameManager.Instance.enemyUnits.Add(runtimeUnit);
                }
            }

            // Restore the same unit data used by the normal unit loader.
            ApplySavedUnitData(runtimeUnit, savedUnit);

            // Place the unit on the asteroid grid.
            Vector3 worldPosition =
                TileManager.Instance.AxialToWorldCenter(
                    savedUnit.currentTileCoord);

            runtimeUnit.transform.position = worldPosition;
            runtimeUnit.currentTileCoord = savedUnit.currentTileCoord;

            // Make this unit visible inside the asteroid.
            runtimeUnit.gameObject.SetActive(true);

            loadedUnitsById[savedUnit.unitId] = runtimeUnit;
        }

        // Restore references between units, for example target_Unit_Script.
        foreach (var savedUnit in wrapper.units)
        {
            if (savedUnit == null ||
                string.IsNullOrWhiteSpace(savedUnit.targetUnitId))
                continue;

            if (!loadedUnitsById.TryGetValue(
                    savedUnit.unitId,
                    out Unit sourceUnit))
                continue;

            if (!loadedUnitsById.TryGetValue(
                    savedUnit.targetUnitId,
                    out Unit targetUnit))
                continue;

            sourceUnit.target_Unit_Script = targetUnit;
        }

        Debug.Log(
            $"LoadAsteroidViewObject: Loaded " +
            $"{loadedUnitsById.Count} units for {asteroid.asteroidName}.");
    }

    [System.Serializable]
    public class ItemInstanceSaveData
    {
        public string itemId;          // reference to ItemDefinition.itemId
        public int amount;             // stack count
        public int currentHealth;
        public int currentPowerUpBoost;
        public bool isOnline;
        public bool isBroken;
        public bool isDestroyed;
        // add any other runtime fields you need to persist
    }

    [Serializable]
    private class UnitSaveData
    {
        public string unitId;           // GUID for this unit in the save file
        public string unitName;
        public UnitType unitType;
        public ShipType shipType;   // <-- save the ShipType enum. Can be found in the ShipClassManager.
        public Vector2Int currentTileCoord;
        public int movedThisTurn;        // For turn-based games, track if the unit has moved this turn
        public bool isPlayerControlled;
        public bool haveAttackedThisTurn;   // For turn-based games, track if the unit has attacked this turn
        public string targetUnitId;     // GUID of referenced target unit (if any)
        public Vector2Int target_lastKnownTileCoord; // Last known tile coordinate of the target unit (if any)
        public bool followtarget; // Whether the unit is set to follow its target
        public bool hasMinedThisTurn;
        public List<Vector2Int> currentMovePath; // Path for unit to move next turn.
        public List<ItemInstanceSaveData> moduleRuntimeList; // List of module on the unit. This is a runtime list of module instances that are currently on the unit. It is used to save and load the state of the unit's modules.
    }
    [Serializable]
    private class UnitsSaveWrapper
    {
        public List<UnitSaveData> units = new List<UnitSaveData>();
    }

    // Save all units. Each runtime unit keeps its stable GUID stored in the save file.
    // References between units are saved as GUIDs (targetUnitId).
    public void SaveAllUnits(string fileName = null)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("SaveAllUnits: GameManager.Instance is null. Aborting save.");
            return;
        }

        var wrapper = new UnitsSaveWrapper();

        foreach (var u in GameManager.Instance.unitsList)
        {
            if (u == null) continue;

            // Ensure unit has a stable id (generate once if missing)
            if (string.IsNullOrEmpty(u.unitId))
            {
                u.unitId = Guid.NewGuid().ToString();
            }

            var modulesSave = new List<ItemInstanceSaveData>();
            if (u.moduleRuntimeList != null)
            {
                // Preserve slot count: save one entry per module slot (even if ItemDefinition is null)
                foreach (var mod in u.moduleRuntimeList)
                {
                    if (mod == null)
                    {
                        modulesSave.Add(new ItemInstanceSaveData
                        {
                            itemId = null,
                            amount = 0,
                            currentHealth = 0,
                            isOnline = false,
                            isBroken = false,
                            isDestroyed = false
                        });
                    }
                    else
                    {
                        modulesSave.Add(new ItemInstanceSaveData
                        {
                            itemId = mod.ItemDefinition != null ? mod.ItemDefinition.itemId : null,
                            amount = 1, // modules on unit are single instances
                            currentHealth = mod.currentHealth,
                            isOnline = mod.isOnline,
                            isBroken = mod.isBroken,
                            isDestroyed = mod.isDestroyd
                        });
                    }
                }
            }
            else
            {
                modulesSave = new List<ItemInstanceSaveData>();
            }

            // Convert target last known position to axial coord (if present)
            Vector2Int targetLastKnownCoord = Vector2Int.zero;
            if (u.target_LastKnownPos != Vector3.zero && TileManager.Instance != null)
            {
                targetLastKnownCoord = TileManager.Instance.WorldToAxial(u.target_LastKnownPos);
            }

            var save = new UnitSaveData
            {
                unitId = u.unitId,
                unitName = u.unitName,
                unitType = u.unitType,
                shipType = u.shipType,
                currentTileCoord = u.currentTileCoord,
                movedThisTurn = u.movedThisTurn,
                isPlayerControlled = u.isPlayerControlled,
                haveAttackedThisTurn = u.haveAttackedThisTurn,
                targetUnitId = u.target_Unit_Script != null ? u.target_Unit_Script.unitId : null,
                target_lastKnownTileCoord = targetLastKnownCoord,
                followtarget = u.followTarget,
                hasMinedThisTurn = u.hasMinedThisTurn,
                currentMovePath = (u.currentMovePath != null && u.currentMovePath.Count > 0) ? new List<Vector2Int>(u.currentMovePath) : new List<Vector2Int>(),
                moduleRuntimeList = modulesSave ?? new List<ItemInstanceSaveData>()
            };

            wrapper.units.Add(save);
        }

        string fname = string.IsNullOrEmpty(fileName) ? defaultFileName : fileName;
        string path = Path.Combine(Application.persistentDataPath, fname);
        try
        {
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(path, json);
            Debug.Log($"SaveManager: Saved {wrapper.units.Count} units to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to save units to {path}: {ex.Message}");
        }
    }

    // Load all units from file. This resolves references by GUID after matching saved entries to existing runtime Unit instances.
    // Matching strategy: locate runtime Unit by unitId (preferred). Falls back to unitName if unitId not found.
    public void LoadAllUnits(string fileName = null)
    {
        string fname = string.IsNullOrEmpty(fileName) ? defaultFileName : fileName;
        string path = Path.Combine(Application.persistentDataPath, fname);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"SaveManager.LoadAllUnits: file not found: {path}");
            return;
        }

        StartCoroutine(LoadRoutine(path));
    }

    private IEnumerator LoadRoutine(string path)
    {
        // Wait until managers and tile lookup are ready
        yield return new WaitUntil(() => GameManager.Instance != null && TileManager.Instance != null && TileManager.Instance.HasBuiltLookup);

        UnitsSaveWrapper wrapper = null;
        try
        {
            string json = File.ReadAllText(path);
            wrapper = JsonUtility.FromJson<UnitsSaveWrapper>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to read/parse save file: {ex.Message}");
            yield break;
        }

        if (wrapper == null || wrapper.units == null)
        {
            Debug.LogWarning("SaveManager: Save file contained no unit data.");
            yield break;
        }


        // Build map of saved GUID -> saved data
        var savedGuidToData = wrapper.units.ToDictionary(s => s.unitId, s => s);

        // Build runtime map GUID -> Unit by matching saved entries to existing runtime units (prefer unitId).
        var guidToRuntimeUnit = new Dictionary<string, Unit>();
        foreach (var saved in wrapper.units)
        {
            // Prefer GUID identity. Unit names are not stable enough because multiple units can share the same name.
            Unit runtime = null;
            string savedUnitKey = string.IsNullOrEmpty(saved.unitId) ? saved.unitName : saved.unitId;

            if (!string.IsNullOrEmpty(saved.unitId))
            {
                runtime = GameManager.Instance.unitsList.FirstOrDefault(u =>
                    u != null && u.unitId == saved.unitId);
            }
            else
            {
                // Legacy fallback only when no unitId exists in the save file.
                runtime = GameManager.Instance.unitsList.FirstOrDefault(u =>
                    u != null && u.unitName == saved.unitName);
            }

            if (runtime == null)
            {
                if (unitPrefab == null)
                {
                    Debug.LogWarning($"SaveManager: Cannot instantiate saved unit {saved.unitName} ({saved.unitId}) because unitPrefab is not assigned.");
                    continue;
                }

                GameObject go = Instantiate(unitPrefab);
                runtime = go.GetComponent<Unit>();
                if (runtime == null)
                {
                    Debug.LogWarning($"SaveManager: Spawned prefab for saved unit {saved.unitName} ({saved.unitId}) does not contain a Unit component.");
                    Destroy(go);
                    continue;
                }

                runtime.unitId = saved.unitId;
                runtime.unitName = saved.unitName;
                runtime.unitType = saved.unitType;
                runtime.shipType = saved.shipType;
                runtime.isPlayerControlled = saved.isPlayerControlled;

                if (!GameManager.Instance.unitsList.Contains(runtime))
                    GameManager.Instance.unitsList.Add(runtime);

                if (runtime.isPlayerControlled && !GameManager.Instance.playerUnits.Contains(runtime))
                    GameManager.Instance.playerUnits.Add(runtime);
                else if (!runtime.isPlayerControlled && !GameManager.Instance.enemyUnits.Contains(runtime))
                    GameManager.Instance.enemyUnits.Add(runtime);
            }

            // Record mapping for later reference resolving.
            guidToRuntimeUnit[savedUnitKey] = runtime;

            // Apply basic and runtime state
            runtime.unitId = string.IsNullOrEmpty(saved.unitId) ? runtime.unitId : saved.unitId;
            runtime.unitName = saved.unitName;
            runtime.unitType = saved.unitType;
            runtime.shipType = saved.shipType;
            runtime.isPlayerControlled = saved.isPlayerControlled;
            runtime.currentTileCoord = saved.currentTileCoord;
            runtime.movedThisTurn = saved.movedThisTurn;
            runtime.haveAttackedThisTurn = saved.haveAttackedThisTurn;
            runtime.followTarget = saved.followtarget;
            runtime.hasMinedThisTurn = saved.hasMinedThisTurn;

            // Apply movement path
            runtime.currentMovePath = (saved.currentMovePath != null) ? new List<Vector2Int>(saved.currentMovePath) : new List<Vector2Int>();

            // Apply last known target world pos if present
            if (saved.target_lastKnownTileCoord != Vector2Int.zero)
            {
                runtime.target_LastKnownPos = TileManager.Instance.AxialToWorldCenter(saved.target_lastKnownTileCoord);
            }

            // Place unit at tile world center if tile exists
            var world = TileManager.Instance.AxialToWorldCenter(runtime.currentTileCoord);
            runtime.transform.position = world;

            runtime.moduleRuntimeList = new List<ItemInstance>();
            if (saved.moduleRuntimeList != null)
            {
                var itemDb = ItemDatabase.Instance; // may be null
                foreach (var m in saved.moduleRuntimeList)
                {
                    var instance = new ItemInstance();

                    if (m != null && !string.IsNullOrEmpty(m.itemId))
                    {
                        ItemDefinition def = null;
                        if (itemDb != null) def = itemDb.GetById(m.itemId);
                        else def = Resources.LoadAll<ItemDefinition>("ItemDefinitions").FirstOrDefault(x => x != null && x.itemId == m.itemId);

                        instance.ItemDefinition = def;
                    }
                    else
                    {
                        // slot deliberately empty — keep ItemDefinition null
                        instance.ItemDefinition = null;
                    }

                    // Restore runtime fields if present (safe when m is null)
                    if (m != null)
                    {
                        instance.currentHealth = m.currentHealth;
                        instance.isOnline = m.isOnline;
                        instance.isBroken = m.isBroken;
                        instance.isDestroyd = m.isDestroyed;
                    }
                    else
                    {
                        instance.currentHealth = 0;
                        instance.currentMaxHealth = 0;
                        instance.isOnline = false;
                        instance.isBroken = false;
                        instance.isDestroyd = false;
                    }

                    // Always add an ItemInstance to preserve slot index
                    runtime.moduleRuntimeList.Add(instance);
                }
            }

            // Init the new unit (e.g., update visuals, health bars, etc.)
            StartCoroutine(runtime.InitUnit()); //To register for FOG OF WAR to work.
        }

        // Resolve cross-unit references (target_Unit_Script)
        int resolved = 0;
        foreach (var saved in wrapper.units)
        {
            if (string.IsNullOrEmpty(saved.targetUnitId)) continue;

            string sourceKey = string.IsNullOrEmpty(saved.unitId) ? saved.unitName : saved.unitId;
            if (!guidToRuntimeUnit.TryGetValue(sourceKey, out var sourceUnit)) continue;
            if (!guidToRuntimeUnit.TryGetValue(saved.targetUnitId, out var targetUnit)) continue;

            sourceUnit.target_Unit_Script = targetUnit;
            resolved++;
        }



        //A check to see that the saved GUID is the same as the runtime GUID. If not, it will log a warning.
        Debug.Log("Saved units:");
        foreach (var s in wrapper.units)
        {
            Debug.Log($"saved unitId={s.unitId}, name={s.unitName}");
        }

        Debug.Log("Runtime units:");
        foreach (var u in GameManager.Instance.unitsList)
        {
            if (u != null)
                Debug.Log($"runtime unitId={u.unitId}, name={u.unitName}");
        }



        Debug.Log($"SaveManager: Loaded {guidToRuntimeUnit.Count} unit(s) and resolved {resolved} unit reference(s) from {path}");
    }

    private void ApplySavedUnitData(
        Unit runtimeUnit,
        UnitSaveData savedUnit)
    {
        runtimeUnit.unitName = savedUnit.unitName;
        runtimeUnit.unitType = savedUnit.unitType;
        runtimeUnit.shipType = savedUnit.shipType;
        runtimeUnit.currentTileCoord = savedUnit.currentTileCoord;
        runtimeUnit.movedThisTurn = savedUnit.movedThisTurn;
        runtimeUnit.isPlayerControlled = savedUnit.isPlayerControlled;
        runtimeUnit.haveAttackedThisTurn = savedUnit.haveAttackedThisTurn;
        runtimeUnit.followTarget = savedUnit.followtarget;
        runtimeUnit.hasMinedThisTurn = savedUnit.hasMinedThisTurn;

        runtimeUnit.currentMovePath =
            savedUnit.currentMovePath != null
                ? new List<Vector2Int>(savedUnit.currentMovePath)
                : new List<Vector2Int>();

        if (savedUnit.target_lastKnownTileCoord != Vector2Int.zero)
        {
            runtimeUnit.target_LastKnownPos =
                TileManager.Instance.AxialToWorldCenter(
                    savedUnit.target_lastKnownTileCoord);
        }

        // Restore modules.
        runtimeUnit.moduleRuntimeList = new List<ItemInstance>();

        if (savedUnit.moduleRuntimeList == null)
            return;

        foreach (var savedModule in savedUnit.moduleRuntimeList)
        {
            var module = new ItemInstance();

            if (savedModule != null &&
                !string.IsNullOrWhiteSpace(savedModule.itemId))
            {
                ItemDefinition definition = null;

                if (ItemDatabase.Instance != null)
                {
                    definition =
                        ItemDatabase.Instance.GetById(savedModule.itemId);
                }

                if (definition == null)
                {
                    definition = Resources
                        .LoadAll<ItemDefinition>("ItemDefinitions")
                        .FirstOrDefault(item =>
                            item != null &&
                            item.itemId == savedModule.itemId);
                }

                module.ItemDefinition = definition;
            }

            if (savedModule != null)
            {
                module.currentHealth = savedModule.currentHealth;
                module.currentPowerUpBoost =
                    savedModule.currentPowerUpBoost;
                module.isOnline = savedModule.isOnline;
                module.isBroken = savedModule.isBroken;
                module.isDestroyd = savedModule.isDestroyed;
            }

            runtimeUnit.moduleRuntimeList.Add(module);
        }
    }

    // ////////////////////// Inventory Save/Load //////////////////////

    [System.Serializable]
    public class InventorySaveData
    {
        public string unitId;   // GUID of unit with current Inventorty to save.
        public List<ItemInstanceSaveData> items = new List<ItemInstanceSaveData>(); //List ItemInstanceSaveData this unit have in inventory to save.
    }

    //Saves all inventoryies of ALL units in Gamemanager.
    public void SaveAllInventories(string fileName = "inventories.json")
    {
        var wrapper = new List<InventorySaveData>();

        foreach (var unit in GameManager.Instance.unitsList)
        {
            if (unit == null || unit.unitInventory == null) continue;
            var invSave = new InventorySaveData { unitId = unit.unitId };

            foreach (var slot in unit.unitInventory.itemInstance)
            {
                if (slot == null || slot.item == null || slot.item.ItemDefinition == null) continue;
                var def = slot.item.ItemDefinition;
                if (string.IsNullOrEmpty(def.itemId))
                {
                    Debug.LogWarning($"ItemDefinition {def.name} has no itemId - skipping or assign one in editor.");
                    continue;
                }

                var itemSave = new ItemInstanceSaveData
                {
                    itemId = def.itemId,
                    amount = slot.amount,
                    currentHealth = slot.item.currentHealth,
                    currentPowerUpBoost = slot.item.currentPowerUpBoost,
                    isOnline = slot.item.isOnline,
                    isBroken = slot.item.isBroken,
                    isDestroyed = slot.item.isDestroyd
                };
                invSave.items.Add(itemSave);
            }

            wrapper.Add(invSave);
        }

        var json = JsonUtility.ToJson(new SerializationWrapper<List<InventorySaveData>>(wrapper), true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), json);
    }

    // Loads all inventories of ALL units in Gamemanager from file. It matches saved unitId to runtime unit.unitId and rebuilds the inventory list.
    public void LoadAllInventories(string fileName = "inventories.json")
    {
        var path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<SerializationWrapper<List<InventorySaveData>>>(json);
        if (wrapper == null || wrapper.value == null) return;

        // Build a lookup for ItemDefinition by itemId (use your ItemDatabase if you have one)
        var defs = ItemDatabase.Instance.GetAllItemDefinitions(); // implement below
        var defById = defs.Where(d => !string.IsNullOrEmpty(d.itemId)).ToDictionary(d => d.itemId, d => d);

        foreach (var invSave in wrapper.value)
        {
            var unit = GameManager.Instance.unitsList.FirstOrDefault(u => u.unitId == invSave.unitId);
            if (unit == null || unit.unitInventory == null) continue;

            unit.unitInventory.itemInstance.Clear();
            foreach (var itemSave in invSave.items)
            {
                if (!defById.TryGetValue(itemSave.itemId, out var def)) continue;
                var instance = new ItemInstance();
                instance.ItemDefinition = def;
                instance.currentHealth = itemSave.currentHealth;
                instance.currentPowerUpBoost = itemSave.currentPowerUpBoost;
                instance.isOnline = itemSave.isOnline;
                instance.isBroken = itemSave.isBroken;
                instance.isDestroyd = itemSave.isDestroyed;
                // copy other runtime fields...

                unit.unitInventory.itemInstance.Add(new InventoryItemInstance(instance, itemSave.amount));
            }
        }
    }
}

[Serializable]
public class SerializationWrapper<T>
{
    // JsonUtility requires a field (not a property) to serialize.
    public T value;

    // parameterless ctor needed for Unity deserialization
    public SerializationWrapper() { }

    public SerializationWrapper(T v)
    {
        value = v;
    }
}
