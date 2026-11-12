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
        lastToggleFrame = Time.frameCount;

        HideAllPortraits();
        panel.SetActive(true);
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

        HideAllPortraits();
        if(panel != null) { panel.SetActive(false); }

        lines = null;
        lastToggleFrame = Time.frameCount;

        // コールバックの中で次の会話を始めても大丈夫なように、先に空にしてから呼ぶ
        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
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

        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;

        // リッチテキストのタグを除いた、実際に表示される文字数を取得する
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;

        if(charInterval > 0f)
        {
            for(int i = 1; i <= total; i++)
            {
                bodyText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(charInterval);
            }
        }

        typingCoroutine = null;
        CompleteLine();
    }

    /// <summary>
    /// 今の文を全文表示する
    /// </summary>
    private void CompleteLine()
    {
        if(typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }

        bodyText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;

        if(nextIcon != null) { nextIcon.SetActive(true); }
    }

    /// <summary>
    /// 次の文へ進む。最後の文なら会話を閉じる
    /// </summary>
    private void Next()
    {
        index++;

        if(index < lines.Length)
        {
            ShowLine();
            return;
        }

        EndDialogue();
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
                SetPortraitColor(portraitLeft, inactivePortraitColor);
                SetPortraitColor(portraitRight, inactivePortraitColor);
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
            SetPortraitColor(otherImage, inactivePortraitColor);
        }
    }

    private void ShowPortrait(Image image, Sprite sprite, bool flipX, Color color)
    {
        image.sprite = sprite;
        image.color = color;

        // Imageには反転の設定がないので、スケールで左右反転する
        Vector3 scale = image.rectTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
        image.rectTransform.localScale = scale;

        image.gameObject.SetActive(true);
    }

    private void SetPortraitColor(Image image, Color color)
    {
        if(image == null || !image.gameObject.activeSelf) { return; }

        image.color = color;
    }

    private void HidePortrait(Image image)
    {
        if(image == null) { return; }

        image.gameObject.SetActive(false);
    }

    private void HideAllPortraits()
    {
        HidePortrait(portraitLeft);
        HidePortrait(portraitRight);

        leftCharacter = null;
        rightCharacter = null;
    }
}
