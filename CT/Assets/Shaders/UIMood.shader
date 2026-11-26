// CT/UI Mood：UI の絵（会話の立ち絵など）に、色調と同じ考え方の色の加工をかける（彩度・コントラスト・明るさ・色をかける・粒子）
// ・UI/Default と同じ作り（マスク・ステンシル・Alpha Clip に対応）。演出の窓口 PresentationDirector の DialogueMoodLayer が使う
// ・置き場所 Assets/Shaders/UIMood.shader、マテリアルは Assets/Resources/Presentation/Materials/Mat_UI_Mood（実行中は複製して使う）
Shader "CT/UI Mood"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Saturation ("Saturation", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Exposure ("Exposure", Float) = 1
        _MoodTint ("Mood Tint", Color) = (1,1,1,1)
        _Grain ("Grain Density", Range(0, 1)) = 0
        _GrainAmount ("Grain Dot Strength", Range(0, 0.5)) = 0.07
        _GrainSeed ("Grain Seed", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
                float4 screenPos     : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _Saturation;
            float _Contrast;
            float _Exposure;
            fixed4 _MoodTint;
            float _Grain;
            float _GrainAmount;
            float _GrainSeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 c = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // 彩度（0 で白黒）→ コントラスト → 明るさ・色をかける
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(l.xxx, c.rgb, _Saturation);
                c.rgb = (c.rgb - 0.5) * _Contrast + 0.5;
                c.rgb *= _Exposure * _MoodTint.rgb;

                // 粒子（画面の 2 ピクセルごとの点）：_Grain＝点の割合（まばらさ）、_GrainAmount＝1 つの点の濃さ
                // 弱くするときは点の数を減らす（濃さを下げると画面の色の段 1/255 を下回って急に消えるため。11-26）
                float2 cell = floor(i.screenPos.xy / max(i.screenPos.w, 0.00001) * _ScreenParams.xy * 0.5) + _GrainSeed;
                float isDot = step(Hash(cell), _Grain);
                float shade = Hash(cell + 17.31) - 0.5;
                c.rgb += isDot * shade * 2.0 * _GrainAmount;
                c.rgb = saturate(c.rgb);

                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif

                return c;
            }
        ENDCG
        }
    }
}
