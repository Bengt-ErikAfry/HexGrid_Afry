using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor window: generate an ore map texture from a MinableComponent
public class OreMapGeneratorWindow : EditorWindow
{
    private MinableComponent targetMinable;
    private int resolution = 512;
    private bool savePng = true;
    private string outputFolder = "Assets/Generated";
    private int seed = 12345;
    private float veinsMultiplier = 10f;
    private float patchesMultiplier = 6f;
    private Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);

    private bool hexMaskEnabled = true;
    private float hexMaskScale = 0.95f;
    private bool hexTiledMode = true;
    private bool useTargetMinableGridSize = false;

    [Serializable]
    private class OreResourceAmountJson
    {
        public string itemName;
        public float amount;
    }

    [Serializable]
    private class AsteroidTileJson
    {
        public int x;
        public int y;
        public string prefabPath = "Assets/Resources/Tiles/tile_Empty_Prefab.prefab";
        public string state = "";
        public List<OreResourceAmountJson> ore = new List<OreResourceAmountJson>();
    }

    [Serializable]
    private class AsteroidLayoutJson
    {
        public int worldRadius;
        public float hexSize;
        public List<AsteroidTileJson> cells = new List<AsteroidTileJson>();
    }

    [MenuItem("Tools/Ore Map Generator")]
    public static void ShowWindow() => GetWindow<OreMapGeneratorWindow>("Ore Map Generator");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target Minable", EditorStyles.boldLabel);
        targetMinable = (MinableComponent)EditorGUILayout.ObjectField("MinableComponent", targetMinable, typeof(MinableComponent), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntPopup(
            "Resolution",
            resolution,
            new[] { "128", "256", "512", "1024" },
            new[] { 128, 256, 512, 1024 });
        savePng = EditorGUILayout.Toggle("Save PNG", savePng);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        seed = EditorGUILayout.IntField("Random Seed", seed);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Feature Multipliers", EditorStyles.boldLabel);
        veinsMultiplier = EditorGUILayout.FloatField("Veins Multiplier", veinsMultiplier);
        patchesMultiplier = EditorGUILayout.FloatField("Patches Multiplier", patchesMultiplier);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Masking / Tiling", EditorStyles.boldLabel);
        hexMaskEnabled = EditorGUILayout.Toggle("Hex shaped (match grid)", hexMaskEnabled);
        if (hexMaskEnabled)
        {
            hexMaskScale = EditorGUILayout.Slider("Hex area scale", hexMaskScale, 0.2f, 1.0f);
            hexTiledMode = EditorGUILayout.Toggle("Hex tiled (match asteroid JSON)", hexTiledMode);
            useTargetMinableGridSize = EditorGUILayout.Toggle("Use asteroid JSON grid size", useTargetMinableGridSize);
            EditorGUILayout.HelpBox("Per-tile ore ratios are written into the asteroid JSON by ore item name using ItemDefinition.colorOnOreMap.", MessageType.Info);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Ore Map"))
        {
            if (targetMinable == null)
            {
                EditorUtility.DisplayDialog("Error", "Assign a MinableComponent first.", "OK");
                return;
            }
            GenerateAndAssign();
        }
    }

    private void GenerateAndAssign()
    {
        try
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var asteroidLayout = GetAsteroidLayout(targetMinable);
            if (asteroidLayout == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not read asteroid JSON from the selected MinableComponent's AsteroidComponent.", "OK");
                return;
            }

            int asteroidWorldRadius = asteroidLayout.worldRadius;
            float asteroidHexSize = asteroidLayout.hexSize;

            var resources = targetMinable.minableResourceList
                .Where(r => r != null && r.item != null)
                .ToList();

            if (resources.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "minableResourceList is empty or contains null item definitions.", "OK");
                return;
            }

            var resourceColors = new List<Color>();
            foreach (var entry in resources)
            {
                string key = GetItemKey(entry.item);
                resourceColors.Add(entry.item.colorOnOreMap != Color.clear ? entry.item.colorOnOreMap : DeterministicColor(key));
            }

            int w = resolution;
            int h = resolution;
            int pxCount = w * h;
            float totalShare = resources.Sum(r => Mathf.Max(0, r.amount));
            if (totalShare <= 0f)
                totalShare = resources.Count;

            System.Random rng = new System.Random(seed);
            var resourceMaps = new List<float[]>();

            for (int i = 0; i < resources.Count; i++)
            {
                var map = new float[pxCount];
                float frac = resources[i].amount / totalShare;
                int patchCount = Mathf.Max(2, Mathf.RoundToInt(patchesMultiplier * frac + 2f));
                int veinCount = Mathf.Max(2, Mathf.RoundToInt(veinsMultiplier * frac + 2f));

                for (int p = 0; p < patchCount; p++)
                {
                    float cx = (float)rng.NextDouble() * (w - 1);
                    float cy = (float)rng.NextDouble() * (h - 1);
                    float radius = Mathf.Lerp(w * 0.04f, w * 0.18f, (float)rng.NextDouble());
                    float strength = Mathf.Lerp(0.7f, 1.8f, (float)rng.NextDouble()) * frac;
                    int minX = Mathf.Clamp(Mathf.FloorToInt(cx - radius), 0, w - 1);
                    int maxX = Mathf.Clamp(Mathf.CeilToInt(cx + radius), 0, w - 1);
                    int minY = Mathf.Clamp(Mathf.FloorToInt(cy - radius), 0, h - 1);
                    int maxY = Mathf.Clamp(Mathf.CeilToInt(cy + radius), 0, h - 1);

                    float r2 = radius * radius;
                    for (int yy = minY; yy <= maxY; yy++)
                    for (int xx = minX; xx <= maxX; xx++)
                    {
                        float dx = xx - cx;
                        float dy = yy - cy;
                        float d2 = dx * dx + dy * dy;
                        if (d2 > r2) continue;
                        float t = 1f - Mathf.Sqrt(d2) / radius;
                        map[yy * w + xx] += strength * t * t;
                    }
                }

                for (int v = 0; v < veinCount; v++)
                {
                    float x = (float)rng.NextDouble() * (w - 1);
                    float y = (float)rng.NextDouble() * (h - 1);
                    int length = Mathf.Max(12, Mathf.RoundToInt(Mathf.Lerp(w * 0.05f, w * 0.6f, (float)rng.NextDouble())));
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float stepSize = Mathf.Lerp(1.5f, w * 0.03f, (float)rng.NextDouble());
                    float thickness = Mathf.Lerp(w * 0.012f, w * 0.04f, (float)rng.NextDouble());

                    for (int s = 0; s < length; s++)
                    {
                        int minX = Mathf.Clamp(Mathf.FloorToInt(x - thickness), 0, w - 1);
                        int maxX = Mathf.Clamp(Mathf.CeilToInt(x + thickness), 0, w - 1);
                        int minY = Mathf.Clamp(Mathf.FloorToInt(y - thickness), 0, h - 1);
                        int maxY = Mathf.Clamp(Mathf.CeilToInt(y + thickness), 0, h - 1);
                        float r2 = thickness * thickness;
                        for (int yy = minY; yy <= maxY; yy++)
                        for (int xx = minX; xx <= maxX; xx++)
                        {
                            float dx = xx - x;
                            float dy = yy - y;
                            float d2 = dx * dx + dy * dy;
                            if (d2 > r2) continue;
                            float t = 1f - Mathf.Sqrt(d2) / thickness;
                            map[yy * w + xx] += frac * (0.7f + 0.3f * t);
                        }

                        angle += Mathf.Lerp(-0.6f, 0.6f, (float)rng.NextDouble()) * 0.6f;
                        x += Mathf.Cos(angle) * (0.5f + (float)rng.NextDouble() * stepSize);
                        y += Mathf.Sin(angle) * (0.5f + (float)rng.NextDouble() * stepSize);
                        x = Mathf.Clamp(x, 0, w - 1);
                        y = Mathf.Clamp(y, 0, h - 1);
                    }
                }

                resourceMaps.Add(map);
            }

            var finalPixels = new Color[pxCount];
            for (int idx = 0; idx < pxCount; idx++)
            {
                float total = 0f;
                for (int i = 0; i < resourceMaps.Count; i++)
                    total += resourceMaps[i][idx];

                if (total <= 0f)
                {
                    finalPixels[idx] = backgroundColor;
                    continue;
                }

                Color mixed = Color.black;
                for (int i = 0; i < resourceMaps.Count; i++)
                    mixed += resourceColors[i] * (resourceMaps[i][idx] / total);
                mixed.a = 1f;
                finalPixels[idx] = mixed;
            }

            if (hexMaskEnabled)
            {
                var visibleCells = GetVisibleCells(asteroidWorldRadius, useTargetMinableGridSize);
                float size = asteroidHexSize > 0f ? asteroidHexSize : 1f;
                float innerW = w * hexMaskScale;
                float innerH = h * hexMaskScale;
                float scale = ComputeHexGridScale(visibleCells, size, w, h, innerW, innerH, out float tx, out float ty);

                var cellPolygons = new List<Vector2[]>();
                foreach (var cell in visibleCells)
                    cellPolygons.Add(BuildHexPolygonInTexture(cell, size, scale, tx, ty));

                for (int idx = 0; idx < pxCount; idx++)
                {
                    int xx = idx % w;
                    int yy = idx / w;
                    Vector2 p = new Vector2(xx + 0.5f, yy + 0.5f);
                    bool insideAny = false;
                    foreach (var poly in cellPolygons)
                    {
                        if (IsPointInPolygon(poly, p))
                        {
                            insideAny = true;
                            break;
                        }
                    }

                    var c = finalPixels[idx];
                    finalPixels[idx] = insideAny ? new Color(c.r, c.g, c.b, 1f) : new Color(c.r, c.g, c.b, 0f);
                }
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(finalPixels);
            tex.Apply();

            if (savePng)
            {
                string fileName = $"{targetMinable.gameObject.name}_oremap_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string assetPath = Path.Combine(outputFolder, fileName);
                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(assetPath, png);
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }

            AsteroidComponent asteroid = targetMinable.GetComponent<AsteroidComponent>();
            if (asteroid == null)
                asteroid = targetMinable.GetComponentInParent<AsteroidComponent>();

            if (asteroid != null && !string.IsNullOrWhiteSpace(asteroid.asteroidTileSatusSave))
            {
                var visibleCells = GetVisibleCells(asteroidWorldRadius, useTargetMinableGridSize);
                var tileList = new List<AsteroidTileJson>();
                float size = asteroidHexSize > 0f ? asteroidHexSize : 1f;
                float innerW = w * hexMaskScale;
                float innerH = h * hexMaskScale;
                float scale = ComputeHexGridScale(visibleCells, size, w, h, innerW, innerH, out float tx, out float ty);

                foreach (var cell in visibleCells)
                {
                    var poly = BuildHexPolygonInTexture(cell, size, scale, tx, ty);
                    var oreValues = new List<OreResourceAmountJson>();

                    foreach (var resource in resources)
                    {
                        if (resource == null || resource.item == null)
                            continue;

                        Color targetColor = resource.item.colorOnOreMap != Color.clear ? resource.item.colorOnOreMap : DeterministicColor(GetItemKey(resource.item));
                        float amount = ComputeResourceCoverage(tex, poly, targetColor);
                        oreValues.Add(new OreResourceAmountJson
                        {
                            itemName = GetItemKey(resource.item),
                            amount = amount
                        });
                    }

                    tileList.Add(new AsteroidTileJson
                    {
                        x = cell.x,
                        y = cell.y,
                        prefabPath = "Assets/Resources/Tiles/tile_Empty_Prefab.prefab",
                        state = "",
                        ore = oreValues
                    });
                }

                var save = new AsteroidLayoutJson
                {
                    worldRadius = asteroidWorldRadius,
                    hexSize = asteroidHexSize,
                    cells = tileList
                };

                string layoutPath = Path.Combine(Application.streamingAssetsPath, asteroid.asteroidTileSatusSave + ".json");
                File.WriteAllText(layoutPath, JsonUtility.ToJson(save, true));
            }

            EditorUtility.DisplayDialog("Ore map generated", "Ore map and per-tile ore ratios were written to the asteroid layout JSON.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    private static List<Vector2Int> GetVisibleCells(int asteroidWorldRadius, bool useTargetMinableGridSize)
    {
        var cells = HexMath.AxialRegion(Mathf.Max(1, asteroidWorldRadius > 0 ? asteroidWorldRadius : 1));
        cells.Sort((a, b) =>
        {
            int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
            int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
        return cells;
    }

    private static float ComputeHexGridScale(List<Vector2Int> visibleCells, float size, int texW, int texH, float innerW, float innerH, out float tx, out float ty)
    {
        var centers = visibleCells.Select(c => HexMath.AxialToWorldCenter_PointTop(c, size)).ToList();
        float minX = centers.Min(p => p.x);
        float minY = centers.Min(p => p.y);
        float maxX = centers.Max(p => p.x);
        float maxY = centers.Max(p => p.y);

        float padX = size * (Mathf.Sqrt(3f) / 2f);
        float padY = size * 1f;
        float worldW = Mathf.Max(1e-5f, (maxX - minX) + padX * 2f);
        float worldH = Mathf.Max(1e-5f, (maxY - minY) + padY * 2f);

        float scale = Mathf.Min(innerW / worldW, innerH / worldH);
        tx = (texW - (worldW * scale)) * 0.5f - minX * scale + padX * scale;
        ty = (texH - (worldH * scale)) * 0.5f - minY * scale + padY * scale;
        return scale;
    }

    private static Vector2[] BuildHexPolygonInTexture(Vector2Int cell, float size, float scale, float tx, float ty)
    {
        var center = HexMath.AxialToWorldCenter_PointTop(cell, size);
        Vector2[] verts = new Vector2[6];
        for (int v = 0; v < 6; v++)
        {
            float deg = -90f + 60f * v;
            float rad = deg * Mathf.Deg2Rad;
            float vx = (center.x * scale) + tx + Mathf.Cos(rad) * (size * scale);
            float vy = (center.y * scale) + ty + Mathf.Sin(rad) * (size * scale);
            verts[v] = new Vector2(vx, vy);
        }
        return verts;
    }

    private static bool IsPointInPolygon(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];
            bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                             (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + float.Epsilon) + pi.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    private static float ComputeResourceCoverage(Texture2D tex, Vector2[] poly, Color targetColor)
    {
        int w = tex.width;
        int h = tex.height;

        int minX = Mathf.Clamp(Mathf.FloorToInt(poly.Min(p => p.x)), 0, w - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(poly.Max(p => p.x)), 0, w - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(poly.Min(p => p.y)), 0, h - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(poly.Max(p => p.y)), 0, h - 1);

        int total = 0;
        int matches = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (!IsPointInPolygon(poly, p))
                    continue;

                total++;
                Color c = tex.GetPixel(x, y);
                float distance = Mathf.Abs(c.r - targetColor.r) + Mathf.Abs(c.g - targetColor.g) + Mathf.Abs(c.b - targetColor.b);
                if (distance < 0.45f)
                    matches++;
            }
        }

        return total == 0 ? 0f : Mathf.Clamp01((float)matches / total);
    }

    private static AsteroidLayoutJson GetAsteroidLayout(MinableComponent minable)
    {
        if (minable == null)
            return null;

        AsteroidComponent asteroid = minable.GetComponent<AsteroidComponent>();
        if (asteroid == null)
            asteroid = minable.GetComponentInParent<AsteroidComponent>();

        if (asteroid == null || string.IsNullOrWhiteSpace(asteroid.asteroidTileSatusSave))
            return null;

        string path = Path.Combine(Application.streamingAssetsPath, asteroid.asteroidTileSatusSave + ".json");
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            var layout = JsonUtility.FromJson<AsteroidLayoutJson>(json);
            if (layout == null)
                return null;
            if (layout.cells == null)
                layout.cells = new List<AsteroidTileJson>();
            return layout;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    private static string GetItemKey(ItemDefinition item)
    {
        if (item == null)
            return "Unknown";
        return !string.IsNullOrWhiteSpace(item.itemName) ? item.itemName : item.name;
    }

    private static Color DeterministicColor(string s)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in s) hash = hash * 31 + c;
            float hue = (hash & 0xFFFF) / (float)0xFFFF;
            float sat = 0.6f + ((hash >> 16 & 0xFF) / 255f) * 0.3f;
            float val = 0.55f + ((hash >> 8 & 0xFF) / 255f) * 0.4f;
            return Color.HSVToRGB(hue, Mathf.Clamp01(sat), Mathf.Clamp01(val));
        }
    }
}
