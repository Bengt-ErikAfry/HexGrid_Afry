using UnityEngine;

public class Col_OtherDetectionRange : MonoBehaviour
{
    public Unit unit_Script;

    private void Start()
    {
        unit_Script = this.transform.root.gameObject.GetComponent<Unit>();
    }
    public void OnTriggerEnter(Collider other)
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
    void OnTriggerExit(Collider other)
    {
        unit_Script.DetectionTriggerExitWithOther(other.transform.root.gameObject);
    }
}
