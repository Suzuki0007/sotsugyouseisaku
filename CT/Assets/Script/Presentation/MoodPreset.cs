/*********************************************************************/
// * \file   MoodPreset.cs
// * \brief  色調のプリセット：場面の「気持ち」を、画面全体の色の加工（ポストプロセス）で決める
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 色調のプリセット（場面の気持ちを画面全体の色で決める）
/// </summary>
///
/// ・彩度・明るさ・色かぶり・周辺を暗く・粒子・にじみを決めておき、部品「色調を変える」やシーンの既定で選ぶ
/// ・マップ・キャラ・物だけが変わる。会話・メニュー・HUD は変わらない
/// ・品質「軽い」では、粒子とにじみを外す
/// ・「明るさで塗り替える」をオンにすると、明るさに合わせて色の段の色に塗り替える（暗い所＝紺・普通＝青・明かりの中＝黄色。光で明るくなった所がそのまま黄色になる）
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

    [Header("暗い所の色（黒を青に沈める）")]

    [SerializeField, Label("暗い所の色")]
    [Tooltip("画面のいちばん暗い所（黒）を、この色に寄せる。紺にすると、暗いステージで暗くした所が黒ではなく青い夜に沈む")]
    private Color shadowLiftColor = new Color(0.10f, 0.16f, 0.40f, 1.0f);

    [SerializeField, Label("寄せる強さ", 0.0f, 1.0f)]
    [Tooltip("0 で寄せない（黒のまま）。0.5 くらいで暗い所が紺になる")]
    private float shadowLift = 0.0f;

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

    [Header("画面の縁（マップの映る 4:3 の端）")]

    [SerializeField, Label("縁をぼかす幅", 0.0f, 0.25f)]
    [Tooltip("4:3 の端を、左右の帯の色へなめらかに消す幅（画面の高さに対する割合）。0 でくっきり切れたまま、0.04 で少し、0.1 で大きくぼかす。「画面が真四角に見える」のを和らげる")]
    private float edgeFade = 0.0f;

    [SerializeField, Label("角の丸み", 0.0f, 1.0f)]
    [Tooltip("ぼかした縁の角の丸さ。0 で四角、1 で丸く（古い写真・ブラウン管のような角）")]
    private float edgeRound = 0.5f;

    [SerializeField, Label("上のグラデーションの強さ", 0.0f, 1.0f)]
    [Tooltip("画面の上から色をかぶせる強さ（空を落とす・重たい感じ）。0 でなし、0.3 弱、0.6 強")]
    private float topShade = 0.0f;

    [SerializeField, Label("上のグラデーションの色")]
    [Tooltip("上にかぶせる色。黒で暗く、紺で夜空、白で白くかすむ")]
    private Color topShadeColor = Color.black;

    [SerializeField, Label("上のグラデーションの高さ", 0.05f, 1.0f)]
    [Tooltip("上からどこまでかかるか（画面の高さに対する割合）")]
    private float topShadeHeight = 0.45f;

    [SerializeField, Label("下のグラデーションの強さ", 0.0f, 1.0f)]
    [Tooltip("画面の下から色をかぶせる強さ。0 でなし")]
    private float bottomShade = 0.0f;

    [SerializeField, Label("下のグラデーションの色")]
    [Tooltip("下にかぶせる色。白・薄い黄色で地面が明るくかすむ（ダリの絵のような地平線）、黒で足元を暗く")]
    private Color bottomShadeColor = Color.white;

    [SerializeField, Label("下のグラデーションの高さ", 0.05f, 1.0f)]
    [Tooltip("下からどこまでかかるか（画面の高さに対する割合）")]
    private float bottomShadeHeight = 0.35f;

    [Header("光漏れ（古いフィルム・思い出。画面の端から光が差し込む）")]

    [SerializeField, Label("光漏れの強さ", 0.0f, 1.0f)]
    [Tooltip("光漏れ全体の強さ。0 でなし、0.3 弱（ふだんの場面）、0.6 中、1 強（回想・思い出）")]
    private float leak = 0.0f;

    [SerializeField, Label("右の帯の強さ", 0.0f, 1.0f)]
    [Tooltip("画面の右の端から差し込む光の帯の濃さ")]
    private float leakRight = 1.0f;

    [SerializeField, Label("右の帯の色")]
    [Tooltip("右の帯の色。橙・赤でフィルムの光漏れらしくなる")]
    private Color leakRightColor = new Color(1.0f, 0.42f, 0.12f, 1.0f);

    [SerializeField, Label("左の帯の強さ", 0.0f, 1.0f)]
    [Tooltip("画面の左の端から差し込む光の帯の濃さ")]
    private float leakLeft = 0.5f;

    [SerializeField, Label("左の帯の色")]
    [Tooltip("左の帯の色。青緑にすると、右の橙と合わせて古い写真のような色になる")]
    private Color leakLeftColor = new Color(0.25f, 0.85f, 0.85f, 1.0f);

    [SerializeField, Label("帯の幅", 0.05f, 0.6f)]
    [Tooltip("帯が画面の内側へ伸びる幅（マップの映る範囲の横幅に対する割合）")]
    private float leakWidth = 0.3f;

    [SerializeField, Label("角の焼けの強さ", 0.0f, 1.0f)]
    [Tooltip("画面の角が白く焼けたようになる強さ。0 でなし")]
    private float leakBurn = 0.6f;

    [SerializeField, Label("角の焼けの色")]
    [Tooltip("焼けた角の色。白〜薄い橙")]
    private Color leakBurnColor = new Color(1.0f, 0.88f, 0.72f, 1.0f);

    [SerializeField, Label("焼ける角")]
    [Tooltip("どの角が焼けるか")]
    private LeakCorner leakBurnCorner = LeakCorner.BottomLeft;

    [SerializeField, Label("縦の筋", 0.0f, 1.0f)]
    [Tooltip("縦に走る細い光の筋の強さ（品質「軽い」では出さない）。0 でなし")]
    private float leakStreaks = 0.3f;

    [SerializeField, Label("揺れ", 0.0f, 1.0f)]
    [Tooltip("光がゆっくり揺れる・強くなったり弱くなったりする大きさ。0 で止まったまま")]
    private float leakSway = 0.5f;

    [SerializeField, Label("一瞬光る回数（1 分に）", 0.0f, 12.0f)]
    [Tooltip("ときどき一瞬だけ強く光る（ピッと入る）回数の目安。0 で光らない。2〜4 くらいがさりげない")]
    private float leakFlash = 2.0f;

    [Header("レンズフレア（場面に「太陽（レンズフレア）」を置いたとき）")]

    [SerializeField, Label("レンズフレアの強さ", 0.0f, 2.0f)]
    [Tooltip("太陽のレンズフレアにかける倍率。1 でそのまま、0 で出さない（夜・室内・回想の色調で 0 にする）")]
    private float flare = 1.0f;

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

    [Header("塗り替え（明るさ → 色。青と黄色の色調など）")]

    [SerializeField, Label("明るさで塗り替える")]
    [Tooltip("オンにすると、画面の明るさに合わせて、下の「色の段」の色に塗り替える（画像編集のグラデーションマップ）。暗い所＝紺、普通＝青、明かりの中＝黄色、のように使う")]
    private bool remap = false;

    [SerializeField, Label("境目")]
    [Tooltip("段階：段の色がそのまま切り替わる（硬い境目。ファミレス型）／なめらか：段と段の間を混ぜる（夏のうちに型）")]
    private RemapMode remapMode = RemapMode.Step;

    [SerializeField]
    [Tooltip("色の段（暗い → 明るい）。「明るさ（から）」は 0〜1（0＝真っ黒、1＝真っ白）。その明るさから次の段まで、この色になる（なめらかなら次の段の色へ混ざる）")]
    private List<RemapStop> remapStops = new List<RemapStop>();

    [SerializeField, Label("元の色を残す", 0.0f, 1.0f)]
    [Tooltip("塗り替えた色に、元の色をどれだけ混ぜるか（0.15 で物の色がわずかに残ってやわらかくなる）")]
    private float keepOriginal = 0.0f;

    [SerializeField, Label("塗り替えの強さ", 0.0f, 1.0f)]
    [Tooltip("1 で全部塗り替える。下げると元の見た目に近づく")]
    private float remapStrength = 1.0f;

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
        liftColor = shadowLiftColor,
        lift = shadowLift,
        vignette = vignette,
        vignetteSmoothness = vignetteSmoothness,
        vignetteColor = vignetteColor,
        edgeFade = edgeFade,
        edgeRound = edgeRound,
        topShade = topShade,
        topShadeColor = topShadeColor,
        topShadeHeight = topShadeHeight,
        bottomShade = bottomShade,
        bottomShadeColor = bottomShadeColor,
        bottomShadeHeight = bottomShadeHeight,
        leak = leak,
        leakRight = leakRight,
        leakRightColor = leakRightColor,
        leakLeft = leakLeft,
        leakLeftColor = leakLeftColor,
        leakWidth = leakWidth,
        leakBurn = leakBurn,
        leakBurnColor = leakBurnColor,
        leakBurnCorner = leakBurnCorner,
        leakStreaks = leakStreaks,
        leakSway = leakSway,
        leakFlash = leakFlash,
        flare = flare,
        grain = grain,
        grainType = grainType,
        bloom = bloom,
        bloomThreshold = bloomThreshold,
        uiAmount = applyToDialogue ? dialogueStrength : 0.0f,
        remap = remap && remapStops.Count > 0 ? remapStrength : 0.0f,
        keepOriginal = keepOriginal,
    };

    /// <summary> 明るさで塗り替えるか </summary>
    public bool Remaps => remap && remapStops.Count > 0;

    public RemapMode Mode => remapMode;
    public IReadOnlyList<RemapStop> Stops => remapStops;

    private Color[] rampCache;

    /// <summary>
    /// 明るさ 0〜255 → 塗り替える色（sRGB の値）の表。プリセットを変えたら作り直す
    /// </summary>
    public Color[] Ramp
    {
        get
        {
            if(rampCache == null) { rampCache = BuildRamp(); }
            return rampCache;
        }
    }

    /// <summary> 明るさ（0〜1）がどの段か（境目の表示に使う。段がなければ -1） </summary>
    public int StopIndexAt(float luminance)
    {
        int found = -1;
        for(int i = 0; i < remapStops.Count; i++)
        {
            if(remapStops[i] != null && luminance >= remapStops[i].from) { found = i; }
        }
        return found < 0 && remapStops.Count > 0 ? 0 : found;
    }

    private Color[] BuildRamp()
    {
        var ramp = new Color[256];
        // 段を明るさの順に並べる（Inspector で順番が崩れていてもよいように）
        var stops = new List<RemapStop>();
        for(int i = 0; i < remapStops.Count; i++) { if(remapStops[i] != null) { stops.Add(remapStops[i]); } }
        stops.Sort((a, b) => a.from.CompareTo(b.from));
        if(stops.Count == 0) { for(int i = 0; i < 256; i++) { ramp[i] = new Color(i / 255.0f, i / 255.0f, i / 255.0f, 1.0f); } return ramp; }

        for(int i = 0; i < 256; i++)
        {
            float l = i / 255.0f;
            int k = 0;
            for(int s = 0; s < stops.Count; s++) { if(l >= stops[s].from) { k = s; } }
            Color c = stops[k].color;
            if(remapMode == RemapMode.Smooth && k + 1 < stops.Count && l >= stops[k].from)
            {
                float span = Mathf.Max(0.0001f, stops[k + 1].from - stops[k].from);
                c = Color.Lerp(stops[k].color, stops[k + 1].color, Mathf.Clamp01((l - stops[k].from) / span));
            }
            c.a = 1.0f;
            ramp[i] = c;
        }
        return ramp;
    }

    private void OnValidate()
    {
        rampCache = null;
        if(Application.isPlaying) { PresentationDirector.NotifyMoodPresetChanged(this); }
    }

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

    /// <summary> エディタの道具・確かめ用：画面の縁を入れる </summary>
    public void EditorSetEdge(MoodValues v)
    {
        edgeFade = v.edgeFade; edgeRound = v.edgeRound;
        topShade = v.topShade; topShadeColor = v.topShadeColor; topShadeHeight = v.topShadeHeight;
        bottomShade = v.bottomShade; bottomShadeColor = v.bottomShadeColor; bottomShadeHeight = v.bottomShadeHeight;
    }

    /// <summary> エディタの道具・確かめ用：光漏れを入れる </summary>
    public void EditorSetLeak(MoodValues v)
    {
        leak = v.leak;
        leakRight = v.leakRight; leakRightColor = v.leakRightColor;
        leakLeft = v.leakLeft; leakLeftColor = v.leakLeftColor;
        leakWidth = v.leakWidth;
        leakBurn = v.leakBurn; leakBurnColor = v.leakBurnColor; leakBurnCorner = v.leakBurnCorner;
        leakStreaks = v.leakStreaks; leakSway = v.leakSway; leakFlash = v.leakFlash;
        flare = v.flare;
    }

    /// <summary> エディタの道具から塗り替えを入れる </summary>
    public void EditorSetRemap(RemapMode mode, List<RemapStop> stops, float keep)
    {
        remap = stops != null && stops.Count > 0;
        remapMode = mode;
        remapStops = stops ?? new List<RemapStop>();
        keepOriginal = keep;
        remapStrength = 1.0f;
        rampCache = null;
    }
#endif
}

/// <summary> 塗り替えの境目 </summary>
public enum RemapMode
{
    [InspectorName("段階（硬い境目）")]
    Step = 0,

    [InspectorName("なめらか")]
    Smooth = 1,
}

/// <summary> 光漏れで焼ける角（番号で保存されるので末尾にだけ足す） </summary>
public enum LeakCorner
{
    [InspectorName("左下")]
    BottomLeft = 0,

    [InspectorName("右下")]
    BottomRight = 1,

    [InspectorName("左上")]
    TopLeft = 2,

    [InspectorName("右上")]
    TopRight = 3,
}

/// <summary> 塗り替えの色の段（この明るさから、この色） </summary>
[System.Serializable]
public class RemapStop
{
    [Label("明るさ（から）", 0.0f, 1.0f)]
    [Tooltip("0＝真っ黒、1＝真っ白。この明るさから次の段まで、この色になる")]
    public float from = 0.0f;

    [Label("色")]
    public Color color = Color.white;
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
    /// <summary> 黒を寄せる色と強さ（Lift） </summary>
    public Color liftColor;
    public float lift;
    public float vignette;
    public float vignetteSmoothness;
    public Color vignetteColor;
    /// <summary> 画面の縁（ScreenLensLayer が UI の絵で描く。ポストプロセスではない） </summary>
    public float edgeFade;
    public float edgeRound;
    public float topShade;
    public Color topShadeColor;
    public float topShadeHeight;
    public float bottomShade;
    public Color bottomShadeColor;
    public float bottomShadeHeight;
    /// <summary> 光漏れ（ScreenLensLayer が描く） </summary>
    public float leak;
    public float leakRight;
    public Color leakRightColor;
    public float leakLeft;
    public Color leakLeftColor;
    public float leakWidth;
    public float leakBurn;
    public Color leakBurnColor;
    public LeakCorner leakBurnCorner;
    public float leakStreaks;
    public float leakSway;
    public float leakFlash;
    /// <summary> 太陽のレンズフレアの倍率（何もしない値は 1） </summary>
    public float flare;
    public float grain;
    public FilmGrainLookup grainType;
    public float bloom;
    public float bloomThreshold;
    /// <summary> 会話画面にかける強さ（0 でかけない） </summary>
    public float uiAmount;
    /// <summary> 明るさで塗り替える強さ（0 で塗り替えない。色の段は窓口が別に混ぜる） </summary>
    public float remap;
    /// <summary> 塗り替えに元の色を残す割合 </summary>
    public float keepOriginal;

    /// <summary> 何も加工しない </summary>
    public static MoodValues Neutral => new MoodValues
    {
        colorFilter = Color.white,
        shadows = new Color(0.5f, 0.5f, 0.5f, 1.0f),
        highlights = new Color(0.5f, 0.5f, 0.5f, 1.0f),
        vignetteSmoothness = 0.4f,
        vignetteColor = Color.black,
        edgeRound = 0.5f,
        topShadeColor = Color.black,
        topShadeHeight = 0.45f,
        bottomShadeColor = Color.white,
        bottomShadeHeight = 0.35f,
        leakRight = 1.0f,
        leakRightColor = new Color(1.0f, 0.42f, 0.12f, 1.0f),
        leakLeft = 0.5f,
        leakLeftColor = new Color(0.25f, 0.85f, 0.85f, 1.0f),
        leakWidth = 0.3f,
        leakBurn = 0.6f,
        leakBurnColor = new Color(1.0f, 0.88f, 0.72f, 1.0f),
        leakStreaks = 0.3f,
        leakSway = 0.5f,
        leakFlash = 2.0f,
        flare = 1.0f,
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
        v.remap = 0.0f;   // 灰色の途中は塗り替えもしない（回想の「色が抜けてから差す」）
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
                   vignette < 0.001f && grain < 0.001f && bloom < 0.001f && remap < 0.001f && lift < 0.001f;
        }
    }

    private static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;
    }

    /// <summary> 強さがある側の値（強さ 0 ならもう一方の値） </summary>
    private static float Pick(float strength, float own, float other) { return strength > 0.0001f ? own : other; }
    private static Color Pick(float strength, Color own, Color other) { return strength > 0.0001f ? own : other; }

    /// <summary> 画面の縁（UI の絵）が何もしないか </summary>
    public bool IsEdgeNeutral => edgeFade < 0.001f && topShade < 0.001f && bottomShade < 0.001f;

    /// <summary> 画面のレンズの層（縁・光漏れ）が何もしないか </summary>
    public bool IsLensNeutral => IsEdgeNeutral && leak < 0.001f;

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
            liftColor = Color.Lerp(a.lift > 0.0001f ? a.liftColor : b.liftColor, b.lift > 0.0001f ? b.liftColor : a.liftColor, t),
            lift = Mathf.Lerp(a.lift, b.lift, t),
            vignette = Mathf.Lerp(a.vignette, b.vignette, t),
            vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, t),
            vignetteColor = Color.Lerp(a.vignetteColor, b.vignetteColor, t),
            // 縁・グラデーションの形と色は、強さが 0 の側に引っぱられないように、ある側の値を使う
            edgeFade = Mathf.Lerp(a.edgeFade, b.edgeFade, t),
            edgeRound = Mathf.Lerp(Pick(a.edgeFade, a.edgeRound, b.edgeRound), Pick(b.edgeFade, b.edgeRound, a.edgeRound), t),
            topShade = Mathf.Lerp(a.topShade, b.topShade, t),
            topShadeColor = Color.Lerp(Pick(a.topShade, a.topShadeColor, b.topShadeColor), Pick(b.topShade, b.topShadeColor, a.topShadeColor), t),
            topShadeHeight = Mathf.Lerp(Pick(a.topShade, a.topShadeHeight, b.topShadeHeight), Pick(b.topShade, b.topShadeHeight, a.topShadeHeight), t),
            bottomShade = Mathf.Lerp(a.bottomShade, b.bottomShade, t),
            bottomShadeColor = Color.Lerp(Pick(a.bottomShade, a.bottomShadeColor, b.bottomShadeColor), Pick(b.bottomShade, b.bottomShadeColor, a.bottomShadeColor), t),
            bottomShadeHeight = Mathf.Lerp(Pick(a.bottomShade, a.bottomShadeHeight, b.bottomShadeHeight), Pick(b.bottomShade, b.bottomShadeHeight, a.bottomShadeHeight), t),
            // 光漏れ：全体の強さだけを混ぜ、形と色は光漏れのある側の値を使う（ない側から入る・ない側へ消えるとき、形が変わらずに薄くなる）
            leak = Mathf.Lerp(a.leak, b.leak, t),
            leakRight = Mathf.Lerp(Pick(a.leak, a.leakRight, b.leakRight), Pick(b.leak, b.leakRight, a.leakRight), t),
            leakRightColor = Color.Lerp(Pick(a.leak, a.leakRightColor, b.leakRightColor), Pick(b.leak, b.leakRightColor, a.leakRightColor), t),
            leakLeft = Mathf.Lerp(Pick(a.leak, a.leakLeft, b.leakLeft), Pick(b.leak, b.leakLeft, a.leakLeft), t),
            leakLeftColor = Color.Lerp(Pick(a.leak, a.leakLeftColor, b.leakLeftColor), Pick(b.leak, b.leakLeftColor, a.leakLeftColor), t),
            leakWidth = Mathf.Lerp(Pick(a.leak, a.leakWidth, b.leakWidth), Pick(b.leak, b.leakWidth, a.leakWidth), t),
            leakBurn = Mathf.Lerp(Pick(a.leak, a.leakBurn, b.leakBurn), Pick(b.leak, b.leakBurn, a.leakBurn), t),
            leakBurnColor = Color.Lerp(Pick(a.leak, a.leakBurnColor, b.leakBurnColor), Pick(b.leak, b.leakBurnColor, a.leakBurnColor), t),
            // 焼ける角は混ぜられないので、光漏れの強い方の角
            leakBurnCorner = b.leak >= a.leak ? b.leakBurnCorner : a.leakBurnCorner,
            leakStreaks = Mathf.Lerp(Pick(a.leak, a.leakStreaks, b.leakStreaks), Pick(b.leak, b.leakStreaks, a.leakStreaks), t),
            leakSway = Mathf.Lerp(Pick(a.leak, a.leakSway, b.leakSway), Pick(b.leak, b.leakSway, a.leakSway), t),
            leakFlash = Mathf.Lerp(Pick(a.leak, a.leakFlash, b.leakFlash), Pick(b.leak, b.leakFlash, a.leakFlash), t),
            flare = Mathf.Lerp(a.flare, b.flare, t),
            grain = Mathf.Lerp(a.grain, b.grain, t),
            // 粒子の種類は混ぜられないので、粒子がある方を使う
            grainType = b.grain > 0.001f ? b.grainType : a.grainType,
            bloom = Mathf.Lerp(a.bloom, b.bloom, t),
            bloomThreshold = Mathf.Lerp(a.bloomThreshold, b.bloomThreshold, t),
            uiAmount = Mathf.Lerp(a.uiAmount, b.uiAmount, t),
            remap = Mathf.Lerp(a.remap, b.remap, t),
            // 元の色を残す割合は、塗り替えがない側に引っぱられないように、塗り替えのある側の値を使う
            keepOriginal = a.remap <= 0.0001f ? b.keepOriginal : (b.remap <= 0.0001f ? a.keepOriginal : Mathf.Lerp(a.keepOriginal, b.keepOriginal, t)),
        };
    }
}
