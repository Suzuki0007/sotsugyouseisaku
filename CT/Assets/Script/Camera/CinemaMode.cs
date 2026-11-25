/*********************************************************************/
// * \file   CinemaMode.cs
// * \brief  イベントの映画風の演出（上下の黒い帯・カメラが寄る・下の帯の字幕）
// *
// * \author
/*********************************************************************/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 映画風の演出（シネマティック・レターボックス）
/// </summary>
///
/// ・イベントの部品「シネマ演出」「字幕」から使う（シーンに置かなくてよい。マップのカメラに自動で付く）
/// ・帯はマップの映る範囲（画面の中央の 4:3）の上下に出す。UI の並び順は 97＝暗転の幕（90）・手紙やスチルの絵（95）より手前、会話の立ち絵（100）より奥
///   （OP の真っ暗な場面でも字幕が見えるように、暗転の幕より手前にしている）
/// ・カメラが寄るのは、カメラの大きさ（orthographicSize）を小さくするだけ。カメラの止まる範囲は FocusCamera がそのまま決める
/// ・会話（DialogueManager の「シネマ演出で出す」）からも使う：会話の間は自動で帯を出し、イベントが終わったら引っ込める
/// ・シーンを移ると、カメラと一緒に消える（帯とズームは元に戻る）
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CinemaMode : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    // 暗転の幕（90）・手紙やスチルの絵（95）より手前、会話の立ち絵（100）より奥
    private const int SortingOrder = 97;

    // 会話が終わってから帯を引っ込めるまでの間（続けて次の会話が始まるときに、帯がちらつかないように）
    private const float DialogueReleaseDelay = 0.25f;

    // 字幕の文字の大きさ（どの文も同じ大きさ。1920×1080 の基準で。帯 0.12 なら2行まで入る）
    private const float SubtitleFontSize = 30.0f;

    // 名前の列の幅・名前と本文の間・帯の中の余白
    private const float NameWidth = 200.0f;
    private const float NameGap = 24.0f;
    private const float SideMargin = 80.0f;
    private const float TopMargin = 14.0f;
    private const float BottomMargin = 28.0f;

    // キーの案内の大きさと、帯の下の端からの高さ（画面の外に出ないように少しだけ上）
    private const float KeyHintSize = 18.0f;
    private const float KeyHintBottom = 3.0f;

    // ▼ の高さ（キーの案内と重ならない所）と、呼吸のような明滅（1回の秒数・いちばん暗い／明るい濃さ）
    private const float NextMarkBottom = 44.0f;
    private const float NextMarkBreath = 2.2f;
    private const float NextMarkAlphaMin = 0.15f;
    private const float NextMarkAlphaMax = 0.8f;

    // 文字の色（真っ白より少し温かい、生成りの白）
    public static readonly Color TextColor = new Color(0.95f, 0.92f, 0.86f, 1.0f);

    // 帯の内側の縁のぼかしの幅（1920×1080 の基準で）
    private const float EdgeFeather = 36.0f;

    //=====================================================================
    // 変数
    //=====================================================================

    private Camera cam;
    private FocusCamera focus;

    private GameObject root;
    private RectTransform viewport;
    private RectTransform topBar;
    private RectTransform bottomBar;

    // 字幕・キーの案内・▼ を置く場所（下の帯が出きったときの大きさで固定。帯と一緒には動かない）
    private RectTransform textArea;
    private TextMeshProUGUI subtitle;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI keyHint;
    private CanvasGroup subtitleGroup;
    private GameObject nextMark;
    private RectTransform nextMarkRect;
    private TextMeshProUGUI nextMarkText;
    private Texture2D featherTexture;
    private RawImage topFeather;
    private RawImage bottomFeather;
    private float nextMarkShownAt;

    // 会話が自動で出した帯か（イベントの部品「シネマ演出」で始めた帯は、会話が終わっても閉じない）
    private bool autoByDialogue;
    private Coroutine autoRelease;

    // 帯を最初から出しておいてほしい一枚絵の数（CinemaStill）
    private int stillHolds;

    // 今の帯の太さ（マップの映る範囲の高さに対する割合）
    private float barRatio;

    // シネマ演出を始める前のカメラの大きさ
    private float baseSize;

    // 寄っている間、追いかける物（null なら決めた場所か、ふだんどおり操作中のキャラ）
    private Transform follow;

    // 自分でカメラの向きを変えたか（終えるときに戻す）
    private bool looking;

    private Coroutine transition;
    private float transitionEnd;
    private Coroutine subtitleFade;
    private Coroutine nameFade;
    private Coroutine bodyFade;

    // 字幕のキー待ちの間と、閉じた直後のフレーム（同じキーで調べる・会話が始まらないように）
    private static bool waitingKey;
    private static int closedFrame = -1;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> マップの映る範囲（画面の中の割合。中央の 4:3） </summary>
    public Rect ViewportRect => cam != null ? cam.rect : new Rect(0.0f, 0.0f, 1.0f, 1.0f);

    /// <summary> シネマ演出の最中か（帯が出ている・出かけている） </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 字幕のキー送りを待っている、または送った直後のフレームか（InputBlock が見る）
    /// </summary>
    /// ・帯が出ている間と、帯が引っ込みきるまでも true（プレイヤーも敵も、帯が消えきるまで動かない。EnemyBlock も InputBlock を見る）
    public static bool IsBusy => waitingKey || Time.frameCount <= closedFrame + 1 || (current != null && current.BlocksPlay);

    /// <summary> 帯が出ている、または出る・引っ込む途中か（この間はプレイヤーと敵を止める） </summary>
    public bool BlocksPlay => IsActive || transition != null;

    // 今のシーンのマップのカメラに付いている CinemaMode（IsBusy から見る）
    private static CinemaMode current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        waitingKey = false;
        closedFrame = -1;
        current = null;
    }

    //=====================================================================
    // 取得
    //=====================================================================

    /// <summary>
    /// マップのカメラ（FocusCamera の付いたカメラ）の CinemaMode。なければ付ける。カメラがなければ null
    /// </summary>
    public static CinemaMode Get()
    {
        FocusCamera fc = FindAnyObjectByType<FocusCamera>();
        if(fc == null || fc.GetComponent<Camera>() == null)
        {
            Debug.LogWarning("[シネマ演出] マップのカメラ（FocusCamera）が見つかりません");
            return null;
        }

        CinemaMode mode = fc.GetComponent<CinemaMode>();
        if(mode == null) { mode = fc.gameObject.AddComponent<CinemaMode>(); }
        return mode;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        cam = GetComponent<Camera>();
        focus = GetComponent<FocusCamera>();
        baseSize = cam.orthographicSize;
        current = this;
        CreateUI();
    }

    private void OnDestroy()
    {
        if(current == this) { current = null; }
        if(root != null) { Destroy(root); }
        if(featherTexture != null) { Destroy(featherTexture); }
        if(waitingKey) { waitingKey = false; closedFrame = Time.frameCount; }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        // 動く物に寄っているときは、毎フレームその位置へ向ける（FocusCamera の LateUpdate より前）
        if(follow != null && focus != null) { focus.LookAt(follow.position, 0.0f); }

        // ▼ を呼吸のように、ゆっくり明るく・暗くする（動かさない。テーマ「儚い」に合わせて、はねる動きはやめた）
        if(nextMark != null && nextMark.activeSelf)
        {
            float t = (Time.unscaledTime - nextMarkShownAt) / NextMarkBreath;
            // 最初は透明から。0 → 明るい → 暗い → 明るい … をくり返す
            float wave = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2.0f);
            Color c = nextMarkText.color;
            c.a = Mathf.Lerp(NextMarkAlphaMin, NextMarkAlphaMax, wave) * Mathf.Clamp01(t * 2.0f);
            nextMarkText.color = c;
        }
    }

    private void LateUpdate()
    {
        // マップの映る範囲（MapCameraAspect が決める cam.rect）に合わせる
        // 変わったときだけ入れ直す（毎フレーム入れると字幕の文字が作り直され、1文字ずつのフェードが崩れることがある）
        Rect r = cam.rect;
        if(viewport.anchorMin != r.min || viewport.anchorMax != r.max)
        {
            viewport.anchorMin = r.min;
            viewport.anchorMax = r.max;
            SetBars(barRatio);
        }
    }

    //=====================================================================
    // 帯とカメラ
    //=====================================================================

    /// <summary>
    /// シネマ演出を始める（帯を出し、カメラを寄せる）
    /// </summary>
    /// <param name="ratio"> 帯の太さ（マップの映る範囲の高さに対する割合。上下それぞれ） </param>
    /// <param name="zoom"> 寄る強さ（1 で寄らない。1.5 で 1.5 倍に大きく映る） </param>
    /// <param name="seconds"> 動く時間 </param>
    /// <param name="followTarget"> 寄る先（動いても追いかける）。null なら point か、ふだんどおり </param>
    /// <param name="point"> 寄る場所（followTarget が null のとき） </param>
    public void Begin(float ratio, float zoom, float seconds, Transform followTarget, Vector2? point)
    {
        // 部品「シネマ演出」で始めたら、会話が終わっても自動では閉じない
        StopAutoRelease();
        autoByDialogue = false;
        BeginCore(ratio, zoom, seconds, followTarget, point);
    }

    private void BeginCore(float ratio, float zoom, float seconds, Transform followTarget, Vector2? point)
    {
        // 元の大きさは、演出していない（戻している途中でもない）ときだけ覚え直す
        if(!IsActive && transition == null) { baseSize = cam.orthographicSize; }
        IsActive = true;

        follow = followTarget;
        if(focus != null)
        {
            if(followTarget != null)  { focus.LookAt(followTarget.position, seconds); looking = true; }
            else if(point.HasValue)   { focus.LookAt(point.Value, seconds); looking = true; }
            else if(looking)          { focus.LookAt(null, seconds); looking = false; }
        }

        float size = baseSize / Mathf.Max(0.1f, zoom);
        SetTextArea(Mathf.Clamp01(ratio));
        StartTransition(Mathf.Clamp01(ratio), size, seconds);
    }

    /// <summary>
    /// シネマ演出を終える（帯を引っ込め、カメラを元の大きさ・操作中のキャラへ戻す。字幕も消す）
    /// </summary>
    public void End(float seconds)
    {
        StopAutoRelease();
        autoByDialogue = false;
        if(!IsActive) { return; }
        IsActive = false;
        SetNextMark(false);
        SetKeyHint(null);

        follow = null;
        if(looking && focus != null) { focus.LookAt(null, seconds); }
        looking = false;

        // 字幕は帯が縮み始める前にすぐ消す（字幕は帯と一緒に動かないので、残すとマップの上に浮いて見える）
        HideSubtitle(Mathf.Min(0.15f, seconds));
        StartTransition(0.0f, baseSize, seconds);
    }

    private void StartTransition(float toRatio, float toSize, float seconds)
    {
        if(transition != null) { StopCoroutine(transition); }
        transitionEnd = Time.unscaledTime + seconds;
        transition = StartCoroutine(Transition(toRatio, toSize, seconds));
    }

    private IEnumerator Transition(float toRatio, float toSize, float seconds)
    {
        float fromRatio = barRatio;
        float fromSize = cam.orthographicSize;

        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(t / seconds));
            SetBars(Mathf.Lerp(fromRatio, toRatio, k));
            cam.orthographicSize = Mathf.Lerp(fromSize, toSize, k);
            yield return null;
        }

        SetBars(toRatio);
        cam.orthographicSize = toSize;
        transition = null;
    }

    // 字幕を置く場所を、下の帯が出きったときの大きさにする（帯の動きの間も動かない）
    private void SetTextArea(float ratio)
    {
        if(textArea == null) { return; }
        Vector2 max = new Vector2(1.0f, ratio);
        if(textArea.anchorMax == max) { return; }

        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = max;
        textArea.offsetMin = Vector2.zero;
        textArea.offsetMax = Vector2.zero;
    }

    private void SetBars(float ratio)
    {
        barRatio = ratio;

        // 横は画面いっぱい、縦はマップの映る範囲の上下の端から（帯の太さはその範囲の高さに対する割合）
        Rect vp = cam != null ? cam.rect : new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        topBar.anchorMin = new Vector2(0.0f, vp.yMax - ratio * vp.height);
        topBar.anchorMax = new Vector2(1.0f, vp.yMax);
        bottomBar.anchorMin = new Vector2(0.0f, vp.yMin);
        bottomBar.anchorMax = new Vector2(1.0f, vp.yMin + ratio * vp.height);

        // 帯が閉じているときは、縁のぼかしも見えないように（帯が細いうちは薄く）
        float featherAlpha = Mathf.Clamp01(ratio / 0.04f);
        if(topFeather != null)    { topFeather.color = new Color(1.0f, 1.0f, 1.0f, featherAlpha); }
        if(bottomFeather != null) { bottomFeather.color = new Color(1.0f, 1.0f, 1.0f, featherAlpha); }

        root.SetActive(ratio > 0.0001f || subtitleGroup.alpha > 0.0f);
    }

    //=====================================================================
    // 会話から使う（DialogueManager の「シネマ演出で出す」）
    //=====================================================================

    /// <summary>
    /// 会話が始まったとき：帯が出ていなければ出す（部品「シネマ演出」で出している帯はそのまま使う）
    /// </summary>
    /// <returns> 帯が出きるまでの残りの秒数（もう出ていれば 0） </returns>
    public float BeginForDialogue(float ratio, float zoom, float seconds)
    {
        StopAutoRelease();
        if(!IsActive)
        {
            autoByDialogue = true;
            BeginCore(ratio, zoom, seconds, null, null);
        }
        return RemainingTransition;
    }

    /// <summary>
    /// 一枚絵（塔のスチルなど）が出ている間：帯を動かさずに、最初から出しておく（CinemaStill から）
    /// 絵が消えたら、会話・イベントが終わるのを待ってから帯を引っ込める
    /// </summary>
    public void HoldForStill(float ratio)
    {
        stillHolds++;
        StopAutoRelease();
        if(IsActive && transition == null) { return; }

        // 出ていない・動いている途中なら、その場で出しきる（寄りはしない）
        if(!IsActive) { autoByDialogue = true; baseSize = cam.orthographicSize; }
        IsActive = true;
        if(transition != null) { StopCoroutine(transition); transition = null; }
        SetTextArea(Mathf.Clamp01(ratio));
        SetBars(Mathf.Clamp01(ratio));
    }

    /// <summary> 一枚絵が消えたとき </summary>
    public void ReleaseStill(float seconds)
    {
        stillHolds = Mathf.Max(0, stillHolds - 1);
        if(stillHolds > 0 || !autoByDialogue || !IsActive) { return; }

        StopAutoRelease();
        autoRelease = StartCoroutine(AutoRelease(seconds));
    }

    /// <summary> 帯が出る・引っ込む動きの残りの秒数（動いていなければ 0） </summary>
    public float RemainingTransition => transition != null ? Mathf.Max(0.0f, transitionEnd - Time.unscaledTime) : 0.0f;

    /// <summary>
    /// 会話が終わったとき：会話が出した帯なら、イベントが終わるのを待ってから引っ込める
    /// （イベントの中で会話が続くときに、帯が出たり引っ込んだりしないように）
    /// </summary>
    /// <param name="textFade"> 字幕が溶けるように消える時間 </param>
    public void ReleaseForDialogue(float seconds, float textFade = 0.5f)
    {
        SetNextMark(false);
        SetKeyHint(null);
        HideSubtitle(textFade);
        if(!autoByDialogue || !IsActive) { return; }

        StopAutoRelease();
        autoRelease = StartCoroutine(AutoRelease(seconds));
    }

    private IEnumerator AutoRelease(float seconds)
    {
        // プレイヤーを止めるイベントが続いている間・次の会話が出ている間は待つ。終わってから少し待って、まだ何もなければ閉じる
        float quiet = 0.0f;
        while(quiet < DialogueReleaseDelay)
        {
            // 字幕が消えきるまでも待つ（文字が溶けて消えてから、帯を引っ込める）
            bool busy = EventSequence.IsBusy || DialogueManager.IsBusy || subtitleGroup.alpha > 0.0f || stillHolds > 0;
            quiet = busy ? 0.0f : quiet + Time.unscaledDeltaTime;
            yield return null;
        }

        autoRelease = null;
        End(seconds);
    }

    private void StopAutoRelease()
    {
        if(autoRelease != null) { StopCoroutine(autoRelease); autoRelease = null; }
    }

    /// <summary>
    /// 会話の文を書く字幕の文字（DialogueManager が1文字ずつ出す）。出したまま見えるようにしてから返す
    /// </summary>
    public TMP_Text DialogueText()
    {
        root.SetActive(true);
        if(subtitleFade != null) { StopCoroutine(subtitleFade); subtitleFade = null; }
        if(bodyFade != null) { StopCoroutine(bodyFade); bodyFade = null; }
        subtitleGroup.alpha = 1.0f;
        if(subtitle.alpha < 1.0f) { subtitle.alpha = 1.0f; }
        return subtitle;
    }

    /// <summary>
    /// 名前の列に話し手の名前を出す（空なら名前なし＝ナレーション。本文の位置は変わらない）
    /// </summary>
    public void SetSpeaker(string speaker, Color color)
    {
        if(nameFade != null) { StopCoroutine(nameFade); nameFade = null; }
        nameText.text = speaker ?? "";
        nameText.color = color;
    }

    /// <summary>
    /// 文を出しきったときの ▼（帯の右下）
    /// </summary>
    public void SetNextMark(bool visible)
    {
        if(nextMark == null) { return; }
        if(visible && !nextMark.activeSelf) { nextMarkShownAt = Time.unscaledTime; }
        nextMark.SetActive(visible);
    }

    /// <summary>
    /// 帯の右下のキーの案内（例：E / Space / Enter　次へ）。空なら消す
    /// </summary>
    public void SetKeyHint(string text)
    {
        if(keyHint == null) { return; }
        bool show = !string.IsNullOrEmpty(text);
        keyHint.text = show ? text : "";
        keyHint.gameObject.SetActive(show);
    }

    //=====================================================================
    // 字幕
    //=====================================================================

    /// <summary>
    /// 下の帯に字幕を出す（前の字幕と入れ替える）
    /// </summary>
    /// <param name="speaker"> 話し手の名前（空なら名前なし） </param>
    /// <param name="nameColor"> 名前の色 </param>
    public void ShowSubtitle(string speaker, Color nameColor, string text, float fadeSeconds)
    {
        root.SetActive(true);

        SetSpeaker(speaker, nameColor);
        if(bodyFade != null) { StopCoroutine(bodyFade); bodyFade = null; }
        subtitle.alpha = 1.0f;
        subtitle.text = text ?? "";
        subtitle.maxVisibleCharacters = int.MaxValue;

        StartFade(1.0f, fadeSeconds, true);
    }

    /// <summary>
    /// 字幕を消す
    /// </summary>
    public void HideSubtitle(float fadeSeconds)
    {
        if(subtitleGroup.alpha <= 0.0f) { return; }
        StartFade(0.0f, fadeSeconds, false);
    }

    private void StartFade(float to, float seconds, bool fromZero)
    {
        if(subtitleFade != null) { StopCoroutine(subtitleFade); }
        if(fromZero) { subtitleGroup.alpha = 0.0f; }
        subtitleFade = StartCoroutine(Fade(to, seconds));
    }

    /// <summary>
    /// 今の字幕（名前と本文）を溶けるように消す（次の文に切り替える前）
    /// </summary>
    /// <param name="keepName"> 名前は消さずに残す（同じ人が続けて話すとき） </param>
    public void FadeOutText(float seconds, bool keepName = false)
    {
        SetNextMark(false);
        if(!keepName) { StartFade(0.0f, seconds, false); return; }

        // 本文だけを消す
        if(bodyFade != null) { StopCoroutine(bodyFade); }
        bodyFade = StartCoroutine(BodyFade(seconds));
    }

    private IEnumerator BodyFade(float seconds)
    {
        float from = subtitle.alpha;
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            subtitle.alpha = Mathf.Lerp(from, 0.0f, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        subtitle.alpha = 0.0f;
        bodyFade = null;
    }

    /// <summary>
    /// 名前をふわっと出す（本文の1文字ずつのフェードに合わせる）
    /// </summary>
    public void FadeInName(float seconds)
    {
        if(nameFade != null) { StopCoroutine(nameFade); }
        nameText.alpha = 0.0f;
        nameFade = StartCoroutine(NameFade(seconds));
    }

    private IEnumerator NameFade(float seconds)
    {
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            nameText.alpha = Mathf.Clamp01(t / seconds);
            yield return null;
        }
        nameText.alpha = 1.0f;
        nameFade = null;
    }

    private IEnumerator Fade(float to, float seconds)
    {
        float from = subtitleGroup.alpha;
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            subtitleGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        subtitleGroup.alpha = to;
        subtitleFade = null;
        if(to <= 0.0f && barRatio <= 0.0001f) { root.SetActive(false); }
    }

    /// <summary>
    /// 送りのキー（E・Space・Enter）が押されるまで待つ（出した瞬間のキーは数えない）
    /// </summary>
    public static IEnumerator WaitForAdvanceKey()
    {
        waitingKey = true;

        // 前の字幕を送ったキーを、すぐ次の字幕で拾わないように1フレーム待つ
        yield return null;

        while(true)
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
    }

    /// <summary>
    /// キー待ちを途中でやめたとき（イベントが止められたときなど）
    /// </summary>
    public static void CancelWait()
    {
        if(!waitingKey) { return; }
        waitingKey = false;
        closedFrame = Time.frameCount;
    }

    //=====================================================================
    // UI を作る
    //=====================================================================

    private void CreateUI()
    {
        root = new GameObject("CinemaBars");
        root.transform.SetParent(transform, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        // ほかの Canvas とそろえる（1920×1080・Match 0.5）
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        // 帯は画面の横いっぱい（一枚絵が画面全体に出ているときも、左右の端まで隠れるように）。高さはマップの映る範囲に合わせる
        topBar = NewBar("Top", new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f));
        bottomBar = NewBar("Bottom", new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f));

        // マップの映る範囲（字幕はこの中。帯より手前に描く）
        viewport = NewRect("Viewport", root.transform);
        Stretch(viewport, Vector2.zero, Vector2.one);

        // 字幕を置く場所：下の帯が「出きったときの大きさ」に固定する（帯の動きにつられて文字が下から上がってこないように）
        textArea = NewRect("TextArea", viewport);
        SetTextArea(0.12f);

        // 字幕（下の帯の中）：左に名前の列、その右に本文の列。本文が折り返しても、2行目は本文の頭にそろう
        //   名前　ああああああああああああああああああ
        //   　　　ああああ
        RectTransform group = NewRect("Subtitle", textArea);
        Stretch(group, Vector2.zero, Vector2.one);
        group.offsetMin = new Vector2(SideMargin, BottomMargin);
        group.offsetMax = new Vector2(-SideMargin, -TopMargin);

        subtitleGroup = group.gameObject.AddComponent<CanvasGroup>();
        subtitleGroup.alpha = 0.0f;
        subtitleGroup.blocksRaycasts = false;
        subtitleGroup.interactable = false;

        RectTransform nameRect = NewRect("Name", group);
        nameRect.anchorMin = new Vector2(0.0f, 0.0f);
        nameRect.anchorMax = new Vector2(0.0f, 1.0f);
        nameRect.pivot = new Vector2(0.0f, 1.0f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(NameWidth, 0.0f);
        nameText = NewText(nameRect, SubtitleFontSize, TextAlignmentOptions.TopLeft);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;

        RectTransform bodyRect = NewRect("Body", group);
        Stretch(bodyRect, Vector2.zero, Vector2.one);
        bodyRect.offsetMin = new Vector2(NameWidth + NameGap, 0.0f);
        subtitle = NewText(bodyRect, SubtitleFontSize, TextAlignmentOptions.TopLeft);
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        subtitle.overflowMode = TextOverflowModes.Overflow;

        // キーの案内（下の帯の右下。会話の枠のときと同じ文）
        // 帯の一番下（画面の下の端から少しだけ上。画面の外には出ない）
        RectTransform hintRect = NewRect("KeyHint", textArea);
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(1.0f, 0.0f);
        hintRect.pivot = new Vector2(1.0f, 0.0f);
        hintRect.anchoredPosition = new Vector2(-SideMargin * 0.5f, KeyHintBottom);
        hintRect.sizeDelta = new Vector2(600.0f, KeyHintSize + 4.0f);
        keyHint = NewText(hintRect, KeyHintSize, TextAlignmentOptions.BottomRight);
        keyHint.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.55f);
        keyHint.textWrappingMode = TextWrappingModes.NoWrap;
        keyHint.gameObject.SetActive(false);

        // 文を出しきったときの ▼（本文の右、キーの案内よりはっきり上。出ている間は上下にはねる）
        RectTransform markRect = NewRect("NextMark", textArea);
        markRect.anchorMin = markRect.anchorMax = new Vector2(1.0f, 0.0f);
        markRect.pivot = new Vector2(1.0f, 0.0f);
        markRect.anchoredPosition = new Vector2(-SideMargin * 0.5f, NextMarkBottom);
        markRect.sizeDelta = new Vector2(40.0f, 28.0f);
        nextMarkRect = markRect;
        TextMeshProUGUI mark = NewText(markRect, 20.0f, TextAlignmentOptions.Center);
        mark.text = "▼";
        mark.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.0f);
        nextMarkText = mark;
        nextMark = markRect.gameObject;
        nextMark.SetActive(false);

        SetBars(0.0f);
    }

    private static TextMeshProUGUI NewText(RectTransform rect, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if(TMP_Settings.defaultFontAsset != null) { text.font = TMP_Settings.defaultFontAsset; }
        text.color = TextColor;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    private RectTransform NewBar(string barName, Vector2 min, Vector2 max)
    {
        RectTransform rt = NewRect(barName, root.transform);
        Stretch(rt, min, max);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        // 帯の内側（マップ側）の縁をぼかす：黒から透明へのグラデーションを、帯の外側（マップの上）に少しはみ出して置く
        bool isTop = min.y >= 1.0f;
        RectTransform edge = NewRect("Feather", rt);
        edge.anchorMin = new Vector2(0.0f, isTop ? 0.0f : 1.0f);
        edge.anchorMax = new Vector2(1.0f, isTop ? 0.0f : 1.0f);
        edge.pivot = new Vector2(0.5f, isTop ? 1.0f : 0.0f);
        edge.anchoredPosition = Vector2.zero;
        edge.sizeDelta = new Vector2(0.0f, EdgeFeather);
        RawImage feather = edge.gameObject.AddComponent<RawImage>();
        feather.texture = FeatherTexture();
        feather.raycastTarget = false;
        // テクスチャは「下が透明・上が黒」。下の帯の上の縁は上下を逆にして「下が黒・上が透明」に
        feather.uvRect = isTop ? new Rect(0.0f, 0.0f, 1.0f, 1.0f) : new Rect(0.0f, 1.0f, 1.0f, -1.0f);
        if(isTop) { topFeather = feather; } else { bottomFeather = feather; }
        return rt;
    }

    // 下が透明・上が黒のなめらかなグラデーション（1×64）
    private Texture2D FeatherTexture()
    {
        if(featherTexture != null) { return featherTexture; }

        const int h = 64;
        featherTexture = new Texture2D(1, h, TextureFormat.RGBA32, false);
        featherTexture.wrapMode = TextureWrapMode.Clamp;
        for(int y = 0; y < h; y++)
        {
            float a = Mathf.SmoothStep(0.0f, 1.0f, y / (float)(h - 1));
            featherTexture.SetPixel(0, y, new Color(0.0f, 0.0f, 0.0f, a));
        }
        featherTexture.Apply();
        return featherTexture;
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
