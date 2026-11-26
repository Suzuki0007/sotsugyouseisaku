/*********************************************************************/
// * \file   PresentationDirector.cs
// * \brief  演出の窓口：色調（ポストプロセス）・光（Global Light 2D と場面の光）・天候・回想を1か所で決めて反映する
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 演出の窓口（色調・光・天候・回想。再生すると自動で作られる。シーンに置かなくてよい）
/// </summary>
///
/// ・色調・光・天候は「層」を重ねて決める。上の層に値があればそちらが勝つ
///   色調・光：シーンの既定 ＜ 範囲（場所の色味）＜ イベント ＜ 回想
///   天候：シーンの既定 ＜ イベント ＜ 範囲（天候の範囲）＜ 回想
/// ・切り替えは、今出ている見た目から次へなめらかに変える（途中でまた切り替えても飛ばない）
/// ・暗いステージ（StageDarkness）の暗さは、決まった光の上に掛ける
/// ・イベントの層は、セーブのときに名前で記録し、ロードで戻す（回想の層は記録しない＝回想に入る前の状態が戻る）
/// ・ほかの仕組み（時代の切り替えなど）からも Set〜 で呼べる
[AddComponentMenu("")]
[DefaultExecutionOrder(500)]
public class PresentationDirector : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    private struct MoodLayer { public bool set; public MoodPreset preset; }
    private struct LightLayer { public bool set; public LightPreset preset; public bool custom; public Color color; public float intensity; }
    private struct WeatherLayer { public bool set; public WeatherPreset preset; }

    private const int LayerCount = 4;
    private const string SaveKeyPrefix = "Presentation/";
    private const string NoneMark = "*";

    // 天候の層の強さの順（弱い → 強い）：既定 ＜ イベント ＜ 範囲 ＜ 回想
    private static readonly int[] WeatherOrder = { (int)PresentationLayer.Base, (int)PresentationLayer.Event, (int)PresentationLayer.Area, (int)PresentationLayer.Flashback };

    //=====================================================================
    // 変数
    //=====================================================================

    private static PresentationDirector instance;
    private static bool quitting;

    // 色調
    private readonly MoodLayer[] moodLayers = new MoodLayer[LayerCount];
    private MoodValues moodCurrent = MoodValues.Neutral;
    private MoodValues moodFrom, moodMid, moodTo;
    private bool moodUseMid;
    private float moodTime, moodDur1, moodDur2;
    private PresentationCurve moodCurve;
    private bool moodBlending;

    private Volume volume;
    private VolumeProfile profile;
    private ColorAdjustments colorAdjustments;
    private WhiteBalance whiteBalance;
    private SplitToning splitToning;
    private Vignette vignette;
    private FilmGrain filmGrain;
    private Bloom bloom;

    private Camera cam;
    private UniversalAdditionalCameraData camData;
    private bool camOriginalPost;
    private bool postOn;

    // 光
    private readonly LightLayer[] lightLayers = new LightLayer[LayerCount];
    private Light2D globalLight;
    private Color originalColor = Color.white;
    private float originalIntensity = 1.0f;
    private Color lightColor = Color.white, lightFromColor, lightToColor;
    private float lightIntensity = 1.0f, lightFromIntensity, lightToIntensity;
    private float lightTime, lightDur;
    private PresentationCurve lightCurve;
    private bool lightBlending;
    private readonly List<SceneLight> blendLights = new List<SceneLight>();
    private readonly List<float> blendFrom = new List<float>();
    private readonly List<float> blendTo = new List<float>();
    private Color writtenColor;
    private float writtenIntensity = -1.0f;

    // 暗いステージ
    private StageDarkness darknessSource;
    private float darkness;
    private Color darknessAmbient = Color.white;

    // 範囲
    private AreaTone currentArea;
    private WeatherArea currentWeatherArea;

    // 天候
    private readonly WeatherLayer[] weatherLayers = new WeatherLayer[LayerCount];
    private readonly List<WeatherView> views = new List<WeatherView>();
    private WeatherPreset weatherTarget;
    private Material weatherLit;
    private Material weatherUnlit;
    private Sprite hazeSprite;
    private Texture2D hazeTexture;

    // 回想
    private FlashbackPreset flashback;
    private bool inFlashback;
    private float lastFlashbackSeconds = 2.0f;

    // セーブの名前 → プリセット
    private static Dictionary<string, ScriptableObject> presetsByName;

    //=====================================================================
    // 作る
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
        presetsByName = null;
        Save.BeforeCommit -= OnBeforeCommit;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        Save.BeforeCommit -= OnBeforeCommit;
        Save.BeforeCommit += OnBeforeCommit;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
        // 最初のシーンは sceneLoaded の前に作られるので、ここで合わせる
        if(instance != null && !instance.boundOnce) { instance.ResetForScene(SceneManager.GetActiveScene()); }
    }

    private bool boundOnce;

    /// <summary> 再生中なら窓口を作る </summary>
    public static void Ensure()
    {
        if(!Application.isPlaying || instance != null || quitting) { return; }
        var go = new GameObject("PresentationDirector（演出・自動）");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PresentationDirector>();
    }

    private static PresentationDirector Get()
    {
        Ensure();
        return instance;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(mode != LoadSceneMode.Single) { return; }
        Ensure();
        if(instance != null) { instance.ResetForScene(scene); }
    }

    private DialogueMoodLayer dialogueMood;

    /// <summary> 会話画面にかけている強さ（0 でかけていない。確かめ用） </summary>
    public static float DialogueMoodAmount => instance != null && instance.dialogueMood != null ? instance.dialogueMood.Amount : 0.0f;

    private void Awake()
    {
        dialogueMood = gameObject.AddComponent<DialogueMoodLayer>();
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100.0f;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "演出の色調（実行中）";
        colorAdjustments = profile.Add<ColorAdjustments>(true);
        whiteBalance = profile.Add<WhiteBalance>(true);
        splitToning = profile.Add<SplitToning>(true);
        vignette = profile.Add<Vignette>(true);
        filmGrain = profile.Add<FilmGrain>(true);
        bloom = profile.Add<Bloom>(true);
        vignette.center.value = new Vector2(0.5f, 0.5f);
        vignette.rounded.value = false;
        filmGrain.response.value = 0.8f;
        bloom.scatter.value = 0.7f;
        volume.sharedProfile = profile;

        weatherLit = Resources.Load<Material>("Presentation/Materials/Mat_Weather_Lit");
        weatherUnlit = Resources.Load<Material>("Presentation/Materials/Mat_Weather_Unlit");
        hazeTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for(int i = 0; i < pixels.Length; i++) { pixels[i] = new Color32(255, 255, 255, 255); }
        hazeTexture.SetPixels32(pixels);
        hazeTexture.Apply();
        hazeSprite = Sprite.Create(hazeTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4.0f);

        ApplyMood();
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
        if(camData != null) { camData.renderPostProcessing = camOriginalPost; }
        if(profile != null) { Destroy(profile); }
        if(hazeSprite != null) { Destroy(hazeSprite); }
        if(hazeTexture != null) { Destroy(hazeTexture); }
    }

    //=====================================================================
    // シーンが変わったとき
    //=====================================================================

    private void ResetForScene(Scene scene)
    {
        boundOnce = true;
        if(dialogueMood != null) { dialogueMood.ResetForScene(); }

        // 回想は持ち越さない
        if(inFlashback) { EndFlashbackState(); }

        for(int i = 0; i < LayerCount; i++)
        {
            moodLayers[i] = default;
            lightLayers[i] = default;
            weatherLayers[i] = default;
        }
        currentArea = null;
        currentWeatherArea = null;
        darknessSource = null;
        darkness = 0.0f;

        for(int i = 0; i < views.Count; i++) { if(views[i] != null) { Destroy(views[i].gameObject); } }
        views.Clear();
        weatherTarget = null;

        // カメラ
        if(camData != null) { camData.renderPostProcessing = camOriginalPost; }
        cam = null;
        camData = null;
        postOn = false;
        foreach(FocusCamera focus in FindObjectsByType<FocusCamera>())
        {
            if(focus.gameObject.scene == scene && focus.TryGetComponent(out Camera c)) { cam = c; break; }
        }
        if(cam == null) { cam = Camera.main; }
        if(cam != null)
        {
            camData = cam.GetUniversalAdditionalCameraData();
            camOriginalPost = camData != null && camData.renderPostProcessing;
        }

        // 全体の光（シーンにある物の今の値を「元」にする）
        globalLight = null;
        foreach(Light2D l in FindObjectsByType<Light2D>())
        {
            if(l.lightType == Light2D.LightType.Global && l.isActiveAndEnabled && l.gameObject.scene == scene) { globalLight = l; break; }
        }
        if(globalLight != null)
        {
            originalColor = globalLight.color;
            originalIntensity = globalLight.intensity;
        }
        writtenIntensity = -1.0f;

        // セーブに残っているイベントの層を戻す
        string sceneName = scene.name;
        if(Save.HasSlot || Save.Count > 0)
        {
            RestoreMoodLayer(Save.Get(SaveKeyPrefix + sceneName + "/Mood", ""));
            RestoreLightLayer(Save.Get(SaveKeyPrefix + sceneName + "/Light", ""));
            RestoreWeatherLayer(Save.Get(SaveKeyPrefix + sceneName + "/Weather", ""));
        }

        StartMood(0.0f, PresentationCurve.Smooth, false);
        StartLight(0.0f, PresentationCurve.Smooth);
        StartWeather(0.0f);
    }

    //=====================================================================
    // 公開（部品・シーンの既定・ほかの仕組みから呼ぶ）
    //=====================================================================

    /// <summary> 今、回想の中か </summary>
    public static bool InFlashback => instance != null && instance.inFlashback;

    /// <summary> 部品が書き込む層（回想の中なら回想の層。戻ると回想に入る前に戻る） </summary>
    public static PresentationLayer EventLayer => InFlashback ? PresentationLayer.Flashback : PresentationLayer.Event;

    /// <summary> 最後に回想に入った・戻ったときの秒数（「フラグでなめらかに出す」が合わせる） </summary>
    public static float LastFlashbackSeconds => instance != null ? instance.lastFlashbackSeconds : 2.0f;

    public static bool IsMoodBlending => instance != null && instance.moodBlending;
    public static bool IsLightBlending => instance != null && instance.lightBlending;
    public static bool IsWeatherBlending
    {
        get
        {
            if(instance == null) { return false; }
            for(int i = 0; i < instance.views.Count; i++) { if(instance.views[i] != null && instance.views[i].IsBlending) { return true; } }
            return false;
        }
    }

    /// <summary> 色調を決める（preset が null なら「通常」） </summary>
    public static void SetMood(PresentationLayer layer, MoodPreset preset, float seconds, PresentationCurve curve = PresentationCurve.Smooth, bool viaGray = false)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.moodLayers[(int)layer] = new MoodLayer { set = true, preset = preset };
        d.StartMood(seconds, curve, viaGray);
    }

    /// <summary> 色調の層を外す（下の層に戻る） </summary>
    public static void ClearMood(PresentationLayer layer, float seconds, PresentationCurve curve = PresentationCurve.Smooth)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.moodLayers[(int)layer] = default;
        d.StartMood(seconds, curve, false);
    }

    /// <summary> 光を決める（preset が null ならシーンの Global Light 2D のまま） </summary>
    public static void SetLight(PresentationLayer layer, LightPreset preset, float seconds, PresentationCurve curve = PresentationCurve.Smooth)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.lightLayers[(int)layer] = new LightLayer { set = true, preset = preset };
        d.StartLight(seconds, curve);
    }

    public static void ClearLight(PresentationLayer layer, float seconds, PresentationCurve curve = PresentationCurve.Smooth)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.lightLayers[(int)layer] = default;
        d.StartLight(seconds, curve);
    }

    /// <summary> 天候を決める（preset が null なら止める） </summary>
    public static void SetWeather(PresentationLayer layer, WeatherPreset preset, float seconds)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.weatherLayers[(int)layer] = new WeatherLayer { set = true, preset = preset };
        d.StartWeather(seconds);
    }

    public static void ClearWeather(PresentationLayer layer, float seconds)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.weatherLayers[(int)layer] = default;
        d.StartWeather(seconds);
    }

    /// <summary> シーンの既定（ScenePresentation・時代の切り替えから） </summary>
    public static void SetBase(MoodPreset mood, LightPreset light, WeatherPreset weather, float seconds)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.moodLayers[(int)PresentationLayer.Base] = new MoodLayer { set = true, preset = mood };
        d.lightLayers[(int)PresentationLayer.Base] = new LightLayer { set = true, preset = light };
        d.weatherLayers[(int)PresentationLayer.Base] = new WeatherLayer { set = true, preset = weather };
        d.StartMood(seconds, PresentationCurve.Smooth, false);
        d.StartLight(seconds, PresentationCurve.Smooth);
        d.StartWeather(seconds);
    }

    /// <summary> 部品「色調を戻す」：イベントで変えた色調を外す（回想の中なら回想の色調へ） </summary>
    public static void ResetMoodForEvent(float seconds, PresentationCurve curve)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        if(d.inFlashback)
        {
            d.moodLayers[(int)PresentationLayer.Flashback] = d.flashback != null && d.flashback.Mood != null ? new MoodLayer { set = true, preset = d.flashback.Mood } : default;
        }
        else { d.moodLayers[(int)PresentationLayer.Event] = default; }
        d.StartMood(seconds, curve, false);
    }

    /// <summary> 部品「光を戻す」 </summary>
    public static void ResetLightForEvent(float seconds, PresentationCurve curve)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        if(d.inFlashback)
        {
            d.lightLayers[(int)PresentationLayer.Flashback] = d.flashback != null && d.flashback.Light != null ? new LightLayer { set = true, preset = d.flashback.Light } : default;
        }
        else { d.lightLayers[(int)PresentationLayer.Event] = default; }
        d.StartLight(seconds, curve);
    }

    /// <summary> 部品「天候を変える（シーンの既定に戻す）」 </summary>
    public static void ResetWeatherForEvent(float seconds)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        if(d.inFlashback) { d.weatherLayers[(int)PresentationLayer.Flashback] = d.FlashbackWeatherLayer(); }
        else { d.weatherLayers[(int)PresentationLayer.Event] = default; }
        d.StartWeather(seconds);
    }

    /// <summary> 回想に入る </summary>
    public static void EnterFlashback(FlashbackPreset preset, float seconds, FlashbackTransition transition)
    {
        PresentationDirector d = Get();
        if(d == null || preset == null) { return; }

        d.flashback = preset;
        d.inFlashback = true;
        d.lastFlashbackSeconds = transition == FlashbackTransition.Instant ? 0.0f : seconds;
        float s = d.lastFlashbackSeconds;

        int fb = (int)PresentationLayer.Flashback;
        d.moodLayers[fb] = preset.Mood != null ? new MoodLayer { set = true, preset = preset.Mood } : default;
        d.lightLayers[fb] = preset.Light != null ? new LightLayer { set = true, preset = preset.Light } : default;
        d.weatherLayers[fb] = d.FlashbackWeatherLayer();

        d.StartMood(s, PresentationCurve.Smooth, transition == FlashbackTransition.GrayThenTint);
        d.StartLight(s, PresentationCurve.Smooth);
        d.StartWeather(s);

        SpriteAnimator.GlobalStepFps = preset.FrameStepFps;
        if(preset.Flag != null) { GameFlags.Set(preset.Flag, true); }
    }

    /// <summary> 回想から戻る（入る前の色調・光・天候へ） </summary>
    public static void ExitFlashback(float seconds, FlashbackTransition transition)
    {
        PresentationDirector d = Get();
        if(d == null || !d.inFlashback) { return; }

        d.lastFlashbackSeconds = transition == FlashbackTransition.Instant ? 0.0f : seconds;
        float s = d.lastFlashbackSeconds;

        int fb = (int)PresentationLayer.Flashback;
        d.moodLayers[fb] = default;
        d.lightLayers[fb] = default;
        d.weatherLayers[fb] = default;
        d.EndFlashbackState();

        d.StartMood(s, PresentationCurve.Smooth, transition == FlashbackTransition.GrayThenTint);
        d.StartLight(s, PresentationCurve.Smooth);
        d.StartWeather(s);
    }

    private void EndFlashbackState()
    {
        FlashbackPreset was = flashback;
        inFlashback = false;
        flashback = null;
        SpriteAnimator.GlobalStepFps = 0.0f;
        if(was != null && was.Flag != null) { GameFlags.Set(was.Flag, false); }
    }

    private WeatherLayer FlashbackWeatherLayer()
    {
        if(flashback == null) { return default; }
        switch(flashback.Weather)
        {
            case FlashbackPreset.WeatherUse.None: return new WeatherLayer { set = true, preset = null };
            case FlashbackPreset.WeatherUse.Use:  return new WeatherLayer { set = true, preset = flashback.WeatherPreset };
            default: return default;
        }
    }

    /// <summary>
    /// 暗いステージ（StageDarkness）から、暗さと暗い所の色を受け取る。true なら窓口が全体の光を書く
    /// </summary>
    public static bool ReportDarkness(StageDarkness source, float value, Color ambient, Light2D light)
    {
        if(!Application.isPlaying) { return false; }
        PresentationDirector d = Get();
        if(d == null) { return false; }
        if(d.darknessSource != source && d.darknessSource != null && d.darknessSource.isActiveAndEnabled) { return true; }
        d.darknessSource = source;
        d.darkness = value;
        d.darknessAmbient = ambient;
        if(d.globalLight == null && light != null)
        {
            d.globalLight = light;
            d.originalColor = light.color;
            d.originalIntensity = light.intensity;
            d.StartLight(0.0f, PresentationCurve.Smooth);
        }
        return true;
    }

    /// <summary> 暗いステージが切られたとき </summary>
    public static bool ReportDarknessEnd(StageDarkness source)
    {
        if(instance == null) { return false; }
        if(instance.darknessSource == source) { instance.darknessSource = null; instance.darkness = 0.0f; }
        return true;
    }

    //=====================================================================
    // 状態（デバッグ・確かめ用）
    //=====================================================================

    public static string CurrentMoodName => instance == null ? "" : NameOf(instance.TopMood(out _));
    public static string CurrentLightName
    {
        get
        {
            if(instance == null) { return ""; }
            LightLayer top = instance.TopLight(out _);
            // 場所の色味（範囲の層）はプリセットではないので名前で出す（11-26：右上の表示が「シーンのまま」になり、効いていないように見えた）
            return top.custom ? "場所の色味" : NameOf(top.preset);
        }
    }
    public static string CurrentWeatherName => instance == null ? "" : NameOf(instance.TopWeather(out _));

    /// <summary> 層ごとの中身（1行） </summary>
    public static string Describe()
    {
        if(instance == null) { return "（窓口なし）"; }
        PresentationDirector d = instance;
        var sb = new System.Text.StringBuilder();
        string[] names = { "既定", "範囲", "イベント", "回想" };
        sb.Append("色調[");
        for(int i = 0; i < LayerCount; i++) { if(d.moodLayers[i].set) { sb.Append(names[i]).Append('=').Append(NameOf(d.moodLayers[i].preset, "通常")).Append(' '); } }
        sb.Append("] 光[");
        for(int i = 0; i < LayerCount; i++) { if(d.lightLayers[i].set) { sb.Append(names[i]).Append('=').Append(d.lightLayers[i].custom ? "場所の色味" : NameOf(d.lightLayers[i].preset, "シーンのまま")).Append(' '); } }
        sb.Append("] 天候[");
        for(int i = 0; i < LayerCount; i++) { if(d.weatherLayers[i].set) { sb.Append(names[i]).Append('=').Append(NameOf(d.weatherLayers[i].preset, "なし")).Append(' '); } }
        sb.Append("] 回想=").Append(d.inFlashback ? NameOf(d.flashback) : "なし");
        sb.Append(" 光の値=").Append(d.globalLight != null ? d.globalLight.color.ToString("F2") + "×" + d.globalLight.intensity.ToString("F2") : "なし");
        sb.Append(" ポストプロセス=").Append(d.postOn ? "オン" : "オフ");
        sb.Append(" 天候の数=").Append(d.views.Count);
        return sb.ToString();
    }

    /// <summary> 今の色調の値（確かめ用） </summary>
    public static MoodValues CurrentMoodValues => instance != null ? instance.moodCurrent : MoodValues.Neutral;

    private static string NameOf(Object o, string empty = "")
    {
        return o != null ? o.name : empty;
    }

    //=====================================================================
    // 色調
    //=====================================================================

    private MoodPreset TopMood(out bool any)
    {
        for(int i = LayerCount - 1; i >= 0; i--) { if(moodLayers[i].set) { any = true; return moodLayers[i].preset; } }
        any = false;
        return null;
    }

    private void StartMood(float seconds, PresentationCurve curve, bool viaGray)
    {
        MoodPreset top = TopMood(out _);
        MoodValues target = top != null ? top.Values : MoodValues.Neutral;
        moodFrom = moodCurrent;
        moodTo = target;
        moodCurve = curve;
        moodTime = 0.0f;

        if(seconds <= 0.0f)
        {
            moodCurrent = target;
            moodBlending = false;
            ApplyMood();
            return;
        }

        moodUseMid = viaGray;
        if(viaGray)
        {
            moodMid = MoodValues.Lerp(moodFrom, moodTo, 0.5f).Desaturated();
            moodDur1 = seconds * 0.4f;
            moodDur2 = seconds - moodDur1;
        }
        else
        {
            moodDur1 = seconds;
            moodDur2 = 0.0f;
        }
        moodBlending = true;
        ApplyMood();
    }

    private void TickMood(float dt)
    {
        if(!moodBlending) { return; }
        moodTime += dt;
        if(moodUseMid)
        {
            if(moodTime < moodDur1) { moodCurrent = MoodValues.Lerp(moodFrom, moodMid, PresentationEase.Apply(moodCurve, moodTime / moodDur1)); }
            else if(moodTime < moodDur1 + moodDur2) { moodCurrent = MoodValues.Lerp(moodMid, moodTo, PresentationEase.Apply(moodCurve, (moodTime - moodDur1) / moodDur2)); }
            else { moodCurrent = moodTo; moodBlending = false; }
        }
        else
        {
            if(moodTime < moodDur1) { moodCurrent = MoodValues.Lerp(moodFrom, moodTo, PresentationEase.Apply(moodCurve, moodTime / moodDur1)); }
            else { moodCurrent = moodTo; moodBlending = false; }
        }
        ApplyMood();
    }

    private void ApplyMood()
    {
        if(profile == null) { return; }
        MoodValues v = moodCurrent;
        bool low = PresentationEase.IsLowQuality;

        // 会話画面（プリセットの「会話画面にもかける」）
        if(dialogueMood != null) { dialogueMood.Apply(v); }

        colorAdjustments.saturation.value = v.saturation;
        colorAdjustments.contrast.value = v.contrast;
        colorAdjustments.postExposure.value = v.brightness;
        colorAdjustments.colorFilter.value = v.colorFilter;
        whiteBalance.temperature.value = v.temperature;
        whiteBalance.tint.value = v.tint;
        splitToning.shadows.value = v.shadows;
        splitToning.highlights.value = v.highlights;
        splitToning.balance.value = v.splitBalance;
        vignette.intensity.value = v.vignette;
        vignette.smoothness.value = Mathf.Max(0.01f, v.vignetteSmoothness);
        vignette.color.value = v.vignetteColor;
        filmGrain.intensity.value = low ? 0.0f : v.grain;
        filmGrain.type.value = v.grainType;
        bloom.intensity.value = low ? 0.0f : v.bloom;
        bloom.threshold.value = v.bloomThreshold;

        // 何も加工しないときはポストプロセスを切る（軽くするため）
        bool need = camOriginalPost || moodBlending || !v.IsNeutral;
        if(camData != null && need != postOn)
        {
            camData.renderPostProcessing = need;
            postOn = need;
        }
    }

    //=====================================================================
    // 光
    //=====================================================================

    private LightLayer TopLight(out bool any)
    {
        for(int i = LayerCount - 1; i >= 0; i--) { if(lightLayers[i].set) { any = true; return lightLayers[i]; } }
        any = false;
        return default;
    }

    private void StartLight(float seconds, PresentationCurve curve)
    {
        LightLayer top = TopLight(out _);
        Color toColor = originalColor;
        float toIntensity = originalIntensity;
        if(top.custom) { toColor = top.color; toIntensity = top.intensity; }
        else if(top.preset != null) { toColor = top.preset.LightColor; toIntensity = top.preset.Intensity; }

        lightFromColor = lightColor;
        lightFromIntensity = lightIntensity;
        lightToColor = toColor;
        lightToIntensity = toIntensity;
        lightCurve = curve;
        lightTime = 0.0f;
        lightDur = seconds;

        // 場面の光
        LightPreset preset = top.custom ? null : top.preset;
        blendLights.Clear();
        blendFrom.Clear();
        blendTo.Clear();
        IReadOnlyList<SceneLight> all = SceneLight.All;
        for(int i = 0; i < all.Count; i++)
        {
            SceneLight s = all[i];
            if(s == null) { continue; }
            blendLights.Add(s);
            blendFrom.Add(s.Factor);
            blendTo.Add(s.Resolve(preset != null ? preset.FactorOf(s.Group) : -1.0f));
        }

        if(seconds <= 0.0f)
        {
            lightColor = toColor;
            lightIntensity = toIntensity;
            for(int i = 0; i < blendLights.Count; i++) { blendLights[i].SetFactor(blendTo[i]); }
            lightBlending = false;
        }
        else { lightBlending = true; }
        ApplyLight();
    }

    private void TickLight(float dt)
    {
        if(lightBlending)
        {
            lightTime += dt;
            float t = PresentationEase.Apply(lightCurve, lightDur > 0.0f ? lightTime / lightDur : 1.0f);
            lightColor = Color.Lerp(lightFromColor, lightToColor, t);
            lightIntensity = Mathf.Lerp(lightFromIntensity, lightToIntensity, t);
            for(int i = 0; i < blendLights.Count; i++)
            {
                if(blendLights[i] != null) { blendLights[i].SetFactor(Mathf.Lerp(blendFrom[i], blendTo[i], t)); }
            }
            if(lightTime >= lightDur) { lightBlending = false; }
        }
        ApplyLight();
    }

    private void ApplyLight()
    {
        if(globalLight == null) { return; }

        Color c = lightColor;
        float i = lightIntensity;
        if(darknessSource != null && darknessSource.isActiveAndEnabled)
        {
            // 光の層がないときは、暗いステージの今までの見た目（色＝暗い所の色、強さ＝元×(1−暗さ)）と同じにする
            TopLight(out bool any);
            c = any || lightBlending ? lightColor * darknessAmbient : darknessAmbient;
            c.a = 1.0f;
            i = lightIntensity * (1.0f - darkness);
        }

        if(c == writtenColor && Mathf.Approximately(i, writtenIntensity)) { return; }
        globalLight.color = c;
        globalLight.intensity = i;
        writtenColor = c;
        writtenIntensity = i;
    }

    //=====================================================================
    // 天候
    //=====================================================================

    private WeatherPreset TopWeather(out bool any)
    {
        for(int k = WeatherOrder.Length - 1; k >= 0; k--)
        {
            int i = WeatherOrder[k];
            if(weatherLayers[i].set) { any = true; return weatherLayers[i].preset; }
        }
        any = false;
        return null;
    }

    private void StartWeather(float seconds)
    {
        WeatherPreset target = TopWeather(out _);
        weatherTarget = target;

        bool found = false;
        for(int i = 0; i < views.Count; i++)
        {
            WeatherView v = views[i];
            if(v == null) { continue; }
            bool isTarget = v.Preset == target;
            if(isTarget) { found = true; }
            v.SetTarget(isTarget ? 1.0f : 0.0f, seconds);
        }

        if(!found && target != null)
        {
            if(weatherLit == null || weatherUnlit == null)
            {
                Debug.LogWarning("天候：Resources/Presentation/Materials/Mat_Weather_Lit・Mat_Weather_Unlit がありません（Tools > 演出 > 最初のプリセットを作る）");
                return;
            }
            WeatherView view = WeatherView.Create(target, transform, weatherLit, weatherUnlit, hazeSprite);
            views.Add(view);
            view.Tick(cam, 0.0f);
            view.SetTarget(1.0f, seconds);
        }
    }

    private void TickWeather(float dt)
    {
        for(int i = views.Count - 1; i >= 0; i--)
        {
            WeatherView v = views[i];
            if(v == null) { views.RemoveAt(i); continue; }
            v.Tick(cam, dt);
            if(v.IsDone && v.Preset != weatherTarget)
            {
                Destroy(v.gameObject);
                views.RemoveAt(i);
            }
        }
    }

    //=====================================================================
    // 範囲（場所の色味・天候の範囲）
    //=====================================================================

    private void TickAreas()
    {
        PlayerSwitchManager psm = PlayerSwitchManager.Instance;
        GameObject player = psm != null ? psm.CurrentPlayer : null;
        if(player == null) { return; }
        Vector2 p = player.transform.position;

        // 場所の色味
        AreaTone bestTone = null;
        IReadOnlyList<AreaTone> tones = AreaTone.Active;
        for(int i = 0; i < tones.Count; i++)
        {
            AreaTone a = tones[i];
            if(a.Contains(p) && (bestTone == null || a.Priority > bestTone.Priority)) { bestTone = a; }
        }
        if(bestTone != currentArea)
        {
            float seconds = bestTone != null ? bestTone.BlendSeconds : (currentArea != null ? currentArea.BlendSeconds : 0.0f);
            currentArea = bestTone;
            lightLayers[(int)PresentationLayer.Area] = bestTone != null
                ? new LightLayer { set = true, custom = true, color = bestTone.ToneColor, intensity = bestTone.ToneIntensity }
                : default;
            StartLight(seconds, PresentationCurve.Smooth);
        }

        // 天候の範囲
        WeatherArea bestWeather = null;
        IReadOnlyList<WeatherArea> areas = WeatherArea.Active;
        for(int i = 0; i < areas.Count; i++)
        {
            WeatherArea a = areas[i];
            if(a.Contains(p) && (bestWeather == null || a.Priority > bestWeather.Priority)) { bestWeather = a; }
        }
        if(bestWeather != currentWeatherArea)
        {
            float seconds = bestWeather != null ? bestWeather.BlendSeconds : (currentWeatherArea != null ? currentWeatherArea.BlendSeconds : 0.0f);
            currentWeatherArea = bestWeather;
            weatherLayers[(int)PresentationLayer.Area] = bestWeather != null ? new WeatherLayer { set = true, preset = bestWeather.Weather } : default;
            StartWeather(seconds);
        }
    }

    //=====================================================================
    // 毎フレーム
    //=====================================================================

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        TickAreas();
        TickMood(dt);
        TickLight(dt);
        TickWeather(dt);
        if(dialogueMood != null) { dialogueMood.Tick(dt); }
    }

    //=====================================================================
    // セーブ
    //=====================================================================

    private static void OnBeforeCommit()
    {
        if(instance == null) { return; }
        string scene = SceneManager.GetActiveScene().name;
        if(string.IsNullOrEmpty(scene)) { return; }
        int ev = (int)PresentationLayer.Event;
        PresentationDirector d = instance;
        Save.SetPending(SaveKeyPrefix + scene + "/Mood", Encode(d.moodLayers[ev].set, d.moodLayers[ev].preset));
        Save.SetPending(SaveKeyPrefix + scene + "/Light", Encode(d.lightLayers[ev].set, d.lightLayers[ev].preset));
        Save.SetPending(SaveKeyPrefix + scene + "/Weather", Encode(d.weatherLayers[ev].set, d.weatherLayers[ev].preset));
    }

    private static string Encode(bool set, Object preset)
    {
        if(!set) { return ""; }
        return preset != null ? preset.name : NoneMark;
    }

    private void RestoreMoodLayer(string code)
    {
        if(string.IsNullOrEmpty(code)) { return; }
        moodLayers[(int)PresentationLayer.Event] = new MoodLayer { set = true, preset = code == NoneMark ? null : Find<MoodPreset>(code) };
    }

    private void RestoreLightLayer(string code)
    {
        if(string.IsNullOrEmpty(code)) { return; }
        lightLayers[(int)PresentationLayer.Event] = new LightLayer { set = true, preset = code == NoneMark ? null : Find<LightPreset>(code) };
    }

    private void RestoreWeatherLayer(string code)
    {
        if(string.IsNullOrEmpty(code)) { return; }
        weatherLayers[(int)PresentationLayer.Event] = new WeatherLayer { set = true, preset = code == NoneMark ? null : Find<WeatherPreset>(code) };
    }

    /// <summary> Resources/Presentation/ にあるプリセットを名前で探す </summary>
    public static T Find<T>(string presetName) where T : ScriptableObject
    {
        if(presetsByName == null)
        {
            presetsByName = new Dictionary<string, ScriptableObject>();
            foreach(ScriptableObject so in Resources.LoadAll<ScriptableObject>("Presentation"))
            {
                if(so is MoodPreset || so is LightPreset || so is WeatherPreset || so is FlashbackPreset)
                {
                    presetsByName[so.GetType().Name + "/" + so.name] = so;
                }
            }
        }
        presetsByName.TryGetValue(typeof(T).Name + "/" + presetName, out ScriptableObject found);
        if(found == null) { Debug.LogWarning($"演出：セーブにあったプリセット「{presetName}」が Resources/Presentation/ にありません"); }
        return found as T;
    }

    /// <summary> Resources/Presentation/ にある全部のプリセット（デバッグウィンドウ用） </summary>
    public static List<T> All<T>() where T : ScriptableObject
    {
        var list = new List<T>(Resources.LoadAll<T>("Presentation"));
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }
}
