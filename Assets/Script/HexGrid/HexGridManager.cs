using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;

// Lightweight manager: register all grids, track the active one.
public class HexGridManager : MonoBehaviour
{
    public static HexGridManager Instance { get; private set; }

    private readonly List<HexGridComponent> grids = new();
    public HexGridComponent CurrentHexGridComponent;
    public List<GameObject> markerPool = new();
    public List<GameObject> markerActive = new();
    public HexGridComponent gameViewHexGridComponent; // assigned in inspector, used for game view pathfinding

    //public List<Vector2Int> LastPath { get; private set; } = new(); //used in movementManager to move unit.


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Optionally DontDestroyOnLoad(gameObject);

        Register(gameViewHexGridComponent);
    }

    public void Register(HexGridComponent g)
    {
        if (!grids.Contains(g)) grids.Add(g);
        //if (CurrentHexGridComponent == null) CurrentHexGridComponent = g;
    }

    public void Unregister(HexGridComponent g)
    {
        grids.Remove(g);
        //if (CurrentHexGridComponent == g) CurrentHexGridComponent = grids.Count > 0 ? grids[0] : null;
    }

    public void SetActiveGrid(HexGridComponent g)
    {
        if (g == null)
        {
            Debug.LogError("Attempted to set active grid to null.");
            return;
        }
        
        if(!grids.Contains(g))
        { 
            Debug.LogError("Attempted to set active grid to a grid that is not registered.");
            return;
        }

        CurrentHexGridComponent = g;

        //Set the highlighet object
        Debug.Log("SetActiveGrid: setting highlightGO to " + g.name);
        HexHighlighter.Instance.highlightGO = g.highlightGO;

        // Optionally enable visuals for CurrentGrid and disable others
        foreach (var grid in grids) grid.SetVisualActive(grid == g);
    }

    public void HandleTapHexGrid(Vector2 screenPos)
    {
        // Convert tap -> world -> axial (point-top)
        var w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
        var target = HexMath.WorldToAxial_PointTop(w, HexGridLinesBaker.Instance.hexSize);

        // Start from selected unit's current cell
        var start = HexMath.WorldToAxial_PointTop(SelectionService.Instance.SelectedUnit.transform.position, HexGridLinesBaker.Instance.hexSize);

        //Clear previous path and markers(markers are cleard when ClearPath())
        ClearPath();

        // Compute A* (even if target is outside range)
        var path = HexAStarPointTop.FindPath(start, target, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);
        DrawLineStrip(path);

        // Place markers every 'movement' steps; always label target with final turn count
        Unit unit_Script = SelectionService.Instance.SelectedUnit.GetComponent<Unit>();
        PlaceTurnMarkers(path, unit_Script.shipRuntimeData.currentMovmentRange);

        // Save path for MovementManager (even if null or empty)
        SelectionService.Instance.SelectedUnit.currentMovePath = path ?? new List<Vector2Int>();
    }

    /*
    public void HandleTapTile(Vector2 screenPos)
    {
        if (CurrentHexGridComponent == null)
        {
            Debug.LogError("No active grid to handle tap.");
            return;
        }

        var minable = MiningUIManager.Instance?.currentMinable;
        if (minable == null)
        {
            Debug.LogWarning("HandleTapMiningObjectView: no active minable.");
            return;
        }

        // Convert screen pos to world point using camera and the tileParent's approximate depth.
        var tileParentTransform = MiningUIManager.Instance?.tileParent;
        var cam = Camera.main;
        if (cam == null || tileParentTransform == null)
        {
            Debug.LogWarning("HandleTapMiningObjectView: missing Camera.main or tileParent.");
            return;
        }

        // Use the tileParent world z as depth reference so ScreenToWorldPoint lands on the same plane as tiles.
        float depthZ = cam.WorldToScreenPoint(tileParentTransform.position).z;
        Vector3 sp = new Vector3(screenPos.x, screenPos.y, depthZ);
        Vector3 worldPoint = cam.ScreenToWorldPoint(sp);

        // Gather all instantiated tile views under tileParent and find the closest to the worldPoint.
        var tileViews = tileParentTransform.GetComponentsInChildren<MiningObjectTileData>(true);
        MiningObjectTileData closestTileView = null;
        float bestDistSq = float.MaxValue;
        foreach (var tv in tileViews)
        {
            if (tv == null) continue;
            var pos = tv.transform.position;
            float d2 = (new Vector2(pos.x, pos.y) - new Vector2(worldPoint.x, worldPoint.y)).sqrMagnitude;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                closestTileView = tv;
            }
        }

        if (closestTileView == null)
        {
            Debug.Log("HandleTapMiningObjectView: no tile view found near pointer.");
            ClearPath();
            SelectionService.Instance.SelectedUnit.currentMovePath = new List<Vector2Int>();
            return;
        }

        // Target axial is taken directly from the tile view's TileData (canonical).
        Vector2Int targetAxial = new Vector2Int(closestTileView.tileData.tileIndexCol, closestTileView.tileData.tileIndexRow);

        // Determine start axial:
        var selectedUnit = SelectionService.Instance.SelectedUnit;
        if (selectedUnit == null)
        {
            Debug.LogWarning("HandleTapMiningObjectView: no selected unit.");
            return;
        }

        Vector2Int startAxial;
        var parentTileOfUnit = selectedUnit.transform.GetComponentInParent<MiningObjectTileData>();
        if (parentTileOfUnit != null)
        {
            // Unit already placed inside the minable UI — use that tile's axial
            startAxial = new Vector2Int(parentTileOfUnit.tileData.tileIndexCol, parentTileOfUnit.tileData.tileIndexRow);
        }
        else
        {
            // Fallback: convert unit world position into axial relative to minable origin (minable.transform.position)
            Vector2 unitRel = new Vector2(
                selectedUnit.transform.position.x - minable.transform.position.x,
                selectedUnit.transform.position.y - minable.transform.position.y);
            startAxial = HexMath.WorldToAxial_PointTop(unitRel, minable.hexSize);
        }

        // Build blocked set from minable.tilesData
        var blocked = new HashSet<Vector2Int>();
        foreach (var td in minable.tilesData)
        {
            if (td.isBlocked) blocked.Add(new Vector2Int(td.tileIndexCol, td.tileIndexRow));
        }

        // Run A* using minable grid parameters
        var path = HexAStarPointTop.FindPath(startAxial, targetAxial, minable.worldRadius, blocked);

        // Convert path to world positions using minable hex size and minable.transform.position as origin
        if (path == null || path.Count == 0)
        {
            ClearPath();
            SelectionService.Instance.SelectedUnit.currentMovePath = new List<Vector2Int>();
            return;
        }

        CurrentHexGridComponent.currentLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 c = HexMath.AxialToWorldCenter_PointTop(path[i], minable.hexSize);
            Vector3 worldPos = new Vector3(c.x + minable.transform.position.x, c.y + minable.transform.position.y, 0f);
            CurrentHexGridComponent.currentLineRenderer.SetPosition(i, worldPos);
        }

        // Place markers using minable hex size / minable origin (uses a helper that mirrors PlaceTurnMarkers but for arbitrary hex size/origin)
        Unit unit_Script = selectedUnit.GetComponent<Unit>();
        int move = unit_Script != null ? unit_Script.shipRuntimeData.currentMovmentRange : 1;
        PlaceTurnMarkers_ForMinable(path, move, minable.hexSize, minable.transform.position);

        // Save path for MovementManager
        SelectionService.Instance.SelectedUnit.currentMovePath = path ?? new List<Vector2Int>();
    }*/

    private void DrawLineStrip(List<Vector2Int> path)
    {
        // Clear previous line
        CurrentHexGridComponent.currentLineRenderer.positionCount = 0;

        if (path == null || path.Count == 0)
        {
            ClearMarkers();
            return;
        }

        // Path contains cells INCLUDING start and target; convert to centers
        CurrentHexGridComponent.currentLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 c = HexMath.AxialToWorldCenter_PointTop(path[i], HexGridLinesBaker.Instance.hexSize);
            CurrentHexGridComponent.currentLineRenderer.SetPosition(i, new Vector3(c.x, c.y, 0f));
        }
    }

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
            Vector2 center = HexMath.AxialToWorldCenter_PointTop(path[idx], HexGridLinesBaker.Instance.hexSize);
            CreateMarker(turnLabel, center + CurrentHexGridComponent.markerOffset);
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
                Vector2 targetCenter = HexMath.AxialToWorldCenter_PointTop(path[targetIdx], HexGridLinesBaker.Instance.hexSize);
                CreateMarker(finalTurn, targetCenter + CurrentHexGridComponent.markerOffset);
            }
        }
    }

    // Helper: variant of PlaceTurnMarkers that uses provided hexSize and origin instead of global baker values.
    private void PlaceTurnMarkers_ForMinable(List<Vector2Int> path, int move, float hexSize, Vector3 origin)
    {
        ClearMarkers();

        if (path == null || path.Count <= 1) return;
        if (move <= 0) move = 1;

        int steps = path.Count - 1; // number of edges to traverse
        if (steps <= 0) return;

        int movedThisTurn = Mathf.Max(0, SelectionService.Instance.SelectedUnit.movedThisTurn);
        move = Mathf.Max(1, move);

        // Compute how many steps remain in the CURRENT turn (clamped)
        int remainingThisTurn = Mathf.Clamp(move - movedThisTurn, 0, move);

        // Helper to compute turn label for a step index (edge count)
        int ComputeTurnLabel(int stepIndex)
        {
            if (remainingThisTurn > 0 && stepIndex <= remainingThisTurn)
                return 1;

            int beyond = Mathf.Max(0, stepIndex - remainingThisTurn);
            int extraTurns = (beyond + move - 1) / move;
            return 1 + extraTurns;
        }

        // Build indices where markers go
        var indices = new List<int>();

        if (remainingThisTurn > 0 && remainingThisTurn <= steps)
            indices.Add(remainingThisTurn);

        int start = (remainingThisTurn > 0) ? (remainingThisTurn + move) : move;
        for (int i = start; i <= steps; i += move)
            indices.Add(i);

        // Place each marker with computed labels
        foreach (int idx in indices)
        {
            int turnLabel = ComputeTurnLabel(idx);
            Vector2 center = HexMath.AxialToWorldCenter_PointTop(path[idx], hexSize);
            Vector2 worldCenter = new Vector2(center.x + origin.x, center.y + origin.y);
            CreateMarker(turnLabel, worldCenter + CurrentHexGridComponent.markerOffset);
        }

        // Final target marker
        int remainingAfterCurrentTurn = Mathf.Max(0, steps - remainingThisTurn);
        int extraFullTurns = (remainingAfterCurrentTurn + move - 1) / move;
        int finalTurn = 1 + extraFullTurns;

        if (CurrentHexGridComponent.alwaysMarkTarget)
        {
            int targetIdx = steps;
            bool alreadyPlaced = indices.Count > 0 && indices[indices.Count - 1] == targetIdx;
            if (!alreadyPlaced)
            {
                Vector2 center = HexMath.AxialToWorldCenter_PointTop(path[targetIdx], hexSize);
                Vector2 worldCenter = new Vector2(center.x + origin.x, center.y + origin.y);
                CreateMarker(finalTurn, worldCenter + CurrentHexGridComponent.markerOffset);
            }
        }
    }

    //---------------------Calculate path from selected unit to vector3-------------

    public void CalculatePath(Vector3 position)
    {
        // Convert world -> axial (point-top)
        var target = HexMath.WorldToAxial_PointTop(position, HexGridLinesBaker.Instance.hexSize);

        // Start from selected unit's current cell
        var start = HexMath.WorldToAxial_PointTop(SelectionService.Instance.SelectedUnit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // Compute A* (even if target is outside range)
        var path = HexAStarPointTop.FindPath(start, target, HexGridLinesBaker.Instance.worldRadius, HexGridLinesBaker.Instance.blocked);
        DrawLineStrip(path);

        // Place markers every 'movement' steps; always label target with final turn count
        Unit unit_Script = SelectionService.Instance.SelectedUnit.GetComponent<Unit>();
        PlaceTurnMarkers(path, unit_Script.shipRuntimeData.currentMovmentRange);

        // Record for MovementManager
        SelectionService.Instance.SelectedUnit.currentMovePath = path ?? new List<Vector2Int>();

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
        Vector2Int currentStart = HexMath.WorldToAxial_PointTop(unit.transform.position, HexGridLinesBaker.Instance.hexSize);

        var combined = new List<Vector2Int>();
        Debug.Log("waypoints.Count " + waypoints.Count);
        foreach (var wp in waypoints)
        {
            Vector2Int targetAxial = HexMath.WorldToAxial_PointTop(wp, HexGridLinesBaker.Instance.hexSize);

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
        // Save path for MovementManager (even if null or empty)
        SelectionService.Instance.SelectedUnit.currentMovePath = combined ?? new List<Vector2Int>();
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
                Vector2 center = HexMath.AxialToWorldCenter_PointTop(combinedPath[cellIndex], HexGridLinesBaker.Instance.hexSize);
                CreateMarker(label, center + CurrentHexGridComponent.markerOffset);
            }
        }

        // Final marker on the target: total turns to reach final cell
        int remainingAfterCurrent = Mathf.Max(0, steps - remainingThisTurn);
        int extraFullTurns = (remainingAfterCurrent + move - 1) / move;
        int finalTurn = 1 + extraFullTurns;
        Vector2 targetCenter = HexMath.AxialToWorldCenter_PointTop(combinedPath[^1], HexGridLinesBaker.Instance.hexSize);
        CreateMarker(finalTurn, targetCenter + CurrentHexGridComponent.markerOffset);
    }

    public void ClearMarkers()
    {
        for (int i = 0; i < markerActive.Count; i++)
        {
            markerActive[i].SetActive(false);
            markerPool.Add(markerActive[i]);
        }
        markerActive.Clear();
    }

    public void ClearPath()
    {
        CurrentHexGridComponent.currentLineRenderer.positionCount = 0;
        ClearMarkers();
        SelectionService.Instance.SelectedUnit.currentMovePath.Clear();
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
            mf.sharedMesh = CurrentHexGridComponent.circleMesh;
            mr.sharedMaterial = CurrentHexGridComponent.turnMarker_BG_Material;
            mr.sortingOrder = CurrentHexGridComponent.turnMarker_BG_SortingOrder;
            //if (mr.sharedMaterial.HasProperty("_BaseColor")) mr.sharedMaterial.SetColor("_BaseColor", markerBackColor);
            //if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.SetColor("_Color", markerBackColor);

            // TextMesh (no TMP dependency)
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var tm = textGO.AddComponent<TextMesh>();
            tm.color = CurrentHexGridComponent.markerTextColor;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = CurrentHexGridComponent.turnMarkerFontSize;              // high font size, scale via transform
            MeshRenderer tr = tm.GetComponent<MeshRenderer>();
            tr.sortingOrder = CurrentHexGridComponent.turnMarker_Text_SortingOrder;
            textGO.transform.localScale = Vector3.one * 0.01f; // shrink to reasonable world size
            textGO.transform.localPosition = Vector3.zero;
            textGO.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        }

        go.transform.position = new Vector3(pos.x, pos.y, -0.05f);   // z-0.05 are to place it infront of the line.
        go.transform.localScale = new Vector3(CurrentHexGridComponent.markerScale, CurrentHexGridComponent.markerScale, CurrentHexGridComponent.markerScale);
        go.transform.eulerAngles = new Vector3(0f, 180f, 0f); // face the camera
        go.transform.SetParent(this.transform, true);

        // Update text
        var tmExisting = go.transform.Find("Text")?.GetComponent<TextMesh>();
        if (tmExisting) tmExisting.text = turnNumber.ToString();

        markerActive.Add(go);
    }
}