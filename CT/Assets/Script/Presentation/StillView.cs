/*********************************************************************/
// * \file   StillView.cs
// * \brief  1枚絵の画面：イラストを1枚、フェードで出し、ゆっくり寄る・引く・流す（部品「1枚絵を出す」が使う）
// *
// * \author
/*********************************************************************/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 1枚絵の画面（シーンごと。初めて使ったときに作る。シーンに置かなくてよい）
/// </summary>
///
/// ・並び順 95：画面の暗転の幕（90）より手前、シネマ演出の帯（97）・会話（100）より奥。出したまま会話を出せる
/// ・色調・光・天候の影響を受けない（UI の Canvas に出すため）
public class StillView : MonoBehaviour
{
    public enum Motion
    {
        [InspectorName("なし（止まったまま）")]
        None = 0,

        [InspectorName("ゆっくり寄る")]
        ZoomIn = 1,

        [InspectorName("ゆっくり引く")]
        ZoomOut = 2,

        [InspectorName("横に流す")]
        PanHorizontal = 3,

        [InspectorName("縦に流す")]
        PanVertical = 4,

        [InspectorName("見上げる（寄って、下から上へ）")]
        LookUp = 5,
    }

    public enum Area
    {
        [InspectorName("マップの範囲（中央 4:3）")]
        Map = 0,

        [InspectorName("画面全体（16:9）")]
        Full = 1,
    }

    public enum Background
    {
        [InspectorName("黒")]
        Black = 0,

        [InspectorName("白")]
        White = 1,

        [InspectorName("透明（後ろのマップが見える）")]
        Clear = 2,
    }

    public enum Fit
    {
        [InspectorName("範囲を埋める（はみ出た所は切る）")]
        Cover = 0,

        [InspectorName("全部見せる（余りは背景の色）")]
        Contain = 1,
    }

    /// <summary> 出し方の一式 </summary>
    public struct Request
    {
        public Sprite sprite;
        public float fadeSeconds;
        public Motion motion;
        public float motionSeconds;
        public float scaleFrom, scaleTo;
        public float panFrom, panTo;
        public Area area;
        public Background background;
        public Fit fit;
        public bool holdCinemaBars;
    }

    private const int SortingOrder = 95;
    private const float BarSize = 0.12f;

    private static StillView instance;
    private static bool waitingKey;
    private static int closedFrame = -10;

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform frame;
    private Image backgroundImage;
    private Image picture;
    private AspectRatioFitter fitter;
    private TextMeshProUGUI keyHint;

    private Request current;
    private bool showing;
    private float motionTime;
    private Coroutine fading;
    private CinemaMode heldBars;
    private StillRunner runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        waitingKey = false;
        closedFrame = -10;
    }

    //=====================================================================
    // 公開
    //=====================================================================

    /// <summary> 1枚絵が出ているか </summary>
    public static bool IsShowing => instance != null && instance.showing;

    /// <summary> キーを待っている（プレイヤーの操作を止める） </summary>
    public static bool IsBusy => waitingKey || Time.frameCount <= closedFrame + 1;

    /// <summary> フェードの途中か </summary>
    public static bool IsFading => instance != null && instance.fading != null;

    /// <summary> 1枚絵を出す </summary>
    public static void Show(Request request)
    {
        StillView view = GetOrCreate();
        view.Begin(request);
    }

    /// <summary> 1枚絵の画面の上で処理を動かす（待たない部品の閉じる処理） </summary>
    public static void RunDetached(IEnumerator routine)
    {
        StillView view = GetOrCreate();
        view.runner.StartCoroutine(routine);
    }

    /// <summary> 1枚絵を閉じる </summary>
    public static void Close(float seconds)
    {
        if(instance == null || !instance.showing) { return; }
        instance.End(seconds);
    }

    /// <summary>
    /// 会話の送りと同じキー（E・Space・Enter）が押されるまで待つ（キーの案内を出す）
    /// </summary>
    public static IEnumerator WaitForKey()
    {
        if(instance == null) { yield break; }
        waitingKey = true;
        instance.keyHint.gameObject.SetActive(true);
        // 出した瞬間のキー（前の会話を送ったキー）は数えない
        yield return null;
        while(instance != null && instance.showing)
        {
            Keyboard kb = Keyboard.current;
            if(kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                              kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                break;
            }
            yield return null;
        }
        waitingKey = false;
        closedFrame = Time.frameCount;
        if(instance != null) { instance.keyHint.gameObject.SetActive(false); }
    }

    //=====================================================================
    // 作る
    //=====================================================================

    private static StillView GetOrCreate()
    {
        if(instance != null) { return instance; }

        var go = new GameObject("StillView（1枚絵）");
        instance = go.AddComponent<StillView>();
        instance.Build();
        var runnerObject = new GameObject("StillView（動かす）");
        runnerObject.transform.SetParent(go.transform.parent, false);
        instance.runner = runnerObject.AddComponent<StillRunner>();
        return instance;
    }

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0.0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        frame = NewRect("範囲", transform);
        frame.gameObject.AddComponent<RectMask2D>();
        backgroundImage = frame.gameObject.AddComponent<Image>();
        backgroundImage.raycastTarget = false;

        RectTransform pic = NewRect("絵", frame);
        pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
        picture = pic.gameObject.AddComponent<Image>();
        picture.raycastTarget = false;
        fitter = pic.gameObject.AddComponent<AspectRatioFitter>();

        RectTransform hint = NewRect("キーの案内", frame);
        hint.anchorMin = hint.anchorMax = new Vector2(1.0f, 0.0f);
        hint.pivot = new Vector2(1.0f, 0.0f);
        hint.anchoredPosition = new Vector2(-24.0f, 16.0f);
        hint.sizeDelta = new Vector2(400.0f, 30.0f);
        keyHint = hint.gameObject.AddComponent<TextMeshProUGUI>();
        if(TMP_Settings.defaultFontAsset != null) { keyHint.font = TMP_Settings.defaultFontAsset; }
        keyHint.fontSize = 20.0f;
        keyHint.alignment = TextAlignmentOptions.BottomRight;
        keyHint.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        keyHint.text = "Space ▶";
        keyHint.raycastTarget = false;
        keyHint.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    //=====================================================================
    // 出す・閉じる
    //=====================================================================

    private void Begin(Request request)
    {
        current = request;
        showing = true;
        motionTime = 0.0f;
        gameObject.SetActive(true);

        // 範囲（マップの 4:3 は、マップのカメラの写す範囲に合わせる）
        Rect r = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        if(request.area == Area.Map)
        {
            Camera cam = FindMapCamera();
            if(cam != null) { r = cam.rect; }
        }
        frame.anchorMin = new Vector2(r.xMin, r.yMin);
        frame.anchorMax = new Vector2(r.xMax, r.yMax);
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;

        switch(request.background)
        {
            case Background.Black: backgroundImage.color = Color.black; break;
            case Background.White: backgroundImage.color = Color.white; break;
            default: backgroundImage.color = Color.clear; break;
        }

        picture.sprite = request.sprite;
        picture.enabled = request.sprite != null;
        if(request.sprite != null)
        {
            Rect sr = request.sprite.rect;
            fitter.aspectRatio = sr.width / Mathf.Max(1.0f, sr.height);
        }
        fitter.aspectMode = request.fit == Fit.Cover ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent;
        keyHint.gameObject.SetActive(false);
        ApplyMotion();

        if(request.holdCinemaBars && heldBars == null)
        {
            heldBars = CinemaMode.Get();
            // 帯の太さは会話と同じ（シネマ演出の見た目の設定があればそちら）
            if(heldBars != null) { heldBars.HoldForStill(CinemaLookSettings.Current != null ? CinemaLookSettings.Current.barHeight : BarSize); }
        }

        StartFade(1.0f, request.fadeSeconds, false);
    }

    private void End(float seconds)
    {
        showing = false;
        StartFade(0.0f, seconds, true);
        if(heldBars != null) { heldBars.ReleaseStill(1.2f); heldBars = null; }
    }

    private void StartFade(float to, float seconds, bool hideAtEnd)
    {
        if(fading != null) { StopCoroutine(fading); fading = null; }
        if(seconds <= 0.0f)
        {
            group.alpha = to;
            if(hideAtEnd) { gameObject.SetActive(false); }
            return;
        }
        fading = StartCoroutine(FadeRoutine(to, seconds, hideAtEnd));
    }

    private IEnumerator FadeRoutine(float to, float seconds, bool hideAtEnd)
    {
        float from = group.alpha;
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        group.alpha = to;
        fading = null;
        if(hideAtEnd) { gameObject.SetActive(false); }
    }

    //=====================================================================
    // 動き
    //=====================================================================

    private void Update()
    {
        if(!showing) { return; }
        motionTime += Time.unscaledDeltaTime;
        ApplyMotion();
    }

    private void ApplyMotion()
    {
        Request q = current;
        float t = q.motionSeconds > 0.0f ? Mathf.Clamp01(motionTime / q.motionSeconds) : 1.0f;
        t = t * t * (3.0f - 2.0f * t);
        RectTransform pic = picture.rectTransform;

        float scale = 1.0f;
        Vector2 offset = Vector2.zero;
        switch(q.motion)
        {
            case Motion.ZoomIn:
            case Motion.ZoomOut:
                scale = Mathf.Lerp(q.scaleFrom, q.scaleTo, t);
                break;
            case Motion.PanHorizontal:
            case Motion.PanVertical:
            {
                // 絵が範囲からはみ出ている分だけ動かす（-1 で左端・下端、1 で右端・上端を見せる）
                float p = Mathf.Lerp(q.panFrom, q.panTo, t);
                Vector2 over = (pic.rect.size - frame.rect.size) * 0.5f;
                offset = q.motion == Motion.PanHorizontal ? new Vector2(-p * Mathf.Max(0.0f, over.x), 0.0f) : new Vector2(0.0f, -p * Mathf.Max(0.0f, over.y));
                break;
            }
            case Motion.LookUp:
            {
                // 見上げる：大きくして（範囲より大きくなった分を縦に動かせる）、下の端から上の端へ。大きさも始め→終わりへ
                // （絵が範囲と同じ比率でも、寄った分だけ上下に動く）
                scale = Mathf.Lerp(q.scaleFrom, q.scaleTo, t);
                float p = Mathf.Lerp(q.panFrom, q.panTo, t);
                float overY = (pic.rect.height * scale - frame.rect.height) * 0.5f;
                offset = new Vector2(0.0f, -p * Mathf.Max(0.0f, overY));
                break;
            }
        }
        pic.localScale = new Vector3(scale, scale, 1.0f);
        pic.anchoredPosition = offset;
    }

    private static Camera FindMapCamera()
    {
        foreach(FocusCamera focus in FindObjectsByType<FocusCamera>())
        {
            if(focus.TryGetComponent(out Camera c)) { return c; }
        }
        return Camera.main;
    }

    private void OnDestroy()
    {
        if(heldBars != null) { heldBars.ReleaseStill(0.0f); heldBars = null; }
        if(instance == this) { instance = null; waitingKey = false; }
    }
}

/// <summary> 1枚絵の閉じる処理を動かす（1枚絵の画面は閉じると非表示になるので、別の物で動かす） </summary>
[AddComponentMenu("")]
public class StillRunner : MonoBehaviour { }
