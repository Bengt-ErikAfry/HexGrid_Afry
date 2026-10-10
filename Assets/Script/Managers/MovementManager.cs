using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MovementResult
{
    public bool Completed;
    public int StepsUsed;
    public bool Failed;
}

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance { get; private set; }

    [Header("Movement Tuning")]
    public float secondsPerHex = 0.25f;
    public bool faceDirection = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Move along path up to maxSteps; update owner.movedThisTurn as movement occurs.
    public IEnumerator MoveAlongPath(Unit unit, List<Vector2Int> path, int maxSteps, Action<MovementResult> callback)
    {
        if (unit == null || path == null || path.Count < 2)
        {
            callback(new MovementResult { Completed = false, StepsUsed = 0, Failed = true });
            yield break;
        }

        // If this is a player-controlled unit, ensure player input is blocked while the movement runs.
        bool changedState = false;
        GameplayStateId previousState = GameplayStateId.Selecting;
        if (unit.isPlayerControlled && GameStateMachine.Instance != null)
        {
            // Only set BlockPlayerInput if we're not already in that state (don't stomp existing block)
            if (GameStateMachine.Instance.CurrentId != GameplayStateId.BlockPlayerInput)
            {
                previousState = GameStateMachine.Instance.CurrentId;
                GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);
                changedState = true;
            }
        }

        // Determine start index from current world position
        Vector2Int curAxial = WorldToAxial_PointTop(unit.transform.position, TileManager.Instance.tileSize);
        int startIndex = IndexOfAxial(path, curAxial);
        if (startIndex < 0)
            startIndex = FindNearestPathIndex(path, unit.transform.position, TileManager.Instance.tileSize);

        int maxStepsOnPath = Mathf.Max(0, (path.Count - 1) - startIndex);
        int stepsToTake = Mathf.Min(maxSteps, maxStepsOnPath);
        if (stepsToTake <= 0)
        {
            // restore state if we set it
            if (changedState && GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentId == GameplayStateId.BlockPlayerInput)
                GameStateMachine.Instance.SetState(previousState);

            callback(new MovementResult { Completed = false, StepsUsed = 0, Failed = false });
            yield break;
        }

        // Build world centers for stepsToTake
        var centers = new List<Vector3>(stepsToTake + 1);
        for (int i = 0; i <= stepsToTake; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[startIndex + i], TileManager.Instance.tileSize);
            centers.Add(new Vector3(c.x, c.y, 0f));
        }

        // >>> ADDED: pass axial coords slice corresponding to centers so MoveAlongCenters can update visibility
        var coordsSlice = path.GetRange(startIndex, stepsToTake + 1);
        // <<< END ADDED

        int movedBefore = unit.movedThisTurn;

        // Animate movement along centers while spending movement points up to stepsToTake
        yield return MoveAlongCenters(unit, centers, coordsSlice, stepsToTake);

        int stepsUsed = Mathf.Max(0, unit.movedThisTurn - movedBefore);

        // --- NEW: Trim the unit.currentMovePath so that tiles already visited are removed.
        // Keep the remaining path starting from the tile where unit currently stands.
        try
        {
            int finalIndex = startIndex + stepsUsed;
            // Clamp finalIndex into valid range
            if (finalIndex < 0) finalIndex = 0;
            if (finalIndex >= path.Count)
            {
                // Consumed entire path
                unit.currentMovePath = new List<Vector2Int>();
            }
            else
            {
                // Include the current tile as first item in remaining path (consistent with existing logic that expects path[0] to be current tile)
                unit.currentMovePath = path.GetRange(finalIndex, path.Count - finalIndex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MovementManager: failed trimming currentMovePath: {ex}");
            // fallback: clear to avoid stale references
            unit.currentMovePath = new List<Vector2Int>();
        }
        // --- END TRIM ---

        // Check if reached final path target (path end)
        Vector2 lastCenter = AxialToWorldCenter_PointTop(path[^1], TileManager.Instance.tileSize);
        float dist = Vector2.Distance(new Vector2(unit.transform.position.x, unit.transform.position.y), lastCenter);
        bool completed = dist < 0.2f;

        // Invoke the callback with results
        callback(new MovementResult { Completed = completed, StepsUsed = stepsUsed, Failed = false });

        // Restore previous gameplay state if we changed it (and it's still BlockPlayerInput)
        if (changedState && GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentId == GameplayStateId.BlockPlayerInput)
        {
            GameStateMachine.Instance.SetState(previousState);
        }
    }

    // CALLD EVERY TIME A UNIT GET TO A NEW TILE/HEX
    private IEnumerator MoveAlongCenters(Unit unit, List<Vector3> centers, List<Vector2Int> coords, int stepBudget)
    {
        float totalLen = 0f;
        var segLen = new List<float>(centers.Count - 1);

        for (int i = 0; i < centers.Count - 1; i++)
        {
            float len = Vector3.Distance(centers[i], centers[i + 1]);
            segLen.Add(len);
            totalLen += len;
        }

        if (totalLen < 1e-6f)
            yield break;

        float[] cumLen = new float[centers.Count];
        for (int i = 1; i < centers.Count; i++)
            cumLen[i] = cumLen[i - 1] + segLen[i - 1];

        float duration = secondsPerHex * (centers.Count - 1);
        float t = 0f;

        int stepsDone = 0;
        int lastRevealedStep = 0;

        while (t < duration)
        {
            float u = Mathf.Clamp01(t / duration);
            float eased = EaseInOutCubic(u);
            float dist = eased * totalLen;

            int newSteps = FindStepsCompleted(cumLen, dist);

            // Spend movement points
            if (newSteps > stepsDone)
            {
                int diff = newSteps - stepsDone;
                int canSpend = Mathf.Min(diff, stepBudget - stepsDone);

                for (int k = 0; k < canSpend; k++)
                    unit.movedThisTurn++;

                stepsDone += canSpend;
            }

            // --- Reveal tiles for any newly entered steps (only when unit is in a Minable view) ---
            if (newSteps > lastRevealedStep)
            {
                // loop each newly entered step index
                for (int s = lastRevealedStep + 1; s <= newSteps && s < centers.Count; s++)
                {
                    Vector3 centerWorld = centers[s];

                    // >>> ADDED: notify TileManager that unit entered tile at coords[s]
                    // Use ceiling of detectionRange to cover fractional ranges
                    if (TileManager.Instance != null && unit != null)
                    {
                        int range = Mathf.CeilToInt(unit.detectionRange);
                        TileManager.Instance.UpdateUnitVisibilityOnMoveFromStored(unit, coords[s], range);
                    }
                    // <<< END ADDED

                    // existing minable reveal logic (preserve)
                    if (unit.unitLocationType == Unit.UnitLocationType.MinableObject)
                    {
                        var minable = unit.minableComponent;
                        if (minable != null)
                        {
                            minable.RevealTileByWorldPos(centerWorld);
                        }
                    }
                }

                lastRevealedStep = newSteps;
            }
            // --- end reveal logic ---

            // Move position
            Vector3 pos = centers[0];
            float accum = 0f;

            for (int i = 0; i < segLen.Count; i++)
            {
                float ln = segLen[i];
                if (accum + ln >= dist)
                {
                    float lerpT = (dist - accum) / Mathf.Max(ln, 1e-6f);
                    pos = Vector3.Lerp(centers[i], centers[i + 1], lerpT);

                    if (faceDirection)
                    {
                        Vector3 dir = (centers[i + 1] - centers[i]).normalized;
                        if (dir.sqrMagnitude > 1e-6f)
                        {
                            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                            unit.transform.rotation = Quaternion.Euler(0, 0, ang);
                        }
                    }
                    break;
                }
                accum += ln;
            }

            unit.transform.position = pos;
            t += Time.deltaTime;
            yield return null;
        }

        unit.transform.position = centers[^1];
    }

    // Helpers (copied from previous implementation)
    private static int FindStepsCompleted(float[] cumLen, float targetDist)
    {
        int i = 0;
        while (i + 1 < cumLen.Length && cumLen[i + 1] <= targetDist) i++;
        return Mathf.Clamp(i, 0, cumLen.Length - 1);
    }

    private static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }

    private static int IndexOfAxial(List<Vector2Int> path, Vector2Int cell)
    {
        for (int i = 0; i < path.Count; i++)
            if (path[i] == cell) return i;
        return -1;
    }

    private static int FindNearestPathIndex(List<Vector2Int> path, Vector3 pos, float size)
    {
        int best = 0;
        float bestD = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[i], size);
            float d = (new Vector2(pos.x, pos.y) - c).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }
        return best;
    }

    private static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + Mathf.Sqrt(3f) / 2f * a.y);
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
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance { get; private set; }

    [Header("Movement Tuning")]
    public float secondsPerHex = 0.25f;
    public bool faceDirection = false;

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

    // ─────────────────────────────────────────────────────────────
    //   MAIN PUBLIC METHOD (player + enemy)
    //   Moves one unit along a given path
    // ─────────────────────────────────────────────────────────────
    public IEnumerator MoveUnitAlongPath(Unit unit, List<Vector2Int> path)
    {
        Debug.Log("MovementManager: starting MoveUnitAlongPath coroutine for." + unit.name + " with path length " + path.Count);
        if (unit == null || path == null || path.Count < 2)
        {
            Debug.Log("MovementManager: invalid unit or path.");
            yield break;
        }

        // STEP 1 — Determine how far the unit can move
        int remainingSteps = Mathf.Max(0, unit.shipRuntimeData.currentMovmentRange - unit.movedThisTurn);
        if (remainingSteps <= 0)
        {
            Debug.Log("MovementManager: unit has no movement points left this turn.");
            yield break;
        }

        // STEP 2 — Find starting point along path
        Vector2Int curAxial = WorldToAxial_PointTop(unit.transform.position, HexGridLinesBaker.Instance.hexSize);
        int startIndex = IndexOfAxial(path, curAxial);
        if (startIndex < 0)
            startIndex = FindNearestPathIndex(path, unit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // STEP 3 — Clamp movement to available steps
        int maxStepsOnPath = Mathf.Max(0, (path.Count - 1) - startIndex);
        int stepsToTake = Mathf.Min(remainingSteps, maxStepsOnPath);
        if (stepsToTake <= 0)
            yield break;

        // STEP 4 — Build world-space positions
        var centers = new List<Vector3>(stepsToTake + 1);
        for (int i = 0; i <= stepsToTake; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[startIndex + i], HexGridLinesBaker.Instance.hexSize);
            centers.Add(new Vector3(c.x, c.y, 0f));
        }

        // STEP 5 — Run the animation (movement)
        yield return MoveAlongCenters(unit, centers, stepsToTake);

        // STEP 6 — Handle player-specific UI (optional but preserved)
        if (unit.isPlayerControlled)
        {
            //Clear previous path
            HexPathClickControllerPointTop_LineStrip.Instance.ClearPath();
            HexHighlighter.Instance.HideHighlight();

            //Set UI
            if (unit.movedThisTurn < unit.shipRuntimeData.currentMovmentRange)
            {
                UIManager.Instance.ResetUI();
                if (unit.haveAttackedThisTurn)
                {
                    UIManager.Instance.HideUnitAttackControlls();
                }
            }
            else
            {
                UIManager.Instance.HideUnitMovmentControlls();
            }
        }

        //Update SelectedUnitView
        UIManager.Instance.ShowSelectedUnitView();

        //Let control back to player
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Center on unit
        CameraManager.Instance.CenterOnSelectedObject();

        //Check if on mining mission
        if (unit.miningComponent != null)
        {
            if (unit.miningComponent.HasMission)
            {
                unit.miningComponent.OnTurn();
            }
        }
    }

    //Route Motion
    public IEnumerator MoveUnitAlongPath_Route(Unit unit, List<Vector2Int> path)
    {
        Debug.Log("MovementManager: starting MoveUnitAlongPath coroutine for." + unit.name + " with path length " + path.Count);
        if (unit == null || path == null || path.Count < 2)
        {
            Debug.Log("MovementManager: invalid unit or path.");
            yield break;
        }

        // STEP 1 — Determine how far the unit can move
        int remainingSteps = Mathf.Max(0, unit.shipRuntimeData.currentMovmentRange - unit.movedThisTurn);
        if (remainingSteps <= 0)
        {
            Debug.Log("MovementManager: unit has no movement points left this turn.");
            yield break;
        }

        // STEP 2 — Find starting point along path
        Vector2Int curAxial = WorldToAxial_PointTop(unit.transform.position, HexGridLinesBaker.Instance.hexSize);
        int startIndex = IndexOfAxial(path, curAxial);
        if (startIndex < 0)
            startIndex = FindNearestPathIndex(path, unit.transform.position, HexGridLinesBaker.Instance.hexSize);

        // STEP 3 — Clamp movement to available steps
        int maxStepsOnPath = Mathf.Max(0, (path.Count - 1) - startIndex);
        int stepsToTake = Mathf.Min(remainingSteps, maxStepsOnPath);
        if (stepsToTake <= 0)
            yield break;

        // STEP 4 — Build world-space positions
        var centers = new List<Vector3>(stepsToTake + 1);
        for (int i = 0; i <= stepsToTake; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[startIndex + i], HexGridLinesBaker.Instance.hexSize);
            centers.Add(new Vector3(c.x, c.y, 0f));
        }

        // STEP 5 — Run the animation (movement)
        yield return MoveAlongCenters(unit, centers, stepsToTake);

        // STEP 6 — Handle player-specific UI (optional but preserved)
        
    }

    // ─────────────────────────────────────────────────────────────
    //   INTERNAL MOVEMENT ANIMATION
    // ─────────────────────────────────────────────────────────────
    private IEnumerator MoveAlongCenters(Unit unit, List<Vector3> centers, int stepBudget)
    {
        float totalLen = 0f;
        var segLen = new List<float>(centers.Count - 1);

        for (int i = 0; i < centers.Count - 1; i++)
        {
            float len = Vector3.Distance(centers[i], centers[i + 1]);
            segLen.Add(len);
            totalLen += len;
        }

        if (totalLen < 1e-6f)
            yield break;

        float[] cumLen = new float[centers.Count];
        for (int i = 1; i < centers.Count; i++)
            cumLen[i] = cumLen[i - 1] + segLen[i - 1];

        float duration = secondsPerHex * (centers.Count - 1);
        float t = 0f;

        int stepsDone = 0;

        while (t < duration)
        {
            float u = Mathf.Clamp01(t / duration);
            float eased = EaseInOutCubic(u);
            float dist = eased * totalLen;

            int newSteps = FindStepsCompleted(cumLen, dist);

            // Spend movement points
            if (newSteps > stepsDone)
            {
                int diff = newSteps - stepsDone;
                int canSpend = Mathf.Min(diff, stepBudget - stepsDone);

                for (int k = 0; k < canSpend; k++)
                    unit.movedThisTurn++;

                stepsDone += canSpend;
            }

            // Move position
            Vector3 pos = centers[0];
            float accum = 0f;

            for (int i = 0; i < segLen.Count; i++)
            {
                float ln = segLen[i];
                if (accum + ln >= dist)
                {
                    float lerpT = (dist - accum) / Mathf.Max(ln, 1e-6f);
                    pos = Vector3.Lerp(centers[i], centers[i + 1], lerpT);

                    if (faceDirection)
                    {
                        Vector3 dir = (centers[i + 1] - centers[i]).normalized;
                        if (dir.sqrMagnitude > 1e-6f)
                        {
                            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                            unit.transform.rotation = Quaternion.Euler(0, 0, ang);
                        }
                    }
                    break;
                }
                accum += ln;
            }

            unit.transform.position = pos;
            t += Time.deltaTime;
            yield return null;
        }

        unit.transform.position = centers[^1];
    }


    // ─────────────────────────────────────────────────────────────
    //   HELPERS (unchanged)
    // ─────────────────────────────────────────────────────────────
    private static int FindStepsCompleted(float[] cumLen, float targetDist)
    {
        int i = 0;
        while (i + 1 < cumLen.Length && cumLen[i + 1] <= targetDist) i++;
        return Mathf.Clamp(i, 0, cumLen.Length - 1);
    }

    private static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }

    private static int IndexOfAxial(List<Vector2Int> path, Vector2Int cell)
    {
        for (int i = 0; i < path.Count; i++)
            if (path[i] == cell) return i;
        return -1;
    }

    private static int FindNearestPathIndex(List<Vector2Int> path, Vector3 pos, float size)
    {
        int best = 0;
        float bestD = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 c = AxialToWorldCenter_PointTop(path[i], size);
            float d = (new Vector2(pos.x, pos.y) - c).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }
        return best;
    }

    private static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + Mathf.Sqrt(3f) / 2f * a.y);
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
*/