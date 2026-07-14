
Shader "Custom/Copilot_StarChart"
{
    Properties
    {
        // --- Stars ---
        _StarDensity("Star Density", Float) = 110.0       // how many stars (count only)
        _StarSize("Star Size (cell units)", Range(0.005,0.06)) = 0.018
        _StarBaseColor("Star Base Color", Color) = (1,1,1,1)
        _StarTintSaturation("Star Tint Saturation (0..1)", Range(0,1)) = 0.25

        // --- Nebula ---
        _NebulaIntensity("Nebula Intensity", Range(0,2)) = 0.9
        _NebulaScale("Nebula Scale (freq)", Range(1, 40)) = 12.0
        _NebulaGain("Nebula Gain (amp decay)", Range(0.2, 0.9)) = 0.55
        _NebulaLacunarity("Nebula Lacunarity (freq growth)", Range(1.5, 3.0)) = 2.0
        _NebulaWarp("Nebula Warp Strength", Range(0, 2)) = 0.6
        _NebulaContrast("Nebula Contrast", Range(0.5, 2.5)) = 1.4
        _NebulaSpeed("Nebula Drift Speed", Range(0, 0.2)) = 0.02

        // Palette (3 colors blended by nebula value)
        _PaletteA("Nebula Color A", Color) = (0.05, 0.08, 0.15, 1)    // deep blue/indigo
        _PaletteB("Nebula Color B", Color) = (0.15, 0.2, 0.35, 1)     // blue
        _PaletteC("Nebula Color C", Color) = (0.7, 0.35, 0.8, 1)      // magenta/purple

        _BackgroundColor("Background Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "IgnoreProjector"="True" }
        Pass
        {
            Name "StarChart"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha      // allows soft blend of stars & nebula over background

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_Position;
                float2 uv          : TEXCOORD0;
            };

            // --- Material params ---
            CBUFFER_START(UnityPerMaterial)
                // Stars
                float  _StarDensity;
                float  _StarSize;
                float4 _StarBaseColor;
                float  _StarTintSaturation;

                // Nebula
                float  _NebulaIntensity;
                float  _NebulaScale;
                float  _NebulaGain;
                float  _NebulaLacunarity;
                float  _NebulaWarp;
                float  _NebulaContrast;
                float  _NebulaSpeed;

                float4 _PaletteA;
                float4 _PaletteB;
                float4 _PaletteC;

                float4 _BackgroundColor;
            CBUFFER_END

            // --- Vertex ---
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(float4(IN.positionOS, 1.0));
                OUT.uv = IN.uv;
                return OUT;
            }

            // --- Hash / noise helpers (cheap & mobile friendly) ---

            // Hash 2D -> 1D in [0,1)
            float hash21(float2 p)
            {
                // IQ-ish hash (very cheap)
                float3 p3 = frac(float3(p.x, p.y, p.x + p.y) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Hash 2D -> 2D in [0,1)^2
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x + p.y) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                float2 q = float2(p3.x, p3.y);
                q = frac(q * 13.13 + p3.z * 37.37);
                return q;
            }

            // Smooth fade for interpolation
            float2 fade2(float2 t) { return t * t * (3.0 - 2.0 * t); }

            // 2D Value noise (grid hash + bilerp)
            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = fade2(f);

                float a = hash21(i + float2(0,0));
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float ab = lerp(a, b, u.x);
                float cd = lerp(c, d, u.x);
                return lerp(ab, cd, u.y);
            }

            // 2D FBM over value noise (4 octaves: cheap & clean)
            float fbmValue(float2 uv)
            {
                float amp = 0.5;
                float freq = 1.0;
                float sum = 0.0;

                // 4 octaves: good compromise for mobile
                [unroll]
                for (int o = 0; o < 4; o++)
                {
                    sum += amp * valueNoise(uv * freq);
                    freq *= _NebulaLacunarity;
                    amp  *= _NebulaGain;
                }
                return sum;
            }

            // Simple palette: blend A->B->C based on t
            float3 palette(float t, float3 A, float3 B, float3 C)
            {
                t = saturate(t);
                // smoother mid-tones
                float t2 = t * t * (3 - 2 * t);
                float3 AB = lerp(A, B, t2);
                // bias the second blend so C comes in at higher values
                float t3 = saturate((t - 0.4) / 0.6);
                return lerp(AB, C, t3);
            }

            // Random RGB per star (independent channels); soft tint
            float3 randomStarRGB(float2 cell)
            {
                float r = hash21(cell + float2(11.1, 3.7));
                float g = hash21(cell + float2(23.5, 9.9));
                float b = hash21(cell + float2(37.7, 17.3));
                return float3(r, g, b);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // --- 1) Stars (uniform size, no parallax) ---
                // Keep a fixed grid scale independent of density to decouple size from count
                const float cellsPerAxis = 140.0;  // slightly finer grid than 120 for smoother distribution
                float2 gridUV  = IN.uv * cellsPerAxis;
                float2 cell    = floor(gridUV);
                float2 local   = frac(gridUV);

                // Density -> spawn probability only
                float spawnProb = saturate(_StarDensity / 220.0);

                // 3x3 neighborhood to keep stars round across borders
                float bestDist = 1e9;
                float2 bestCell = cell;
                bool found = false;

                [unroll]
                for (int oy = -1; oy <= 1; oy++)
                {
                    [unroll]
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        float2 nCell = cell + float2(ox, oy);

                        float seed = hash21(nCell);
                        if (seed > spawnProb) continue;

                        // Jittered center
                        float2 center = hash22(nCell);
                        float2 posInNeighbor = local - float2(ox, oy);
                        float dist = length(posInNeighbor - center);

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestCell = nCell;
                            found = true;
                        }
                    }
                }

                // Star disc (uniform size from slider)
                float starIntensity = 0.0;
                float3 starColor = float3(0,0,0);
                if (found)
                {
                    // fixed star size in "cell units"
                    float r = _StarSize;
                    // soft disc
                    starIntensity = 1.0 - smoothstep(0.0, r, bestDist);

                    // star color = base -> random RGB based on saturation
                    float3 randRGB = randomStarRGB(bestCell);
                    float3 tinted  = lerp(_StarBaseColor.rgb, randRGB, saturate(_StarTintSaturation));
                    starColor = tinted;
                }

                // --- 2) Nebula (FBM value noise + domain warp + palette) ---
                // Slow drift over time (independent of camera)
                float t = _Time.y * _NebulaSpeed;

                // Base frequency (bigger scale => bigger features)
                float2 uvN = IN.uv * _NebulaScale;

                // Domain warp: displace sampling coords by low-frequency noise vector
                float2 warpOffset = float2(
                    valueNoise(uvN * 0.45 + t),
                    valueNoise(uvN * 0.52 - t)
                );
                warpOffset = (warpOffset - 0.5) * _NebulaWarp;

                float n = fbmValue(uvN + warpOffset);

                // Shape and contrast
                // Normalize FBM ~ [0,1], then apply gentle contrast
                n = saturate(n);
                n = pow(n, _NebulaContrast);

                // Tiny dithering to reduce banding (hash of pixel)
                float dither = (hash21(IN.uv * 1024.0) - 0.5) * (1.0/255.0);
                n = saturate(n + dither);

                // Palette
                float3 nebulaColor = palette(n, _PaletteA.rgb, _PaletteB.rgb, _PaletteC.rgb);

                // Intensity & background blend
                float3 background = _BackgroundColor.rgb;
                float3 nebulaFinal = lerp(background, nebulaColor, saturate(_NebulaIntensity));

                // --- 3) Composite stars over nebula ---
                // Stars brighten over nebula; clamp to avoid overblown
                float3 outRGB = lerp(nebulaFinal, starColor, saturate(starIntensity));
                return float4(outRGB, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
