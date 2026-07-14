using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance;

    [Header("Map Settings")]
    public Vector2 mapSize = new Vector2(100, 100);  // world size
    public int fogResolution = 512;                  // texture resolution

    [Header("Materials")]
    public Material revealMaterial;  // VisionReveal_Mat
    public Material mergeMaterial;   // FogMerge_Mat
    public Material displayMaterial; // FogDisplay_Mat

    [Header("Fog Settings")]
    [Range(0f, 1f)]
    public float exploredAlpha = 0.4f; // transparency of explored areas
    public float smoothEdgeWidth = 0.15f;

    // RenderTextures
    private RenderTexture visionRT;
    private RenderTexture exploredRT;

    // All units with fog vision
    public List<FogVisionSource> sources = new List<FogVisionSource>();

    // --- Singleton setup ---
    private void Awake()
    {
        Instance = this;
        CreateFogTextures();
        AssignTexturesToDisplay();
    }

    // --- Public API ---
    public void Register(FogVisionSource source)
    {
        if (!sources.Contains(source))
            sources.Add(source);
    }

    public void Unregister(FogVisionSource source)
    {
        if (sources.Contains(source))
            sources.Remove(source);
    }

    // --- Core loop ---
    private void LateUpdate()
    {
        // 1️⃣ Clear current vision
        ClearVision();

        // 2️⃣ Draw vision from each unit
        foreach (var s in sources)
        {
            RevealVision(s.transform.position, s.radius);
        }

        // 3️⃣ Merge vision into explored
        MergeVisionIntoExplored();
    }

    // --- Create RenderTextures ---
    private void CreateFogTextures()
    {
        visionRT = new RenderTexture(fogResolution, fogResolution, 0, RenderTextureFormat.R16);
        exploredRT = new RenderTexture(fogResolution, fogResolution, 0, RenderTextureFormat.R16);

        visionRT.wrapMode = exploredRT.wrapMode = TextureWrapMode.Clamp;
        visionRT.filterMode = exploredRT.filterMode = FilterMode.Bilinear;

        visionRT.Create();
        exploredRT.Create();

        // Initialize ExploredRT to black (unexplored)
        RenderTexture.active = exploredRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = null;
    }

    // --- Clear current frame's vision ---
    private void ClearVision()
    {
        RenderTexture.active = visionRT;
        //GL.Clear(false, true, Color.black);
        GL.Clear(false, true, new Color(0.07f,0.07f,0.07f));    //CHANGE THIS BACK TO ABOVE TO MAKE FOG BLACK.
        RenderTexture.active = null;
    }

    // --- Draw unit vision into VisionRT ---
    private void RevealVision(Vector3 worldPos, float radius)
    {
        Vector2 uv = new Vector2(
            (worldPos.x + mapSize.x * 0.5f) / mapSize.x,
            (worldPos.y + mapSize.y * 0.5f) / mapSize.y
        );

        revealMaterial.SetTexture("_MainTex", visionRT); // important
        revealMaterial.SetVector("_Center", uv);
        revealMaterial.SetFloat("_RadiusX", (radius + (smoothEdgeWidth*2)) / mapSize.x);
        revealMaterial.SetFloat("_RadiusY", (radius + (smoothEdgeWidth*2)) / mapSize.y);
        revealMaterial.SetFloat("_EdgeWidth", smoothEdgeWidth);

        RenderTexture temp = RenderTexture.GetTemporary(visionRT.descriptor);
        Graphics.Blit(visionRT, temp, revealMaterial);
        Graphics.Blit(temp, visionRT);
        RenderTexture.ReleaseTemporary(temp);
    }

    // --- Merge VisionRT into ExploredRT ---
    private void MergeVisionIntoExplored()
    {
        mergeMaterial.SetTexture("_Explored", exploredRT);
        mergeMaterial.SetTexture("_Vision", visionRT);

        RenderTexture temp = RenderTexture.GetTemporary(exploredRT.descriptor);
        Graphics.Blit(exploredRT, temp, mergeMaterial);
        Graphics.Blit(temp, exploredRT);
        RenderTexture.ReleaseTemporary(temp);
    }

    // --- Assign textures to display material ---
    private void AssignTexturesToDisplay()
    {
        displayMaterial.SetTexture("_VisionTex", visionRT);
        displayMaterial.SetTexture("_ExploredTex", exploredRT);
        displayMaterial.SetFloat("_ExploredAlpha", exploredAlpha);
    }
}
