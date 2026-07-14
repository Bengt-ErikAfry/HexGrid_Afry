using System;
using System.Collections.Generic;
using UnityEngine;

public class HexHighlighter : MonoBehaviour
{
    public static HexHighlighter Instance { get; private set; }

    [Header("Highlight Hex")]
    public Material hexHighlight_Mat;       // Highlight material
    public Material weaponRange_Mat;        // Range highlight material (can be different)  
    public Material movmentRange_Mat;        // Movment range material
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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Create the reusable filled hex mesh for highlighting
        if (hexMesh == null)
        {
            hexMesh = BuildFilledPointTopHex(HexGridLinesBaker.Instance.hexSize, Color.yellow);
        }
    }

    // ---------- Hex Highlighting HEX----------
    public void HighlightHexUnderScreenPosition(Vector2 screenPos)  //!!! SCREEN POSITION !!!
    {
        // show the highlightGO if it's not active
        ShowHighlight();

        //Debug.Log("HighlightHexUnderScreenPosition called " + screenPos);
        // --- Highlight the hex under the selected object ---
        // For an orthographic 2D camera, Z is not used.
        var w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        // Convert selected object's world position -> axial (POINT-TOP), clamp to world radius
        var axial = WorldToAxial_PointTop(w, HexGridLinesBaker.Instance.hexSize);
        //Debug.Log("Axial coords: " + axial);

        //Check if inside world radius
        //Debug.Log("Inside world radius: " + InsideHexRadius(axial, worldRadius));
        if (!InsideHexRadius(axial, HexGridLinesBaker.Instance.worldRadius)) return;

        // Compute the exact center of that hex in world space
        Vector2 center = AxialToWorldCenter_PointTop(axial, HexGridLinesBaker.Instance.hexSize);
        //Debug.Log("World center of hex: " + center);

        // Create the highlight the first time, or move it
        var mf = highlightGO.GetComponent<MeshFilter>();
        var mr = highlightGO.GetComponent<MeshRenderer>();
        mf.sharedMesh = hexMesh;          // created once in Awake (filled point-top hex of radius hexSize)
        mr.sharedMaterial = hexHighlight_Mat; // Unlit or Sprites/Default; tint handled via vertex colors/material
        mr.sortingOrder = highligtSortingOrder; // ensure it renders above your background 
        highlightGO.transform.position = new Vector3(center.x, center.y, 0f);
        highlightGO.transform.eulerAngles = new Vector3(0, 180, 0); // Flip to face camera in some setups
    }
    public void HighlightHexUnderWorldPosition(Vector3 worldpos)  //!!! SCREEN POSITION !!!
    {
        // show the highlightGO if it's not active
        ShowHighlight();

        // Convert selected object's world position -> axial (POINT-TOP), clamp to world radius
        var axial = WorldToAxial_PointTop(worldpos, HexGridLinesBaker.Instance.hexSize);
        //Debug.Log("Axial coords: " + axial);

        //Check if inside world radius
        //Debug.Log("Inside world radius: " + InsideHexRadius(axial, worldRadius));
        if (!InsideHexRadius(axial, HexGridLinesBaker.Instance.worldRadius)) return;

        // Compute the exact center of that hex in world space
        Vector2 center = AxialToWorldCenter_PointTop(axial, HexGridLinesBaker.Instance.hexSize);
        //Debug.Log("World center of hex: " + center);

        // Create the highlight the first time, or move it
        var mf = highlightGO.GetComponent<MeshFilter>();
        var mr = highlightGO.GetComponent<MeshRenderer>();
        mf.sharedMesh = hexMesh;          // created once in Awake (filled point-top hex of radius hexSize)
        mr.sharedMaterial = hexHighlight_Mat; // Unlit or Sprites/Default; tint handled via vertex colors/material
        mr.sortingOrder = highligtSortingOrder; // ensure it renders above your background 
        highlightGO.transform.position = new Vector3(center.x, center.y, 0f);
        highlightGO.transform.eulerAngles = new Vector3(0, 180, 0); // Flip to face camera in some setups
    }

    public void HideHighlight()
    { 
        highlightGO.SetActive(false); 
    }

    public void ShowHighlight()
    {
        highlightGO.SetActive(true);
    }


    // ---------- RANGE HIGHLIGHTING ----------

    /// <summary>
    /// Highlights all hexes within 'range' of the hex under the provided screen position.
    /// Uses pooling to avoid allocations. Keeps 'highlightGO' (single) separate from range overlays.
    /// </summary>
    public void HighlightRangeUnderScreenPosition(Vector3 worldPosition, int range, Material highLight_Mat)
    {
        if (range < 0) return;

        // Make sure our reusable mesh exists
        if (hexMesh == null)
            hexMesh = BuildFilledPointTopHex(HexGridLinesBaker.Instance.hexSize, Color.yellow);



        Vector2Int centerAxial = WorldToAxial_PointTop(worldPosition, HexGridLinesBaker.Instance.hexSize);

        // If the clicked hex is outside the world bounds, bail
        if (!InsideHexRadius(centerAxial, HexGridLinesBaker.Instance.worldRadius))
        {
            ClearRangeHighlights();
            return;
        }

        // Compute the set of axial coords in range, clamped to world radius
        var inRange = HexesInRangeAxial(centerAxial, range, HexGridLinesBaker.Instance.worldRadius);

        // Update pooled instances: clear the previous active, then show the new set
        ClearRangeHighlights();

        foreach (var a in inRange)
        {
            var go = AcquireHighlightInstance();

            // Ensure components/material
            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();
            mf.sharedMesh = hexMesh;
            mr.sharedMaterial = highLight_Mat;
            mr.sortingOrder = highligtSortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Position at exact hex center
            Vector2 center = AxialToWorldCenter_PointTop(a, HexGridLinesBaker.Instance.hexSize);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.transform.eulerAngles = new Vector3(0, 180, 0); // match your single highlight orientation

            go.SetActive(true);
            active.Add(go);
            highlighted[a] = go;
        }
    }

    /// <summary>
    /// Returns all axial (q,r) coords within 'range' of 'center', clamped to a hex world radius.
    /// Uses cube coord math under the hood.
    /// </summary>
    private static List<Vector2Int> HexesInRangeAxial(Vector2Int center, int range, int worldRadius)
    {
        var results = new List<Vector2Int>(1 + 3 * range * (range + 1));

        // Convert axial (q,r) to cube (x,y,z) with y = -x - z
        int cx = center.x;
        int cz = center.y;
        int cy = -cx - cz;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(range, -dx + range); dy++)
            {
                int dz = -dx - dy;

                // Candidate cube coord
                int x = cx + dx;
                int y = cy + dy;
                int z = cz + dz;

                // Convert back to axial (q=x, r=z)
                var a = new Vector2Int(x, z);

                // Clamp to world radius
                if (InsideHexRadius(a, worldRadius))
                    results.Add(a);
            }
        }

        return results;
    }

    /// <summary>
    /// Only highlight the outer ring at an exact radius (optional helper).
    /// </summary>
    private static List<Vector2Int> HexRingAxial(Vector2Int center, int radius, int worldRadius)
    {
        var results = new List<Vector2Int>(radius == 0 ? 1 : radius * 6);
        if (radius == 0)
        {
            if (InsideHexRadius(center, worldRadius)) results.Add(center);
            return results;
        }

        // Cube directions for point-top axial
        // Directions in cube (x,y,z):
        var dirs = new (int x, int y, int z)[]
        {
        ( 1,-1, 0), ( 1, 0,-1), ( 0, 1,-1),
        (-1, 1, 0), (-1, 0, 1), ( 0,-1, 1)
        };

        // Start at one corner of the ring: center + dir5 * radius
        int cx = center.x, cz = center.y, cy = -cx - cz;
        int x = cx + dirs[4].x * radius;
        int y = cy + dirs[4].y * radius;
        int z = cz + dirs[4].z * radius;

        for (int side = 0; side < 6; side++)
        {
            var dir = dirs[side];
            for (int step = 0; step < radius; step++)
            {
                // Add axial
                var a = new Vector2Int(x, z);
                if (InsideHexRadius(a, worldRadius))
                    results.Add(a);

                // Move along this side
                x += dir.x;
                y += dir.y;
                z += dir.z;
            }
        }

        return results;
    }

    /// <summary>
    /// Hide and recycle all range highlight instances.
    /// </summary>
    public void ClearRangeHighlights()
    {
        for (int i = 0; i < active.Count; i++)
        {
            var go = active[i];
            if (go == null) continue;
            go.SetActive(false);
            pool.Add(go);
        }
        active.Clear();
        highlighted.Clear();
    }

    /// <summary>
    /// Get a pooled highlight instance or create a new one (simple MeshFilter+MeshRenderer GO).
    /// </summary>
    private GameObject AcquireHighlightInstance()
    {
        GameObject go;
        if (pool.Count > 0)
        {
            int last = pool.Count - 1;
            go = pool[last];
            pool.RemoveAt(last);
            return go;
        }

        go = new GameObject("HexRangeHighlight");
        go.hideFlags = HideFlags.None;
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        return go;
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

    //Check if inside world radius
    public static bool InsideHexRadius(Vector2Int a, int R)
    {
        int x = a.x, z = a.y, y = -x - z;
        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R;
    }
    
    // Compute the exact center of that hex in world space to place units/highlighter correctly
    public static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + (Mathf.Sqrt(3f) / 2f) * a.y);
        float y = size * (1.5f * a.y);
        return new Vector2(x, y);
    }
    
    // To get the clicked Hex
    public static Vector2Int WorldToAxial_PointTop(Vector2 world, float size)
    {
        // From redblobgames point-top conversions:
        // q = (sqrt(3)/3 * x - 1/3 * y) / size
        // r = (2/3 * y) / size
        float qf = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.y) / size;
        float rf = (2f / 3f * world.y) / size;
        return CubeRound(qf, -qf - rf, rf); // cube (x,y,z) -> axial (x,z)
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
}
