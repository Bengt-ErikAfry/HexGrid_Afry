using System;
using System.Collections.Generic;
using UnityEngine;

//
//Thin wrapper placed on each grid GameObject (rename your HexGridLinesBaker to inherit or compose this)
//DO NOT change the Linereder data manualy in Inspector. This script load the data on awak.

[DisallowMultipleComponent]
public class HexGridComponent : MonoBehaviour
{
    [Header("HexGrid")]
    [Tooltip("Hex 'radius' in cells (cube distance). World will be a hex of this radius.")]
    public int worldRadius = 10;

    [Tooltip("Hex size in world units (center to corner distance).")]
    public float hexSize = 1f;

    [Tooltip("Use the referenced MinableComponent's worldRadius/hexSize when baking this grid.")]
    public bool useMinableComponentHexGridSize = false;
    public MinableComponent minableReference;

    [Tooltip("Line thickness in world units. For 64 PPU, 1 px ≈ 1/64 ≈ 0.015625")]
    public float lineThickness = 0.02f;

    public Material hexGrid_Mat;   //Background hexgrid maeterial

    [Header("Optional: blocked cells")]
    public HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>(); // fill externally if you have obstacles

    [Header("Highlight Hex")]
    public Material hexHighlight_Mat;       // Highlight material
    public GameObject highlightGO; // Highlight GameObject get modified when highlighting hex under mouse
    public int highligtSortingOrder = 1;          // Rendering order of highlight

    [Header("Path Line style")]
    public float lineWidth = 0.03f;
    public Color lineColor = new Color(1f, 0.9f, 0.1f, 0.85f); // warm yellow
    public List<Vector2Int> LastPath { get; private set; } = new(); //used in movementManager to move unit.

    // Internal
    public LineRenderer currentLineRenderer;
    
    [Header("Marker style")]
    public Color markerTextColor = Color.white;
    public int turnMarkerFontSize = 64;                     // scale down via transform
    public Material turnMarker_BG_Material;                       // optional custom text material
    //public Color markerBackColor = new Color(0f, 0f, 0f, 0.6f); // semi-transparent dark bg
    public Vector2 markerOffset = new Vector2(0f, 0.08f);       // lift labels off the line
    public bool alwaysMarkTarget = true;
    public float markerScale = 1f;
    public Mesh circleMesh;
    public int turnMarker_BG_SortingOrder = 2;
    public int turnMarker_Text_SortingOrder = 3;

    [Tooltip("Radius of the round marker background in world units.")]
    public float markerRadius = 0.07f;               // ~4.5 px if 1 world unit = 64 px
    [Tooltip("Number of segments used to approximate the circle (12–24 is fine).")]
    public int circleSegments = 20;
    private Material unlitMat;  // for optional marker backgrounds

    


    private void Awake()
    {
        HexGridManager.Instance?.Register(this);    //To show/hide grids in the manager.
                                                    // If manager may not exist in scene, consider lazy-creating a GameObject with manager here.

        currentLineRenderer = GetComponent<LineRenderer>();

        // Configure LineRenderer (unlit look)
        currentLineRenderer.positionCount = 0;
        currentLineRenderer.loop = false;
        currentLineRenderer.startWidth = lineWidth;
        currentLineRenderer.endWidth = lineWidth;
        currentLineRenderer.useWorldSpace = true;
        currentLineRenderer.numCornerVertices = 2;  // simple rounded corners
        currentLineRenderer.numCapVertices = 2;     // simple end caps
        currentLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        currentLineRenderer.startColor = lineColor;
        currentLineRenderer.endColor = lineColor;

        // Build an unlit material for marker backgrounds (optional)
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        unlitMat = new Material(shader);
        if (unlitMat.HasProperty("_BaseColor")) unlitMat.SetColor("_BaseColor", Color.white);
        if (unlitMat.HasProperty("_Color")) unlitMat.SetColor("_Color", Color.white);

        circleMesh = HexMath.BuildCircleMesh(markerRadius, circleSegments);

    }
    private void Start()
    {
        blockedCells.Add(new Vector2Int(0, 0)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(0, -1)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(0, 1)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(-1, 0)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(-1, 1)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(1, -1)); // Example blocked cell at center
        blockedCells.Add(new Vector2Int(1, 0)); // Example blocked cell at center
    }

    private void OnDestroy()
    {
        HexGridManager.Instance?.Unregister(this);
    }

    public void SetVisualActive(bool active)
    {
        // enable/disable renderers/line renderers/highlight meshes to save draw cost
        var rs = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rs) r.enabled = active;
    }

    // Keep your Bake() logic here; use HexMath for calculations instead of static singletons.
    /// <summary>Bake one mesh of quads along all unique hex edges (seamless grid lines).</summary>
    [ContextMenu("Bake Grid Lines")]
    public void Bake()
    {
        // Choose authoritative geometry: either this component's values or the referenced MinableComponent
        int radius = (useMinableComponentHexGridSize && minableReference != null) ? minableReference.worldRadius : worldRadius;
        float size = (useMinableComponentHexGridSize && minableReference != null) ? minableReference.hexSize : hexSize;

        // 1) Collect unique edges (unordered)
        var edges = new HashSet<(Vector2 a, Vector2 b)>(new EdgeComparer());

        foreach (var cell in HexMath.AxialInsideHex(radius))
        {
            var dirs = HexMath.NeighborDirs();
            for (int i = 0; i < 6; i++)
            {
                var dir = dirs[i];
                var neighbor = cell + dir;
                bool neighborInside = HexMath.InsideHex(neighbor, radius);

                // Add boundary and interior edges once (lexicographic tie-breaker)
                bool add = !neighborInside || HexMath.IsLexSmaller(cell, neighbor);
                if (!add) continue;

                // Edge lies on the bisector between cell centers
                Vector2 cA = HexMath.AxialToWorldCenter_PointTop(cell, size);
                Vector2 cB = HexMath.AxialToWorldCenter_PointTop(neighbor, size);
                Vector2 d = (cB - cA);
                if (d.sqrMagnitude < 1e-9f) continue;

                Vector2 m = (cA + cB) * 0.5f;
                Vector2 perp = new Vector2(-d.y, d.x).normalized;

                // For a regular hex, side length equals the circumradius (hexSize).
                float sideLen = size;
                Vector2 p0 = m + perp * (sideLen * 0.5f);
                Vector2 p1 = m - perp * (sideLen * 0.5f);

                HexMath.AddEdge(edges, p0, p1);
            }
        }

        // 2) Build the quad mesh
        var mesh = new Mesh { name = "HexGridLines" };
        var verts = new List<Vector3>(edges.Count * 4);
        var cols = new List<Color>(edges.Count * 4);
        var tris = new List<int>(edges.Count * 6);

        foreach (var (a, b) in edges)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x) * (lineThickness * 0.5f);

            Vector3 v0 = new Vector3(a.x + n.x, a.y + n.y, 0);
            Vector3 v1 = new Vector3(a.x - n.x, a.y - n.y, 0);
            Vector3 v2 = new Vector3(b.x + n.x, b.y + n.y, 0);
            Vector3 v3 = new Vector3(b.x - n.x, b.y - n.y, 0);

            int idx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            //cols.Add(lineColor); cols.Add(lineColor); cols.Add(lineColor); cols.Add(lineColor);

            tris.Add(idx + 0); tris.Add(idx + 2); tris.Add(idx + 1);
            tris.Add(idx + 2); tris.Add(idx + 3); tris.Add(idx + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        // 3) Assign to GameObject
        var mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = hexGrid_Mat;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
        UnityEditor.EditorUtility.DisplayDialog("Hex Grid Lines",
            $"Baked {edges.Count} edges into one mesh.", "OK");
#endif
}
    private class EdgeComparer : IEqualityComparer<(Vector2 a, Vector2 b)>
    {
        private const float EPS = 1e-5f;

        public bool Equals((Vector2 a, Vector2 b) e1, (Vector2 a, Vector2 b) e2)
        {
            // order-insensitive equality
            return (Approximately(e1.a, e2.a) && Approximately(e1.b, e2.b))
                || (Approximately(e1.a, e2.b) && Approximately(e1.b, e2.a));
        }

        public int GetHashCode((Vector2 a, Vector2 b) e)
        {
            // Quantize to avoid float noise and use a robust combiner
            int ax = Mathf.RoundToInt(e.a.x / EPS);
            int ay = Mathf.RoundToInt(e.a.y / EPS);
            int bx = Mathf.RoundToInt(e.b.x / EPS);
            int by = Mathf.RoundToInt(e.b.y / EPS);
            return HashCode.Combine(ax, ay, bx, by);
        }

        private static bool Approximately(Vector2 v1, Vector2 v2)
            => (v1 - v2).sqrMagnitude < EPS * EPS;
    }
}