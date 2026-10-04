using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class HexTemplateCell { public int x; public int y; public string prefabPath; }
[System.Serializable]
public class HexTemplate { public int worldRadius; public float hexSize; public List<HexTemplateCell> cells = new(); }

public class HexLayoutEditor : EditorWindow
{
    // Editor settings
    GameObject tilePrefab;
    string parentName = "HexLayoutObjects";
    int worldRadius = 5;
    float hexSize = 1f;
    bool showGhostGrid = true;
    Color gridColor = new Color(1f, 1f, 1f, 0.2f);
    Color highlightColor = new Color(0.1f, 0.8f, 0.2f, 0.35f);

    // Runtime state
    Dictionary<Vector2Int, GameObject> placed = new();
    GameObject parentContainer;
    Vector2Int hoveredCell;

    [MenuItem("Window/Hex Layout Editor")]
    public static void ShowWindow() => GetWindow<HexLayoutEditor>("Hex Layout Editor");

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EnsureParent();
        RebuildPlacedMap();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        placed.Clear();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Hex Layout Editor", EditorStyles.boldLabel);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        parentName = EditorGUILayout.TextField("Parent Name", parentName);

        EditorGUILayout.Space();
        worldRadius = EditorGUILayout.IntField("World Radius", worldRadius);
        hexSize = EditorGUILayout.FloatField("Hex Size", hexSize);
        showGhostGrid = EditorGUILayout.Toggle("Show Ghost Grid", showGhostGrid);

        EditorGUILayout.Space();
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear all tiles", "Delete all tiles created by the editor?", "Yes", "No"))
            {
                ClearAll();
            }
        }

        if (GUILayout.Button("Export JSON Template"))
        {
            ExportTemplate();
        }

        if (GUILayout.Button("Import JSON Template"))
        {
            ImportTemplateFromFile();
        }

        // New: Fill all tiles within worldRadius with the selected prefab
        if (GUILayout.Button("Fill All Tiles"))
        {
            if (tilePrefab == null)
            {
                EditorUtility.DisplayDialog("No Tile Prefab", "Assign a Tile Prefab in the editor before filling.", "OK");
            }
            else
            {
                if (EditorUtility.DisplayDialog("Fill all tiles", $"Instantiate tile prefab at every cell within radius {worldRadius}?\nExisting tiles will be preserved.", "Fill", "Cancel"))
                {
                    FillAllTiles();
                }
            }
        }

        if (GUILayout.Button("Refresh From Scene"))
        {
            EnsureParent();
            RebuildPlacedMap();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Scene interaction:\n- Left click: place tile\n- Shift+Left click: remove tile\n- Move mouse to preview hex", MessageType.Info);
    }

    void EnsureParent()
    {
        parentContainer = GameObject.Find(parentName);
        if (parentContainer == null)
        {
            parentContainer = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parentContainer, "Create HexLayout parent");
        }
    }

    void RebuildPlacedMap()
    {
        placed.Clear();
        EnsureParent();
        foreach (Transform t in parentContainer.transform)
        {
            var pos = t.position;
            var axial = HexMath.GetGridPosFromWorldPos(pos, hexSize);
            if (!placed.ContainsKey(axial)) placed.Add(axial, t.gameObject);
        }
    }

    void OnSceneGUI(SceneView sv)
    {
        var evt = Event.current;
        // Convert mouse to world point on XY plane (z = 0)
        Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter)) return;
        Vector3 worldPoint = ray.GetPoint(enter);
        hoveredCell = HexMath.GetGridPosFromWorldPos(worldPoint, hexSize);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        if (showGhostGrid)
        {
            DrawGhostGrid();
        }

        // Draw hover highlight
        var center = HexMath.AxialToWorldCenter_PointTop(hoveredCell, hexSize);
        Handles.color = highlightColor;
        Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, hexSize * 0.45f);

        HandleUtility.Repaint();

        // Mouse interactions
        if (evt.type == EventType.MouseDown && evt.button == 0 && !evt.alt) // left click (ignore alt-drag orbit)
        {
            Vector2Int cell = hoveredCell;
            if (evt.shift)
            {
                // remove
                if (placed.TryGetValue(cell, out var go))
                {
                    Undo.DestroyObjectImmediate(go);
                    placed.Remove(cell);
                }
            }
            else
            {
                // place
                if (tilePrefab == null)
                {
                    Debug.LogWarning("No tile prefab assigned in Hex Layout Editor.");
                }
                else if (!placed.ContainsKey(cell))
                {
                    PlaceTileAt(cell);
                }
            }
            evt.Use();
        }
    }

    void PlaceTileAt(Vector2Int cell) => PlaceTileAt(cell, tilePrefab);

    // Overload: instantiate a specific prefab asset at the axial cell
    void PlaceTileAt(Vector2Int cell, GameObject prefabAsset)
    {
        var center = HexMath.AxialToWorldCenter_PointTop(cell, hexSize);
        GameObject inst;
#if UNITY_2018_3_OR_NEWER
        if (prefabAsset != null)
        {
            inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            if (inst == null) inst = (GameObject)Object.Instantiate(prefabAsset);
        }
        else
        {
            inst = null;
        }
#else
        inst = (prefabAsset != null) ? (GameObject)Object.Instantiate(prefabAsset) : null;
#endif
        if (inst == null)
        {
            Debug.LogError("Failed to instantiate prefab for tile.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(inst, "Place Hex Tile");
        inst.transform.position = new Vector3(center.x, center.y, 0f);
        inst.transform.SetParent(parentContainer.transform, true);

        // Fill tileIndexRow / tileIndexCol on HexGrid_TilePrefab if present.
        var tileScript = inst.GetComponent<HexGrid_TilePrefab>();
        if (tileScript != null)
        {
            tileScript.tileIndexRow = cell.y;
            tileScript.tileIndexCol = cell.x;
        }

        // Optional: name the instance for easier scene browsing
        inst.name = $"{(prefabAsset != null ? prefabAsset.name : "Tile")}_q{cell.x}_r{cell.y}";

        placed[cell] = inst;
    }

    // New: instantiate prefab at every axial cell inside radius (skips existing)
    void FillAllTiles()
    {
        EnsureParent();
        int created = 0;
        foreach (var cell in HexMath.AxialInsideHex(worldRadius))
        {
            if (placed.ContainsKey(cell)) continue;
            PlaceTileAt(cell);
            created++;
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneView.RepaintAll();
#endif
        Debug.Log($"Filled {created} tiles within radius {worldRadius}.");
    }

    void ClearAll()
    {
        EnsureParent();
        var children = new List<GameObject>();
        foreach (Transform t in parentContainer.transform) children.Add(t.gameObject);
        foreach (var go in children)
        {
            Undo.DestroyObjectImmediate(go);
        }
        placed.Clear();
    }

    void ExportTemplate()
    {
        EnsureParent();
        var template = new HexTemplate { worldRadius = worldRadius, hexSize = hexSize };

        foreach (var kv in placed)
        {
            var axial = kv.Key;
            var go = kv.Value;
            string prefabPath = "";

#if UNITY_EDITOR
            // Try to get the source prefab asset for this instance
            Object sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (sourcePrefab != null)
            {
                prefabPath = AssetDatabase.GetAssetPath(sourcePrefab);
            }
            else
            {
                // fallback: if user selected a default tilePrefab in the editor use that path
                if (tilePrefab != null)
                    prefabPath = AssetDatabase.GetAssetPath(tilePrefab);
            }
#endif
            template.cells.Add(new HexTemplateCell { x = axial.x, y = axial.y, prefabPath = prefabPath });
        }

        string json = JsonUtility.ToJson(template, true);
        string path = EditorUtility.SaveFilePanel("Save Hex Template", Application.dataPath, "hex_template.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"Saved hex template to {path}");
        }
    }

    void ImportTemplateFromFile()
    {
        string path = EditorUtility.OpenFilePanel("Open Hex Template", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        HexTemplate template;
        try
        {
            template = JsonUtility.FromJson<HexTemplate>(json);
        }
        catch
        {
            EditorUtility.DisplayDialog("Import failed", "Could not parse JSON file.", "OK");
            return;
        }

        if (template == null || template.cells == null)
        {
            EditorUtility.DisplayDialog("Import failed", "File did not contain a valid hex template.", "OK");
            return;
        }

        bool clear = EditorUtility.DisplayDialog("Import Hex Template", "Clear existing tiles before import?", "Clear and Import", "Merge with existing");
        if (clear) ClearAll();

        // Adopt template geometry if present
        if (template.hexSize > 0) hexSize = template.hexSize;
        worldRadius = template.worldRadius;

        EnsureParent();

        if (tilePrefab == null)
        {
            // Allow import even if tilePrefab not assigned — per-cell prefab paths will be used where available
            if (!EditorUtility.DisplayDialog("No default Tile Prefab", "No default Tile Prefab assigned. Import will attempt to use per-cell prefab paths. Continue?", "Yes", "Cancel"))
                return;
        }

        int created = 0;
        foreach (var cell in template.cells)
        {
            var axial = new Vector2Int(cell.x, cell.y);
            if (placed.ContainsKey(axial)) continue;

            GameObject prefabToUse = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(cell.prefabPath))
            {
                var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(cell.prefabPath);
                if (loaded == null)
                {
                    Debug.LogWarning($"Could not load prefab at path '{cell.prefabPath}' for cell {axial}. Falling back to default tile prefab.");
                }
                else prefabToUse = loaded;
            }
#endif
            if (prefabToUse == null) prefabToUse = tilePrefab;

            if (prefabToUse == null)
            {
                Debug.LogWarning($"Skipping cell {axial} — no prefab available to instantiate.");
                continue;
            }

            PlaceTileAt(axial, prefabToUse);
            created++;
        }

        RebuildPlacedMap();
        Debug.Log($"Imported {created} tiles from {Path.GetFileName(path)}");
    }

    // Draw each hex as a wire polygon
    void DrawGhostGrid()
    {
        Handles.color = gridColor;
        foreach (var cell in HexMath.AxialInsideHex(worldRadius))
        {
            var center = HexMath.AxialToWorldCenter_PointTop(cell, hexSize);
            var pts = GetHexCornerPoints(new Vector2(center.x, center.y), hexSize);
            var worldPts = pts.Select(p => new Vector3(p.x, p.y, 0f)).ToArray();
            Handles.DrawPolyLine(worldPts.Concat(new[] { worldPts[0] }).ToArray());
        }
    }

    static Vector2[] GetHexCornerPoints(Vector2 center, float size)
    {
        // point-top (pointy) hex; corner angles: 60*i - 30 degrees
        Vector2[] pts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float ang = Mathf.Deg2Rad * (60f * i - 30f);
            pts[i] = new Vector2(center.x + size * Mathf.Cos(ang), center.y + size * Mathf.Sin(ang));
        }
        return pts;
    }
}