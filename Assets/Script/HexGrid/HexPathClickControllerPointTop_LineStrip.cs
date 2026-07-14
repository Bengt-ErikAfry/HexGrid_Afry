
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Tap a hex to compute an A* path from the selected unit and draw it as a line strip.
/// Adds numeric markers every 'movement' steps (turn boundaries),
/// and labels the target with the final turn count.
/// POINT-TOP hex layout; no Tilemap required.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class HexPathClickControllerPointTop_LineStrip : MonoBehaviour
{
    public static HexPathClickControllerPointTop_LineStrip Instance { get; private set; }

    [Header("Line style")]
    public float lineWidth = 0.03f;
    public Color lineColor = new Color(1f, 0.9f, 0.1f, 0.85f); // warm yellow

    [Header("Marker style")]
    public Color markerTextColor = Color.white;
    public int turnMarkerFontSize = 64;                     // scale down via transform
    public Material turnMarker_BG_Material;                       // optional custom text material
    //public Color markerBackColor = new Color(0f, 0f, 0f, 0.6f); // semi-transparent dark bg
    public Vector2 markerOffset = new Vector2(0f, 0.08f);       // lift labels off the line
    public bool alwaysMarkTarget = true;
    public float markerScale = 1f;
    private Mesh circleMesh;
    public int turnMarker_BG_SortingOrder = 2;
    public int turnMarker_Text_SortingOrder = 3;

    [Tooltip("Radius of the round marker background in world units.")]
    public float markerRadius = 0.07f;               // ~4.5 px if 1 world unit = 64 px
    [Tooltip("Number of segments used to approximate the circle (12–24 is fine).")]
    public int circleSegments = 20;


    // Internal
    private LineRenderer lr;
    private Material unlitMat;  // for optional marker backgrounds
    private readonly List<GameObject> markerPool = new();
    private readonly List<GameObject> markerActive = new();

    public List<Vector2Int> LastPath { get; private set; } = new(); //used in movementManager to move unit.

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    
        lr = GetComponent<LineRenderer>();

        // Configure LineRenderer (unlit look)
        lr.positionCount = 0;
        lr.loop = false;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.numCornerVertices = 2;  // simple rounded corners
        lr.numCapVertices = 2;     // simple end caps
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;

        // Build an unlit material for marker backgrounds (optional)
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        unlitMat = new Material(shader);
        if (unlitMat.HasProperty("_BaseColor")) unlitMat.SetColor("_BaseColor", Color.white);
        if (unlitMat.HasProperty("_Color")) unlitMat.SetColor("_Color", Color.white);

        circleMesh = BuildCircleMesh(markerRadius, circleSegments);
    }

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    // ------------------- Calculate path when click/touch on screen -------------------
    public void HandleTap(Vector2 screenPos)
    {
        // Convert tap -> world -> axial (point-top)
        var w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
        var target = WorldToAxial_PointTop(w, HexGridLinesBaker.Instance.hexSize);

        // Start from selected unit's current cell
        var start = WorldToAxial_PointTop(SelectionService.Instance.SelectedUnit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // Compute A* (even if target is outside range)
        var path = HexAStarPointTop.FindPath(start, target, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);
        DrawLineStrip(path);

        // Place markers every 'movement' steps; always label target with final turn count
        Unit unit_Script = SelectionService.Instance.SelectedUnit.GetComponent<Unit>();
        PlaceTurnMarkers(path, unit_Script.shipRuntimeData.currentMovmentRange);

        // Record for MovementManager
        LastPath = path ?? new List<Vector2Int>();

    }

    public void HandleTapToObject(Vector3 objectPos, bool drawPath)
    {
        // Convert tap -> world -> axial (point-top)
        var target = WorldToAxial_PointTop(objectPos, HexGridLinesBaker.Instance.hexSize);

        // Start from selected unit's current cell
        var start = WorldToAxial_PointTop(SelectionService.Instance.SelectedUnit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // Compute A* (even if target is outside range)
        var path = HexAStarPointTop.FindPath(start, target, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);
        if(drawPath) DrawLineStrip(path);

        // Place markers every 'movement' steps; always label target with final turn count
        Unit unit_Script = SelectionService.Instance.SelectedUnit.GetComponent<Unit>();
        if (drawPath) PlaceTurnMarkers(path, unit_Script.shipRuntimeData.currentMovmentRange);

        // Record for MovementManager
        LastPath = path ?? new List<Vector2Int>();

    }

    //---------------------Calculate path from selected unit to vector3-------------
    
    public void CalculatePath(Vector3 position)
    {
        // Convert world -> axial (point-top)
        var target = WorldToAxial_PointTop(position, HexGridLinesBaker.Instance.hexSize);

        // Start from selected unit's current cell
        var start = WorldToAxial_PointTop(SelectionService.Instance.SelectedUnit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // Compute A* (even if target is outside range)
        var path = HexAStarPointTop.FindPath(start, target, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);
        DrawLineStrip(path);

        // Place markers every 'movement' steps; always label target with final turn count
        Unit unit_Script = SelectionService.Instance.SelectedUnit.GetComponent<Unit>();
        PlaceTurnMarkers(path, unit_Script.shipRuntimeData.currentMovmentRange);

        // Record for MovementManager
        LastPath = path ?? new List<Vector2Int>();

    }

    // -------------------- Calculate a route from a given start to a target --------------------

    // Calculate entire route path for a unit (unit -> wp1 -> wp2 -> ...)
    public void CalculateRoutePath(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("CalculateRoutePath: unit is null");
            ClearPath();
            return;
        }

        var rc = unit.GetComponent<RouteComponent>();
        if (rc == null || rc.routeActions == null || rc.routeActions.Count == 0)
        {
            ClearPath();
            return;
        }

        var waypoints = new List<Vector3>();
        foreach (var action in rc.routeActions)
        {
            if (action is MoveToAction m)
            {
                var pos = m.waypointObject != null ? m.waypointObject.transform.position : m.waypointPosition;
                waypoints.Add(pos);
            }
        }

        if (waypoints.Count == 0)
        {
            ClearPath();
            return;
        }

        CalculateRoutePathFromWaypoints(unit, waypoints);
    }

    // Build a single combined axial path for the sequence of waypoints and draw + mark it.
    // Markers are calculated continuously across the whole path so movement 'spills over' between segments.
    public void CalculateRoutePathFromWaypoints(Unit unit, List<Vector3> waypoints)
    {
        if (unit == null || waypoints == null || waypoints.Count == 0)
        {
            ClearPath();
            return;
        }

        // start cell (axial) from unit
        Vector2Int currentStart = WorldToAxial_PointTop(unit.transform.position, HexGridLinesBaker.Instance.hexSize);

        var combined = new List<Vector2Int>();

        foreach (var wp in waypoints)
        {
            Vector2Int targetAxial = WorldToAxial_PointTop(wp, HexGridLinesBaker.Instance.hexSize);

            var segment = HexAStarPointTop.FindPath(currentStart, targetAxial, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);

            if (segment == null || segment.Count == 0)
            {
                Debug.LogWarning($"CalculateRoutePathFromWaypoints: no path from {currentStart} to {targetAxial}. Skipping segment.");
                currentStart = targetAxial;
                continue;
            }

            // Avoid duplicating the junction cell when appending
            if (combined.Count > 0 && segment.Count > 0)
                segment.RemoveAt(0);

            combined.AddRange(segment);

            currentStart = targetAxial;
        }

        // Draw the single combined path
        DrawLineStrip(combined);

        // Place continuous turn markers across combined path (movement spills over)
        int move = Mathf.Max(1, unit.shipRuntimeData.currentMovmentRange);
        int movedThisTurn = Mathf.Max(0, unit.movedThisTurn);
        PlaceContinuousTurnMarkers(combined, move, movedThisTurn);

        // Save for movement usage
        LastPath = combined ?? new List<Vector2Int>();
    }

    // Place markers across a combined path using continuous movement budget.
    // - combinedPath: list of axial centers (cells) including start cell at index 0.
    // - move: max steps per full turn.
    // - movedThisTurn: how many steps already spent this turn (affects first boundary).
    private void PlaceContinuousTurnMarkers(List<Vector2Int> combinedPath, int move, int movedThisTurn)
    {
        ClearMarkers();

        if (combinedPath == null || combinedPath.Count <= 1) return;
        if (move <= 0) move = 1;

        int steps = combinedPath.Count - 1; // number of edges

        // compute remaining steps in current turn
        int remainingThisTurn = Mathf.Clamp(move - movedThisTurn, 0, move);

        // compute indices (edge counts) where markers should be placed (1-based edge index -> cell index = edgeIndex)
        var indices = new List<int>();

        // First marker (within current turn)
        if (remainingThisTurn > 0 && remainingThisTurn <= steps)
            indices.Add(remainingThisTurn);

        // Subsequent markers every 'move' steps after the first marker position
        int firstAfter = (remainingThisTurn > 0) ? remainingThisTurn + move : move;
        for (int e = firstAfter; e <= steps; e += move)
            indices.Add(e);

        // Place each marker with computed turn label (1 = current turn)
        foreach (int edgeIndex in indices)
        {
            // compute turn label: if within remainingThisTurn -> 1; else ceil((edgeIndex - remainingThisTurn) / move) + 1
            int label;
            if (remainingThisTurn > 0 && edgeIndex <= remainingThisTurn)
                label = 1;
            else
            {
                int beyond = Mathf.Max(0, edgeIndex - remainingThisTurn);
                int extraTurns = (beyond + move - 1) / move; // ceil
                label = 1 + extraTurns;
            }

            int cellIndex = edgeIndex; // cell index in combinedPath where this edge finishes
            if (cellIndex >= 0 && cellIndex < combinedPath.Count)
            {
                Vector2 center = AxialToWorldCenter_PointTop(combinedPath[cellIndex], HexGridLinesBaker.Instance.hexSize);
                CreateMarker(label, center + markerOffset);
            }
        }

        // Final marker on the target: total turns to reach final cell
        int remainingAfterCurrent = Mathf.Max(0, steps - remainingThisTurn);
        int extraFullTurns = (remainingAfterCurrent + move - 1) / move;
        int finalTurn = 1 + extraFullTurns;
        Vector2 targetCenter = AxialToWorldCenter_PointTop(combinedPath[^1], HexGridLinesBaker.Instance.hexSize);
        CreateMarker(finalTurn, targetCenter + markerOffset);
    }

    // ------------------- Line strip -------------------

    private void DrawLineStrip(List<Vector2Int> path)
    {
        // Clear previous line
        lr.positionCount = 0;

        if (path == null || path.Count == 0)
        {
            ClearMarkers();
            return;
        }

        // Path contains cells INCLUDING start and target; convert to centers
        lr.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[i], HexGridLinesBaker.Instance.hexSize);
            lr.SetPosition(i, new Vector3(c.x, c.y, 0f));
        }
    }

    public void ClearPath()
    {
        lr.positionCount = 0;
        ClearMarkers();
        LastPath.Clear();
    }

    // ------------------- Turn markers -------------------

    private void PlaceTurnMarkers(List<Vector2Int> path, int move)
    {

        /// <summary>
        /// Places turn markers along a path:
        /// - First marker is placed at 'movesLeftThisTurn'
        /// - Subsequent markers every 'maxMovesPerTurn'
        /// - Optionally always mark the target with the computed final turn number
        /// </summary>

        int movedThisTurn = SelectionService.Instance.SelectedUnit.movedThisTurn;     // how many steps the player can still move this current turn
        int maxMovesPerTurn = SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange;       // player’s maximum steps per full turn
        bool alwaysMarkTarget = true;

/// <summary>
/// Places turn markers along a path using how much the player has ALREADY moved this turn.
/// - movedThisTurn: steps already spent in the current turn
/// - maxMovesPerTurn: stride size for full turns
/// Behavior:
///   * If remainingThisTurn > 0, first marker is at remainingThisTurn with label 1 (this turn),
///     then every maxMovesPerTurn after that with labels 2, 3, ...
///   * If remainingThisTurn == 0 (all moves used), first marker is at maxMovesPerTurn with label 2,
///     then 3, 4, ...
/// Also optionally marks the target with the final turn count.
/// </summary>

        ClearMarkers();

        if (path == null || path.Count <= 1) return;

        int steps = path.Count - 1; // number of edges to traverse
        if (steps <= 0) return;

        movedThisTurn = Mathf.Max(0, movedThisTurn);
        maxMovesPerTurn = Mathf.Max(1, maxMovesPerTurn);

        // Compute how many steps remain in the CURRENT turn (clamped)
        int remainingThisTurn = Mathf.Clamp(maxMovesPerTurn - movedThisTurn, 0, maxMovesPerTurn);

        // Helper: compute the turn label for any stepIndex along the path
        // Turn 1 = the current turn (the one in which player already moved movedThisTurn).
        // Everything after finishing this turn increments by ceiling of strides of maxMovesPerTurn.
        int ComputeTurnLabel(int stepIndex)
        {
            // If we can still move 'remainingThisTurn' and we're within it → still turn 1
            if (remainingThisTurn > 0 && stepIndex <= remainingThisTurn)
                return 1;

            // Steps beyond the remaining portion are in future turns:
            // - If remainingThisTurn > 0, subtract that first, then ceil-div by stride.
            // - Base turn is always 1 (the current turn), so add +1*ceil_div for future chunks.
            int beyond = Mathf.Max(0, stepIndex - remainingThisTurn);
            int extraTurns = (beyond + maxMovesPerTurn - 1) / maxMovesPerTurn; // ceil division
            return 1 + extraTurns; // current turn (1) + how many full next-turn chunks we need
        }

        // Build indices where markers go
        var indices = new List<int>();

        // If there are steps remaining in THIS turn, place the first marker at that boundary.
        if (remainingThisTurn > 0 && remainingThisTurn <= steps)
            indices.Add(remainingThisTurn);

        // Then place markers every maxMovesPerTurn thereafter.
        // If remainingThisTurn == 0, we jump straight to the next full turn boundary.
        int start = (remainingThisTurn > 0) ? (remainingThisTurn + maxMovesPerTurn) : maxMovesPerTurn;
        for (int i = start; i <= steps; i += maxMovesPerTurn)
            indices.Add(i);

        // Place markers with computed labels
        foreach (int idx in indices)
        {
            int turnLabel = ComputeTurnLabel(idx);
            Vector2 center = AxialToWorldCenter_PointTop(path[idx], HexGridLinesBaker.Instance.hexSize);
            CreateMarker(turnLabel, center + markerOffset);
        }

        // Final turn for the target:
        // Use up remainingThisTurn in turn 1, then ceil-div the rest by maxMovesPerTurn.
        int remainingAfterCurrentTurn = Mathf.Max(0, steps - remainingThisTurn);
        int extraFullTurns = (remainingAfterCurrentTurn + maxMovesPerTurn - 1) / maxMovesPerTurn; // ceil
        int finalTurn = 1 + extraFullTurns;

        // Optionally mark target (avoid dup if last index already equals target)
        if (alwaysMarkTarget)
        {
            int targetIdx = steps;
            bool alreadyPlaced = indices.Count > 0 && indices[indices.Count - 1] == targetIdx;
            if (!alreadyPlaced)
            {
                Vector2 targetCenter = AxialToWorldCenter_PointTop(path[targetIdx], HexGridLinesBaker.Instance.hexSize);
                CreateMarker(finalTurn, targetCenter + markerOffset);
            }
        }
    



        /*
        ClearMarkers();
        if (path == null || path.Count <= 1 || move <= 0) return;

        int steps = path.Count - 1;            // number of edges between cells
        int maxTurn = Mathf.CeilToInt(steps / (float)move);

        // Place markers at multiples of 'move' steps from the start: move, 2*move, ...
        for (int step = move, turn = 1; step <= steps; step += move, turn++)
        {
            int index = step; // cell index along the path (since start is index 0)
            Vector2 center = AxialToWorldCenter_PointTop(path[index], HexGridLinesBaker.Instance.hexSize);
            CreateMarker(turn, center + markerOffset);
        }

        // Always mark target with final turn count (useful when target isn't on a boundary)
        if (alwaysMarkTarget)
        {
            Vector2 targetCenter = AxialToWorldCenter_PointTop(path[^1], HexGridLinesBaker.Instance.hexSize);
            CreateMarker(maxTurn, targetCenter + markerOffset);
        }
        */
    }


    private void ClearMarkers()
    {
        for (int i = 0; i < markerActive.Count; i++)
        {
            markerActive[i].SetActive(false);
            markerPool.Add(markerActive[i]);
        }
        markerActive.Clear();
    }

    private void CreateMarker(int turnNumber, Vector2 pos)
    {
        // Reuse from pool or create new
        GameObject go;
        if (markerPool.Count > 0)
        {
            go = markerPool[^1];
            markerPool.RemoveAt(markerPool.Count - 1);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject("TurnMarker");


            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            var mf = bg.AddComponent<MeshFilter>();
            var mr = bg.AddComponent<MeshRenderer>();
            mf.sharedMesh = circleMesh;
            mr.sharedMaterial = turnMarker_BG_Material;
            mr.sortingOrder = turnMarker_BG_SortingOrder;
            //if (mr.sharedMaterial.HasProperty("_BaseColor")) mr.sharedMaterial.SetColor("_BaseColor", markerBackColor);
            //if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.SetColor("_Color", markerBackColor);

            // TextMesh (no TMP dependency)
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var tm = textGO.AddComponent<TextMesh>();
            tm.color = markerTextColor;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = turnMarkerFontSize;              // high font size, scale via transform
            MeshRenderer tr = tm.GetComponent<MeshRenderer>();
            tr.sortingOrder = turnMarker_Text_SortingOrder;
            textGO.transform.localScale = Vector3.one * 0.01f; // shrink to reasonable world size
            textGO.transform.localPosition = Vector3.zero;
            textGO.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        }

        go.transform.position = new Vector3(pos.x, pos.y, -0.05f);   // z-0.05 are to place it infront of the line.
        go.transform.localScale = new Vector3(markerScale, markerScale, markerScale);
        go.transform.eulerAngles = new Vector3(0f, 180f, 0f); // face the camera
        go.transform.SetParent(this.transform, true);

        // Update text
        var tmExisting = go.transform.Find("Text")?.GetComponent<TextMesh>();
        if (tmExisting) tmExisting.text = turnNumber.ToString();

        markerActive.Add(go);
    }


    // ------------------- Circle mesh (round background) -------------------

    private static Mesh BuildCircleMesh(float radius, int segments)
    {
        segments = Mathf.Clamp(segments, 8, 128); // keep it sane
        var mesh = new Mesh { name = "MarkerCircle" };

        var verts = new List<Vector3>(segments + 1);
        var tris = new List<int>(segments * 3);
        var cols = new List<Color>(segments + 1);

        // center
        verts.Add(Vector3.zero);
        cols.Add(Color.white);

        // ring vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            verts.Add(new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f));
            cols.Add(Color.white);
        }

        // triangle fan
        for (int i = 1; i <= segments; i++)
        {
            int next = (i == segments) ? 1 : i + 1;
            tris.Add(0); tris.Add(i); tris.Add(next);
        }

        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }


    // ------------------- Point-top axial math -------------------

    private static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + (Mathf.Sqrt(3f) / 2f) * a.y);
        float y = size * (1.5f * a.y);
        return new Vector2(x, y);
    }

    private static Vector2Int WorldToAxial_PointTop(Vector2 world, float size)
    {
        float qf = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.y) / size;
        float rf = (2f / 3f * world.y) / size;
        return CubeRound(qf, -qf - rf, rf);
    }

    private static Vector2Int CubeRound(float x, float y, float z)
    {
        int rx = Mathf.RoundToInt(x), ry = Mathf.RoundToInt(y), rz = Mathf.RoundToInt(z);
        float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector2Int(rx, rz);
    }
}
