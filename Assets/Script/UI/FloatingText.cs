using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("References")]
    public TMP_Text tmp;                     // Assign in inspector

    [Header("Defaults")]
    public float lifeTime = 0.9f;            // total display time
    public float riseDistance = 1.0f;        // units to move up in world or local units
    public Vector2 randomJitter = new Vector2(0.15f, 0.05f); // slight random offset
    public AnimationCurve alphaOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public AnimationCurve riseOverLife = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleOverLife = AnimationCurve.EaseInOut(0, 0.9f, 0.2f, 1.1f);

    // Internal
    private RectTransform _rect;
    private Vector3 _startPos;
    private Color _baseColor;
    private System.Action<FloatingText> _returnToPool;
    private bool _worldSpace;

    void Awake()
    {
        if (tmp == null) tmp = GetComponentInChildren<TMP_Text>(true);
        _rect = GetComponent<RectTransform>();
        _baseColor = tmp != null ? tmp.color : Color.white;
    }

    /// <summary>
    /// Initialize before playing the animation.
    /// If using a world-space Canvas or 3D TextMeshPro, pass worldSpace=true and provide a world position.
    /// If using a screen-space UI, pass worldSpace=false and provide a local anchored position.
    /// </summary>
    public void Init(System.Action<FloatingText> returnToPool, bool worldSpace)
    {
        _returnToPool = returnToPool;
        _worldSpace = worldSpace;
    }

    public void Play(string text, Vector3 position, Color color, float fontSize, float? lifetimeOverride = null, float? riseOverride = null)
    {
        if (tmp == null) return;

        tmp.text = text;
        tmp.color = color;
        _baseColor = color;
        tmp.fontSize = fontSize;

        lifeTime = lifetimeOverride ?? lifeTime;
        riseDistance = riseOverride ?? riseDistance;

        // random slight offset to reduce stacking overlap
        var jitter = new Vector3(Random.Range(-randomJitter.x, randomJitter.x),
                                 Random.Range(-randomJitter.y, randomJitter.y), 0f);

        if (_worldSpace)
        {
            transform.position = position + jitter;
            _startPos = transform.position;
        }
        else
        {
            // For screen-space UI, treat as anchored position
            if (_rect != null)
            {
                _rect.anchoredPosition = (Vector2)position + new Vector2(jitter.x, jitter.y);
                _startPos = _rect.anchoredPosition3D;
            }
            else
            {
                transform.localPosition = position + jitter;
                _startPos = transform.localPosition;
            }
        }

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float t = 0f;
        Vector3 endPos = _startPos + Vector3.up * riseDistance;

        while (t < lifeTime)
        {
            float n = Mathf.Clamp01(t / lifeTime);

            // Position
            Vector3 pos = Vector3.LerpUnclamped(_startPos, endPos, riseOverLife.Evaluate(n));
            if (_worldSpace)
            {
                transform.position = pos;
            }
            else if (_rect != null)
            {
                _rect.anchoredPosition3D = pos;
            }
            else
            {
                transform.localPosition = pos;
            }

            // Scale pulse
            float s = scaleOverLife.Evaluate(n);
            transform.localScale = Vector3.one * s;

            // Fade alpha
            var c = _baseColor;
            c.a = alphaOverLife.Evaluate(n);
            tmp.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        // Return to pool
        tmp.color = _baseColor;
        transform.localScale = Vector3.one;

        gameObject.SetActive(false);
        _returnToPool?.Invoke(this);
    }
}

