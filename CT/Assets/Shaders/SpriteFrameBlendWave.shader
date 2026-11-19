// SpriteFrameBlendWave.shader
// 連番アニメのコマとコマの間を混ぜ（線形補間）、水のようにゆらす 2D スプライト用のシェーダー（URP 2D・明かりなし）
// ・今のコマ（スプライトの絵）と次のコマ（_NextTex の _UvB の範囲）を _Blend で混ぜる
// ・横・縦に少しずつずらして、水面がゆれているように見せる（ずらしはピクセルで指定。コマの外ははみ出さない）
// ・値は SpriteFrameBlend（部品）が毎フレーム入れる。手で触る項目は部品の Inspector にある
Shader "CT/Sprite Frame Blend Wave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _NextTex ("Next Frame", 2D) = "white" {}
        [PerRendererData] _Blend ("Blend", Range(0, 1)) = 0
        [PerRendererData] _UvA ("Frame A uv (min.xy, size.zw)", Vector) = (0, 0, 1, 1)
        [PerRendererData] _UvB ("Frame B uv (min.xy, size.zw)", Vector) = (0, 0, 1, 1)
        [PerRendererData] _Wave ("Wave (ampX px, ampY px, freq, speed)", Vector) = (0, 0, 8, 1)
        [PerRendererData] _WaveTime ("Wave Time", Float) = 0
        [PerRendererData] _Shift ("Vertical shift A, B (frame local)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_NextTex); SAMPLER(sampler_NextTex);
        float4 _MainTex_TexelSize;
        float4 _NextTex_TexelSize;
        float _Blend;
        float4 _UvA;
        float4 _UvB;
        float4 _Wave;
        float _WaveTime;
        float4 _Shift;
        half4 _Color;
        half4 _RendererColor;

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4  color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(v.positionOS);
            o.uv = v.uv;
            o.color = v.color * _Color * _RendererColor;
            return o;
        }

        // コマの中の位置（0〜1）→ そのコマのテクスチャの uv（コマの外にはみ出さない）
        float2 FrameUv(float2 local, float4 rect, float4 texel)
        {
            float2 uv = rect.xy + saturate(local) * rect.zw;
            float2 margin = texel.xy * 0.5;
            return clamp(uv, rect.xy + margin, rect.xy + rect.zw - margin);
        }

        half4 frag(Varyings i) : SV_Target
        {
            // スプライトの uv（今のコマ）→ コマの中の位置
            float2 local = (i.uv - _UvA.xy) / max(_UvA.zw, 1e-5);

            // 水のゆれ：高さで横に、横の位置で縦に、ゆっくり波打つずらし（ピクセルをコマの中の割合に直す）
            float2 framePx = _UvA.zw * _MainTex_TexelSize.zw;
            float t = _WaveTime * _Wave.w;
            float dx = sin(local.y * _Wave.z * 6.2831 + t) + 0.5 * sin(local.y * _Wave.z * 13.1 - t * 1.7);
            float dy = sin(local.x * _Wave.z * 3.1416 + t * 0.8);
            local.x += dx * _Wave.x / max(framePx.x, 1.0) / 1.5;
            local.y += dy * _Wave.y / max(framePx.y, 1.0);

            // 混ぜている間、今のコマは次のコマの水の高さへ、次のコマは今のコマの高さから動いてくる（波が上下して見える）
            //  下の端（深い所）は動かさず、上（水面）ほど大きく動かす（下の端が引き伸ばされて筋にならないように）
            //  （_Shift.z ＝ 動かし始める高さ。そこから上の白い波の帯は丸ごと動かす）
            float lift = saturate(local.y / max(_Shift.z, 0.05));
            half4 a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, FrameUv(local - float2(0, _Shift.x * lift), _UvA, _MainTex_TexelSize));
            half4 b = SAMPLE_TEXTURE2D(_NextTex, sampler_NextTex, FrameUv(local - float2(0, _Shift.y * lift), _UvB, _NextTex_TexelSize));

            // 色は透明さで重みをつけて混ぜる（透明な所の色がにじまないように）
            half4 c;
            c.a = lerp(a.a, b.a, _Blend);
            half3 pa = a.rgb * a.a;
            half3 pb = b.rgb * b.a;
            c.rgb = lerp(pa, pb, _Blend);   // 掛けた形（premultiplied）のまま
            c.rgb *= i.color.rgb * i.color.a;
            c.a *= i.color.a;
            return c;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
