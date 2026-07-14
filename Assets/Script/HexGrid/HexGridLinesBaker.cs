using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HexGridLinesBaker : MonoBehaviour
{
    public static HexGridLinesBaker Instance { get; private set; }

    [Header("HexGrid")]
    [Tooltip("Hex 'radius' in cells (cube distance). World will be a hex of this radius.")]
    public int worldRadius = 10;

    [Tooltip("Hex size in world units (center to corner distance).")]
    public float hexSize = 1f;

    [Tooltip("Line thickness in world units. For 64 PPU, 1 px ≈ 1/64 ≈ 0.015625")]
    public float lineThickness = 0.02f;

    public Material hexGrid_Mat;   //Background hexgrid maeterial

    [Header("Optional: blocked cells")]
    public HashSet<Vector2Int> blocked = new HashSet<Vector2Int>(); // fill externally if you have obstacles

    [Header("Highlight Hex")]
    public Material hexHighlight_Mat;       // Highlight material
    public GameObject highlightGO; // Highlight GameObject get modified when highlighting hex under mouse
    public int highligtSortingOrder = 1;          // Rendering order of highlight

    private Mesh hexMesh; // reusable filled-hex mesh
    private readonly Dictionary<Vector2Int, GameObject> highlighted = new();

    private readonly List<GameObject> pool = new();   // pooled highlight instances
    private readonly List<GameObject> active = new(); // currently active highlights

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        blocked.Add(new Vector2Int(0, 0)); // Example blocked cell at center
        blocked.Add(new Vector2Int(0, -1)); // Example blocked cell at center
        blocked.Add(new Vector2Int(0, 1)); // Example blocked cell at center
        blocked.Add(new Vector2Int(-1, 0)); // Example blocked cell at center
        blocked.Add(new Vector2Int(-1, 1)); // Example blocked cell at center
        blocked.Add(new Vector2Int(1, -1)); // Example blocked cell at center
        blocked.Add(new Vector2Int(1, 0)); // Example blocked cell at center
    }

    /// <summary>Bake one mesh of quads along all unique hex edges (seamless grid lines).</summary>
    [ContextMenu("Bake Grid Lines")]
    public void Bake()
    {
        // 1) Collect unique edges (unordered)
        var edges = new HashSet<(Vector2 a, Vector2 b)>(new EdgeComparer());

        foreach (var cell in AxialInsideHex(worldRadius))
        {
            var dirs = NeighborDirs();
            for (int i = 0; i < 6; i++)
            {
                var dir = dirs[i];
                var neighbor = cell + dir;
                bool neighborInside = InsideHex(neighbor, worldRadius);

                // Add boundary and interior edges once (lexicographic tie-breaker)
                bool add = !neighborInside || IsLexSmaller(cell, neighbor);
                if (!add) continue;

                // Edge lies on the bisector between cell centers
                Vector2 cA = AxialToWorldCenter(cell, hexSize);
                Vector2 cB = AxialToWorldCenter(neighbor, hexSize);
                Vector2 d = (cB - cA);
                if (d.sqrMagnitude < 1e-9f) continue;

                Vector2 m = (cA + cB) * 0.5f;
                Vector2 perp = new Vector2(-d.y, d.x).normalized;

                // For a regular hex, side length equals the circumradius (hexSize).
                float sideLen = hexSize;
                Vector2 p0 = m + perp * (sideLen * 0.5f);
                Vector2 p1 = m - perp * (sideLen * 0.5f);

                AddEdge(edges, p0, p1);
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

    // ---------- Hex math ----------
    private static readonly Vector2Int[] PointyDirs =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    private static readonly Vector2Int[] FlatDirs =
    {
        new(1, 0), new(0, -1), new(-1, -1),
        new(-1, 0), new(0, 1), new(1, 1)
    };

    private Vector2Int[] NeighborDirs() => true ? PointyDirs : FlatDirs;

    private static IEnumerable<Vector2Int> AxialInsideHex(int R)
    {
        for (int r = -R; r <= R; r++)
            for (int q = -R; q <= R; q++)
            {
                int x = q, z = r, y = -x - z;
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R)
                    yield return new Vector2Int(q, r);
            }
    }

    private static bool InsideHex(Vector2Int axial, int R)
    {
        int x = axial.x, z = axial.y, y = -x - z;
        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R;
    }

    private static Vector2 AxialToWorldCenter(Vector2Int axial, float size)
    {
            float x = size * (Mathf.Sqrt(3f) * axial.x + Mathf.Sqrt(3f) / 2f * axial.y);
            float y = size * (1.5f * axial.y);
            return new Vector2(x, y);

    }

    private static bool IsLexSmaller(Vector2Int a, Vector2Int b)
        => (a.x < b.x) || (a.x == b.x && a.y < b.y);

    private static void AddEdge(HashSet<(Vector2 a, Vector2 b)> set, Vector2 a, Vector2 b)
    {
        if ((b - a).sqrMagnitude > 1e-9f)
            set.Add((a, b));
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

    // ---------- Point-top axial math HELP FUNCTIONS ----------


    // Build a filled POINT-TOP hex as a triangle fan with baked vertex colors.
    public static Mesh BuildFilledPointTopHex(float size, Color c)
    {
        var mesh = new Mesh { name = "HexFillPointTop" };

        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();

        // Center vertex
        verts.Add(Vector3.zero);
        cols.Add(c);

        // 6 corners: point-top starts at -90° then steps of 60°
        for (int i = 0; i < 6; i++)
        {
            float deg = -90f + 60f * i;
            float rad = deg * Mathf.Deg2Rad;
            float x = size * Mathf.Cos(rad);
            float y = size * Mathf.Sin(rad);
            verts.Add(new Vector3(x, y, 0));
            cols.Add(c);
        }

        // Triangles: center(0) -> i -> i+1
        for (int i = 1; i <= 6; i++)
        {
            int next = (i == 6) ? 1 : i + 1;
            tris.Add(0); tris.Add(i); tris.Add(next);
        }

        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    public static readonly Vector2Int[] NeighborsPointTop =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    //Check if inside world radius
    public static bool InsideHexRadius(Vector2Int a, int R)
    {
        int x = a.x, z = a.y, y = -x - z;
        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R;
    }

    // Compute the exact center of that hex in world space
    public static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + (Mathf.Sqrt(3f) / 2f) * a.y);
        float y = size * (1.5f * a.y);
        return new Vector2(x, y);
    }

    // World -> axial(q,r) (POINT-TOP) with cube rounding
    public static Vector2Int WorldToAxial_PointTop(Vector2 world, float size)
    {
        // From redblobgames point-top conversions:
        // q = (sqrt(3)/3 * x - 1/3 * y) / size
        // r = (2/3 * y) / size
        float qf = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.y) / size;
        float rf = (2f / 3f * world.y) / size;
        return CubeRound(qf, -qf - rf, rf); // cube (x,y,z) -> axial (x,z)
    }

    public Vector2Int GetGridPosFromWorldPos(Vector3 worldPos)
    {
        Vector2 worldV2 = new Vector2(worldPos.x, worldPos.y);
        return WorldToAxial_PointTop(worldV2, hexSize);
    }

    // Standard cube-rounding to nearest hex
    public static Vector2Int CubeRound(float x, float y, float z)
    {
        int rx = Mathf.RoundToInt(x), ry = Mathf.RoundToInt(y), rz = Mathf.RoundToInt(z);
        float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector2Int(rx, rz); // axial (q=rx, r=rz)
    }

    // Use to calculate disrtanc between two hexes in axial coordinates forexample WEAPON RANGE
    public int AxialDistance(Vector3 target_To, Vector3 target_From)
    {

        // Project to XY
        Vector2 aWorld2 = new Vector2(target_To.x, target_To.y);
        Vector2 bWorld2 = new Vector2(target_From.x, target_From.y);

        // World -> axial (q,r)
        Vector2Int aAx = WorldToAxial_PointTop(aWorld2, hexSize);
        Vector2Int bAx = WorldToAxial_PointTop(bWorld2, hexSize);

        // Axial -> cube and Chebyshev norm
        int ax = aAx.x, az = aAx.y, ay = -ax - az;
        int bx = bAx.x, bz = bAx.y, by = -bx - bz;

        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);
        int dz = Mathf.Abs(az - bz);

        return Mathf.Max(dx, dy, dz); // integer number of tiles

    }

}
