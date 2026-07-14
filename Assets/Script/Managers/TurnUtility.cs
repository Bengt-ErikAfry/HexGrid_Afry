
using System.Collections;
using UnityEngine;

public static class TurnUtility
{
    public static IEnumerator WaitForNextTurn()
    {
        bool triggered = false;

        void Handler()
        {
            triggered = true;
        }

        GameManager.Instance.OnNewTurn += Handler;

        yield return new WaitUntil(() => triggered);

        GameManager.Instance.OnNewTurn -= Handler;
    }
}

