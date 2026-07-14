using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MoveToAction : RouteAction
{
    public Vector3 waypointPosition;
    public GameObject waypointObject;

    public override IEnumerator Perform(Unit owner, Action<ActionResult> callback)
    {
        if (owner == null)
        {
            callback(ActionResult.Failed);
            yield break;
        }

        Vector3 target = waypointObject == null ? waypointPosition : waypointObject.transform.position;

        // Visual helpers (UI only) — okay to call from action (not from movement service)
        HexHighlighter.Instance.HighlightHexUnderWorldPosition(target);

        // Calculate path once
        HexPathClickControllerPointTop_LineStrip.Instance.CalculatePath(target);
        List<Vector2Int> path = HexPathClickControllerPointTop_LineStrip.Instance.LastPath;

        if (path == null || path.Count < 2)
        {
            callback(ActionResult.Failed);
            yield break;
        }

        int remainingSteps = Mathf.Max(0, owner.shipRuntimeData.currentMovmentRange - owner.movedThisTurn);
        if (remainingSteps <= 0)
        {
            // no movement this turn → retry next turn if configured
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
            yield break;
        }

        bool finished = false;
        MovementResult moveRes = default;

        // Ask movement manager to move up to remainingSteps
        yield return MovementManager.Instance.MoveAlongPath(owner, path, remainingSteps, res =>
        {
            moveRes = res;
            finished = true;
        });

        while (!finished) yield return null;

        if (moveRes.Failed)
        {
            callback(ActionResult.Failed);
            yield break;
        }

        // If we reached path end -> Completed; otherwise we used full budget -> Retry
        if (moveRes.Completed)
            callback(ActionResult.Completed);
        else
            callback(waitIfActionAreNotComplete ? ActionResult.Retry : ActionResult.Failed);
    }
}
/*
using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class MoveToAction : RouteAction
{
    public Vector3 waypointPosition;
    public GameObject waypointObject;

    public override IEnumerator Perform(System.Action<ActionResult> callback)
    {
        Vector3 target = waypointObject == null
            ? waypointPosition
            : waypointObject.transform.position;

        //Put the hex highligheter on waypoint(Path finding calculate path to highlighter.)
        HexHighlighter.Instance.HighlightHexUnderWorldPosition(waypointPosition);

        // ✅ Calculate path once
        HexPathClickControllerPointTop_LineStrip.Instance.CalculatePath(target);

        var path = HexPathClickControllerPointTop_LineStrip.Instance.LastPath;

        // ✅ Run movement coroutine fully
        yield return MovementManager.Instance.MoveUnitAlongPath_Route(ownerUnit, path);

        // ✅ Check result AFTER movement
        if (Vector3.Distance(ownerUnit.transform.position, target) < 0.1f)
        {
            Debug.Log("Reached target");
            callback(ActionResult.Completed);
        }
        else
        {
            Debug.Log("Not reached target → retry next turn");
            callback(ActionResult.Retry);
        }
    }
}
*/