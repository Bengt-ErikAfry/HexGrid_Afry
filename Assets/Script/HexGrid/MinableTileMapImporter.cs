using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor tool: paint an ore map texture, then import per-tile yields/types for a selected MinableComponent.
// Requires MiningTileDTO and MiningTileCollectionDTO classes (DTOs used in earlier sample).
public class MinableTileMapImporter : EditorWindow
{
    private MinableComponent targetMinable;
    private Texture2D oreMap;
    private Texture2D enemyMask; // optional mask: red > threshold means enemy/outpost
    private float maxYield = 100f;
    private Color[] paletteColors = new Color[0];
    private ItemDefinition[] paletteTypes = new ItemDefinition[0];
    private float enemyThreshold = 0.5f;
    private string exportPath = "Assets/minable_tiles.json";

    [MenuItem("Tools/Minable Tile Map Importer")]
    public static void ShowWindow() => GetWindow<MinableTileMapImporter>("Minable Tile Map Importer");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target Minable (select GameObject with MinableComponent)", EditorStyles.boldLabel);
        targetMinable = (MinableComponent)EditorGUILayout.ObjectField("MinableComponent", targetMinable, typeof(MinableComponent), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Maps", EditorStyles.boldLabel);
        oreMap = (Texture2D)EditorGUILayout.ObjectField("Ore Map (RGBA)", oreMap, typeof(Texture2D), false);
        enemyMask = (Texture2D)EditorGUILayout.ObjectField("Enemy / Outpost Mask (optional)", enemyMask, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mapping / Output", EditorStyles.boldLabel);
        maxYield = EditorGUILayout.FloatField("Max Yield (per tile)", maxYield);
        enemyThreshold = EditorGUILayout.Slider("Enemy Mask Threshold", enemyThreshold, 0f, 1f);

        int newLen = Mathf.Max(0, EditorGUILayout.IntField("Palette size", paletteColors.Length));
        if (newLen != paletteColors.Length)
        {
            Array.Resize(ref paletteColors, newLen);
            Array.Resize(ref paletteTypes, newLen);
        }

        for (int i = 0; i < paletteColors.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            paletteColors[i] = EditorGUILayout.ColorField(paletteColors[i], GUILayout.Width(120));
            paletteTypes[i] = (ItemDefinition)EditorGUILayout.ObjectField(paletteTypes[i], typeof(ItemDefinition), false);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        exportPath = EditorGUILayout.TextField("Export JSON Path", exportPath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate tile DTOs and export JSON"))
        {
            if (targetMinable == null) { EditorUtility.DisplayDialog("Error", "Select a MinableComponent first.", "OK"); return; }
            if (oreMap == null) { EditorUtility.DisplayDialog("Error", "Assign an oreMap texture.", "OK"); return; }
            ImportAndExport();
        }
    }

    private void ImportAndExport()
    {
        try
        {
            // Resolve axial cells consistently with the rest of the codebase:
            // - If Minable has tile entries -> generate canonical cells for that count.
            // - Otherwise use the Minable.worldRadius axial region.
            List<Vector2Int> cells;
            if (targetMinable.tilesData != null && targetMinable.tilesData.Count > 0)
            {
                cells = HexMath.GenerateCellsForCountSorted(targetMinable.tilesData.Count);
            }
            else
            {
                cells = HexMath.AxialRegion(targetMinable.worldRadius);
                // ensure canonical ordering when using AxialRegion
                cells.Sort((a, b) =>
                {
                    int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
                    int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
                    if (da != db) return da.CompareTo(db);
                    if (a.x != b.x) return a.x.CompareTo(b.x);
                    return a.y.CompareTo(b.y);
                });
            }

            int count = cells.Count;
            // If tiles exist and count > tiles.Count, clamp to tile list count to preserve mapping
            if (targetMinable.tilesData != null && targetMinable.tilesData.Count > 0)
                count = Math.Min(count, targetMinable.tilesData.Count);

            // Compute centers using size=1 (scale doesn't matter; we normalize later)
            var centers = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                var axial = cells[i];
                centers.Add(HexMath.AxialToWorldCenter_PointTop(axial, 1f));
            }

            // Normalize centers to texture UV (0..1)
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var c in centers)
            {
                minX = Mathf.Min(minX, c.x);
                minY = Mathf.Min(minY, c.y);
                maxX = Mathf.Max(maxX, c.x);
                maxY = Mathf.Max(maxY, c.y);
            }
            float width = Mathf.Max(1e-5f, maxX - minX);
            float height = Mathf.Max(1e-5f, maxY - minY);

            var wrapper = new MiningTileCollectionDTO { nrOfTiles = count };

            for (int i = 0; i < count; i++)
            {
                var center = centers[i];
                float u = (center.x - minX) / width;
                float v = (center.y - minY) / height;

                // Sample color from oreMap (bilinear)
                Color sampled = oreMap.GetPixelBilinear(u, v);

                // Compute yield by grayscale (or use a channel/alpha as you prefer)
                float yield = sampled.grayscale * maxYield;

                // Determine ore type by nearest palette color (optional)
                ItemDefinition chosenType = null;
                if (paletteColors.Length > 0 && paletteTypes.Length == paletteColors.Length)
                {
                    int best = -1;
                    float bestDist = float.MaxValue;
                    for (int p = 0; p < paletteColors.Length; p++)
                    {
                        float d = ColorDistanceSquared(sampled, paletteColors[p]);
                        if (d < bestDist) { bestDist = d; best = p; }
                    }
                    if (best >= 0) chosenType = paletteTypes[best];
                }

                // Enemy/outpost detection from enemyMask
                bool hasEnemy = false;
                if (enemyMask != null)
                {
                    Color em = enemyMask.GetPixelBilinear(u, v);
                    hasEnemy = em.grayscale > enemyThreshold;
                }

                var dto = new MiningTileDTO
                {
                    tileIndexRow = cells[i].y,
                    tileIndexCol = cells[i].x,
                    isSurveyed = false,
                    hasMiningOutpost = false,
                    hasAbandonMiningOutpost = hasEnemy,
                    oreAmount = yield
                };

                // Attach ore type info if you store it in DTO; here we store by index/name if needed.
                wrapper.tiles.Add(dto);
            }

            // Serialize and save
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(exportPath, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Import complete", $"Exported {wrapper.tiles.Count} tiles to\n{exportPath}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    private static float ColorDistanceSquared(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return dr * dr + dg * dg + db * db;
    }
}