using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteComponent : MonoBehaviour
{
    [SerializeField]
    public List<RouteAction> routeActions = new List<RouteAction>();
    public int actionIndex = 0;
    public bool loopRoute;
    public bool IsRouteStarted;
    public bool HasActiveRoute() => routeActions != null && routeActions.Count > 0;

    // NOTE: Execution is handled by RouteExecutor. This component only stores route state.
}