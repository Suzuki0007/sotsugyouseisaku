/*********************************************************************/
// * \file   RippleView.cs
// * \brief  波紋 1 つの見た目：中の光（点の光が広がる）と縁の光（輪の絵の光が広がる）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 波紋 1 つ（RippleSystem が作る。シーンに置かなくてよい）
/// </summary>
///
/// ・中の光：点の光（Light 2D）が半径 0 から広がる。色が戻るでは、全体の光の青を打ち消す色を、全体の光から毎回求める
/// ・縁の光：輪の絵の光（Sprite の Light 2D）が一緒に広がり、広がりきると消える。品質「軽い」では作らない
/// ・明かりに反応する絵（Sprite-Lit-Default・Tilemap）とキャラが照らされる。波（SpriteFrameBlend）・光らない絵・文字は変わらない
[AddComponentMenu("")]
public class RippleView : MonoBehaviour
{
    private RipplePreset preset;
    private Light2D inner;
    private Light2D edge;
    private float time;
    private float duration;
    private float fade = 1.0f;      // 全体の強さ（消えるとき 1 → 0）
    private float fadeSpeed;
    private bool expanded;

    public RipplePreset Preset => preset;
    public string Id { get; set; }
    public bool Persist { get; set; }

    /// <summary> 広がりきったか </summary>
    public bool IsExpanded => expanded;

    /// <summary> 消えきったか </summary>
    public bool IsGone => fade <= 0.0f && fadeSpeed > 0.0f;

    public void Begin(RipplePreset p, float seconds, bool instant, Sprite ringSprite)
    {
        preset = p;
        duration = instant ? 0.0f : Mathf.Max(0.0f, seconds);
        time = instant ? duration : 0.0f;
        expanded = instant;

        inner = NewLight("中の光");
        inner.lightType = Light2D.LightType.Point;
        inner.falloffIntensity = 0.55f;

        if(!PresentationEase.IsLowQuality && p.EdgeIntensity > 0.0f && ringSprite != null && !instant)
        {
            edge = NewLight("縁の光");
            edge.lightType = Light2D.LightType.Sprite;
            edge.lightCookieSprite = ringSprite;
            edge.color = p.EdgeColor;
        }
        Apply();
    }

    private Light2D NewLight(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        Light2D l = go.AddComponent<Light2D>();
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for(int i = 0; i < layers.Length; i++) { ids[i] = layers[i].id; }
        l.targetSortingLayers = ids;
        return l;
    }

    /// <summary> ゆっくり消す（seconds 0 ならすぐ） </summary>
    public void FadeOut(float seconds)
    {
        fadeSpeed = seconds <= 0.0f ? 999.0f : 1.0f / seconds;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if(!expanded)
        {
            time += dt;
            if(time >= duration)
            {
                expanded = true;
                if(preset.AfterMode == RipplePreset.After.FadeOut && fadeSpeed <= 0.0f) { FadeOut(preset.FadeSeconds); }
            }
        }
        if(fadeSpeed > 0.0f) { fade = Mathf.Max(0.0f, fade - dt * fadeSpeed); }
        Apply();
    }

    private void Apply()
    {
        float t = duration > 0.0f ? Mathf.Clamp01(time / duration) : 1.0f;
        float eased = 1.0f - (1.0f - t) * (1.0f - t);   // 始め速く、ゆっくり止まる
        float r = Mathf.Max(0.01f, preset.Radius * eased);
        transform.localScale = new Vector3(1.0f, preset.Ellipse, 1.0f);

        if(inner != null)
        {
            inner.pointLightOuterRadius = r;
            inner.pointLightInnerRadius = r * 0.75f;
            if(preset.Mode == RipplePreset.Look.LightUp)
            {
                inner.color = preset.LightColor;
                inner.intensity = preset.Intensity * fade;
            }
            else
            {
                // 色が戻る：全体の光（青）に足すと、白（強さ×戻す明るさ）になる色
                Color g = PresentationDirector.CurrentGlobalLight;
                float level = preset.Intensity;
                var add = new Color(Mathf.Max(0.0f, level - g.r), Mathf.Max(0.0f, level - g.g), Mathf.Max(0.0f, level - g.b), 1.0f);
                float max = Mathf.Max(add.r, Mathf.Max(add.g, add.b));
                inner.color = max > 0.0f ? new Color(add.r / max, add.g / max, add.b / max, 1.0f) : Color.white;
                inner.intensity = max * fade;
            }
            inner.enabled = inner.intensity > 0.001f;
        }

        if(edge != null)
        {
            // 縁：広がっている間は明るく、広がりきると縁が消える秒数で消える
            float edgeAlpha = expanded ? Mathf.Clamp01(1.0f - (time - duration) / Mathf.Max(0.01f, preset.EdgeFadeSeconds)) : 1.0f;
            float size = r * 2.0f * (1.0f + preset.EdgeWidth);
            edge.transform.localScale = new Vector3(size, size, 1.0f);
            edge.intensity = preset.EdgeIntensity * edgeAlpha * fade;
            edge.enabled = edge.intensity > 0.001f;
            if(expanded && edgeAlpha <= 0.0f) { Destroy(edge.gameObject); edge = null; }
        }
    }

    private void Update()
    {
        // 広がりきったあとも、縁が消えるまでの時間を進める
        if(expanded && edge != null) { time += Time.deltaTime; }
    }
}
