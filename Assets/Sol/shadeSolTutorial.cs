using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class shadeSolTutorial : MonoBehaviour
{
    [Header("Renderers")]
    private SpriteRenderer srA;
    private SpriteRenderer srB;

    [Header("Animation")]
    //[SerializeField] Sprite[] sprites;
    public float frameTime = 0.15f;   // time between frames
    public float fadeDuration = 0.1f; // fade length
    public float rotSpeed = 10f;
    public GameObject image1_GO;
    public GameObject image2_GO;

    int spriteIndex;
    bool isFading;

    SpriteRenderer visible;
    SpriteRenderer hidden;

    void Awake()
    {
        srA= image1_GO.GetComponent<SpriteRenderer>();
        srB= image2_GO.GetComponent<SpriteRenderer>();

        visible = srA;
        hidden = srB;

        //visible.sprite = sprites[0];
        visible.color = Color.white;

        hidden.color = new Color(1, 1, 1, 0);
    }

    void Start()
    {
        StartCoroutine(Animate());
    }

    void Update()
    {
        image1_GO.transform.Rotate(Vector3.forward, rotSpeed * Time.deltaTime);
        image2_GO.transform.Rotate(Vector3.forward, -rotSpeed * Time.deltaTime);
    }

    IEnumerator Animate()
    {
        while (true)
        {
            yield return new WaitForSeconds(frameTime);
            yield return StartCoroutine(FadeToNext());
        }
    }

    IEnumerator FadeToNext()
    {
        if (isFading) yield break;
        isFading = true;

        //spriteIndex = (spriteIndex + 1) % sprites.Length;
        //hidden.sprite = sprites[spriteIndex];

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeDuration;

            visible.color = new Color(1, 1, 1, 1 - a);
            hidden.color = new Color(1, 1, 1, a);

            yield return null;
        }

        // snap final values
        visible.color = new Color(1, 1, 1, 0);
        hidden.color = new Color(1, 1, 1, 1);

        // swap roles
        var temp = visible;
        visible = hidden;
        hidden = temp;

        isFading = false;
    }
}
