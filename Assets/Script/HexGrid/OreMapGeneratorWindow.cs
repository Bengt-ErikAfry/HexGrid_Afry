using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    private float veinsMultiplier = 10f;   // scales number of vein features
    private float patchesMultiplier = 6f;  // scales number of patch features
    private Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);

    // Mask / tiled options
    private bool hexMaskEnabled = true;
    private float hexMaskScale = 0.95f; // fraction of the texture size the hex area should occupy (0..1)
    private bool hexTiledMode = true;   // produce hex tiles matching minable.worldRadius (but keep per-pixel detail)

    // New option: use target MinableComponent's worldRadius/hexSize when generating (if a targetMinable is assigned)
    private bool useTargetMinableGridSize = true;

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
            new[] { "128", "256", "512", "1024" },    // displayed strings
            new[] { 128, 256, 512, 1024 }             // actual int values
        );
        savePng = EditorGUILayout.Toggle("Save PNG", savePng);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        seed = EditorGUILayout.IntField("Random Seed", seed);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Feature Multipliers (scale with resource fraction)", EditorStyles.boldLabel);
        veinsMultiplier = EditorGUILayout.FloatField("Veins Multiplier", veinsMultiplier);
        patchesMultiplier = EditorGUILayout.FloatField("Patches Multiplier", patchesMultiplier);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Masking / Tiling (optional)", EditorStyles.boldLabel);
        hexMaskEnabled = EditorGUILayout.Toggle("Hex shaped (match grid)", hexMaskEnabled);
        if (hexMaskEnabled)
        {
            hexMaskScale = EditorGUILayout.Slider("Hex area scale", hexMaskScale, 0.2f, 1.0f);
            hexTiledMode = EditorGUILayout.Toggle("Hex tiled (match Minable.worldRadius)", hexTiledMode);
            useTargetMinableGridSize = EditorGUILayout.Toggle("Use MinableComponent grid size", useTargetMinableGridSize);
            EditorGUILayout.HelpBox("Tiles use point-top geometry; masking uses the union of those hex tiles so the outer shape matches the game's grid. Blocked tiles (tileData.isBlocked) are excluded from the mask.", MessageType.Info);
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

            var resources = targetMinable.minableResourceList;
            if (resources == null || resources.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "minableResourceList is empty on the selected MinableComponent.", "OK");
                return;
            }

            float sumAmounts = resources.Sum(r => Mathf.Max(0, r.amount));
            if (sumAmounts <= 0) sumAmounts = 1f;

            System.Random rng = new System.Random(seed);
            int w = resolution, h = resolution;
            int pxCount = w * h;

            // allocate accumulators: one float array per resource
            int rc = resources.Count;
            float[][] accum = new float[rc][];
            for (int i = 0; i < rc; i++) accum[i] = new float[pxCount];

            // deterministic color for each resource (based on item name)
            Color[] colors = new Color[rc];
            for (int i = 0; i < rc; i++)
                colors[i] = DeterministicColor(resources[i].item != null ? resources[i].item.name : ("res" + i));

            // Create procedural detail (patches + veins) into accum arrays (per-pixel)
            for (int i = 0; i < rc; i++)
            {
                var resItem = resources[i];
                float frac = Mathf.Max(0.0f, resItem.amount) / sumAmounts; // relative share
                int veinCount = Mathf.Max(1, Mathf.RoundToInt(veinsMultiplier * frac));
                int patchCount = Mathf.Max(1, Mathf.RoundToInt(patchesMultiplier * frac));

                // Patches
                for (int p = 0; p < patchCount; p++)
                {
                    float cx = (float)rng.NextDouble() * (w - 1);
                    float cy = (float)rng.NextDouble() * (h - 1);
                    float radius = Mathf.Lerp(w * 0.03f, w * 0.18f, (float)rng.NextDouble());
                    float strength = Mathf.Lerp(0.6f, 1.6f, (float)rng.NextDouble()) * frac * 1.5f;

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
                            float add = strength * t * t;
                            accum[i][yy * w + xx] += add;
                        }
                }

                // Veins: random walk
                for (int v = 0; v < veinCount; v++)
                {
                    float x = (float)rng.NextDouble() * (w - 1);
                    float y = (float)rng.NextDouble() * (h - 1);
                    int length = Mathf.Max(8, Mathf.RoundToInt(Mathf.Lerp(w * 0.05f, w * 0.6f, (float)rng.NextDouble())));
                    float stepAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float stepSize = Mathf.Lerp(1.0f, w * 0.025f, (float)rng.NextDouble());
                    float thickness = Mathf.Lerp(w * 0.01f, w * 0.03f, (float)rng.NextDouble());
                    float strength = Mathf.Lerp(0.4f, 1.1f, (float)rng.NextDouble()) * frac;

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
                                float add = strength * (0.5f + 0.5f * t) * 0.8f;
                                accum[i][yy * w + xx] += add;
                            }

                        stepAngle += Mathf.Lerp(-0.6f, 0.6f, (float)rng.NextDouble()) * 0.4f;
                        x += Mathf.Cos(stepAngle) * (0.5f + (float)rng.NextDouble() * stepSize);
                        y += Mathf.Sin(stepAngle) * (0.5f + (float)rng.NextDouble() * stepSize);
                        x = Mathf.Clamp(x, 0, w - 1);
                        y = Mathf.Clamp(y, 0, h - 1);
                    }
                }
            } // end per-resource accumulation

            // Build continuous per-pixel color result from accum arrays (keeps veins+patches detail)
            Color[] continuousPixels = new Color[pxCount];
            for (int idx = 0; idx < pxCount; idx++)
            {
                float total = 0f;
                for (int i = 0; i < rc; i++) total += accum[i][idx];
                if (total <= 0f)
                {
                    continuousPixels[idx] = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 1f);
                    continue;
                }
                Color c = Color.black;
                for (int i = 0; i < rc; i++)
                {
                    float wgt = accum[i][idx] / total;
                    c += colors[i] * wgt;
                }
                float intensity = Mathf.Clamp01(Mathf.Sqrt(total) * 0.6f);
                c = Color.Lerp(backgroundColor, c, intensity);
                c.a = 1f;
                continuousPixels[idx] = c;
            }

            // Prepare output pixels: either masked to hex union (keep continuous detail) or fallback continuous entire image
            Color[] outPixels = new Color[pxCount];
            for (int i = 0; i < pxCount; i++) outPixels[i] = continuousPixels[i];

            if (hexMaskEnabled && hexTiledMode)
            {
                // Build axial list from Minable.worldRadius (if requested), otherwise fall back to canonical count logic
                List<Vector2Int> cells;
                if (useTargetMinableGridSize && targetMinable != null)
                {
                    cells = HexMath.AxialRegion(targetMinable.worldRadius);
                }
                else
                {
                    // fallback: use tile count equal to number of tiles stored in Minable (if any)
                    int desired = (targetMinable != null) ? targetMinable.tilesData.Count : 1;
                    cells = HexMath.GenerateCellsForCountSorted(desired);
                }

                // Sort canonically
                cells.Sort((a, b) =>
                {
                    int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
                    int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
                    if (da != db) return da.CompareTo(db);
                    if (a.x != b.x) return a.x.CompareTo(b.x);
                    return a.y.CompareTo(b.y);
                });

                // Build blocked set from targetMinable.tiles (tileData.isBlocked)
                var blockedSet = new HashSet<Vector2Int>();
                if (targetMinable != null)
                {
                    foreach (var t in targetMinable.tilesData)
                    {
                        if (t == null) continue;
                        if (t.isBlocked)
                            blockedSet.Add(new Vector2Int(t.tileIndexCol, t.tileIndexRow));
                    }
                }

                // Use only non-blocked cells when computing bounds and masks
                var visibleCells = cells.Where(c => !blockedSet.Contains(c)).ToList();
                int count = visibleCells.Count;

                // Compute centers using selected size (use Minable's hexSize when requested)
                float size = (useTargetMinableGridSize && targetMinable != null) ? targetMinable.hexSize : 1f;

                // Find world bounds of centers (only visible)
                List<Vector2> centersWorld = new List<Vector2>(count);
                for (int i = 0; i < count; i++)
                {
                    centersWorld.Add(HexMath.AxialToWorldCenter_PointTop(visibleCells[i], size));
                }

                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int i = 0; i < centersWorld.Count; i++)
                {
                    var c = centersWorld[i];
                    minX = Mathf.Min(minX, c.x);
                    minY = Mathf.Min(minY, c.y);
                    maxX = Mathf.Max(maxX, c.x);
                    maxY = Mathf.Max(maxY, c.y);
                }

                float padX = size * (Mathf.Sqrt(3f) / 2f); // max horizontal corner offset for point-top hex
                float padY = size * 1f;                    // max vertical corner offset

                // expand bounds by hex corner extents so full hex polygons fit
                float minXAdj = minX - padX;
                float minYAdj = minY - padY;
                float maxXAdj = maxX + padX;
                float maxYAdj = maxY + padY;

                float worldW = Mathf.Max(1e-5f, maxXAdj - minXAdj);
                float worldH = Mathf.Max(1e-5f, maxYAdj - minYAdj);

                // optional tiny safety margin to avoid edge rounding issues
                const float SAFETY_MARGIN = 1.02f;
                worldW *= SAFETY_MARGIN;
                worldH *= SAFETY_MARGIN;

                // Desired inner pixel area to fit the hex grid
                float innerW = w * hexMaskScale;
                float innerH = h * hexMaskScale;

                float scale = Mathf.Min(innerW / worldW, innerH / worldH);

                // Offset so center of world maps to center of texture; use adjusted mins
                float tx = (w - (worldW * scale)) * 0.5f - minXAdj * scale;
                float ty = (h - (worldH * scale)) * 0.5f - minYAdj * scale;

                // Precompute hex polygons and covered pixels (only visible cells)
                var hexPixelCovered = new bool[pxCount];
                for (int i = 0; i < count; i++)
                {
                    var cell = visibleCells[i];
                    var cw = centersWorld[i];
                    float cx = cw.x * scale + tx;
                    float cy = cw.y * scale + ty;
                    Vector2[] verts = new Vector2[6];
                    float sizePixels = scale * size;
                    for (int v = 0; v < 6; v++)
                    {
                        float deg = -90f + 60f * v; // point-top corner angles (game grid)
                        float rad = deg * Mathf.Deg2Rad;
                        float vx = cx + Mathf.Cos(rad) * sizePixels;
                        float vy = cy + Mathf.Sin(rad) * sizePixels;
                        verts[v] = new Vector2(vx, vy);
                    }

                    float minVX = verts.Min(p => p.x);
                    float maxVX = verts.Max(p => p.x);
                    float minVY = verts.Min(p => p.y);
                    float maxVY = verts.Max(p => p.y);
                    int ix0 = Mathf.Clamp(Mathf.FloorToInt(minVX), 0, w - 1);
                    int ix1 = Mathf.Clamp(Mathf.CeilToInt(maxVX), 0, w - 1);
                    int iy0 = Mathf.Clamp(Mathf.FloorToInt(minVY), 0, h - 1);
                    int iy1 = Mathf.Clamp(Mathf.CeilToInt(maxVY), 0, h - 1);

                    for (int yy = iy0; yy <= iy1; yy++)
                        for (int xx = ix0; xx <= ix1; xx++)
                        {
                            int idx = yy * w + xx;
                            if (IsPointInPolygon(verts, new Vector2(xx + 0.5f, yy + 0.5f)))
                                hexPixelCovered[idx] = true;
                        }
                }

                // Apply mask: keep continuous color where covered, otherwise make transparent
                for (int idx = 0; idx < pxCount; idx++)
                {
                    if (!hexPixelCovered[idx])
                    {
                        var c = outPixels[idx];
                        outPixels[idx] = new Color(c.r, c.g, c.b, 0f);
                    }
                    else
                    {
                        var c = outPixels[idx];
                        outPixels[idx] = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }
            else if (hexMaskEnabled && !hexTiledMode)
            {
                // simple flat-top large hex mask (keep continuous detail inside hex)
                Vector2[] hexVerts = BuildFlatTopHexVertices(w, h, hexMaskScale);
                for (int yy = 0; yy < h; yy++)
                    for (int xx = 0; xx < w; xx++)
                    {
                        int idx = yy * w + xx;
                        if (!IsPointInPolygon(hexVerts, new Vector2(xx + 0.5f, yy + 0.5f)))
                        {
                            var c = outPixels[idx];
                            outPixels[idx] = new Color(c.r, c.g, c.b, 0f);
                        }
                        else
                        {
                            var c = outPixels[idx];
                            outPixels[idx] = new Color(c.r, c.g, c.b, 1f);
                        }
                    }
            }

            // Create runtime texture (ensure it supports alpha)
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(outPixels);
            tex.Apply();

            // Optionally save PNG to disk
            string fileName = $"{targetMinable.gameObject.name}_oremap_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string assetPath = Path.Combine(outputFolder, fileName);
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();

            // Create sprite and assign for preview
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Undo.RecordObject(targetMinable, "Assign Ore Map Sprite");
            targetMinable.backgroundSprite = sprite;
            EditorUtility.SetDirty(targetMinable);

            EditorUtility.DisplayDialog("Ore map generated", $"PNG saved to {assetPath} and preview assigned to MinableComponent.backgroundSprite.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    // Build a regular flat-top hex polygon centered in the texture. Returns vertices in pixel coordinates.
    private static Vector2[] BuildFlatTopHexVertices(int texW, int texH, float scale)
    {
        float cx = (texW - 1) * 0.5f;
        float cy = (texH - 1) * 0.5f;
        float radius = Mathf.Min(texW, texH) * 0.5f * Mathf.Clamp01(scale);
        Vector2[] verts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float deg = 0f + 60f * i; // flat-top start angle 0
            float rad = deg * Mathf.Deg2Rad;
            float vx = cx + Mathf.Cos(rad) * radius;
            float vy = cy + Mathf.Sin(rad) * radius;
            verts[i] = new Vector2(vx, vy);
        }
        return verts;
    }

    // Winding / crossing number point-in-polygon test
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

    // deterministic color from a string
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