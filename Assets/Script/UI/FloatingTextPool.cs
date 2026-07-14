using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [Header("Pool")]
    public FloatingText prefab;  // assign your FloatingHitText prefab
    public int prewarmCount = 8;
    public bool worldSpace = true;  // set true if your text lives in world space Canvas or 3D TMP

    private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();
    private Transform _root;

    void Awake()
    {
        _root = transform;
        Prewarm();
    }

    private void Prewarm()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            var item = CreateNew();
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
    }

    private FloatingText CreateNew()
    {
        var go = Instantiate(prefab, _root);
        go.Init(ReturnToPool, worldSpace);
        return go;
    }

    private void ReturnToPool(FloatingText item)
    {
        _pool.Enqueue(item);
    }

    public FloatingText Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }
        return CreateNew();
    }

    /// <summary>
    /// Spawn a text at position.
    /// For worldSpace=true, 'position' is world position.
    /// For worldSpace=false, 'position' is anchored position relative to the pool's RectTransform.
    /// </summary>
    public void Spawn(string text, Vector3 position, Color color, float fontSize, float? lifeTime = null, float? rise = null)
    {
        var item = Get();
        item.Play(text, position, color, fontSize, lifeTime, rise);
    }
}

