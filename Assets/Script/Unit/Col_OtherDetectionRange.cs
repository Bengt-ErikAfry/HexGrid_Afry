using UnityEngine;

public class Col_OtherDetectionRange : MonoBehaviour
{
    public Unit unit_Script;

    private void Start()
    {
        unit_Script = this.transform.parent.gameObject.GetComponent<Unit>();
    }
    public void OnTriggerEnter(Collider other)
    {
        //Check to see if somethings wrong in the inspector
        if(unit_Script == null)
        {
            Debug.LogError("Col_OtherDetectionRange: unit_Script is null on " + this.transform.parent.name);
            return;
        }
        //When player move in MinavbelObject view the fog is not present so no reveal of enemies nessesary.
        if (unit_Script.unitLocationType == Unit.UnitLocationType.GameView)
        {
            //Debug.Log("Col_PlayerDetectionRange call Other: " + other.transform.root.name + " this: " + this.transform.root.name);

            if (other.transform.root.gameObject.tag == "Player" &&
            this.gameObject.tag != "ShipSize" &&
            other.gameObject.tag != "DetectionRange" &&
            other.transform.root.gameObject.tag != this.transform.root.gameObject.tag)
            {
                unit_Script.DetectionTriggerColidedWithOther(other.transform.root.gameObject);
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        //When player move in MinavbelObject view the fog is not present so no reveal of enemies nessesary.
        if (unit_Script.unitLocationType == Unit.UnitLocationType.GameView)
        {
            unit_Script.DetectionTriggerExitWithOther(other.transform.root.gameObject);
        }
    }
}
