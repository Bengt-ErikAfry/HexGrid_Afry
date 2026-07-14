using UnityEngine;

public class FogVisionSource : MonoBehaviour
{
    public float radius = 6f;

    private void Start()
    {
        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.Register(this);

        radius = this.gameObject.GetComponent<Unit>().detectionRange;
    }
}