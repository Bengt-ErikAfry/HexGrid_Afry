using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class SaveManager : MonoBehaviour
{
    public GameObject unitPrefab; // Assign the Unit prefab in the Inspector
    public GameObject asteroidPrefab; // Assign the Asteroid prefab in the Inspector
    public Transform unitFolderForAllUnits; // Assign a parent transform in the Inspector to hold all units
    public Transform asteroidFolderForAllAsteroids; // Assign a parent transform in the Inspector to hold all asteroids
    public static SaveManager Instance { get; private set; }
    [Tooltip("Relative filename under Application.persistentDataPath")]
    public string defaultFileName = "units_save.json";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void HideGameViewObjects(AsteroidComponent asteroidToShow)
    {
        Debug.Log($"Hiding game-view units and asteroids.{asteroidToShow.asteroidName}");

        if (AsteroidManager.Instance != null && AsteroidManager.Instance.unitInOrbitListContent != null)
        {
            foreach (var unit in GameManager.Instance.unitsList.ToList())
            {
                if (unit == null) continue;
                if (unit.currentTileCoord == asteroidToShow.currentTileCoord)
                {
                    unit.transform.SetParent(GameManager.Instance.UnitsInOrbit, false);
                    unit.transform.localPosition = Vector3.zero;
                    unit.transform.localRotation = Quaternion.identity;
                    unit.gameObject.SetActive(true);
                    unit.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        foreach (var unit in GameManager.Instance.unitsList.ToList())
        {
            if (unit == null) continue;


            TileManager.Instance.UnregisterUnit(unit);

            GameManager.Instance.unitsList.Remove(unit);
            GameManager.Instance.playerUnits.Remove(unit);
            GameManager.Instance.enemyUnits.Remove(unit);

            if (unit.currentTileCoord != asteroidToShow.currentTileCoord)
            {
                if (unit.currentTileCoord == asteroidToShow.currentTileCoord)
                {
                    unit.transform.SetParent(GameManager.Instance.UnitsInOrbit, false);
                    unit.transform.localPosition = Vector3.zero;
                    unit.transform.localRotation = Quaternion.identity;
                    unit.gameObject.SetActive(true);
                    unit.transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    Destroy(unit.gameObject);
                }
            }

            foreach (var asteroid in GameManager.Instance.asteroidsList.ToList())
            {
                if (asteroid == null) continue;

                GameManager.Instance.asteroidsList.Remove(asteroid);

                if (asteroid.currentTileCoord == asteroidToShow.currentTileCoord)
                {
                    asteroid.transform.localPosition = Vector3.zero;
                    asteroid.transform.localRotation = Quaternion.identity;
                    asteroid.gameObject.SetActive(true);
                    asteroid.transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    Destroy(asteroid.gameObject);
                }
            }

            /*
            GameManager.Instance.unitsList.RemoveAll(u => u == null || u.currentTileCoord != asteroidToShow.currentTileCoord);
            GameManager.Instance.playerUnits.RemoveAll(u => u == null || u.currentTileCoord != asteroidToShow.currentTileCoord);
            GameManager.Instance.enemyUnits.RemoveAll(u => u == null || u.currentTileCoord != asteroidToShow.currentTileCoord);
            */

            GameManager.Instance.unitsList.Clear();
            GameManager.Instance.playerUnits.Clear();
            GameManager.Instance.enemyUnits.Clear();
            GameManager.Instance.asteroidsList.Clear();

            Debug.Log("Game-view units and asteroids hidden.");
        }
    }

    public void HideAsteroidViewObjects()
    {
        Debug.Log($"Hiding astroid-view units and asteroids.{AsteroidManager.Instance.currentAsteroidComponent.asteroidName}");

        var asteroidToKeep = AsteroidManager.Instance != null ? AsteroidManager.Instance.currentAsteroidComponent : null;

        if (asteroidToKeep != null)
        {
            foreach (var unit in GameManager.Instance.unitsList.ToList())
            {
                if (unit == null) continue;
                if (unit.currentTileCoord == asteroidToKeep.currentTileCoord)
                {
                    if (AsteroidManager.Instance != null && AsteroidManager.Instance.unitInOrbitListContent != null)
                    {
                        unit.transform.SetParent(AsteroidManager.Instance.unitInOrbitListContent, false);
                        unit.transform.localPosition = Vector3.zero;
                        unit.transform.localRotation = Quaternion.identity;
                        unit.gameObject.SetActive(true);
                    }
                }
            }
        }

        foreach (var unit in GameManager.Instance.unitsList.ToList())
        {
            if (unit == null) continue;
            if (asteroidToKeep != null && unit.currentTileCoord == asteroidToKeep.currentTileCoord)
            {
                continue;
            }

            TileManager.Instance.UnregisterUnit(unit);

            GameManager.Instance.unitsList.Remove(unit);
            GameManager.Instance.playerUnits.Remove(unit);
            GameManager.Instance.enemyUnits.Remove(unit);

            Destroy(unit.gameObject);
        }

        if (asteroidToKeep != null)
        {
            GameManager.Instance.unitsList.RemoveAll(u => u == null || u.currentTileCoord != asteroidToKeep.currentTileCoord);
            GameManager.Instance.playerUnits.RemoveAll(u => u == null || u.currentTileCoord != asteroidToKeep.currentTileCoord);
            GameManager.Instance.enemyUnits.RemoveAll(u => u == null || u.currentTileCoord != asteroidToKeep.currentTileCoord);
        }
        else
        {
            GameManager.Instance.unitsList.Clear();
            GameManager.Instance.playerUnits.Clear();
            GameManager.Instance.enemyUnits.Clear();
        }

        GameManager.Instance.asteroidsList.Clear();

        Debug.Log("Asteroid-view units and asteroids hidden.");
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
        if (string.IsNullOrWhiteSpace(asteroid.unitsOnSurfaceSave))
        {
            Debug.Log(
                $"LoadAsteroidViewObject: No saved units for asteroid " +
                $"{asteroid.asteroidName}. First visit.");
            return;
        }

        // SaveAllUnits writes to persistentDataPath, so load from there too.
        string path = Path.Combine(
            Application.persistentDataPath,
            asteroid.unitsOnSurfaceSave);

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
                Debug.Log("Spawned child parent is: " + spawned.transform.parent?.name);
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

    /// <summary>
    /// /////////////////////////  UNITS
    /// </summary>

    [Serializable]
    public class UnitSaveData
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
    public void SaveAllUnits(string fileName = null, AsteroidComponent asteroidToExclude = null)
    {
        var wrapper = new UnitsSaveWrapper();

        foreach (var u in GameManager.Instance.unitsList)
        {
            if (u == null)
                continue;

            if (asteroidToExclude != null && u.currentTileCoord == asteroidToExclude.currentTileCoord)
                continue;

            if (string.IsNullOrEmpty(u.unitId))
                u.unitId = Guid.NewGuid().ToString();

            var modulesSave = new List<ItemInstanceSaveData>();

            if (u.moduleRuntimeList != null)
            {
                foreach (var mod in u.moduleRuntimeList)
                {
                    if (mod == null)
                    {
                        modulesSave.Add(new ItemInstanceSaveData
                        {
                            itemId = null,
                            amount = 0,
                            currentHealth = 0,
                            currentPowerUpBoost = 1,
                            nextTurnPowerUpboost = 1,
                            isOnline = false,
                            isBroken = false,
                            isDestroyed = false
                        });
                        continue;
                    }

                    modulesSave.Add(new ItemInstanceSaveData
                    {
                        itemId = mod.ItemDefinition != null ? mod.ItemDefinition.itemId : null,
                        amount = 1,
                        currentHealth = mod.currentHealth,
                        currentPowerUpBoost = mod.currentPowerUpBoost,
                        nextTurnPowerUpboost = mod.nextTurnPowerUpboost,
                        isOnline = mod.isOnline,
                        isBroken = mod.isBroken,
                        isDestroyed = mod.isDestroyd
                    });
                }
            }

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
                currentMovePath = u.currentMovePath != null && u.currentMovePath.Count > 0
                    ? new List<Vector2Int>(u.currentMovePath)
                    : new List<Vector2Int>(),
                moduleRuntimeList = modulesSave
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

                GameObject go = Instantiate(unitPrefab, unitFolderForAllUnits);
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
                        instance.currentPowerUpBoost = m.currentPowerUpBoost;
                        instance.nextTurnPowerUpboost = m.nextTurnPowerUpboost;
                        instance.isOnline = m.isOnline;
                        instance.isBroken = m.isBroken;
                        instance.isDestroyd = m.isDestroyed;
                    }
                    else
                    {
                        instance.currentHealth = 0;
                        instance.currentMaxHealth = 0;
                        instance.currentPowerUpBoost = 1;
                        instance.nextTurnPowerUpboost = 1;
                        instance.isOnline = false;
                        instance.isBroken = false;
                        instance.isDestroyd = false;
                    }

                    // Always add an ItemInstance to preserve slot index
                    runtime.moduleRuntimeList.Add(instance);
                }
            }

            // Init the new unit (e.g., update visuals, health bars, etc.)
            StartCoroutine(runtime.InitUnit(false)); //To register for FOG OF WAR to work.
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

    private void ApplySavedUnitData(Unit runtimeUnit, UnitSaveData savedUnit)
    {
        if (runtimeUnit == null || savedUnit == null)
            return;

        runtimeUnit.unitName = savedUnit.unitName;
        runtimeUnit.unitType = savedUnit.unitType;
        runtimeUnit.shipType = savedUnit.shipType;
        runtimeUnit.currentTileCoord = savedUnit.currentTileCoord;
        runtimeUnit.movedThisTurn = savedUnit.movedThisTurn;
        runtimeUnit.isPlayerControlled = savedUnit.isPlayerControlled;
        runtimeUnit.haveAttackedThisTurn = savedUnit.haveAttackedThisTurn;
        runtimeUnit.followTarget = savedUnit.followtarget;
        runtimeUnit.hasMinedThisTurn = savedUnit.hasMinedThisTurn;

        runtimeUnit.currentMovePath = savedUnit.currentMovePath != null
            ? new List<Vector2Int>(savedUnit.currentMovePath)
            : new List<Vector2Int>();

        if (savedUnit.target_lastKnownTileCoord != Vector2Int.zero && TileManager.Instance != null)
        {
            runtimeUnit.target_LastKnownPos = TileManager.Instance.AxialToWorldCenter(savedUnit.target_lastKnownTileCoord);
        }

        runtimeUnit.moduleRuntimeList = new List<ItemInstance>();

        if (savedUnit.moduleRuntimeList == null)
            return;

        foreach (var savedModule in savedUnit.moduleRuntimeList)
        {
            var module = new ItemInstance();

            if (savedModule != null && !string.IsNullOrWhiteSpace(savedModule.itemId))
            {
                module.ItemDefinition = ResolveSavedItemDefinition(savedModule.itemId);
            }

            if (savedModule != null)
            {
                module.currentHealth = savedModule.currentHealth;
                module.currentPowerUpBoost = savedModule.currentPowerUpBoost;
                module.nextTurnPowerUpboost = savedModule.nextTurnPowerUpboost;
                module.isOnline = savedModule.isOnline;
                module.isBroken = savedModule.isBroken;
                module.isDestroyd = savedModule.isDestroyed;

                // Keep these defaults if they were not saved, but do not replace real values with false/1.
                if (module.currentPowerUpBoost <= 0)
                    module.currentPowerUpBoost = 1;
                if (module.nextTurnPowerUpboost <= 0)
                    module.nextTurnPowerUpboost = 1;
            }
            else
            {
                module.currentPowerUpBoost = 1;
                module.nextTurnPowerUpboost = 1;
                module.isOnline = false;
                module.isBroken = false;
                module.isDestroyd = false;
            }

            runtimeUnit.moduleRuntimeList.Add(module);
        }
    }
    public ItemDefinition ResolveSavedItemDefinition(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (ItemDatabase.Instance != null)
        {
            var dbDef = ItemDatabase.Instance.GetById(itemId);
            if (dbDef != null)
                return dbDef;
        }

        // Important: do NOT restrict to a single folder like "ItemDefinitions".
        // Your modules live under Resources/Modules, Resources/Ships, etc.
        var allDefs = Resources.LoadAll<ItemDefinition>("");
        foreach (var def in allDefs)
        {
            if (def != null && def.itemId == itemId)
                return def;
        }

        return null;
    }
    // Used in asteroid view to load a saved unit by its unitId. If a runtime instance already exists, it returns that instance. Otherwise, it instantiates a new unit from the prefab and applies the saved data.
    public Unit LoadSavedUnitById(string unitId, string saveFileName = "units_save.json", Vector2Int axialcoord = default(Vector2Int))
    {
        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogWarning("LoadSavedUnitById: unitId is empty.");
            return null;
        }

        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"LoadSavedUnitById: file not found -> {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        UnitsSaveWrapper wrapper = JsonUtility.FromJson<UnitsSaveWrapper>(json);

        if (wrapper == null || wrapper.units == null || wrapper.units.Count == 0)
        {
            Debug.LogWarning("LoadSavedUnitById: no unit data in save file.");
            return null;
        }

        UnitSaveData savedUnit = wrapper.units
            .FirstOrDefault(u => u != null && u.unitId == unitId);

        if (savedUnit == null)
        {
            Debug.LogWarning($"LoadSavedUnitById: no saved unit found for unitId={unitId}");
            return null;
        }

        // 1) Check if a runtime instance already exists
        Unit existingUnit = GameManager.Instance != null
            ? GameManager.Instance.unitsList.FirstOrDefault(u =>
                u != null && u.unitId == savedUnit.unitId)
            : null;

        if (existingUnit != null)
            return existingUnit;

        // 2) Instantiate a new unit from prefab
        if (unitPrefab == null)
        {
            Debug.LogWarning($"LoadSavedUnitById: unitPrefab is null for unitId={unitId}");
            return null;
        }

        GameObject obj = Instantiate(unitPrefab, unitFolderForAllUnits);
        Unit runtimeUnit = obj.GetComponent<Unit>();

        if (runtimeUnit == null)
        {
            Debug.LogWarning($"LoadSavedUnitById: prefab does not contain a Unit component. unitId={unitId}");
            Destroy(obj);
            return null;
        }

        // 3) Apply saved data
        runtimeUnit.unitId = savedUnit.unitId;
        runtimeUnit.unitName = savedUnit.unitName;
        runtimeUnit.unitType = savedUnit.unitType;
        runtimeUnit.shipType = savedUnit.shipType;
        runtimeUnit.currentTileCoord = savedUnit.currentTileCoord;
        runtimeUnit.movedThisTurn = savedUnit.movedThisTurn;
        runtimeUnit.isPlayerControlled = savedUnit.isPlayerControlled;
        runtimeUnit.haveAttackedThisTurn = savedUnit.haveAttackedThisTurn;
        runtimeUnit.followTarget = savedUnit.followtarget;
        runtimeUnit.hasMinedThisTurn = savedUnit.hasMinedThisTurn;
        runtimeUnit.currentMovePath = savedUnit.currentMovePath != null
            ? new List<Vector2Int>(savedUnit.currentMovePath)
            : new List<Vector2Int>();

        if (savedUnit.target_lastKnownTileCoord != Vector2Int.zero && TileManager.Instance != null)
        {
            runtimeUnit.target_LastKnownPos = TileManager.Instance.AxialToWorldCenter(savedUnit.target_lastKnownTileCoord);
        }

        // Restore modules
        runtimeUnit.moduleRuntimeList = new List<ItemInstance>();

        if (savedUnit.moduleRuntimeList != null)
        {
            foreach (var savedModule in savedUnit.moduleRuntimeList)
            {
                var item = new ItemInstance();

                if (savedModule != null && !string.IsNullOrWhiteSpace(savedModule.itemId))
                {
                    ItemDefinition def = null;

                    if (ItemDatabase.Instance != null)
                    {
                        def = ItemDatabase.Instance.GetById(savedModule.itemId);
                    }

                    if (def == null)
                    {
                        def = Resources.LoadAll<ItemDefinition>("ItemDefinitions")
                            .FirstOrDefault(x => x != null && x.itemId == savedModule.itemId);
                    }

                    item.ItemDefinition = def;
                }

                if (savedModule != null)
                {
                    item.currentHealth = savedModule.currentHealth;
                    item.currentPowerUpBoost = savedModule.currentPowerUpBoost;
                    item.nextTurnPowerUpboost = savedModule.nextTurnPowerUpboost;
                    item.isOnline = savedModule.isOnline;
                    item.isBroken = savedModule.isBroken;
                    item.isDestroyd = savedModule.isDestroyed;
                }

                runtimeUnit.moduleRuntimeList.Add(item);
            }
        }

        // 4) Place it in world
        if (TileManager.Instance != null)
        {
            runtimeUnit.transform.position = TileManager.Instance.AxialToWorldCenter(axialcoord);
        }
        runtimeUnit.currentTileCoord = savedUnit.currentTileCoord;

        // 5) add it to manager lists
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.unitsList.Contains(runtimeUnit))
                GameManager.Instance.unitsList.Add(runtimeUnit);

            if (runtimeUnit.isPlayerControlled && !GameManager.Instance.playerUnits.Contains(runtimeUnit))
                GameManager.Instance.playerUnits.Add(runtimeUnit);
            else if (!runtimeUnit.isPlayerControlled && !GameManager.Instance.enemyUnits.Contains(runtimeUnit))
                GameManager.Instance.enemyUnits.Add(runtimeUnit);
        }

        // 6) put it back into scene
        runtimeUnit.gameObject.SetActive(true);

        // 7) if this unit needs fog registration / visual init, do it
        if (TileManager.Instance != null)
        {
            StartCoroutine(runtimeUnit.InitUnit(false));
        }

        return runtimeUnit;
    }

    // Just to get the saved data NOT THE UNIT script.
    public UnitSaveData GetUnitSaveDataById(string unitId, string fileToLookIn)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            Debug.LogWarning("GetUnitScriptById: unitId is empty.");
            return null;
        }

        string path = Path.Combine(Application.persistentDataPath, fileToLookIn);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"GetUnitScriptById: file not found -> {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        UnitsSaveWrapper wrapper = JsonUtility.FromJson<UnitsSaveWrapper>(json);

        if (wrapper == null || wrapper.units == null || wrapper.units.Count == 0)
        {
            Debug.LogWarning("GetUnitScriptById: no unit data in save file.");
            return null;
        }

        UnitSaveData savedUnit = wrapper.units
            .FirstOrDefault(u => u != null && u.unitId == unitId);

        if (savedUnit == null)
        {
            Debug.LogWarning($"LoadSavedUnitById: no saved unit found for unitId={unitId}");
            return null;
        }

        return savedUnit;
    }
    /// <summary>
    /// /////////////////////////  ASTEROIDS
    /// </summary>

    [Serializable]
    private class AsteroidSaveData
    {
        public string asteroidID;           // GUID for this unit in the save file
        public string asteroidName;
        public Vector2Int currentTileCoord;
        public string asteroidTileSatusSave;
        public string unitsOnSurfaceSave; // Filename for saved units on the asteroid surface
        public string asteroidSpritePath; // Reference to the asteroid's sprite (if needed)
    }

    [Serializable]
    private class AsteroidSaveWrapper
    {
        public List<AsteroidSaveData> asteroids = new List<AsteroidSaveData>();
    }

    //Helper to below SaveAllAsteroids to get the resource path of a sprite. This is useful for saving the sprite reference in a way that can be loaded later.
    private string GetResourcePath(Sprite sprite)
    {
        if (sprite == null) return string.Empty;

#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(sprite);
        if (!string.IsNullOrEmpty(assetPath))
        {
            // If sprite is inside Assets/Resources, convert to Resources-relative path
            const string resourcesFolder = "Assets/Resources/";
            if (assetPath.StartsWith(resourcesFolder))
            {
                return assetPath.Substring(resourcesFolder.Length).Replace(".png", "").Replace(".jpg", "");
            }

            return assetPath;
        }
#endif

        // Runtime fallback: if you already know the resource path
        return sprite.name;
    }

    public void SaveAllAsteroids(string fileName = "asteroids_save.json")
    {
        var wrapper = new AsteroidSaveWrapper();

        foreach (var a in GameManager.Instance.asteroidsList)
        {
            if (a == null) continue;

            if(a == AsteroidManager.Instance.currentAsteroidComponent) continue; // Skip the AsteroidTo view.

            // Ensure asteroid has a stable id (generate once if missing)
            if (string.IsNullOrEmpty(a.asteroidID))
            {
                a.asteroidID = Guid.NewGuid().ToString();
            }

            var save = new AsteroidSaveData
            {
                asteroidID = a.asteroidID,
                asteroidName = a.asteroidName,
                currentTileCoord = a.currentTileCoord,
                asteroidTileSatusSave = a.asteroidTileSatusSave,
                unitsOnSurfaceSave = a.unitsOnSurfaceSave,

                asteroidSpritePath = a.asteroidIcon != null
                ? GetResourcePath(a.asteroidIcon)
                : ""
            };

            wrapper.asteroids.Add(save);
        }

        string fname = string.IsNullOrEmpty(fileName) ? defaultFileName : fileName;
        string path = Path.Combine(Application.persistentDataPath, fname);
        try
        {
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(path, json);
            Debug.Log($"SaveManager: Saved {wrapper.asteroids.Count} asteroids to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to save asteroids to {path}: {ex.Message}");
        }
    }

    public void LoadAllAsteroids(string fileName = null)
    {
        string fname = string.IsNullOrEmpty(fileName) ? defaultFileName : fileName;
        string path = Path.Combine(Application.persistentDataPath, fname);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"SaveManager.LoadAllUnits: file not found: {path}");
            return;
        }

        StartCoroutine(LoadAsteroidsRoutine(path));
    }

    private IEnumerator LoadAsteroidsRoutine(string path)
    {
        // Wait until managers and tile lookup are ready
        yield return new WaitUntil(() => GameManager.Instance != null && TileManager.Instance != null && TileManager.Instance.HasBuiltLookup);

        AsteroidSaveWrapper wrapper = null;
        try
        {
            string json = File.ReadAllText(path);
            wrapper = JsonUtility.FromJson<AsteroidSaveWrapper>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveManager: Failed to read/parse save file: {ex.Message}");
            yield break;
        }

        if (wrapper == null || wrapper.asteroids == null)
        {
            Debug.LogWarning("SaveManager: Save file contained no asteroid data.");
            yield break;
        }


        // Build map of saved GUID -> saved data
        var savedGuidToData = wrapper.asteroids.ToDictionary(s => s.asteroidID, s => s);

        // Build runtime map GUID -> Unit by matching saved entries to existing runtime units (prefer unitId).
        var guidToRuntimeUnit = new Dictionary<string, AsteroidComponent>();
        foreach (var saved in wrapper.asteroids)
        {
            if (saved == null) continue;

            AsteroidComponent asteroid = GameManager.Instance.asteroidsList
                .FirstOrDefault(a => a != null && a.asteroidID == saved.asteroidID);

            if (asteroid == null)
            {
                if (asteroidPrefab == null)
                {
                    Debug.LogWarning("Missing asteroidPrefab");
                    continue;
                }

                GameObject go = Instantiate(asteroidPrefab, asteroidFolderForAllAsteroids);
                asteroid = go.GetComponent<AsteroidComponent>();

                if (asteroid == null)
                {
                    Debug.LogWarning("Prefab has no AsteroidComponent");
                    Destroy(go);
                    continue;
                }

                asteroid.asteroidID = saved.asteroidID;
                GameManager.Instance.asteroidsList.Add(asteroid);
            }

            ApplySavedAsteroidData(asteroid, saved);
        }



        //A check to see that the saved GUID is the same as the runtime GUID. If not, it will log a warning.
        Debug.Log("Saved asteroids:");
        foreach (var s in wrapper.asteroids)
        {
            Debug.Log($"saved asteroidId={s.asteroidID}, name={s.asteroidName}");
        }

        Debug.Log("Runtime asteroids:");
        foreach (var u in GameManager.Instance.asteroidsList)
        {
            if (u != null)
                Debug.Log($"runtime asteroidId={u.asteroidID}, name={u.asteroidName}");
        }



        Debug.Log($"SaveManager: Loaded {guidToRuntimeUnit.Count} asteroid(s) asteroid reference(s) from {path}");
    }

    private void ApplySavedAsteroidData(
        AsteroidComponent runtimeAsteroid,
        AsteroidSaveData savedAsteroid)
    {
        runtimeAsteroid.asteroidID = savedAsteroid.asteroidID;
        runtimeAsteroid.asteroidName = savedAsteroid.asteroidName;
        runtimeAsteroid.currentTileCoord = savedAsteroid.currentTileCoord;
        runtimeAsteroid.asteroidTileSatusSave = savedAsteroid.asteroidTileSatusSave;
        runtimeAsteroid.unitsOnSurfaceSave = savedAsteroid.unitsOnSurfaceSave;
    }


    // ////////////////////// Inventory Save/Load //////////////////////

    [System.Serializable]
    public class ItemInstanceSaveData
    {
        public string itemId;          // reference to ItemDefinition.itemId
        public int amount;             // stack count
        public int currentHealth;
        public int currentPowerUpBoost;
        public int nextTurnPowerUpboost;
        public bool isOnline;
        public bool isBroken;
        public bool isDestroyed;
        // add any other runtime fields you need to persist
    }

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
