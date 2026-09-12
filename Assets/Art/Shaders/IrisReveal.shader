// Unity's built-in UI-Default shader, repurposed as an iris reveal: the mirror image of
// IrisClose. Instead of sampling a sprite it paints a solid fill everywhere OUTSIDE a hole of
// radius _Radius (in aspect-corrected UV space) centred on _Center. Drive _Radius from 0 up past
// the far screen corner and a hole blooms open from a point until the fill is gone, uncovering
// whatever sits behind. Colour comes from the vertex / material tint (white for the menu), unlike
// IrisClose which hardcodes black. Alpha and every Canvas stencil / clip-rect op are left as the
// stock shader so the material still nests correctly inside a Canvas. Driven per-instance from
// MenuIntroSequence; expects a full-stretch Image so texcoord spans the screen.
Shader "NestLabs/UI/IrisReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _Radius ("Radius", Range(0, 3)) = 0
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.0005, 0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _Radius;
            float4 _Center;
            float _Aspect;
            float _EdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Aspect-corrected distance from the hole centre. _MainTex is not sampled — the
                // fill is a flat colour, the texture stays bound only for the UI batch path.
                float2 d = IN.texcoord - _Center.xy;
                d.x *= _Aspect;
                float outside = smoothstep(_Radius, _Radius + _EdgeSoftness, length(d));

                fixed4 col = fixed4(IN.color.rgb, outside * IN.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
        ENDCG
        }
    }
}
