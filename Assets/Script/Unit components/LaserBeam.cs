using System.Collections;
using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("References")]
    public LineRenderer laserPrefab;   // Assign your LineRenderer prefab in inspector

    [Header("Timing")]
    public float beamDuration = 0.08f; // how long the beam is fully visible
    public float fadeDuration = 0.12f; // how long it fades out

    [Header("Visuals")]
    public Gradient beamGradient;      // optional: override color over time
    public AnimationCurve widthOverLife = AnimationCurve.Linear(0, 1, 1, 0);

    /// <summary>
    /// Call this to shoot from 'from' to 'to'. For example, from your ship to the target ship.
    /// </summary>
    public void Fire(Vector3 from, Vector3 to)
    {
        StartCoroutine(FireBeamRoutine(from, to));
    }

    private IEnumerator FireBeamRoutine(Vector3 from, Vector3 to)
    {
        LineRenderer lr = Instantiate(laserPrefab);
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        // Initialize beam
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        // Optional: apply gradient if provided
        if (beamGradient != null)
            lr.colorGradient = beamGradient;

        // Cache initial widths if using a width curve
        float startWidth = lr.startWidth;
        float endWidth = lr.endWidth;

        // Show beam at full intensity
        float t = 0f;
        while (t < beamDuration)
        {
            // Keep the beam locked in case either ship moves during the flash
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            t += Time.deltaTime;
            yield return null;
        }

        // Fade out (both alpha and width)
        float ft = 0f;
        while (ft < fadeDuration)
        {
            float n = Mathf.Clamp01(ft / fadeDuration);

            // Fade color (alpha)
            if (lr.colorGradient != null)
            {
                // If using a gradient, we can modulate overall alpha via material color
                if (lr.material != null && lr.material.HasProperty("_Color"))
                {
                    Color c = lr.material.color;
                    c.a = 1f - n;
                    lr.material.color = c;
                }
            }
            else
            {
                // If no gradient, fade start/end colors directly
                Color s = lr.startColor; s.a = 1f - n;
                Color e = lr.endColor; e.a = 1f - n;
                lr.startColor = s;
                lr.endColor = e;
            }

            // Taper width
            float widthFactor = widthOverLife.Evaluate(n);
            lr.startWidth = startWidth * widthFactor;
            lr.endWidth = endWidth * widthFactor;

            // Keep endpoints updated (optional)
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);

            ft += Time.deltaTime;
            yield return null;
        }

        Destroy(lr.gameObject);
    }
}

