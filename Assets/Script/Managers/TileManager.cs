using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static HexGrid_TilePrefab;

// TileManager: authoritative runtime tile API (highlighting, pathfinding, markers, doors/walls).
public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("Tile scene")]
    public Transform tilesParent;                       // parent that holds tile GameObjects (HexGrid_TilePrefab children)
    public float tileSize = 1f;                         // same size used by editor/HexMath conversions
    public float nearestTileSearchRadius = 0.5f;        // fallback nearest search radius in world units
    public bool autoBuildLookupOnStart = true;

    [Header("Path/marker visuals")]
    public Material markerMaterial;                     // optional material for marker mesh/text
    public Vector3 markerOffset = new Vector3(0f, 0.08f, 0f);

    // Runtime structures
    private readonly Dictionary<Vector2Int, HexGrid_TilePrefab> tileLookup = new();
    private readonly Dictionary<(Vector2Int a, Vector2Int b), DoorComponent> doorLookup = new();
    private readonly List<HexGrid_TilePrefab> activeHighlights = new();
    private readonly List<GameObject> markerPool = new();
    private readonly List<GameObject> activeMarkers = new();

    // Tracks how many units currently see each coord
    private readonly Dictionary<Vector2Int, int> visibilityRefCount = new();

    // For each registered unit, the set of coords it currently sees
    private readonly Dictionary<Unit, HashSet<Vector2Int>> unitSeenTiles = new();

    // Large radius used for HexesInRangeAxial (avoid unintended clamp)
    private const int largeWorldRadius = 10000;

    // Path visuals
    private LineRenderer pathLineRenderer;
    public List<Vector2Int> LastPath { get; private set; } = new();

    //To be abel to initunit in unit script wait unitll tileLookup is built yet.
    public bool HasBuiltLookup => tileLookup != null && tileLookup.Count > 0;
    [Serializable]
    private class FogCell { public int x; public int y; public string state; }
    [Serializable]
    private class FogSave { public string layoutName; public List<FogCell> cells = new(); }

    public void SaveFogState(string layoutName)
    {
        var save = new FogSave { layoutName = layoutName };
        foreach (var kv in tileLookup)
        {
            var coord = kv.Key;
            var tile = kv.Value;
            save.cells.Add(new FogCell { x = coord.x, y = coord.y, state = tile.tileVisibilityState.ToString() });
        }
        var path = Path.Combine(Application.persistentDataPath, $"fog_{layoutName}.json");
        File.WriteAllText(path, JsonUtility.ToJson(save));
    }

    public void LoadFogState(string layoutName)
    {
        var path = Path.Combine(Application.persistentDataPath, $"fog_{layoutName}.json");
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var save = JsonUtility.FromJson<FogSave>(json);
        // Apply saved states to tiles (sets visuals immediately)
        foreach (var c in save.cells)
        {
            var coord = new Vector2Int(c.x, c.y);
            if (tileLookup.TryGetValue(coord, out var tile))
            {
                if (Enum.TryParse<TileVisibilityState>(c.state, out var s))
                {
                    tile.tileVisibilityState = s;
                    tile.ApplyVisibilityVisuals();
                }
            }
        }
        // Clear runtime refcounts — they will be rebuilt by RegisterUnit calls
        visibilityRefCount.Clear();
        unitSeenTiles.Clear();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoBuildLookupOnStart) BuildTileLookup();
        EnsureLineRenderer();
        BuildDoorLookup();
    }

    // ---- FOG OF WAR / VISIBILITY -------------------------------------------------

    // Register unit (compute initial seen set)
    public void RegisterUnit(Unit u, int range)
    {
        if (u == null || unitSeenTiles.ContainsKey(u)) return;
        var center = WorldToAxial(u.transform.position);
        var seen = new HashSet<Vector2Int>(HexMath.HexesInRangeAxial(center, range, largeWorldRadius));
        unitSeenTiles[u] = seen;
        foreach (var coord in seen) IncrementVisibilityRef(coord);
    }

    // Unregister unit (remove its contributions)
    public void UnregisterUnit(Unit u)
    {
        if (u == null) return;
        if (!unitSeenTiles.TryGetValue(u, out var seen)) return;
        foreach (var coord in seen) DecrementVisibilityRef(coord);
        unitSeenTiles.Remove(u);
    }

    // Call when unit has moved; supply new center in axial coords
    public void UpdateUnitVisibilityOnMoveFromStored(Unit u, Vector2Int newCenter, int range)
    {
        if (u == null) return;
        unitSeenTiles.TryGetValue(u, out var oldSet);
        oldSet ??= new HashSet<Vector2Int>();
        var newSet = new HashSet<Vector2Int>(HexMath.HexesInRangeAxial(newCenter, range, largeWorldRadius));

        foreach (var coord in oldSet)
            if (!newSet.Contains(coord)) DecrementVisibilityRef(coord);
        foreach (var coord in newSet)
            if (!oldSet.Contains(coord)) IncrementVisibilityRef(coord);

        unitSeenTiles[u] = newSet;
    }

    private void IncrementVisibilityRef(Vector2Int coord)
    {
        if (!tileLookup.ContainsKey(coord)) return;
        visibilityRefCount.TryGetValue(coord, out var v);
        visibilityRefCount[coord] = v + 1;
        if (tileLookup.TryGetValue(coord, out var tile) && tile.tileVisibilityState != TileVisibilityState.Visible)
        {
            tile.tileVisibilityState = TileVisibilityState.Visible;
            tile.ApplyVisibilityVisuals();
        }
    }

    private void DecrementVisibilityRef(Vector2Int coord)
    {
        if (!tileLookup.ContainsKey(coord)) return;
        if (!visibilityRefCount.TryGetValue(coord, out var v) || v <= 0) return;
        v--;
        if (v <= 0)
        {
            visibilityRefCount.Remove(coord);
            if (tileLookup.TryGetValue(coord, out var tile) && tile.tileVisibilityState == TileVisibilityState.Visible)
            {
                tile.tileVisibilityState = TileVisibilityState.Explored;
                tile.ApplyVisibilityVisuals();
            }
        }
        else visibilityRefCount[coord] = v;
    }

    // --- Lookup builders -------------------------------------------------

    // Build or rebuild the coordinate -> tile component lookup.
    // Convention: coord.x = tileIndexCol, coord.y = tileIndexRow
    public void BuildTileLookup()
    {
        tileLookup.Clear();
        if (tilesParent == null) return;

        foreach (Transform t in tilesParent)
        {
            var comp = t.GetComponent<HexGrid_TilePrefab>();
            if (comp == null) continue;
            var coord = new Vector2Int(comp.tileIndexCol, comp.tileIndexRow);
            tileLookup[coord] = comp;
        }
    }

    // Build door lookup from DoorComponent instances in the scene.
    public void BuildDoorLookup()
    {
        doorLookup.Clear();
        var doors = FindObjectsOfType<DoorComponent>();
        foreach (var door in doors)
        {
            var key = NormalizePair(door.tileA, door.tileB);
            doorLookup[key] = door;
        }
    }

    // Helper to normalize unordered pair
    private static (Vector2Int a, Vector2Int b) NormalizePair(Vector2Int x, Vector2Int y)
    {
        if (HexMath.IsLexSmaller(x, y)) return (x, y);
        return (y, x);
    }

    // --- Tile lookup / helpers ------------------------------------------

    // Get the HexGrid_TilePrefab under the world position. Uses nearest child search (no physics).
    public HexGrid_TilePrefab GetTileFromWorldPosition(Vector3 worldPosition)
    {
        if (tilesParent == null) return null;

        Vector2 world2 = new Vector2(worldPosition.x, worldPosition.y);
        float bestDist = float.MaxValue;
        HexGrid_TilePrefab best = null;

        foreach (Transform t in tilesParent)
        {
            float d = Vector2.SqrMagnitude((Vector2)t.position - world2);
            if (d < bestDist && d <= nearestTileSearchRadius * nearestTileSearchRadius)
            {
                var comp = t.GetComponent<HexGrid_TilePrefab>();
                if (comp != null)
                {
                    bestDist = d;
                    best = comp;
                }
            }
        }

        return best;
    }

    // Convert world position to axial coord using HexMath helper (point-top).
    public Vector2Int WorldToAxial(Vector3 worldPos)
        => HexMath.WorldToAxial_PointTop(new Vector2(worldPos.x, worldPos.y), tileSize);

    // Convert axial coord to world center
    public Vector2 AxialToWorldCenter(Vector2Int axial)
        => HexMath.AxialToWorldCenter_PointTop(axial, tileSize);

    // --- Walls / doors / passability -----------------------------------

    // Check whether movement from `from` to adjacent `to` is allowed (walls and doors considered).
    public bool CanPass(Vector2Int from, Vector2Int to)
    {
        if (!tileLookup.TryGetValue(from, out var fromTile)) return false;
        if (!tileLookup.TryGetValue(to, out var toTile)) return false;

        var dirs = HexMath.NeighborDirs();
        int dirIndex = -1;
        for (int i = 0; i < dirs.Length; i++)
            if (from + dirs[i] == to) { dirIndex = i; break; }
        if (dirIndex == -1) return false; // not neighbors

        // wall on from side?
        if (fromTile.HasWall(dirIndex)) return false;
        // reciprocal wall on to side?
        int opposite = (dirIndex + 3) % 6;
        if (toTile.HasWall(opposite)) return false;

        // door between tiles
        var key = NormalizePair(from, to);
        if (doorLookup.TryGetValue(key, out var door))
        {
            if (!door.isOpen) return false;
        }

        return true;
    }

    // Toggle or set a door state between two tile coords.
    public bool SetDoorState(Vector2Int a, Vector2Int b, bool open)
    {
        var key = NormalizePair(a, b);
        if (doorLookup.TryGetValue(key, out var door))
        {
            door.isOpen = open;
            return true;
        }
        return false;
    }

    // --- Range / reachability (BFS/Dijkstra) ----------------------------

    // Return all reachable axial coords (including start) within integer movementBudget.
    // Uses CanPass for neighbor validity; uniform cost = 1 per move.
    public List<Vector2Int> GetReachableTiles(Vector2Int start, int movementBudget)
    {
        var results = new List<Vector2Int>();
        if (movementBudget < 0) return results;
        if (!tileLookup.ContainsKey(start)) return results;

        var bestCost = new Dictionary<Vector2Int, int> { [start] = 0 };
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        var dirs = HexMath.NeighborDirs();

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int costSoFar = bestCost[cur];
            if (costSoFar > movementBudget) continue;

            results.Add(cur);

            foreach (var d in dirs)
            {
                var nb = cur + d;
                if (!tileLookup.ContainsKey(nb)) continue;
                if (!CanPass(cur, nb)) continue;
                int newCost = costSoFar + 1;
                if (newCost > movementBudget) continue;
                if (!bestCost.TryGetValue(nb, out var prev) || newCost < prev)
                {
                    bestCost[nb] = newCost;
                    q.Enqueue(nb);
                }
            }
        }

        return results;
    }

    // --- Pathfinding A* over tile graph --------------------------------

    // Heuristic: axial (cube) distance
    public int AxialDistance(Vector2Int a, Vector2Int b)
    {
        int ax = a.x, az = a.y, ay = -ax - az;
        int bx = b.x, bz = b.y, by = -bx - bz;
        return Math.Max(Math.Abs(ax - bx), Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
    }

    // Find path from start to target using A*; returns list of axial coords (start..target) or empty list on failure.
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, int maxSearch = 10000)
    {
        var path = new List<Vector2Int>();
        if (!tileLookup.ContainsKey(start) || !tileLookup.ContainsKey(target)) return path;
        if (start == target) { path.Add(start); return path; }

        var openSet = new SimplePriorityQueue();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = AxialDistance(start, target) };

        openSet.Push(start, fScore[start]);
        int iterations = 0;
        var dirs = HexMath.NeighborDirs();

        while (openSet.Count > 0 && iterations++ < maxSearch)
        {
            var current = openSet.Pop();
            if (current == target)
            {
                // Reconstruct path
                var cur = current;
                var rev = new List<Vector2Int> { cur };
                while (cameFrom.TryGetValue(cur, out var prev))
                {
                    rev.Add(prev);
                    cur = prev;
                }
                rev.Reverse();
                path.AddRange(rev);
                LastPath = new List<Vector2Int>(path);
                return path;
            }

            foreach (var d in dirs)
            {
                var nb = current + d;
                if (!tileLookup.ContainsKey(nb)) continue;
                if (!CanPass(current, nb)) continue;

                int tentativeG = gScore[current] + 1;
                if (!gScore.TryGetValue(nb, out var oldG) || tentativeG < oldG)
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentativeG;
                    int h = AxialDistance(nb, target);
                    fScore[nb] = tentativeG + h;
                    if (!openSet.Contains(nb))
                        openSet.Push(nb, fScore[nb]);
                    else
                        openSet.UpdatePriority(nb, fScore[nb]);
                }
            }
        }

        // No path
        LastPath = new List<Vector2Int>();
        return path;
    }

    // --- Path visuals: line + markers ---------------------------------

    // Ensure a LineRenderer exists for path drawing
    private void EnsureLineRenderer()
    {
        if (pathLineRenderer == null)
        {
            pathLineRenderer = GetComponent<LineRenderer>();
            if (pathLineRenderer == null)
                pathLineRenderer = gameObject.AddComponent<LineRenderer>();
            // Basic setup (tune in inspector if needed)
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.useWorldSpace = true;
            pathLineRenderer.loop = false;
            pathLineRenderer.widthCurve = AnimationCurve.Constant(0, 1, 0.06f);
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.startColor = Color.yellow;
            pathLineRenderer.endColor = Color.yellow;
            pathLineRenderer.numCapVertices = 2;
            pathLineRenderer.numCornerVertices = 2;
        }
    }

    // Draw a line strip along axial path (centers). Stores LastPath.
    public void DrawLineStrip(List<Vector2Int> path)
    {
        LastPath = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
        EnsureLineRenderer();

        if (path == null || path.Count == 0)
        {
            pathLineRenderer.positionCount = 0;
            return;
        }

        pathLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            var c = AxialToWorldCenter(path[i]);
            pathLineRenderer.SetPosition(i, new Vector3(c.x, c.y, 0f));
        }
    }

    // Place numeric turn markers along a path using pooled TextMesh gameobjects.
    // movementPerTurn controls how many tiles fit into one turn (e.g., movement range).
    public void PlaceTurnMarkers(List<Vector2Int> path, int movementPerTurn)
    {
        ClearMarkers();
        if (path == null || path.Count == 0 || movementPerTurn <= 0) return;

        int step = 0;
        int turn = 1;
        for (int i = 0; i < path.Count; i++)
        {
            if (i == 0) continue; // skip start marker (optional)
            step++;
            if (step >= movementPerTurn || i == path.Count - 1)
            {
                var coord = path[i];
                var tile = GetTileAtCoord(coord);
                if (tile != null)
                {
                    var marker = AcquireMarker();
                    var center = AxialToWorldCenter(coord);
                    marker.transform.position = new Vector3(center.x, center.y, 0f) + markerOffset;
                    marker.SetActive(true);
                    // set text
                    var tm = marker.GetComponent<TextMesh>();
                    if (tm != null) tm.text = turn.ToString();
                    activeMarkers.Add(marker);
                }
                step = 0;
                turn++;
            }
        }
    }

    // Clear path visuals and markers
    public void ClearPath()
    {
        LastPath = new List<Vector2Int>();
        if (pathLineRenderer != null) pathLineRenderer.positionCount = 0;
        ClearMarkers();
    }

    // --- Marker pooling ------------------------------------------------

    private GameObject AcquireMarker()
    {
        if (markerPool.Count > 0)
        {
            var go = markerPool[markerPool.Count - 1];
            markerPool.RemoveAt(markerPool.Count - 1);
            return go;
        }

        var obj = new GameObject("TileMarker");
        obj.transform.SetParent(transform, false);
        var tm = obj.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.08f;
        tm.fontSize = 64;
        tm.color = Color.white;
        if (markerMaterial != null)
        {
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = markerMaterial;
        }
        obj.SetActive(false);
        return obj;
    }

    private void ClearMarkers()
    {
        foreach (var m in activeMarkers)
        {
            if (m == null) continue;
            m.SetActive(false);
            markerPool.Add(m);
        }
        activeMarkers.Clear();
    }

    // --- Highlighting --------------------------------------------------

    // Highlight a set of axial coords by toggling each tile's highlight child via HexGrid_TilePrefab.SetHighlight(true)
    public void HighlightCoords(IEnumerable<Vector2Int> coords, Color highlightColor)
    {
        ClearHighlights();
        if (coords == null) return;
        foreach (var a in coords)
        {
            if (tileLookup.TryGetValue(a, out var tile))
            {
                tile.SetHighlight(true, highlightColor);
                activeHighlights.Add(tile);
            }
        }
    }

    public void ClearHighlights()
    {
        for (int i = activeHighlights.Count - 1; i >= 0; i--)
        {
            var tile = activeHighlights[i];
            if (tile != null) tile.SetHighlight(false, Color.clear);
        }
        activeHighlights.Clear();
    }

    // Helper: get tile component by coord
    public HexGrid_TilePrefab GetTileAtCoord(Vector2Int coord)
    {
        tileLookup.TryGetValue(coord, out var tile);
        return tile;
    }

    // --- Highlighting When player click a tile --------------------------------------------------

    //Highlight tile att screenpos and unhide all other tiles when player click a tile.
    public void HighlightTileWorldPosition(Vector3 worldPosition)
    {
        // Convert screen position to world position
        //Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
        // Get the tile coordinate from the world position

        // select unit via selection service
        var hexClicked = TileManager.Instance.GetTileFromWorldPosition(worldPosition);
        if (hexClicked != null)
        {
            var hexcoord = new Vector2Int(hexClicked.tileIndexRow, hexClicked.tileIndexCol);

            foreach (Transform tileTransform in tilesParent)
            {
                var tileComp = tileTransform.GetComponent<HexGrid_TilePrefab>();
                if (tileComp != null)
                {
                    if (tileComp.tileIndexRow == hexcoord.x && tileComp.tileIndexCol == hexcoord.y)
                    {
                        tileComp.SetHighlight(true, ColorManager.Instance.hex_Select_Empty);
                    }
                    else
                    {
                        tileComp.SetHighlight(false, Color.clear);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No tile found under the given screen position.");
        }

    }

    //Used for when player click a empty tile.
    public void ClearHighlightedTiles()
    {
        foreach (Transform tileTransform in tilesParent)
        {
            var tileComp = tileTransform.GetComponent<HexGrid_TilePrefab>();
            if (tileComp != null)
            {
                tileComp.SetHighlight(false, Color.clear);
            }
        }
    }

    // --- Utilities -----------------------------------------------------

    // Acquire axial centers for debug / external use
    public Vector3[] PathToWorldPositions(IEnumerable<Vector2Int> path)
    {
        if (path == null) return Array.Empty<Vector3>();
        return path.Select(a =>
        {
            var v = AxialToWorldCenter(a);
            return new Vector3(v.x, v.y, 0f);
        }).ToArray();
    }

    // --- Small priority queue for A* ----------------------------------
    private class SimplePriorityQueue
    {
        private readonly List<(Vector2Int key, int prio)> data = new();
        public int Count => data.Count;
        public void Push(Vector2Int key, int prio)
        {
            data.Add((key, prio));
        }
        public Vector2Int Pop()
        {
            int bestI = 0;
            int bestP = data[0].prio;
            for (int i = 1; i < data.Count; i++)
            {
                if (data[i].prio < bestP) { bestP = data[i].prio; bestI = i; }
            }
            var item = data[bestI].key;
            data.RemoveAt(bestI);
            return item;
        }
        public bool Contains(Vector2Int key) => data.Any(d => d.key == key);
        public void UpdatePriority(Vector2Int key, int newPrio)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].key == key) { data[i] = (key, newPrio); return; }
            }
            data.Add((key, newPrio));
        }
    }

    //////////////////////////// TileLayout helpers ///////////////////////////////////////
    
    ///// Add these types and methods inside the existing TileManager class (append near other public API methods)

    [Serializable]
    public class RuntimeHexCell
    {
        public int x;
        public int y;
        // Editor export may contain a full asset path; at runtime we prefer a prefab name.
        public string prefabName;
        public string prefabPath;
    }

    [Serializable]
    public class RuntimeHexTemplate
    {
        public int worldRadius;
        public float hexSize;
        public List<RuntimeHexCell> cells = new();
    }

    [Serializable]
    public struct PrefabMapping
    {
        public string key;      // name/key used in JSON (e.g. prefab name)
        public GameObject prefab;
    }

    // Registry you populate in inspector: map prefab names (or keys) -> prefab assets used at runtime.
    public List<PrefabMapping> prefabRegistry = new();

    // Optional fallback prefab used when a mapping is missing.
    public GameObject defaultTilePrefab;

    // Clears all instantiated tiles under tilesParent (runtime-safe).
    public void ClearAllTiles()
    {
        if (tilesParent == null) return;

#if UNITY_EDITOR
        // In editor, use DestroyImmediate so the scene updates immediately
        var children = new List<GameObject>();
        foreach (Transform t in tilesParent) children.Add(t.gameObject);
        foreach (var go in children)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
#else
    foreach (Transform t in tilesParent) Destroy(t.gameObject);
#endif

        tileLookup.Clear();
        ClearHighlights();
        ClearPath();
        ClearMarkers();
    }

    // Spawn one tile GameObject at axial coordinate using the given prefab (runtime instantiate).
    public HexGrid_TilePrefab SpawnTileAt(Vector2Int axial, GameObject prefab)
    {
        Debug.Log($"SpawnTileAt: axial={axial}, prefab={(prefab != null ? prefab.name : "null")}");
        if (tilesParent == null) Debug.LogWarning("SpawnTileAt: tilesParent is null.");
        if (prefab == null)
        {
            if (defaultTilePrefab != null) prefab = defaultTilePrefab;
            else
            {
                Debug.LogWarning($"SpawnTileAt: no prefab supplied and no defaultTilePrefab configured for cell {axial}.");
                return null;
            }
        }

        var center = AxialToWorldCenter(axial);
        var inst = GameObject.Instantiate(prefab, new Vector3(center.x, center.y, 0f), Quaternion.identity, tilesParent);
        inst.name = $"{prefab.name}_q{axial.x}_r{axial.y}";

        var tileComp = inst.GetComponent<HexGrid_TilePrefab>();
        if (tileComp != null)
        {
            tileComp.tileIndexCol = axial.x;
            tileComp.tileIndexRow = axial.y;
        }

        // Add to lookup immediately
        if (tileComp != null) tileLookup[axial] = tileComp;
        else
        {
            // If prefab doesn't include HexGrid_TilePrefab, warn and attempt to add a lightweight component
            Debug.LogWarning($"Spawned prefab '{prefab.name}' does not contain HexGrid_TilePrefab component.");
        }

        return tileComp;
    }

    // Replace the existing ResolvePrefabForCell method with this implementation.
    // This version ignores `prefabName` and resolves using only the `prefabPath` (filename part).
    private GameObject ResolvePrefabForCell(RuntimeHexCell cell)
    {
        // prefer to resolve from the prefabPath filename (e.g. "Assets/Prefabs/Tiles/tile_Empty_Prefab.prefab" -> "tile_Empty_Prefab")
        if (!string.IsNullOrEmpty(cell.prefabPath))
        {
            string fileName = Path.GetFileNameWithoutExtension(cell.prefabPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                // 1) Try registry match by filename (if you populated prefabRegistry in the inspector)
                if (prefabRegistry != null)
                {
                    var match = prefabRegistry.Find(m => string.Equals(m.key, fileName, StringComparison.OrdinalIgnoreCase));
                    if (match.prefab != null)
                    {
                        Debug.Log($"ResolvePrefabForCell: resolved '{fileName}' from prefabRegistry -> {match.prefab.name}");
                        return match.prefab;
                    }
                }

                // 2) Try Resources fallback (runtime-safe). Put prefabs under Resources/Tiles/ or Resources/
                var res = Resources.Load<GameObject>("Tiles/" + fileName) ?? Resources.Load<GameObject>(fileName);
                if (res != null)
                {
                    Debug.Log($"ResolvePrefabForCell: loaded '{fileName}' from Resources.");
                    return res;
                }

#if UNITY_EDITOR
                // 3) Editor-only: try AssetDatabase.LoadAssetAtPath for convenience while testing in Editor
                var ed = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(cell.prefabPath);
                if (ed != null)
                {
                    Debug.Log($"ResolvePrefabForCell: loaded '{cell.prefabPath}' via AssetDatabase (Editor only) -> {ed.name}");
                    return ed;
                }
#endif
            }
            else
            {
                Debug.LogWarning($"ResolvePrefabForCell: prefabPath provided but filename extraction failed: '{cell.prefabPath}'");
            }
        }
        else
        {
            Debug.LogWarning("ResolvePrefabForCell: no prefabPath provided in JSON cell.");
        }

        // 4) Fallback to default prefab if configured
        if (defaultTilePrefab != null)
        {
            Debug.Log($"ResolvePrefabForCell: using defaultTilePrefab '{defaultTilePrefab.name}' as fallback.");
            return defaultTilePrefab;
        }

        Debug.LogWarning($"ResolvePrefabForCell: could not resolve prefab for JSON entry prefabPath='{cell.prefabPath}'.");
        return null;
    }

    // Load layout JSON text (already-read) and instantiate tiles accordingly.
    // This will clear existing tiles and spawn new ones, then rebuild internal lookup.
    public void LoadLayoutFromJsonText(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("LoadLayoutFromJsonText: empty json.");
            return;
        }

        RuntimeHexTemplate templ;
        try
        {
            templ = JsonUtility.FromJson<RuntimeHexTemplate>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadLayoutFromJsonText: failed to parse JSON: {ex}");
            return;
        }

        ClearAllTiles();

        // Adopt geometry if provided
        if (templ.hexSize > 0) tileSize = templ.hexSize;

        int created = 0;
        foreach (var c in templ.cells)
        {
            Debug.Log($"LoadLayoutFromJsonText: processing cell x={c.x}, y={c.y}, prefabName={c.prefabName}, prefabPath={c.prefabPath}");
            var axial = new Vector2Int(c.x, c.y);
            var prefab = ResolvePrefabForCell(c);
            var tile = SpawnTileAt(axial, prefab);
            if (tile != null) created++;
        }

        // Rebuild any internal structures
        BuildTileLookup();
        BuildDoorLookup();

        Debug.Log($"LoadLayoutFromJsonText: instantiated {created} tiles (template radius={templ.worldRadius}, hexSize={templ.hexSize}).");
    }

    // Convenience: load layout from a file path (useful for StreamingAssets or persistent paths).
    // Example usage at runtime:
    //   string path = Path.Combine(Application.streamingAssetsPath, "level1.json");
    //   TileManager.Instance.LoadLayoutFromFile(path);
    public void LoadLayoutFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"LoadLayoutFromFile: file not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        LoadLayoutFromJsonText(json);

        // Use the filename (without extension) as the layout id for fog state files
        string layoutId = Path.GetFileNameWithoutExtension(filePath);

        // Apply saved fog state (if any)
        LoadFogState(layoutId);

        // Re-register player units so visibility reference counts are rebuilt and visible tiles update
        if (GameManager.Instance != null)
        {
            foreach (var p in GameManager.Instance.playerUnits)
            {
                if (p == null) continue;
                RegisterUnit(p, Mathf.CeilToInt(p.detectionRange));
            }
        }
    }

    // Editor helper: import layout assets packaged in Resources by prefab name
    // Example: put prefab under Resources/Tiles/ and set registry keys accordingly.
    // You can call LoadLayoutFromJsonText at runtime after supplying a registry mapping prefab keys to actual prefabs.


}
