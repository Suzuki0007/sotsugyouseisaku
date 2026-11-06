/*********************************************************************/
// * \file   StageDarkness.cs
// * \brief  ステージを暗くし、プレイヤーの周囲だけ明るくする
// *
// *  使い方:
// *   暗くしたいステージのシーンに空のGameObjectを作り、このコンポーネントを付ける。
// *   付けないステージは今まで通り明るいまま。
// *   値はInspectorから調整でき、再生中に変えても即座に反映される。
// *
// *  仕組み:
// *   URP 2D の Global Light 2D の明るさを下げて全体を暗くし、
// *   プレイヤーに追従する Point Light 2D で周囲だけを照らす。
// *   ※ Sprite-Lit-Default など「Lit」系マテリアルのスプライトだけが暗くなる
// *     (Sprites-Default などUnlitのものは暗くならない)
/*********************************************************************/

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class StageDarkness : MonoBehaviour
{
    [Header("ステージ全体の暗さ")]
    [Tooltip("0 = いつも通りの明るさ / 1 = 真っ暗")]
    [SerializeField, Range(0f, 1f)] float darkness = 0.9f;

    [Tooltip("暗い部分の色味(少し青くすると夜っぽくなる)")]
    [SerializeField] Color ambientColor = Color.white;

    [Tooltip("シーンのGlobal Light 2D。空なら自動で探す(無ければ作る)")]
    [SerializeField] Light2D globalLight;

    [Header("プレイヤー周囲の明かり")]
    [Tooltip("照らす対象。空なら Player タグ → kari_player_test の順で自動で探す")]
    [SerializeField] Transform target;

    [Tooltip("明かりの位置のずれ(足元ではなく体の中心を照らしたい時など)")]
    [SerializeField] Vector2 offset = Vector2.zero;

    [Tooltip("明るい範囲の半径(この外側は真っ暗)")]
    [SerializeField, Min(0f)] float radius = 4f;

    [Tooltip("完全に明るい中心部分の半径")]
    [SerializeField, Min(0f)] float innerRadius = 1f;

    [Tooltip("明かりの強さ")]
    [SerializeField, Min(0f)] float lightIntensity = 1f;

    [Tooltip("明かりの色")]
    [SerializeField] Color lightColor = Color.white;

    [Tooltip("境目のぼかし具合(0 = なめらか / 1 = くっきり減衰)")]
    [SerializeField, Range(0f, 1f)] float falloff = 0.5f;

    [Header("ゆらぎ(たいまつ風)")]
    [Tooltip("0でゆらぎなし。半径が最大でこの割合だけ揺れる")]
    [SerializeField, Range(0f, 0.5f)] float flickerAmount = 0f;

    [Tooltip("ゆらぎの速さ")]
    [SerializeField, Min(0f)] float flickerSpeed = 3f;

    Light2D playerLight;
    float originalGlobalIntensity;
    Color originalGlobalColor;
    bool createdGlobalLight;
    Coroutine fadeRoutine;

    static StageDarkness active;

    /// <summary>今のステージが暗いステージか(StageDarknessが有効で暗さ > 0)</summary>
    public static bool IsDarkStage => active != null && active.darkness > 0f;

    /// <summary>現在有効なStageDarkness(無ければnull)</summary>
    public static StageDarkness Active => active;

    /// <summary>現在の暗さ(0〜1)</summary>
    public float Darkness => darkness;

    /// <summary>現在の明かりの半径</summary>
    public float Radius => radius;

    void Awake()
    {
        SetupGlobalLight();
        SetupPlayerLight();
    }

    void OnEnable()
    {
        active = this;
        if (playerLight != null) playerLight.enabled = true;
        Apply();
    }

    void OnDisable()
    {
        if (active == this) active = null;

        // コンポーネントを切ったら元の明るさに戻す
        if (globalLight != null)
        {
            globalLight.intensity = originalGlobalIntensity;
            globalLight.color = originalGlobalColor;
        }
        if (playerLight != null) playerLight.enabled = false;
    }

    void OnDestroy()
    {
        if (playerLight != null) Destroy(playerLight.gameObject);
        if (createdGlobalLight && globalLight != null) Destroy(globalLight.gameObject);
    }

    void LateUpdate()
    {
        if (target == null) target = FindTarget();
        Apply();
    }

    // Inspectorで値を変えた時(再生中)にすぐ反映する
    void OnValidate()
    {
        if (innerRadius > radius) innerRadius = radius;
        if (Application.isPlaying && isActiveAndEnabled) Apply();
    }

    //------------------------------------------------------------
    // 外部から呼べる調整用メソッド
    //------------------------------------------------------------

    /// <summary>暗さを変更する。duration > 0 ならその秒数かけて変化</summary>
    public void SetDarkness(float value, float duration = 0f)
    {
        StartFade(Mathf.Clamp01(value), radius, duration);
    }

    /// <summary>明かりの半径を変更する(アイテムで視界が広がる等)。duration > 0 ならその秒数かけて変化</summary>
    public void SetRadius(float value, float duration = 0f)
    {
        StartFade(darkness, Mathf.Max(0f, value), duration);
    }

    /// <summary>明かりの強さを変更する</summary>
    public void SetLightIntensity(float value) => lightIntensity = Mathf.Max(0f, value);

    /// <summary>明かりの色を変更する</summary>
    public void SetLightColor(Color value) => lightColor = value;

    /// <summary>照らす対象を変更する</summary>
    public void SetTarget(Transform value) => target = value;

    //------------------------------------------------------------

    void StartFade(float toDarkness, float toRadius, float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (duration <= 0f || !isActiveAndEnabled)
        {
            darkness = toDarkness;
            radius = toRadius;
            innerRadius = Mathf.Min(innerRadius, radius);
            Apply();
            return;
        }
        fadeRoutine = StartCoroutine(Fade(toDarkness, toRadius, duration));
    }

    IEnumerator Fade(float toDarkness, float toRadius, float duration)
    {
        float fromDarkness = darkness;
        float fromRadius = radius;
        float fromInner = innerRadius;
        float innerRatio = fromRadius > 0f ? fromInner / fromRadius : 0f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            darkness = Mathf.Lerp(fromDarkness, toDarkness, k);
            radius = Mathf.Lerp(fromRadius, toRadius, k);
            innerRadius = radius * innerRatio;
            yield return null;
        }
        darkness = toDarkness;
        radius = toRadius;
        innerRadius = radius * innerRatio;
        fadeRoutine = null;
    }

    void Apply()
    {
        if (globalLight != null)
        {
            globalLight.intensity = originalGlobalIntensity * (1f - darkness);
            globalLight.color = ambientColor;
        }

        if (playerLight == null) return;

        float flicker = 1f;
        if (flickerAmount > 0f && Application.isPlaying)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f) * 2f - 1f; // -1〜1
            flicker = 1f + n * flickerAmount;
        }

        playerLight.pointLightOuterRadius = radius * flicker;
        playerLight.pointLightInnerRadius = Mathf.Min(innerRadius, radius) * flicker;
        playerLight.intensity = lightIntensity;
        playerLight.color = lightColor;
        playerLight.falloffIntensity = falloff;

        if (target != null)
        {
            Vector3 p = target.position;
            playerLight.transform.position = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
        }
    }

    void SetupGlobalLight()
    {
        if (globalLight == null)
        {
            foreach (var l in FindObjectsByType<Light2D>())
            {
                if (l.lightType == Light2D.LightType.Global)
                {
                    globalLight = l;
                    break;
                }
            }
        }

        if (globalLight == null)
        {
            var go = new GameObject("Global Light 2D (StageDarkness)");
            globalLight = go.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
            globalLight.targetSortingLayers = AllSortingLayerIds();
            createdGlobalLight = true;
        }

        originalGlobalIntensity = globalLight.intensity;
        originalGlobalColor = globalLight.color;
    }

    void SetupPlayerLight()
    {
        var go = new GameObject("PlayerLight (StageDarkness)");
        go.transform.SetParent(transform, false);
        playerLight = go.AddComponent<Light2D>();
        playerLight.lightType = Light2D.LightType.Point;
        playerLight.targetSortingLayers = AllSortingLayerIds();
        playerLight.blendStyleIndex = 0;

        if (target == null) target = FindTarget();
    }

    static Transform FindTarget()
    {
        GameObject tagged = null;
        try { tagged = GameObject.FindWithTag("Player"); }
        catch (UnityException) { /* Playerタグ未定義 */ }
        if (tagged != null) return tagged.transform;

        var player = FindAnyObjectByType<kari_player_test>();
        return player != null ? player.transform : null;
    }

    static int[] AllSortingLayerIds()
    {
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
        return ids;
    }
}
