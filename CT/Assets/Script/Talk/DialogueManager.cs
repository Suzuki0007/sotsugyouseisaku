/*********************************************************************/
// * \file   DialogueManager.cs
// * \brief  会話の表示・送りを管理するクラス
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 会話の表示・送りを管理するクラス
/// </summary>
///
/// ・会話パネルはシーンに1つだけ置き、このクラスが表示を切り替える
/// ・送りキーで「表示途中 → 全文表示」「全文表示済み → 次の文」「最後の文 → 閉じる」
/// ・話者（CharacterProfile）と表情に合わせて、左右の立ち絵を切り替える
/// ・会話中は IsBusy が true になるので、プレイヤーの操作側はこれを見て入力を止める
/// ・「シネマ演出で出す」がオンなら、会話の枠の代わりに上下の黒い帯を出し、文は下の帯の中に字幕で出す（CinemaMode）
///   立ち絵は「シネマ演出でも立ち絵を出す」がオンのときだけ出す（今立ち絵が出ている会話の一覧は Docs/シネマ演出で隠した立ち絵.md）
public class DialogueManager : Singleton<DialogueManager>
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("UI")]

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text speakerText;

    [SerializeField]
    private TMP_Text bodyText;

    [Tooltip("全文表示後に出す ▼ マーク（任意）")]
    [SerializeField]
    private GameObject nextIcon;

    [Header("立ち絵")]

    [Tooltip("左側の立ち絵（任意）")]
    [SerializeField]
    private Image portraitLeft;

    [Tooltip("右側の立ち絵（任意）")]
    [SerializeField]
    private Image portraitRight;

    [Tooltip("話していないキャラクターの立ち絵の色（暗くする）")]
    [SerializeField]
    private Color inactivePortraitColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("話者なしの文（看板・ナレーション）でも立ち絵を暗くして残すか。オフなら消す")]
    [SerializeField]
    private bool keepPortraitsOnNarration = false;

    [Header("設定")]

    [SerializeField]
    private Key advanceKey = Key.E;

    [Tooltip("送りに使えるほかのキー（初期値：Space・Enter・テンキーの Enter）")]
    [SerializeField]
    private Key[] extraAdvanceKeys = { Key.Space, Key.Enter, Key.NumpadEnter };

    [Header("キーの案内（テキストボックスの下・右下）")]

    [SerializeField, Label("キーの案内を出す")]
    private bool showKeyHint = true;

    [SerializeField, Label("キーの案内の文")]
    [Tooltip("テキストボックスの下の右側に出す。会話の画面が出ている間だけ")]
    private string keyHintText = "<b>E</b> / <b>Space</b> / <b>Enter</b>　次へ";

    [SerializeField, Label("キーの案内の大きさ", 8.0f)]
    private float keyHintSize = 24.0f;

    private TMP_Text keyHint;

    [Tooltip("1文字あたりの表示間隔（秒）。0以下なら一度に全文表示")]
    [SerializeField]
    private float charInterval = 0.04f;

    [Header("文字送りの音")]

    [SerializeField, Label("文字送りの音")]
    [Tooltip("文字が出るときに鳴らす小さな音（空なら鳴らさない）。キャラごとの音は、キャラの設定（CharacterProfile）の「文字送りの音」が先")]
    private SeData typeSe;

    [SerializeField, Label("何文字ごとに鳴らす", 1)]
    [Tooltip("1 なら毎文字。2〜3 にすると、うるさくならない。空白・記号では鳴らさない")]
    private int typeSeEvery = 2;

    [Header("シネマ演出（上下の帯・字幕）")]

    [SerializeField, Label("シネマ演出で出す")]
    [Tooltip("オン：会話の枠の代わりに、上下に黒い帯を出して文を下の帯に字幕で出す（カメラも少し寄る）／オフ：今までの会話の枠")]
    private bool useCinema = true;

    [SerializeField, Label("帯の太さ", 0.05f, 0.3f)]
    [Tooltip("上下それぞれの帯の太さ（マップの映る範囲の高さに対する割合）。字幕が入るので 0.1 以上")]
    private float cinemaBarSize = 0.12f;

    [SerializeField, Label("寄る強さ", 1.0f, 2.0f)]
    [Tooltip("会話の間、カメラが寄る強さ。1 で寄らない（操作中のキャラを中心に寄る）")]
    private float cinemaZoom = 1.1f;

    [SerializeField, Label("帯が出る時間（秒）", 0.0f)]
    [Tooltip("帯が出る・引っ込む、カメラが寄る・戻るまでの時間")]
    private float cinemaMoveTime = 1.2f;

    [SerializeField, Label("帯が出てから文字が出るまで（秒）", 0.0f)]
    [Tooltip("帯が出きってから、最初の文字が出始めるまでの間。帯がもう出ているとき（会話が続くとき）は待たない")]
    private float cinemaTextDelay = 0.5f;

    [SerializeField, Label("1文字が浮かび上がる時間（秒）", 0.0f)]
    [Tooltip("シネマ演出のとき、1文字ずつふわっと浮かび上がる時間。0 なら今までどおりパッと出る。出る間隔は「Char Interval」")]
    private float cinemaCharFade = 0.4f;

    [SerializeField, Label("文が消える時間（秒）", 0.0f)]
    [Tooltip("シネマ演出のとき、次の文へ送ったときに前の文が溶けるように消える時間。0 ならすぐ入れ替わる")]
    private float cinemaLineFade = 0.3f;

    [SerializeField, Label("会話の終わりに消える時間（秒）", 0.0f)]
    [Tooltip("シネマ演出のとき、会話が終わって字幕と立ち絵が溶けるように消える時間。0 ならすぐ消える")]
    private float cinemaEndFade = 0.5f;

    [SerializeField, Label("話していない立ち絵の色（シネマ）")]
    [Tooltip("シネマ演出のとき、話していない方の立ち絵の色。灰色にせず、少し透けさせる（右の濃さ＝A を小さくするほど薄い）")]
    private Color cinemaInactivePortraitColor = new Color(0.85f, 0.85f, 0.9f, 0.45f);

    // 話していない方の立ち絵の色（シネマ演出なら透ける色、そうでなければ今までの暗い色）
    private Color InactiveColor => cinema != null ? cinemaInactivePortraitColor : inactivePortraitColor;

    [SerializeField, Label("シネマ演出でも立ち絵を出す")]
    [Tooltip("オン：シネマ演出のときも立ち絵を出す（下の帯の上に立つように出る）／オフ：立ち絵を出さない。" +
             "立ち絵が出る会話の一覧は Docs/シネマ演出で隠した立ち絵.md")]
    private bool cinemaShowPortraits = true;

    [SerializeField, Label("立ち絵の大きさ（シネマ）", 0.3f, 1.5f)]
    [Tooltip("シネマ演出のときの立ち絵の大きさ（1 で会話の枠のときと同じ）。上下の帯の間に収まるように小さめにしている")]
    private float cinemaPortraitScale = 0.75f;

    [SerializeField, Label("立ち絵の横のずらし（シネマ）")]
    [Tooltip("シネマ演出のときに、立ち絵を画面の端から内側へずらす量（1920×1080 の基準で。マイナスで外側へ）")]
    private float cinemaPortraitInset = 0.0f;

    [SerializeField, Label("立ち絵のフェードイン（秒）", 0.0f)]
    [Tooltip("シネマ演出のときに、立ち絵が出てくるときのフェードの時間。0 ならパッと出る")]
    private float cinemaPortraitFade = 0.2f;

    // 立ち絵の元の位置と大きさ（シネマ演出の配置から、会話の枠の配置へ戻すため）
    private Vector2 portraitLeftPos, portraitRightPos;
    private Vector3 portraitLeftScale, portraitRightScale;
    private Coroutine portraitLeftFade, portraitRightFade;

    // 今の会話をシネマ演出で出しているときの CinemaMode（枠で出しているときは null）
    private CinemaMode cinema;

    // シネマ演出で、帯が出きるのを待っているか（最初の文を出す前）
    private bool waitingStart;

    // シネマ演出で、前の文が溶けて消えている途中か
    private bool switchingLine;

    // シネマ演出で、今名前の列に出している名前（同じ人が続けて話すときは出し直さない。null なら出していない）
    private string shownSpeaker;

    // 文を書く先（シネマ演出なら下の帯の字幕、そうでなければ会話の枠）
    private TMP_Text Body => cinema != null ? cinema.DialogueText() : bodyText;

    // 現在の会話
    private DialogueData.Line[] lines;

    private int index;

    private bool isTyping;

    // 会話を開いた／閉じたフレーム（同じフレームのキー入力を無視するため）
    private int lastToggleFrame = -1;

    private Coroutine typingCoroutine;

    private Action onFinished;

    // 名前欄の元の色（キャラクター未指定のときに使う）
    private Color defaultNameColor = Color.white;

    // 左右の立ち絵に表示中のキャラクター
    private CharacterProfile leftCharacter;
    private CharacterProfile rightCharacter;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// 会話中かどうか
    /// </summary>
    public bool IsTalking => lines != null;

    /// <summary>
    /// 会話を閉じた直後のフレームかどうか
    /// </summary>
    public bool JustClosed => !IsTalking && Time.frameCount == lastToggleFrame;

    /// <summary>
    /// 会話中、または閉じた直後か（プレイヤーの入力を止める判定に使う）
    /// </summary>
    public static bool IsBusy =>
        Instance != null && (Instance.IsTalking || Instance.JustClosed);

    /// <summary>
    /// 会話画面の色調（演出の DialogueMoodLayer）をかける立ち絵と文字を渡す（11-26）
    /// </summary>
    public void GetMoodTargets(System.Collections.Generic.List<Image> portraits, System.Collections.Generic.List<TMP_Text> texts)
    {
        if(portraitLeft != null)  { portraits.Add(portraitLeft); }
        if(portraitRight != null) { portraits.Add(portraitRight); }
        if(speakerText != null)   { texts.Add(speakerText); }
        if(bodyText != null)      { texts.Add(bodyText); }
        if(keyHint != null)       { texts.Add(keyHint); }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this) { return; }

        if(panel == null)       { Debug.LogError($"{name}: Panelが設定されていません", this); }
        if(bodyText == null)    { Debug.LogError($"{name}: Body Textが設定されていません", this); }
        if(speakerText == null) { Debug.LogWarning($"{name}: Speaker Textが設定されていません", this); }

        if(speakerText != null) { defaultNameColor = speakerText.color; }

        // 立ち絵の縦横比を保つ
        if(portraitLeft != null)  { portraitLeft.preserveAspect = true; }
        if(portraitRight != null) { portraitRight.preserveAspect = true; }

        // 立ち絵の元の位置と大きさを覚えておく（シネマ演出のときだけ動かす）
        if(portraitLeft != null)  { portraitLeftPos = portraitLeft.rectTransform.anchoredPosition;  portraitLeftScale = portraitLeft.rectTransform.localScale; }
        if(portraitRight != null) { portraitRightPos = portraitRight.rectTransform.anchoredPosition; portraitRightScale = portraitRight.rectTransform.localScale; }

        HideAllPortraits();
        BuildKeyHint();

        if(panel != null) { panel.SetActive(false); }
    }

    /// <summary>
    /// テキストボックスの下（右下）にキーの案内を作る（会話の枠の子なので、枠と一緒に出たり消えたりする）
    /// </summary>
    private void BuildKeyHint()
    {
        if(!showKeyHint || panel == null || keyHint != null) { return; }

        var go = new GameObject("キーの案内", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1.0f, 0.0f);
        rect.pivot = new Vector2(1.0f, 1.0f);
        rect.anchoredPosition = new Vector2(-8.0f, -4.0f);
        rect.sizeDelta = new Vector2(600.0f, keyHintSize + 8.0f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if(bodyText != null) { text.font = bodyText.font; }
        text.text = keyHintText;
        text.fontSize = keyHintSize;
        text.alignment = TextAlignmentOptions.Right;
        text.color = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        text.raycastTarget = false;
        keyHint = text;
    }

    private bool AdvancePressed()
    {
        Keyboard kb = Keyboard.current;
        if(kb[advanceKey].wasPressedThisFrame) { return true; }
        if(extraAdvanceKeys == null) { return false; }
        foreach(Key key in extraAdvanceKeys)
        {
            if(key != Key.None && kb[key].wasPressedThisFrame) { return true; }
        }
        return false;
    }

    //=====================================================================
    // 会話の開始・終了
    //=====================================================================

    /// <summary>
    /// 会話データから会話を開始する
    /// </summary>
    /// <param name="data"> 会話データ </param>
    /// <param name="finished"> 会話終了時に呼ばれる処理（任意） </param>
    public void StartDialogue(DialogueData data, Action finished = null)
    {
        if(data == null) { Debug.LogWarning("DialogueDataがnullです", this); return; }

        StartDialogue(data.Lines, finished);
    }

    /// <summary>
    /// 文の配列から会話を開始する
    /// </summary>
    public void StartDialogue(DialogueData.Line[] newLines, Action finished = null)
    {
        if(IsTalking) { return; }
        if(newLines == null || newLines.Length == 0) { return; }
        if(panel == null || bodyText == null) { return; }

        lines = newLines;
        index = 0;
        onFinished = finished;
        shownSpeaker = null;
        lastToggleFrame = Time.frameCount;

        HideAllPortraits();

        // シネマ演出：枠は出さずに、上下の帯と字幕で出す（マップのカメラがないシーンでは今までの枠）
        cinema = useCinema ? CinemaMode.Get() : null;
        LayoutPortraits(cinema != null);
        if(cinema != null)
        {
            float remaining = cinema.BeginForDialogue(cinemaBarSize, cinemaZoom, cinemaMoveTime);

            // 帯が出ている途中なら、出きってから少し待って最初の文を出す（もう出ていれば、すぐ出す）
            if(remaining > 0.0f)
            {
                cinema.SetSpeaker("", Color.white);
                cinema.DialogueText().text = "";
                cinema.SetKeyHint(null);
                typingCoroutine = StartCoroutine(ShowFirstLineAfter(remaining + cinemaTextDelay));
                return;
            }

            // キーの案内は、会話の枠のときと同じ文を帯の右下に
            cinema.SetKeyHint(showKeyHint ? keyHintText : null);
        }
        else { panel.SetActive(true); }

        ShowLine();
    }

    // 帯が出きるのを待ってから最初の文を出す（待っている間のキーは数えない）
    private IEnumerator ShowFirstLineAfter(float seconds)
    {
        waitingStart = true;
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        waitingStart = false;
        typingCoroutine = null;

        if(!IsTalking) { yield break; }
        if(cinema != null) { cinema.SetKeyHint(showKeyHint ? keyHintText : null); }
        lastToggleFrame = Time.frameCount;
        ShowLine();
    }

    /// <summary>
    /// 会話を途中で強制的に閉じる（エリアから出たときなど）
    /// </summary>
    public void ForceClose()
    {
        if(!IsTalking) { return; }

        EndDialogue();
    }

    private void EndDialogue()
    {
        if(typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        isTyping = false;
        waitingStart = false;
        switchingLine = false;

        // シネマ演出：立ち絵と字幕は溶けるように消す（会話の枠のときは今までどおりすぐ消す）
        if(cinema != null && cinemaEndFade > 0.0f) { FadeOutAllPortraits(cinemaEndFade); }
        else                                     { HideAllPortraits(); }
        if(panel != null) { panel.SetActive(false); }

        // シネマ演出：字幕を消し、イベントが終わったら帯を引っ込める（次の会話の前に閉じる）
        bool fading = cinema != null && cinemaEndFade > 0.0f;
        if(cinema != null) { cinema.ReleaseForDialogue(cinemaMoveTime, cinemaEndFade); }
        cinema = null;

        lines = null;
        lastToggleFrame = Time.frameCount;

        // コールバックの中で次の会話を始めても大丈夫なように、先に空にしてから呼ぶ
        Action callback = onFinished;
        onFinished = null;

        // シネマ演出：文字と立ち絵が消えきってから、イベントの次の部品へ進む
        // （会話のすぐあとに揺れ・音などを置いたとき、消えかけの文字の上で起きないように）
        if(fading && callback != null) { StartCoroutine(InvokeAfter(cinemaEndFade, callback)); return; }
        callback?.Invoke();
    }

    private static IEnumerator InvokeAfter(float seconds, Action callback)
    {
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        callback();
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(!IsTalking) { return; }
        if(Keyboard.current == null) { return; }

        // 開いたフレームのキー入力で、すぐに送られないようにする
        if(Time.frameCount == lastToggleFrame) { return; }

        // シネマ演出で、帯が出きるのを待っている間は送らない
        if(waitingStart) { return; }

        // 前の文が溶けて消えている間は送らない
        if(switchingLine) { return; }

        if(AdvancePressed())
        {
            if(isTyping)
            {
                // 表示途中なら全文を表示する
                CompleteLine();
            }
            else
            {
                // 全文表示済みなら次の文へ
                Next();
            }
        }
    }

    //=====================================================================
    // 文の表示
    //=====================================================================

    private void ShowLine()
    {
        DialogueData.Line line = lines[index];

        if(cinema != null)
        {
            // 字幕：名前は文の前にキャラの色で。立ち絵は「シネマ演出でも立ち絵を出す」のときだけ
            if(cinemaShowPortraits) { UpdatePortraits(line); }
            else                    { HideAllPortraits(); }

            // 名前は左の名前の列に（本文が折り返しても、2行目は本文の頭にそろう）
            // 同じ人が続けて話すときは、名前は出したまま（消したり出し直したりしない）
            string speaker = line.DisplayName ?? "";
            if(shownSpeaker != speaker)
            {
                cinema.SetSpeaker(speaker, line.character != null ? line.character.NameColor : defaultNameColor);
                if(cinemaCharFade > 0.0f) { cinema.FadeInName(cinemaCharFade); }
                shownSpeaker = speaker;
            }

            if(typingCoroutine != null) { StopCoroutine(typingCoroutine); }
            typingCoroutine = StartCoroutine(TypeLine(line.text ?? ""));
            return;
        }

        UpdateSpeakerName(line);
        UpdatePortraits(line);

        if(typingCoroutine != null) { StopCoroutine(typingCoroutine); }
        typingCoroutine = StartCoroutine(TypeLine(line.text ?? ""));
    }

    /// <summary>
    /// 名前欄を更新する
    /// </summary>
    private void UpdateSpeakerName(DialogueData.Line line)
    {
        if(speakerText == null) { return; }

        string displayName = line.DisplayName;
        bool hasName = !string.IsNullOrEmpty(displayName);

        speakerText.text = hasName ? displayName : "";
        speakerText.color = line.character != null ? line.character.NameColor : defaultNameColor;
        speakerText.gameObject.SetActive(hasName);
    }

    /// <summary>
    /// 1文字ずつ表示するコルーチン
    /// </summary>
    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
        if(nextIcon != null) { nextIcon.SetActive(false); }
        if(cinema != null) { cinema.SetNextMark(false); }

        // 書く先（シネマ演出なら字幕、そうでなければ会話の枠）
        TMP_Text body = Body;
        body.text = text;
        body.maxVisibleCharacters = 0;

        // リッチテキストのタグを除いた、実際に表示される文字数を取得する
        body.ForceMeshUpdate();
        int total = body.textInfo.characterCount;

        // シネマ演出：1文字ずつふわっと浮かび上がる
        if(cinema != null && cinemaCharFade > 0.0f && charInterval > 0.0f)
        {
            yield return FadeInChars(body, total);
            typingCoroutine = null;
            CompleteLine();
            yield break;
        }

        if(charInterval > 0f)
        {
            SeData se = TypeSeOf(lines != null && index < lines.Length ? lines[index] : null);
            int every = Mathf.Max(1, typeSeEvery);
            int sounding = 0;
            for(int i = 1; i <= total; i++)
            {
                body.maxVisibleCharacters = i;
                if(se != null && IsSoundingChar(body.textInfo.characterInfo[i - 1].character))
                {
                    if(sounding % every == 0) { SeManager.PlaySe(se); }
                    sounding++;
                }
                yield return new WaitForSeconds(charInterval);
            }
        }

        typingCoroutine = null;
        CompleteLine();
    }

    /// <summary>
    /// 1文字ずつ、透明から浮かび上がらせる（「Char Interval」おきに次の文字が始まり、「1文字が浮かび上がる時間」で出きる）
    /// 文字の頂点の色の透明度だけを変える（文字の作り直しはしない）
    /// </summary>
    private IEnumerator FadeInChars(TMP_Text body, int total)
    {
        body.maxVisibleCharacters = int.MaxValue;
        body.ForceMeshUpdate();
        TMP_TextInfo info = body.textInfo;

        SeData se = TypeSeOf(lines != null && index < lines.Length ? lines[index] : null);
        int every = Mathf.Max(1, typeSeEvery);
        int sounding = 0;
        int started = 0;

        float duration = (total - 1) * charInterval + cinemaCharFade;
        float elapsed = 0.0f;
        while(true)
        {
            // 始まった文字の音（今までの文字送りと同じ鳴らし方）
            while(started < total && elapsed >= started * charInterval)
            {
                if(se != null && IsSoundingChar(info.characterInfo[started].character))
                {
                    if(sounding % every == 0) { SeManager.PlaySe(se); }
                    sounding++;
                }
                started++;
            }

            SetCharAlphas(body, info, total, elapsed);
            if(elapsed >= duration) { break; }

            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // 文字ごとの透明度を、経過時間に合わせて頂点の色に入れる
    private void SetCharAlphas(TMP_Text body, TMP_TextInfo info, int total, float elapsed)
    {
        for(int i = 0; i < total && i < info.characterInfo.Length; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if(!c.isVisible) { continue; }

            float a = Mathf.Clamp01((elapsed - i * charInterval) / cinemaCharFade);
            Color32[] colors = info.meshInfo[c.materialReferenceIndex].colors32;
            if(colors == null || c.vertexIndex + 3 >= colors.Length) { continue; }

            byte alpha = (byte)(c.color.a * a);
            for(int v = 0; v < 4; v++) { colors[c.vertexIndex + v].a = alpha; }
        }
        body.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary> その文で鳴らす文字送りの音（キャラの設定が先、なければ全体の設定） </summary>
    private SeData TypeSeOf(DialogueData.Line line)
    {
        if(line != null && line.character != null && line.character.TypeSe != null) { return line.character.TypeSe; }
        return typeSe;
    }

    /// <summary> 音を鳴らす文字か（空白・句読点・記号・括弧では鳴らさない） </summary>
    private static bool IsSoundingChar(char c)
    {
        if(char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c)) { return false; }
        return "、。…ー～―・「」『』（）！？".IndexOf(c) < 0;
    }

    /// <summary>
    /// 今の文を全文表示する
    /// </summary>
    private void CompleteLine()
    {
        if(typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }

        TMP_Text body = Body;
        body.maxVisibleCharacters = int.MaxValue;
        // 1文字ずつのフェードの途中で送られたときも、全部の文字をくっきり出す（頂点の色を作り直す）
        if(cinema != null) { body.ForceMeshUpdate(); }
        isTyping = false;

        if(cinema != null) { cinema.SetNextMark(true); }
        else if(nextIcon != null) { nextIcon.SetActive(true); }
    }

    /// <summary>
    /// 次の文へ進む。最後の文なら会話を閉じる
    /// </summary>
    private void Next()
    {
        index++;

        if(index < lines.Length)
        {
            // シネマ演出：前の文を溶けるように消してから、次の文を浮かび上がらせる
            if(cinema != null && cinemaLineFade > 0.0f)
            {
                if(typingCoroutine != null) { StopCoroutine(typingCoroutine); }
                typingCoroutine = StartCoroutine(SwitchLine());
                return;
            }

            ShowLine();
            return;
        }

        EndDialogue();
    }

    private IEnumerator SwitchLine()
    {
        switchingLine = true;

        // 次も同じ人が話すなら、名前は残して本文だけ消す
        string nextSpeaker = lines[index].DisplayName ?? "";
        bool sameSpeaker = nextSpeaker == shownSpeaker;
        cinema.FadeOutText(cinemaLineFade, sameSpeaker);
        if(!sameSpeaker) { shownSpeaker = null; }

        float t = 0.0f;
        while(t < cinemaLineFade)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        switchingLine = false;
        typingCoroutine = null;
        if(IsTalking && cinema != null) { ShowLine(); }
    }

    //=====================================================================
    // 立ち絵
    //=====================================================================

    /// <summary>
    /// 文の話者と表情に合わせて立ち絵を更新する
    /// </summary>
    private void UpdatePortraits(DialogueData.Line line)
    {
        CharacterProfile character = line.character;

        // 話者なし（看板・ナレーション）
        if(character == null)
        {
            if(keepPortraitsOnNarration)
            {
                SetPortraitColor(portraitLeft, InactiveColor);
                SetPortraitColor(portraitRight, InactiveColor);
            }
            else
            {
                HideAllPortraits();
            }
            return;
        }

        bool isLeft = character.Side == CharacterProfile.PortraitSide.Left;

        Image speakerImage = isLeft ? portraitLeft : portraitRight;
        Image otherImage   = isLeft ? portraitRight : portraitLeft;

        // 話者の立ち絵を表示する
        Sprite sprite = character.GetSprite(line.expression);
        if(speakerImage != null && sprite != null)
        {
            ShowPortrait(speakerImage, sprite, character.FlipX, Color.white);
            if(isLeft) { leftCharacter = character; } else { rightCharacter = character; }
        }
        else
        {
            // 立ち絵の画像や枠がない場合は、その側を消す
            HidePortrait(speakerImage);
            if(isLeft) { leftCharacter = null; } else { rightCharacter = null; }

            if(sprite == null)
            {
                Debug.LogWarning($"{character.name}: 表情 {line.expression} の立ち絵がありません", character);
            }
        }

        // もう一方の立ち絵は、表示中なら暗くする
        CharacterProfile otherCharacter = isLeft ? rightCharacter : leftCharacter;
        if(otherCharacter != null && otherCharacter != character)
        {
            SetPortraitColor(otherImage, InactiveColor);
        }
    }

    /// <summary>
    /// 立ち絵の配置：シネマ演出のときは、小さめにして下の帯の上に立たせる（帯と字幕に重ならない）。そうでなければ元の配置
    /// </summary>
    private void LayoutPortraits(bool cinemaLayout)
    {
        float lift = 0.0f;
        float scale = 1.0f;
        float inset = 0.0f;
        if(cinemaLayout && cinema != null)
        {
            // 下の帯の高さ（立ち絵の Canvas の単位で）＝帯の太さ × マップの映る範囲の高さ
            Image any = portraitLeft != null ? portraitLeft : portraitRight;
            if(any != null && any.canvas != null)
            {
                var canvasRect = (RectTransform)any.canvas.rootCanvas.transform;
                lift = canvasRect.rect.height * cinema.ViewportRect.height * cinemaBarSize;
            }
            scale = cinemaPortraitScale;
            inset = cinemaPortraitInset;
        }

        PlacePortrait(portraitLeft, portraitLeftPos + new Vector2(inset, lift), portraitLeftScale, scale);
        PlacePortrait(portraitRight, portraitRightPos + new Vector2(-inset, lift), portraitRightScale, scale);
    }

    private static void PlacePortrait(Image image, Vector2 position, Vector3 originalScale, float scale)
    {
        if(image == null) { return; }
        RectTransform rt = image.rectTransform;
        rt.anchoredPosition = position;

        // 左右反転（scale.x の向き）はそのまま、大きさだけ変える
        float sign = rt.localScale.x < 0.0f ? -1.0f : 1.0f;
        rt.localScale = new Vector3(Mathf.Abs(originalScale.x) * scale * sign, originalScale.y * scale, originalScale.z);
    }

    private void ShowPortrait(Image image, Sprite sprite, bool flipX, Color color)
    {
        bool appearing = !image.gameObject.activeSelf;
        Color current = image.color;

        image.sprite = sprite;
        // シネマ演出では、色は下のフェードで変える（いったん今の色のまま）
        image.color = cinema != null && cinemaPortraitFade > 0.0f && !appearing ? current : color;

        // Imageには反転の設定がないので、スケールで左右反転する
        Vector3 scale = image.rectTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
        image.rectTransform.localScale = scale;

        image.gameObject.SetActive(true);

        // シネマ演出：新しく出てくる立ち絵は短いフェードインで、もう出ていれば今の色からなめらかに
        if(cinema != null && cinemaPortraitFade > 0.0f)
        {
            Color from = image.color;
            if(appearing) { from = color; from.a = 0.0f; }
            StartPortraitFade(image, from, color, cinemaPortraitFade, false);
        }
        else
        {
            StopPortraitFade(image);
        }
    }

    private void SetPortraitColor(Image image, Color color)
    {
        if(image == null || !image.gameObject.activeSelf) { return; }

        // シネマ演出：話していない方へ、なめらかに透けていく
        if(cinema != null && cinemaPortraitFade > 0.0f)
        {
            StartPortraitFade(image, image.color, color, cinemaPortraitFade, false);
            return;
        }

        StopPortraitFade(image);
        image.color = color;
    }

    private void HidePortrait(Image image)
    {
        if(image == null) { return; }

        // フェードの途中で消すときは、フェードも止める
        StopPortraitFade(image);
        image.gameObject.SetActive(false);
    }

    /// <summary>
    /// 会話の終わりに、立ち絵を溶けるように消す（消えきったら非表示）
    /// </summary>
    private void FadeOutAllPortraits(float seconds)
    {
        foreach(Image image in new[] { portraitLeft, portraitRight })
        {
            if(image == null || !image.gameObject.activeSelf) { continue; }
            Color to = image.color;
            to.a = 0.0f;
            StartPortraitFade(image, image.color, to, seconds, true);
        }

        leftCharacter = null;
        rightCharacter = null;
    }

    private void StartPortraitFade(Image image, Color from, Color to, float seconds, bool hideAtEnd)
    {
        StopPortraitFade(image);
        Coroutine fade = StartCoroutine(PortraitFade(image, from, to, seconds, hideAtEnd));
        if(image == portraitLeft) { portraitLeftFade = fade; } else { portraitRightFade = fade; }
    }

    private void StopPortraitFade(Image image)
    {
        if(image == portraitLeft && portraitLeftFade != null)   { StopCoroutine(portraitLeftFade); portraitLeftFade = null; }
        if(image == portraitRight && portraitRightFade != null) { StopCoroutine(portraitRightFade); portraitRightFade = null; }
    }

    private IEnumerator PortraitFade(Image image, Color from, Color to, float seconds, bool hideAtEnd)
    {
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            image.color = Color.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        image.color = to;
        if(hideAtEnd) { image.gameObject.SetActive(false); }
        if(image == portraitLeft) { portraitLeftFade = null; } else { portraitRightFade = null; }
    }

    private void HideAllPortraits()
    {
        HidePortrait(portraitLeft);
        HidePortrait(portraitRight);

        leftCharacter = null;
        rightCharacter = null;
    }
}
