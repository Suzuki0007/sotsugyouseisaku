/*********************************************************************/
// * \file   MoodPreset.cs
// * \brief  色調のプリセット：場面の「気持ち」を、画面全体の色の加工（ポストプロセス）で決める
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 色調のプリセット（場面の気持ちを画面全体の色で決める）
/// </summary>
///
/// ・彩度・明るさ・色かぶり・周辺を暗く・粒子・にじみを決めておき、部品「色調を変える」やシーンの既定で選ぶ
/// ・マップ・キャラ・物だけが変わる。会話・メニュー・HUD は変わらない
/// ・品質「軽い」では、粒子とにじみを外す
/// ・「会話画面にもかける」をオンにすると、会話の立ち絵・文字にも色をかけ、粒子・周辺の暗さを会話画面の上にも重ねる（回想は最初からオン）
/// ・置き場所は Assets/Resources/Presentation/Mood/（セーブから名前で戻すため）
[CreateAssetMenu(menuName = "CT/演出/色調のプリセット", fileName = "Mood_")]
public class MoodPreset : ScriptableObject
{
    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [Header("色")]

    [SerializeField, Label("彩度", -100.0f, 100.0f)]
    [Tooltip("色の鮮やかさ。-100 で白黒、0 でそのまま")]
    private float saturation = 0.0f;

    [SerializeField, Label("コントラスト", -100.0f, 100.0f)]
    [Tooltip("明るい所と暗い所の差。マイナスで眠い・重い感じ、プラスでくっきり")]
    private float contrast = 0.0f;

    [SerializeField, Label("明るさ", -3.0f, 3.0f)]
    [Tooltip("画面全体の明るさ（段階。-1 で半分、+1 で 2 倍）。0 でそのまま")]
    private float brightness = 0.0f;

    [SerializeField, Label("色をかける")]
    [Tooltip("画面全体にかける色（白＝そのまま。少し黄色で古い写真、少し青で冷たい感じ）")]
    private Color colorFilter = Color.white;

    [SerializeField, Label("暖色・寒色", -100.0f, 100.0f)]
    [Tooltip("プラスで暖かい色（橙）、マイナスで寒い色（青）")]
    private float temperature = 0.0f;

    [SerializeField, Label("緑・桃色", -100.0f, 100.0f)]
    [Tooltip("プラスで桃色寄り、マイナスで緑寄り")]
    private float tint = 0.0f;

    [Header("暗い所と明るい所の色分け（映画らしい色味）")]

    [SerializeField, Label("暗い所の色")]
    [Tooltip("暗い所に足す色。灰色（0.5, 0.5, 0.5）でそのまま")]
    private Color shadowsColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    [SerializeField, Label("明るい所の色")]
    [Tooltip("明るい所に足す色。灰色（0.5, 0.5, 0.5）でそのまま")]
    private Color highlightsColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    [SerializeField, Label("色分けの境目", -100.0f, 100.0f)]
    [Tooltip("プラスで明るい所の色が広く、マイナスで暗い所の色が広くなる")]
    private float splitBalance = 0.0f;

    [Header("周辺を暗くする（孤独・集中）")]

    [SerializeField, Label("強さ", 0.0f, 1.0f)]
    [Tooltip("マップの映る範囲（中央 4:3）の四隅を暗くする強さ。0 でなし、0.2 弱、0.35 中、0.5 強")]
    private float vignette = 0.0f;

    [SerializeField, Label("ぼかし", 0.01f, 1.0f)]
    [Tooltip("暗くなる所の境目のなめらかさ")]
    private float vignetteSmoothness = 0.4f;

    [SerializeField, Label("周辺の色")]
    [Tooltip("ふつうは黒")]
    private Color vignetteColor = Color.black;

    [Header("重い項目（品質「軽い」では外す）")]

    [SerializeField, Label("粒子（Film Grain）", 0.0f, 1.0f)]
    [Tooltip("古いフィルムのようなざらざら。回想・古い記録に。0 でなし、0.3 中")]
    private float grain = 0.0f;

    [SerializeField, Label("粒子の種類")]
    [Tooltip("ざらざらの細かさ")]
    private FilmGrainLookup grainType = FilmGrainLookup.Medium3;

    [SerializeField, Label("にじみ（Bloom）", 0.0f, 3.0f)]
    [Tooltip("明るい所が柔らかくにじむ。光る物があるときに効く。0 でなし、0.3 弱")]
    private float bloom = 0.0f;

    [SerializeField, Label("にじみ始める明るさ", 0.0f, 2.0f)]
    [Tooltip("これより明るい所がにじむ（小さいほど広くにじむ）")]
    private float bloomThreshold = 0.9f;

    [Header("会話画面")]

    [SerializeField, Label("会話画面にもかける")]
    [Tooltip("オンにすると、会話の立ち絵・名前・本文にも同じ考え方の色をかけ、会話を出している間は粒子・周辺の暗さを会話画面の上にも重ねる（回想など）。オフなら会話画面はいつもの色")]
    private bool applyToDialogue = false;

    [SerializeField, Label("会話画面の強さ", 0.0f, 1.0f)]
    [Tooltip("会話画面にかける強さ（1 でマップと同じ。0.8 くらいが読みやすい）")]
    private float dialogueStrength = 0.8f;

    //=====================================================================
    // 公開
    //=====================================================================

    public string Memo => memo;

    /// <summary> 値の一式 </summary>
    public MoodValues Values => new MoodValues
    {
        saturation = saturation,
        contrast = contrast,
        brightness = brightness,
        colorFilter = colorFilter,
        temperature = temperature,
        tint = tint,
        shadows = shadowsColor,
        highlights = highlightsColor,
        splitBalance = splitBalance,
        vignette = vignette,
        vignetteSmoothness = vignetteSmoothness,
        vignetteColor = vignetteColor,
        grain = grain,
        grainType = grainType,
        bloom = bloom,
        bloomThreshold = bloomThreshold,
        uiAmount = applyToDialogue ? dialogueStrength : 0.0f,
    };

#if UNITY_EDITOR
    /// <summary> エディタの道具（最初のプリセットを作る）から値を入れる </summary>
    public void EditorSet(string memoText, MoodValues v)
    {
        memo = memoText;
        saturation = v.saturation; contrast = v.contrast; brightness = v.brightness;
        colorFilter = v.colorFilter; temperature = v.temperature; tint = v.tint;
        shadowsColor = v.shadows; highlightsColor = v.highlights; splitBalance = v.splitBalance;
        vignette = v.vignette; vignetteSmoothness = v.vignetteSmoothness; vignetteColor = v.vignetteColor;
        grain = v.grain; grainType = v.grainType; bloom = v.bloom; bloomThreshold = v.bloomThreshold;
        applyToDialogue = v.uiAmount > 0.0f;
        if(applyToDialogue) { dialogueStrength = v.uiAmount; }
    }
#endif
}

/// <summary>
/// 色調の値の一式（混ぜて使う）
/// </summary>
[System.Serializable]
public struct MoodValues
{
    public float saturation;
    public float contrast;
    public float brightness;
    public Color colorFilter;
    public float temperature;
    public float tint;
    public Color shadows;
    public Color highlights;
    public float splitBalance;
    public float vignette;
    public float vignetteSmoothness;
    public Color vignetteColor;
    public float grain;
    public FilmGrainLookup grainType;
    public float bloom;
    public float bloomThreshold;
    /// <summary> 会話画面にかける強さ（0 でかけない） </summary>
    public float uiAmount;

    /// <summary> 何も加工しない </summary>
    public static MoodValues Neutral => new MoodValues
    {
        colorFilter = Color.white,
        shadows = new Color(0.5f, 0.5f, 0.5f, 1.0f),
        highlights = new Color(0.5f, 0.5f, 0.5f, 1.0f),
        vignetteSmoothness = 0.4f,
        vignetteColor = Color.black,
        grainType = FilmGrainLookup.Medium3,
        bloomThreshold = 0.9f,
    };

    /// <summary> 色を抜いた（灰色の）値。回想の「色が抜けてから差す」の途中 </summary>
    public MoodValues Desaturated()
    {
        MoodValues v = this;
        v.saturation = -100.0f;
        v.colorFilter = Color.white;
        v.temperature = 0.0f;
        v.tint = 0.0f;
        v.shadows = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        v.highlights = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        return v;
    }

    /// <summary> ほとんど加工がないか（ポストプロセスを切ってよいか） </summary>
    public bool IsNeutral
    {
        get
        {
            const float e = 0.01f;
            return Mathf.Abs(saturation) < e && Mathf.Abs(contrast) < e && Mathf.Abs(brightness) < 0.001f &&
                   Near(colorFilter, Color.white) && Mathf.Abs(temperature) < e && Mathf.Abs(tint) < e &&
                   Near(shadows, new Color(0.5f, 0.5f, 0.5f, 1.0f)) && Near(highlights, new Color(0.5f, 0.5f, 0.5f, 1.0f)) &&
                   vignette < 0.001f && grain < 0.001f && bloom < 0.001f;
        }
    }

    private static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;
    }

    public static MoodValues Lerp(MoodValues a, MoodValues b, float t)
    {
        return new MoodValues
        {
            saturation = Mathf.Lerp(a.saturation, b.saturation, t),
            contrast = Mathf.Lerp(a.contrast, b.contrast, t),
            brightness = Mathf.Lerp(a.brightness, b.brightness, t),
            colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t),
            temperature = Mathf.Lerp(a.temperature, b.temperature, t),
            tint = Mathf.Lerp(a.tint, b.tint, t),
            shadows = Color.Lerp(a.shadows, b.shadows, t),
            highlights = Color.Lerp(a.highlights, b.highlights, t),
            splitBalance = Mathf.Lerp(a.splitBalance, b.splitBalance, t),
            vignette = Mathf.Lerp(a.vignette, b.vignette, t),
            vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, t),
            vignetteColor = Color.Lerp(a.vignetteColor, b.vignetteColor, t),
            grain = Mathf.Lerp(a.grain, b.grain, t),
            // 粒子の種類は混ぜられないので、粒子がある方を使う
            grainType = b.grain > 0.001f ? b.grainType : a.grainType,
            bloom = Mathf.Lerp(a.bloom, b.bloom, t),
            bloomThreshold = Mathf.Lerp(a.bloomThreshold, b.bloomThreshold, t),
            uiAmount = Mathf.Lerp(a.uiAmount, b.uiAmount, t),
        };
    }
}
