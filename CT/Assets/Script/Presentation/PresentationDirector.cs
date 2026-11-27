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
    private static float sceneGlobalScale = 1.0f;

    /// <summary> シーンの「全体の光の倍率」（ScenePresentation が入れる。光のプリセットの全体の光にだけ掛かる） </summary>
    public static float SceneGlobalLightScale
    {
        get => sceneGlobalScale;
        set { sceneGlobalScale = Mathf.Max(0.01f, value); if(instance != null) { instance.StartLight(0.0f, PresentationCurve.Smooth); } }
    }
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
    private ColorLookup colorLookup;

    // 明るさ → 色の塗り替え（色の対応表 LUT を実行中に作る。11-26 依頼書「青と黄色の哀愁」）
    private const int LutSize = 32;
    private Texture2D lutTexture;
    private Color32[] lutPixels;
    private readonly Color[] rampFrom = new Color[256];
    private readonly Color[] rampTo = new Color[256];
    private readonly Color[] rampCurrent = new Color[256];
    private MoodPreset rampPreset;
    private float rampT = 1.0f;
    private float builtRampT = -1.0f;
    private float builtKeep = -1.0f;
    private bool rampDirty = true;
    private static bool showBands;
    private static bool identityLut;

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
    private readonly List<Color> blendColorFrom = new List<Color>();
    private readonly List<Color> blendColorTo = new List<Color>();
    private Color writtenColor;

    // 色の戻り具合（0〜1。シーンの既定の「色が戻ったときの色調・光」へ近づける割合。セーブ Presentation/<シーン>/Restore）
    private float restoreAmount;

    /// <summary> 今の全体の光（色×強さ）。波紋の「色が戻る」が使う </summary>
    public static Color CurrentGlobalLight => instance != null && instance.writtenIntensity >= 0.0f ? instance.writtenColor * instance.writtenIntensity : Color.white;

    /// <summary> 今の色の戻り具合 </summary>
    public static float RestoreAmount => instance != null ? instance.restoreAmount : 0.0f;

    /// <summary> 色の戻り具合を変える（add なら足す）。セーブに残る </summary>
    public static void SetRestoreAmount(float value, bool add, float seconds)
    {
        PresentationDirector d = Get();
        if(d == null) { return; }
        d.restoreAmount = Mathf.Clamp01(add ? d.restoreAmount + value : value);
        string scene = SceneManager.GetActiveScene().name;
        Save.SetPending(SaveKeyPrefix + scene + "/Restore", d.restoreAmount);
        d.StartMood(seconds, PresentationCurve.Smooth, false);
        d.StartLight(seconds, PresentationCurve.Smooth);
    }

    private int TopMoodLayerIndex()
    {
        for(int i = LayerCount - 1; i >= 0; i--) { if(moodLayers[i].set) { return i; } }
        return -1;
    }

    private int TopLightLayerIndex()
    {
        for(int i = LayerCount - 1; i >= 0; i--) { if(lightLayers[i].set) { return i; } }
        return -1;
    }
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
        sceneGlobalScale = 1.0f;
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

    private ScreenLensLayer lens;

    /// <summary> 画面のレンズの層（縁など）を出しているか（確かめ用） </summary>
    public static bool LensVisible => instance != null && instance.lens != null && instance.lens.Visible;

    /// <summary> 光漏れの「一瞬光る」の今の強さ（確かめ用） </summary>
    public static float LeakFlashAmount => instance != null && instance.lens != null ? instance.lens.FlashAmount : 0.0f;

    /// <summary> 確かめ用：光漏れをすぐに一瞬光らせる </summary>
    public static void FlashLeakNow()
    {
        if(instance != null && instance.lens != null) { instance.lens.FlashNow(); }
    }

    private void Awake()
    {
        dialogueMood = gameObject.AddComponent<DialogueMoodLayer>();
        lens = gameObject.AddComponent<ScreenLensLayer>();
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
        colorLookup = profile.Add<ColorLookup>(true);
        lutTexture = new Texture2D(LutSize * LutSize, LutSize, TextureFormat.RGBA32, false, true) { name = "演出の塗り替え（実行中）", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        lutPixels = new Color32[LutSize * LutSize * LutSize];
        for(int i = 0; i < 256; i++) { rampCurrent[i] = rampFrom[i] = rampTo[i] = new Color(i / 255.0f, i / 255.0f, i / 255.0f, 1.0f); }
        colorLookup.texture.value = lutTexture;
        colorLookup.contribution.value = 0.0f;
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
        if(lutTexture != null) { Destroy(lutTexture); }
    }

    //=====================================================================
    // シーンが変わったとき
    //=====================================================================

    private void ResetForScene(Scene scene)
    {
        boundOnce = true;
        sceneGlobalScale = 1.0f;
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
        if(lens != null) { lens.SetCamera(cam); }

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
        restoreAmount = Save.Get(SaveKeyPrefix + sceneName + "/Restore", 0.0f);

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
        // 光が空なら既定の層は入れない（シーンの Global Light のまま＝入れていないのと同じ。暗いステージの色の扱いも前のまま）
        d.lightLayers[(int)PresentationLayer.Base] = light != null ? new LightLayer { set = true, preset = light } : default;
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
        // 色の戻り具合：いちばん上がシーンの既定の層なら、「色が戻ったときの色調」へ割合だけ近づける
        ScenePresentation sp = ScenePresentation.Current;
        if(restoreAmount > 0.0001f && sp != null && sp.RestoredMood != null && TopMoodLayerIndex() == (int)PresentationLayer.Base)
        {
            target = MoodValues.Lerp(target, sp.RestoredMood.Values, restoreAmount);
        }
        moodFrom = moodCurrent;
        moodTo = target;
        moodCurve = curve;
        moodTime = 0.0f;

        // 塗り替えの色の段：今の色 → 次の色（次が塗り替えなしなら今の色のまま強さだけ下げる。今が塗り替えなしなら色は動かさず強さだけ上げる）
        System.Array.Copy(rampCurrent, rampFrom, 256);
        if(top != null && top.Remaps) { System.Array.Copy(top.Ramp, rampTo, 256); } else { System.Array.Copy(rampCurrent, rampTo, 256); }
        if(moodCurrent.remap <= 0.0001f) { System.Array.Copy(rampTo, rampFrom, 256); }
        rampPreset = top != null && top.Remaps ? top : rampPreset;
        rampT = 0.0f;
        rampDirty = true;

        if(seconds <= 0.0f)
        {
            moodCurrent = target;
            moodBlending = false;
            rampT = 1.0f;
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
            // 灰色の途中は塗り替えが 0 になるので、色の段は前半＝今の色・後半＝次の色
            if(moodTime < moodDur1) { moodCurrent = MoodValues.Lerp(moodFrom, moodMid, PresentationEase.Apply(moodCurve, moodTime / moodDur1)); rampT = 0.0f; }
            else if(moodTime < moodDur1 + moodDur2) { moodCurrent = MoodValues.Lerp(moodMid, moodTo, PresentationEase.Apply(moodCurve, (moodTime - moodDur1) / moodDur2)); rampT = 1.0f; }
            else { moodCurrent = moodTo; moodBlending = false; rampT = 1.0f; }
        }
        else
        {
            if(moodTime < moodDur1) { rampT = PresentationEase.Apply(moodCurve, moodTime / moodDur1); moodCurrent = MoodValues.Lerp(moodFrom, moodTo, rampT); }
            else { moodCurrent = moodTo; moodBlending = false; rampT = 1.0f; }
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
        // 画面の縁（UI の絵）
        if(lens != null) { lens.Apply(v); }

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

        // 明るさ → 色の塗り替え
        UpdateLut(v);

        // 何も加工しないときはポストプロセスを切る（軽くするため）
        bool need = camOriginalPost || moodBlending || !v.IsNeutral || identityLut;
        if(camData != null && need != postOn)
        {
            camData.renderPostProcessing = need;
            postOn = need;
        }
    }

    //=====================================================================
    // 明るさ → 色の塗り替え（色の対応表）
    //=====================================================================

    /// <summary> 境目の表示：塗り替える色の代わりに、色の段ごとにはっきりした色（紺・青・水色・黄・橙・赤…）で塗る（光の強さを決めるとき用） </summary>
    public static bool ShowRemapBands
    {
        get => showBands;
        set { showBands = value; if(instance != null) { instance.rampDirty = true; instance.ApplyMood(); } }
    }

    /// <summary> 確かめ用：何もしない対応表を強さ 1 で当てる（見た目が変わらなければ、対応表の向き・色の空間が正しい） </summary>
    public static bool IdentityLutTest
    {
        get => identityLut;
        set { identityLut = value; if(instance != null) { instance.rampDirty = true; instance.ApplyMood(); } }
    }

    /// <summary> 色調のプリセットを Inspector で変えたとき（今使っている物なら、すぐ反映する） </summary>
    public static void NotifyMoodPresetChanged(MoodPreset preset)
    {
        if(instance == null || preset == null) { return; }
        if(instance.TopMood(out _) == preset) { instance.StartMood(0.0f, PresentationCurve.Smooth, false); }
    }

    private static readonly Color[] BandColors =
    {
        new Color(0.10f, 0.10f, 0.35f), new Color(0.15f, 0.40f, 1.00f), new Color(0.20f, 0.95f, 0.95f), new Color(1.00f, 0.90f, 0.10f),
        new Color(1.00f, 0.50f, 0.10f), new Color(1.00f, 0.15f, 0.15f), new Color(0.90f, 0.20f, 0.90f), Color.white,
    };

    private float builtRemap = -1.0f, builtLift = -1.0f;
    private Color builtLiftColor;

    private void UpdateLut(MoodValues v)
    {
        if(colorLookup == null) { return; }
        // 塗り替えと「暗い所の色」は、どちらも対応表の中で混ぜる（強さも表に焼き込む）。どちらもなければ対応表を使わない
        bool use = identityLut || v.remap > 0.0001f || v.lift > 0.0001f;
        colorLookup.contribution.value = use ? 1.0f : 0.0f;
        if(!use && !rampDirty) { return; }

        float keep = v.keepOriginal;
        bool same = !rampDirty && Mathf.Approximately(rampT, builtRampT) && Mathf.Abs(keep - builtKeep) < 0.002f &&
                    Mathf.Abs(v.remap - builtRemap) < 0.002f && Mathf.Abs(v.lift - builtLift) < 0.002f && v.liftColor == builtLiftColor;
        if(same) { return; }
        rampDirty = false;
        builtRampT = rampT;
        builtKeep = keep;
        builtRemap = v.remap;
        builtLift = v.lift;
        builtLiftColor = v.liftColor;

        for(int i = 0; i < 256; i++) { rampCurrent[i] = Color.Lerp(rampFrom[i], rampTo[i], rampT); }
        BuildLut(keep, v.remap, v.lift, v.liftColor);
    }

    // 暗い所の色が効く明るさの上限（これより明るい所には効かない）
    private const float LiftRange = 0.45f;

    /// <summary>
    /// 対応表（横 32×32・縦 32 の帯）を作る。入る色（sRGB の値）の明るさで色の段を引き、元の色を残す割合・塗り替えの強さで混ぜ、
    /// 最後に暗い所ほど「暗い所の色」を足す（黒 → 紺。明るさ 0.45 より上には効かない）
    /// </summary>
    private void BuildLut(float keep, float remapAmount, float lift, Color liftColor)
    {
        const float inv = 1.0f / (LutSize - 1);
        bool bands = showBands && rampPreset != null;
        for(int b = 0; b < LutSize; b++)
        {
            for(int g = 0; g < LutSize; g++)
            {
                for(int r = 0; r < LutSize; r++)
                {
                    float rf = r * inv, gf = g * inv, bf = b * inv;
                    var orig = new Color(rf, gf, bf);
                    Color c;
                    if(identityLut) { c = orig; }
                    else
                    {
                        float l = Mathf.Clamp01(0.2126f * rf + 0.7152f * gf + 0.0722f * bf);
                        if(bands)
                        {
                            int k = rampPreset.StopIndexAt(l);
                            c = BandColors[Mathf.Clamp(k, 0, BandColors.Length - 1)];
                        }
                        else
                        {
                            c = orig;
                            if(remapAmount > 0.0001f)
                            {
                                Color m = rampCurrent[Mathf.Clamp(Mathf.RoundToInt(l * 255.0f), 0, 255)];
                                if(keep > 0.0f) { m = Color.Lerp(m, orig, keep); }
                                c = Color.Lerp(orig, m, remapAmount);
                            }
                            if(lift > 0.0001f)
                            {
                                float w = 1.0f - Mathf.Clamp01(l / LiftRange);
                                w = w * w * lift;
                                c = new Color(c.r + liftColor.r * w, c.g + liftColor.g * w, c.b + liftColor.b * w);
                            }
                        }
                    }
                    c.a = 1.0f;
                    lutPixels[g * LutSize * LutSize + b * LutSize + r] = c;
                }
            }
        }        lutTexture.SetPixels32(lutPixels);
        lutTexture.Apply(false);
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
        else if(top.preset != null) { toColor = top.preset.LightColor; toIntensity = top.preset.Intensity * sceneGlobalScale; }
        // 色の戻り具合：いちばん上がシーンの既定（または光の層がない）なら、「色が戻ったときの光」へ割合だけ近づける
        ScenePresentation spl = ScenePresentation.Current;
        if(restoreAmount > 0.0001f && spl != null && spl.RestoredLight != null && TopLightLayerIndex() <= (int)PresentationLayer.Base)
        {
            toColor = Color.Lerp(toColor, spl.RestoredLight.LightColor, restoreAmount);
            toIntensity = Mathf.Lerp(toIntensity, spl.RestoredLight.Intensity * sceneGlobalScale, restoreAmount);
        }

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
        blendColorFrom.Clear();
        blendColorTo.Clear();
        IReadOnlyList<SceneLight> all = SceneLight.All;
        for(int i = 0; i < all.Count; i++)
        {
            SceneLight s = all[i];
            if(s == null) { continue; }
            blendLights.Add(s);
            blendFrom.Add(s.Factor);
            blendTo.Add(s.Resolve(preset != null ? preset.FactorOf(s.Group) : -1.0f));
            blendColorFrom.Add(s.CurrentColor);
            blendColorTo.Add(preset != null && preset.TryGetColor(s.Group, out Color pc) ? pc : s.BaseColor);
        }

        // キャラの周りの光
        glowFrom = glowCurrent;
        glowTo = preset != null ? preset.PlayerGlow : 0.0f;
        glowRadius = preset != null ? preset.PlayerGlowRadius : glowRadius;

        if(seconds <= 0.0f)
        {
            lightColor = toColor;
            lightIntensity = toIntensity;
            for(int i = 0; i < blendLights.Count; i++) { blendLights[i].SetFactor(blendTo[i]); blendLights[i].SetColor(blendColorTo[i]); }
            glowCurrent = glowTo;
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
            glowCurrent = Mathf.Lerp(glowFrom, glowTo, t);
            for(int i = 0; i < blendLights.Count; i++)
            {
                if(blendLights[i] != null) { blendLights[i].SetFactor(Mathf.Lerp(blendFrom[i], blendTo[i], t)); blendLights[i].SetColor(Color.Lerp(blendColorFrom[i], blendColorTo[i], t)); }
            }
            if(lightTime >= lightDur) { lightBlending = false; }
        }
        ApplyLight();
        UpdatePlayerGlow();
    }

    //=====================================================================
    // キャラの周りの光（光のプリセットの「キャラの周りを明るく」）
    //=====================================================================

    private float glowFrom, glowTo, glowCurrent;
    private float glowRadius = 1.6f;
    private Light2D glowLight;

    private void UpdatePlayerGlow()
    {
        bool want = glowCurrent > 0.001f;
        if(!want) { if(glowLight != null && glowLight.enabled) { glowLight.enabled = false; } return; }
        PlayerSwitchManager psm = PlayerSwitchManager.Instance;
        GameObject player = psm != null ? psm.CurrentPlayer : null;
        if(player == null) { return; }
        if(glowLight == null)
        {
            var go = new GameObject("キャラの周りの光（演出・自動）");
            go.transform.SetParent(transform, false);
            glowLight = go.AddComponent<Light2D>();
            glowLight.lightType = Light2D.LightType.Point;
            glowLight.falloffIntensity = 0.7f;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for(int i = 0; i < layers.Length; i++) { ids[i] = layers[i].id; }
            glowLight.targetSortingLayers = ids;
        }
        if(!glowLight.enabled) { glowLight.enabled = true; }
        glowLight.intensity = glowCurrent;
        glowLight.pointLightOuterRadius = glowRadius;
        glowLight.pointLightInnerRadius = glowRadius * 0.15f;
        Vector3 pp = player.transform.position;
        glowLight.transform.position = new Vector3(pp.x, pp.y + 0.4f, 0.0f);
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
                if(so is MoodPreset || so is LightPreset || so is WeatherPreset || so is FlashbackPreset || so is RipplePreset || so is GesturePreset)
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
