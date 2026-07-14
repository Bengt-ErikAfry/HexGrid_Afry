Shader "Custom/FogOfWar/FogOfWarDisplay"
{
    Properties
    {
        _ExploredTex ("Explored", 2D) = "black" {}
        _VisionTex ("Vision", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _ExploredAlpha ("Explored Alpha", Range(0,1)) = 0.4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ExploredTex;
            sampler2D _VisionTex;
            float4 _FogColor;
            float _ExploredAlpha;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // Sample textures
                float explored = tex2D(_ExploredTex, i.uv).r;
                float vision   = tex2D(_VisionTex, i.uv).r;

                // Smooth alpha for vision
                // Fully visible areas fade from 1→0 smoothly
                float alphaVision = 1.0 - vision;

                // Blend explored areas
                float alphaExplored = lerp(1.0, _ExploredAlpha, explored);

                // Combine: take the minimum alpha to preserve smooth vision edges
                float alpha = min(alphaVision, alphaExplored);

                return fixed4(_FogColor.rgb, alpha);
            }
            ENDCG
        }
    }
}