using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Unit unit_script;
    public int enemyHitChance = 50;

    private void Start()
    {
        unit_script = this.GetComponent<Unit>();
        unit_script.gunnerHitChance = enemyHitChance;
    }

    //Called from MovmentManager when mov are complete.
    /*
     * public void TryToAttack()
    {
        Debug.Log("TryToAttack Called");

        unit_script.TryToAttack(unit_script.otherInDetectionRange[0]);

        // End Enemy turn
        GameManager.Instance.EndOfTurneBtPressed();
    }*/

    /*
    public void TakeTurn()
    {
        // Placeholder for enemy AI turn logic
        Debug.Log("Enemy AI is taking its turn.");

        if(unit_script.otherInDetectionRange.Count > 0)
        {
            // Example: Move towards the first detected unit
            GameObject target = unit_script.otherInDetectionRange[0];
            Debug.Log("Enemy AI detected a unit: " + target.name);
            // Implement movement logic here

            // Find Path
            HexPathClickControllerPointTop_LineStrip.Instance.HandleTapToObject(target.transform.position);

            // Move to Target
            MovementManager.Instance.AdvanceOneTurn();

            // End Turn
            GameManager.Instance.EndTurn();

            //Reset Unit and UI for new turn.
            InputManager.Instance.selectedObject_Unit_Script.movedThisTurn = 0;
        }
        else
        {
            Debug.Log("No units detected. Enemy AI will wait.");
        }
    }*/
}
