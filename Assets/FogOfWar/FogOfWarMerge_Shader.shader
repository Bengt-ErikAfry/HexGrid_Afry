Shader "Custom/FogOfWar/FogOfWarMerge"
{
    Properties
    {
        _Explored ("Explored", 2D) = "black" {}
        _Vision ("Vision", 2D) = "black" {}
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

            sampler2D _Explored;
            sampler2D _Vision;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float explored = tex2D(_Explored, i.uv).r;
                float vision   = tex2D(_Vision, i.uv).r;

                float result = max(explored, vision);

                return fixed4(result, result, result, 1);
            }
            ENDCG
        }
    }
}
