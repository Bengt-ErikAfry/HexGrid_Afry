using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RouteExecutor : MonoBehaviour
{
    public static RouteExecutor Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }
    // Tracks running route coroutines per unit
    private readonly Dictionary<Unit, Coroutine> runningRoutes = new Dictionary<Unit, Coroutine>();
    public bool IsRouteRunning(Unit unit)
    {
        return unit != null && runningRoutes.ContainsKey(unit);
    }
    public void StartRouteFor(Unit unit)
    {
        if (unit == null || unit.routeComponent == null) return;
        if (IsRouteRunning(unit)) return;

        Coroutine c = StartCoroutine(RunRouteCoroutine(unit));
        runningRoutes[unit] = c;
    }
    public void StopRouteFor(Unit unit)
    {
        if (unit == null) return;
        if (!runningRoutes.TryGetValue(unit, out var c)) return;

        StopCoroutine(c);
        runningRoutes.Remove(unit);
    }
    private IEnumerator RunRouteCoroutine(Unit unit)
    {
        yield return ExecuteRoute(unit);
        // Ensure dictionary cleanup even if ExecuteRoute exits early
        if (runningRoutes.ContainsKey(unit))
            runningRoutes.Remove(unit);
    }
    public IEnumerator ExecuteRoute(Unit unit)
    {
        if (unit == null || unit.routeComponent == null) yield break;

        var rc = unit.routeComponent;

        while (rc.actionIndex < rc.routeActions.Count)
        {
            var action = rc.routeActions[rc.actionIndex];
            bool finished = false;
            ActionResult result = ActionResult.Failed;

            yield return StartCoroutine(action.Perform(unit, r =>
            {
                result = r;
                finished = true;
            }));

            while (!finished) yield return null;

            if (result == ActionResult.Completed)
            {
                rc.actionIndex++;
                continue;
            }
            else if (result == ActionResult.Retry)
            {
                // Pause until next turn
                yield break;
            }
            else // Failed
            {
                // Skip failed action
                rc.actionIndex++;
            }
        }

        // Route completed
        //rc.routeActions.Clear();
        if(rc.loopRoute)rc.actionIndex = 0;
    }
    public IEnumerator ExecuteAllPlayerRoutes(IEnumerable<Unit> units)
    {
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.routeComponent == null) continue;

            // SKIP if there's no active route or if auto-run was disabled by the player
            if (!u.routeComponent.IsRouteStarted)
                continue;

            // Optional camera move/notification can be done by caller
            yield return StartCoroutine(ExecuteRoute(u));
        }
    }
}