// Adds a noise-driven "black flame" silhouette to the top rim of the fog sprite
// (Assets/Art/Sprites/Fog/handsFOG 1.png) so the leading edge the player actually watches
// reads as a living mass instead of one flat image dragged upward. Everything below
// _EdgeBandStart in UV space is the untouched source art; only the band above it is carved.
//
// Noise samples off positionOS (object space, pre-TRS), not uv: the Surface child this
// renders on is stretched 60x400 by its parent transform (FogSystem.prefab), so uv-space
// noise would squash into a 6.67:1 oval instead of round flame cells. Object space stays a
// fixed quad regardless of how the GameObject is scaled, so _NoiseWorldScale sizes flame
// cells independent of that stretch.
Shader "NestLabs/Hazards/FogFlameEdge"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Flame Edge)]
        _EdgeBandStart ("Edge Band Start (UV.y)", Range(0, 1)) = 0.55
        _NoiseWorldScale ("Noise Cell Size", Float) = 1.6
        _ScrollSpeed ("Scroll Speed", Float) = 2.5
        _FlameThreshold ("Flame Threshold", Range(0, 1)) = 0.22
        _FlameSoftness ("Flame Softness", Range(0.001, 0.5)) = 0.08
        _RimColor ("Rim Color", Color) = (0, 0, 0, 1)
        _RimWidth ("Rim Width", Range(0.001, 0.3)) = 0.08
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.3)) = 0.12
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "CanUseSpriteAtlas" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // NOTE: do not ifdef these — SRP batcher requires every material in the batch to
            // share the same cbuffer layout.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _EdgeBandStart;
                float _NoiseWorldScale;
                float _ScrollSpeed;
                float _FlameThreshold;
                float _FlameSoftness;
                half4 _RimColor;
                float _RimWidth;
                float _OutlineThickness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // iq-style bilinear value noise: cheap, no texture lookup, good enough for a
            // flickering silhouette edge at two octaves.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.positionOS = input.positionOS;
                // unity_SpriteColor is the SpriteRenderer's own .color, wired in by the
                // renderer per-draw (or per-instance) regardless of GPU instancing state.
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 0 below the band, ramping to 1 at the sprite's top edge.
                float edgeMask = saturate((input.uv.y - _EdgeBandStart) / max(1.0 - _EdgeBandStart, 1e-4));

                float2 noiseUV = input.positionOS.xy / max(_NoiseWorldScale, 1e-4);
                noiseUV.y -= _Time.y * _ScrollSpeed;
                float coarse = ValueNoise(noiseUV);
                float fine = ValueNoise(noiseUV * 2.7 - _Time.y * _ScrollSpeed * 1.3);
                float noise = saturate(coarse * 0.7 + fine * 0.3) * edgeMask;

                float flame = smoothstep(_FlameThreshold, _FlameThreshold + _FlameSoftness, noise);
                float rim = saturate(smoothstep(_FlameThreshold - _RimWidth, _FlameThreshold, noise) - flame);

                // Thick outline ridge: a band that lights up only right where the flame fill
                // ends and fades out again _OutlineThickness further into "empty" noise space.
                // Riding the same scrolling noise as the fill, it reads as a heavy black rim
                // that licks/writhes past the solid mass rather than a static traced border.
                float outlineStart = _FlameThreshold + _FlameSoftness;
                float outline = smoothstep(outlineStart, outlineStart + _OutlineThickness, noise)
                               - smoothstep(outlineStart + _OutlineThickness, outlineStart + _OutlineThickness * 2.0, noise);
                outline = saturate(outline) * edgeMask;

                half3 rgb = lerp(baseTex.rgb, _RimColor.rgb, saturate(rim * edgeMask + outline));
                // max, not lerp: flame/outline ADD onto the source silhouette, they never fade
                // the original hand art out where the noise happens to stay weak.
                float alpha = max(baseTex.a, max(flame, outline));

                half4 col = half4(rgb, alpha) * input.color;
                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }
}
