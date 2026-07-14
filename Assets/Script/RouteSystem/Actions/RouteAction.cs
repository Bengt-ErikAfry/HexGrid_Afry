
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class RouteAction
{
    public bool waitIfActionAreNotComplete;

    // New contract: owner passed explicitly, callback must be invoked with ActionResult.
    public abstract IEnumerator Perform(Unit owner, Action<ActionResult> callback);
}

public enum ActionResult
{
    Completed,
    Retry,   // pause and resume next turn
    Failed
}
/*
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[System.Serializable]
public abstract class RouteAction 
{
    //public int MaxRetries = 0;   // set by player
    //protected int currentRetries = 0;
    public bool waitIfActionAreNotComplete;
    public Unit ownerUnit;  //So that actrions know what unit to .. move

    public IEnumerator Execute(System.Action<ActionResult> callback)
    {
        yield return Perform(result =>
        {
            if (result == ActionResult.Completed)
            {
                //currentRetries = 0;
                callback(ActionResult.Completed);
            }
            else
            {
                //Debug.Log("Retrying action: " + currentRetries);
                if (waitIfActionAreNotComplete)
                {
                    callback(ActionResult.Retry);
                    Debug.Log("Retry action");
                }
                else
                {
                    callback(ActionResult.Failed);
                    Debug.Log("Failed action no waiting");
                }
                // }
            }
        });
    }

    public abstract IEnumerator Perform(System.Action<ActionResult> callback);

}

public enum ActionResult
{
    Completed,
    Retry,
    Failed
}
*/