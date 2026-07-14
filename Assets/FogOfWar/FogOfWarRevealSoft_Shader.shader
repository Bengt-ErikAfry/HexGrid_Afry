Shader "Custom/FogOfWar/FogOfWarRevealSoft"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _RadiusX ("Radius X", Float) = 0.05
        _RadiusY ("Radius Y", Float) = 0.05
        _EdgeWidth ("Edge Width", Float) = 0.15
    }

    SubShader
    {
        ZTest Always
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _Center;
            float _RadiusX;
            float _RadiusY;
            float _EdgeWidth;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // Previous fog value
                float oldFog = tex2D(_MainTex, i.uv).r;

                // Distance from center, scaled by radius
                float2 d = (i.uv - _Center) / float2(_RadiusX, _RadiusY);
                float dist = length(d);

                // Smooth circular reveal
                float reveal = 1.0 - smoothstep(1.0 - _EdgeWidth, 1.0, dist);

                // Combine with old fog
                float result = max(oldFog, reveal);

                return fixed4(result, result, result, 1);
            }
            ENDCG
        }
    }
}
