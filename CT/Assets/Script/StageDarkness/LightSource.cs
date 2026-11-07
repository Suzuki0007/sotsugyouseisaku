/*********************************************************************/
// * \file   LightSource.cs
// * \brief  付けたオブジェクトを光源にする(たいまつ・ランプ・光る画像など)
// *
// *  使い方:
// *   光らせたいGameObject(スプライト・空オブジェクト何でもOK)にこのコンポーネントを付ける。
// *   オブジェクトの子として 2D ライトが作られるので、オブジェクトが動けば明かりも付いてくる。
// *   値はInspectorから調整でき、再生中に変えても即座に反映される。
// *
// *  形:
// *   Circle … 円形の明かり(半径・ぼかしで調整)
// *   Sprite … 指定した画像の形・色で照らす(窓から差す光、模様のある明かりなど)
// *
// *  「画像自体も光らせる」をONにすると、暗いステージでもその画像だけは暗くならない。
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class LightSource : MonoBehaviour
{
    public enum Shape
    {
        Circle, // 円形
        Sprite, // 画像の形
    }

    [Header("形")]
    [SerializeField] Shape shape = Shape.Circle;

    [Tooltip("Shape = Sprite の時に使う画像。空なら自分のSpriteRendererの画像を使う")]
    [SerializeField] Sprite lightSprite;

    [Tooltip("Shape = Sprite の時の明かりの大きさ(倍率)")]
    [SerializeField, Min(0f)] float spriteScale = 1f;

    [Header("明かり")]
    [Tooltip("明かりの位置のずれ")]
    [SerializeField] Vector2 offset = Vector2.zero;

    [Tooltip("明るい範囲の半径(Circle)")]
    [SerializeField, Min(0f)] float radius = 3f;

    [Tooltip("完全に明るい中心部分の半径(Circle)")]
    [SerializeField, Min(0f)] float innerRadius = 0.5f;

    [Tooltip("明かりの強さ")]
    [SerializeField, Min(0f)] float intensity = 1f;

    [Tooltip("明かりの色")]
    [SerializeField] Color color = new Color(1f, 0.85f, 0.6f); // 暖色

    [Tooltip("境目のぼかし具合(Circle / 0 = なめらか / 1 = くっきり減衰)")]
    [SerializeField, Range(0f, 1f)] float falloff = 0.5f;

    [Header("ゆらぎ(たいまつ風)")]
    [Tooltip("0でゆらぎなし。大きさ・強さが最大でこの割合だけ揺れる")]
    [SerializeField, Range(0f, 0.5f)] float flickerAmount = 0f;

    [Tooltip("ゆらぎの速さ")]
    [SerializeField, Min(0f)] float flickerSpeed = 3f;

    [Header("その他")]
    [Tooltip("ONなら StageDarkness があるステージ(暗いステージ)でだけ点灯する")]
    [SerializeField] bool onlyInDarkStage = true;

    [Tooltip("ONなら自分の画像(SpriteRenderer)は暗闇の影響を受けず明るく表示される")]
    [SerializeField] bool selfIlluminate = false;

    [Tooltip("selfIlluminate用のUnlitマテリアル。空なら Sprite-Unlit-Default を自動で使う\n(ビルドで画像が表示されない時はここに Sprite-Unlit-Default を設定)")]
    [SerializeField] Material unlitMaterial;

    Light2D light2D;
    SpriteRenderer spriteRenderer;
    Material originalMaterial;
    float noiseSeed;

    static Material sharedUnlitMaterial;

    /// <summary>明かりが点いているか</summary>
    public bool IsOn => light2D != null && light2D.enabled;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalMaterial = spriteRenderer.sharedMaterial;
        noiseSeed = Random.value * 100f; // 複数のたいまつが同じ揺れ方にならないように

        var go = new GameObject("Light (LightSource)");
        go.transform.SetParent(transform, false);
        light2D = go.AddComponent<Light2D>();
        light2D.targetSortingLayers = AllSortingLayerIds();
        light2D.blendStyleIndex = 0;
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnDisable()
    {
        if (light2D != null) light2D.enabled = false;
        RestoreMaterial();
    }

    void OnDestroy()
    {
        if (light2D != null) Destroy(light2D.gameObject);
    }

    void LateUpdate()
    {
        Apply();
    }

    void OnValidate()
    {
        if (innerRadius > radius) innerRadius = radius;
        if (Application.isPlaying && isActiveAndEnabled) Apply();
    }

    //------------------------------------------------------------
    // 外部から呼べる調整用メソッド
    //------------------------------------------------------------

    /// <summary>明かりの半径を変更(Circle)</summary>
    public void SetRadius(float value)
    {
        radius = Mathf.Max(0f, value);
        innerRadius = Mathf.Min(innerRadius, radius);
    }

    /// <summary>明かりの強さを変更(0で消灯と同じ見た目)</summary>
    public void SetIntensity(float value) => intensity = Mathf.Max(0f, value);

    /// <summary>明かりの色を変更</summary>
    public void SetColor(Color value) => color = value;

    /// <summary>明かりの形に使う画像を変更</summary>
    public void SetLightSprite(Sprite value) => lightSprite = value;

    //------------------------------------------------------------

    void Apply()
    {
        if (light2D == null) return;

        bool on = !onlyInDarkStage || StageDarkness.IsDarkStage;
        light2D.enabled = on;
        UpdateMaterial(on && selfIlluminate);
        if (!on) return;

        float flicker = 1f;
        if (flickerAmount > 0f && Application.isPlaying)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseSeed) * 2f - 1f; // -1〜1
            flicker = 1f + n * flickerAmount;
        }

        light2D.intensity = intensity * flicker;
        light2D.color = color;
        light2D.transform.localPosition = offset;

        if (shape == Shape.Sprite)
        {
            Sprite s = lightSprite;
            if (s == null && spriteRenderer != null) s = spriteRenderer.sprite;

            if (light2D.lightType != Light2D.LightType.Sprite) light2D.lightType = Light2D.LightType.Sprite;
            if (light2D.lightCookieSprite != s) light2D.lightCookieSprite = s;
            light2D.transform.localScale = Vector3.one * (spriteScale * flicker);
        }
        else
        {
            if (light2D.lightType != Light2D.LightType.Point) light2D.lightType = Light2D.LightType.Point;
            light2D.transform.localScale = Vector3.one;
            light2D.pointLightOuterRadius = radius * flicker;
            light2D.pointLightInnerRadius = Mathf.Min(innerRadius, radius) * flicker;
            light2D.falloffIntensity = falloff;
        }
    }

    void UpdateMaterial(bool useUnlit)
    {
        if (spriteRenderer == null) return;

        if (!useUnlit)
        {
            RestoreMaterial();
            return;
        }

        Material m = unlitMaterial != null ? unlitMaterial : GetSharedUnlitMaterial();
        if (m != null && spriteRenderer.sharedMaterial != m) spriteRenderer.sharedMaterial = m;
    }

    void RestoreMaterial()
    {
        if (spriteRenderer != null && originalMaterial != null && spriteRenderer.sharedMaterial != originalMaterial)
        {
            spriteRenderer.sharedMaterial = originalMaterial;
        }
    }

    static Material GetSharedUnlitMaterial()
    {
        if (sharedUnlitMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                Debug.LogWarning("[LightSource] Sprite-Unlit-Default シェーダーが見つかりません。Unlit Material を Inspector で設定してください。");
                return null;
            }
            sharedUnlitMaterial = new Material(shader) { name = "LightSource Unlit (auto)" };
        }
        return sharedUnlitMaterial;
    }

    static int[] AllSortingLayerIds()
    {
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
        return ids;
    }
}
