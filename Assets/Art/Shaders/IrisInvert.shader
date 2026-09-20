Shader "NestLabs/UI/IrisInvert"
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
        Blend OneMinusDstColor Zero
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
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                half2 texcoord       : TEXCOORD0;
                #ifdef UNITY_UI_CLIP_RECT
                float4 worldPosition : TEXCOORD1;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;

            half _Radius;
            half4 _Center;
            half _Aspect;
            half _EdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                #ifdef UNITY_UI_CLIP_RECT
                OUT.worldPosition = v.vertex;
                #endif
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = (half2)v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Aspect-corrected distance from the circle centre (_Center).
                half2 d = IN.texcoord - _Center.xy;
                d.x *= _Aspect;
                half dist = length(d);
                half inside = 1.0h - smoothstep(_Radius, _Radius + _EdgeSoftness, dist);

                #ifdef UNITY_UI_CLIP_RECT
                inside *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                // Discard pixels outside the circle so background remains untouched
                clip(inside - 0.001h);

                return fixed4(1, 1, 1, inside * IN.color.a);
            }
        ENDCG
        }
    }
}
