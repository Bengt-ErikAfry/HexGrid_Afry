using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool isMovingCamera = false; //Used to prevent clicking when camera are moving.

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.02f;
    public float minZoom = 3f;
    public float maxZoom = 10f;

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
    public IEnumerator MoveCameraTo(Vector3 destination, float duration, bool unscaled)
    {
        isMovingCamera = true;
        //Debug.Log("CameraManager: Moving camera to " + destination + " over " + duration + " seconds. Unscaled time: " + unscaled);
        Vector3 destinationCameraPos = new Vector3(destination.x, destination.y, Camera.main.transform.position.z);
        Vector3 start = Camera.main.transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(duration, 0.0001f);
            float k = ease.Evaluate(Mathf.Clamp01(t)); // easing curve

            Camera.main.transform.position = Vector3.LerpUnclamped(start, destinationCameraPos, k);
            yield return null;
        }

        Camera.main.transform.position = destinationCameraPos;
        isMovingCamera = false;
    }

    public void CenterOnSelectedObject()
    {
        //Show Highlighter
        HexHighlighter.Instance.ShowHighlight();

        //Higlight tile under the current unit
        HexHighlighter.Instance.HighlightHexUnderScreenPosition(Camera.main.WorldToScreenPoint(SelectionService.Instance.SelectedUnit.transform.position));

        //Move Camera
        StartCoroutine(MoveCameraTo(SelectionService.Instance.SelectedUnit.gameObject.transform.position, 1f, false));
    }
}
