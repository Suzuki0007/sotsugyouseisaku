/*********************************************************************/
// * \file   PuzzleUI.cs
// * \brief  謎解きの画面（番号錠の入力・リストから選ぶ・文章を読む）。必要なときに自動で作られる
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 謎解きの画面
/// </summary>
///
/// ・番号錠（OpenCode）：←→ で桁を選び、↑↓ で変える。決定で確かめる
/// ・リストから選ぶ（OpenChoice）：置き台にはめるアイテム・読むメモ など
/// ・文章を読む（OpenText）：メモ・手紙。長いときは ←→ でページをめくる
/// ・開いている間はプレイヤーの操作を止める（InputBlock）。見た目は Resources/PuzzleUI.asset
/// ・キー：決定 Space / Enter / E ・やめる Esc / Tab / Backspace
public class PuzzleUI : MonoBehaviour
{
    private enum Mode { None, Code, Choice, Text }

    //=====================================================================
    // 状態
    //=====================================================================

    private static PuzzleUI instance;
    private static int closedFrame = -10;

    private PuzzleUISettings settings;
    private Mode mode = Mode.None;
    private int openedFrame;

    // 共通の部品
    private GameObject root;
    private Image box;
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private TextMeshProUGUI hint;
    private Image picture;
    private RectTransform row;
    private RectTransform list;

    // 番号錠
    private string symbols;
    private int[] values;
    private int cursor;
    private readonly List<Image> slotBoxes = new List<Image>();
    private readonly List<TextMeshProUGUI> slotTexts = new List<TextMeshProUGUI>();
    private Action<int[]> onSubmit;

    // 選ぶ
    private readonly List<Image> rowBoxes = new List<Image>();
    private Action<int> onChoose;
    private int choiceCount;

    // 読む
    private Action onClose;
    private Action onCancel;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 開いているか </summary>
    public static bool IsOpen => instance != null && instance.mode != Mode.None;

    /// <summary> 開いている、または閉じた直後か（プレイヤーの入力を止める判定） </summary>
    public static bool IsBusy => IsOpen || Time.frameCount - closedFrame <= 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { instance = null; closedFrame = -10; }

    private static PuzzleUI Get()
    {
        if(instance == null)
        {
            var go = new GameObject("謎解きの画面（自動で作られる）");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PuzzleUI>();
            instance.Build();
        }
        return instance;
    }

    //=====================================================================
    // 開く
    //=====================================================================

    /// <summary>
    /// 番号錠の入力を開く
    /// </summary>
    /// <param name="symbols"> 使える文字（"0123456789" など。1 文字ずつ） </param>
    /// <param name="initial"> 最初の並び（symbols の何番目か） </param>
    /// <param name="submit"> 決定したとき（入力した並び） </param>
    /// <param name="cancel"> やめたとき </param>
    public static void OpenCode(string titleText, string symbols, int[] initial, Action<int[]> submit, Action cancel)
    {
        PuzzleUI ui = Get();
        ui.Begin(Mode.Code, titleText, cancel);
        ui.symbols = string.IsNullOrEmpty(symbols) ? "0123456789" : symbols;
        ui.values = (int[])initial.Clone();
        ui.cursor = 0;
        ui.onSubmit = submit;
        ui.BuildSlots();
        ui.hint.text = "←→：桁を選ぶ　↑↓：変える　Space / Enter：決定　Esc：やめる";
        ui.RefreshCode();
    }

    /// <summary>
    /// リストから選ぶ
    /// </summary>
    /// <param name="labels"> 並べる名前 </param>
    /// <param name="icons"> 名前の左に出す絵（なくてもよい） </param>
    /// <param name="message"> 上に出す説明（なくてもよい） </param>
    public static void OpenChoice(string titleText, IList<string> labels, IList<Sprite> icons, int start, Action<int> choose, Action cancel, string message = "")
    {
        PuzzleUI ui = Get();
        ui.Begin(Mode.Choice, titleText, cancel);
        ui.onChoose = choose;
        ui.choiceCount = labels.Count;
        ui.cursor = Mathf.Clamp(start, 0, Mathf.Max(0, labels.Count - 1));
        ui.body.text = message;
        ui.body.gameObject.SetActive(!string.IsNullOrEmpty(message));
        ui.BuildRows(labels, icons);
        ui.hint.text = "↑↓：選ぶ　Space / Enter：決定　Esc：もどる";
        ui.RefreshChoice();
    }

    /// <summary>
    /// 文章を読む（メモ・手紙）
    /// </summary>
    public static void OpenText(string titleText, string text, Sprite image, Action close)
    {
        PuzzleUI ui = Get();
        ui.Begin(Mode.Text, titleText, close);
        ui.onClose = close;

        ui.box.color = ui.settings != null ? ui.settings.paperColor : new Color(0.93f, 0.9f, 0.82f);
        Color ink = ui.settings != null ? ui.settings.paperTextColor : Color.black;
        ui.title.color = ink;
        ui.body.color = ink;

        ui.picture.sprite = image;
        ui.picture.gameObject.SetActive(image != null);
        ui.picture.preserveAspect = true;

        ui.body.gameObject.SetActive(true);
        ui.body.text = text ?? "";
        ui.body.overflowMode = TextOverflowModes.Page;
        ui.body.pageToDisplay = 1;
        ui.body.ForceMeshUpdate();
        ui.RefreshTextHint();
    }

    private void Begin(Mode newMode, string titleText, Action cancel)
    {
        settings = Resources.Load<PuzzleUISettings>("PuzzleUI");
        ApplyFont();

        mode = newMode;
        openedFrame = Time.frameCount;
        onCancel = cancel;
        onSubmit = null;
        onChoose = null;
        onClose = null;

        root.SetActive(true);
        box.color = settings != null ? settings.boxColor : new Color(0.08f, 0.08f, 0.12f, 0.96f);
        Color text = settings != null ? settings.textColor : Color.white;
        title.color = text;
        body.color = text;
        title.text = titleText ?? "";
        body.text = "";
        body.overflowMode = TextOverflowModes.Overflow;
        body.gameObject.SetActive(false);
        picture.gameObject.SetActive(false);
        row.gameObject.SetActive(newMode == Mode.Code);
        list.gameObject.SetActive(newMode == Mode.Choice);

        UISound.Play(UISoundType.MenuOpen);
    }

    private void Close(bool playSe = true)
    {
        mode = Mode.None;
        root.SetActive(false);
        closedFrame = Time.frameCount;
        if(playSe) { UISound.Play(UISoundType.MenuClose); }
    }

    //=====================================================================
    // 操作
    //=====================================================================

    private void Update()
    {
        if(mode == Mode.None || Keyboard.current == null) { return; }
        if(Time.frameCount == openedFrame) { return; }

        Keyboard k = Keyboard.current;
        bool up = k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
        bool down = k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
        bool left = k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
        bool right = k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
        bool decide = k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
        bool cancel = k.escapeKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame;

        switch(mode)
        {
            case Mode.Code:   UpdateCode(up, down, left, right, decide, cancel); break;
            case Mode.Choice: UpdateChoice(up, down, decide, cancel); break;
            case Mode.Text:   UpdateText(left, right, decide, cancel); break;
        }
    }

    private void UpdateCode(bool up, bool down, bool left, bool right, bool decide, bool cancel)
    {
        int n = values.Length;
        if(left)  { cursor = (cursor - 1 + n) % n; UISound.Play(UISoundType.Select); RefreshCode(); }
        if(right) { cursor = (cursor + 1) % n; UISound.Play(UISoundType.Select); RefreshCode(); }
        if(up)    { values[cursor] = (values[cursor] + 1) % symbols.Length; UISound.Play(UISoundType.Select); RefreshCode(); }
        if(down)  { values[cursor] = (values[cursor] - 1 + symbols.Length) % symbols.Length; UISound.Play(UISoundType.Select); RefreshCode(); }

        if(decide)
        {
            Action<int[]> submit = onSubmit;
            int[] result = (int[])values.Clone();
            Close(false);
            submit?.Invoke(result);
            return;
        }
        if(cancel) { Cancel(); }
    }

    private void UpdateChoice(bool up, bool down, bool decide, bool cancel)
    {
        if(choiceCount > 0)
        {
            if(up)   { cursor = (cursor - 1 + choiceCount) % choiceCount; UISound.Play(UISoundType.Select); RefreshChoice(); }
            if(down) { cursor = (cursor + 1) % choiceCount; UISound.Play(UISoundType.Select); RefreshChoice(); }
        }

        if(decide && choiceCount > 0)
        {
            Action<int> choose = onChoose;
            int index = cursor;
            UISound.Play(UISoundType.Decide);
            Close(false);
            choose?.Invoke(index);
            return;
        }
        if(cancel || (decide && choiceCount == 0)) { Cancel(); }
    }

    private void UpdateText(bool left, bool right, bool decide, bool cancel)
    {
        int pages = Mathf.Max(1, body.textInfo.pageCount);
        if(right && body.pageToDisplay < pages) { body.pageToDisplay++; UISound.Play(UISoundType.Select); RefreshTextHint(); return; }
        if(left && body.pageToDisplay > 1)      { body.pageToDisplay--; UISound.Play(UISoundType.Select); RefreshTextHint(); return; }

        // 決定は「次のページ」、最後のページなら閉じる
        if(decide && body.pageToDisplay < pages) { body.pageToDisplay++; UISound.Play(UISoundType.Select); RefreshTextHint(); return; }
        if(decide || cancel)
        {
            Action close = onClose;
            Close();
            close?.Invoke();
        }
    }

    private void Cancel()
    {
        Action cancel = onCancel;
        UISound.Play(UISoundType.Cancel);
        Close(false);
        cancel?.Invoke();
    }

    //=====================================================================
    // 表示の更新
    //=====================================================================

    private void RefreshCode()
    {
        for(int i = 0; i < slotTexts.Count; i++)
        {
            slotTexts[i].text = symbols[Mathf.Clamp(values[i], 0, symbols.Length - 1)].ToString();
            slotBoxes[i].color = i == cursor ? Selected : Normal;
            slotTexts[i].color = i == cursor ? Color.black : TextColor;
        }
    }

    private void RefreshChoice()
    {
        for(int i = 0; i < rowBoxes.Count; i++)
        {
            rowBoxes[i].color = i == cursor ? Selected : Normal;
            var t = rowBoxes[i].GetComponentInChildren<TextMeshProUGUI>();
            if(t != null) { t.color = i == cursor ? Color.black : TextColor; }
        }
    }

    private void RefreshTextHint()
    {
        int pages = Mathf.Max(1, body.textInfo.pageCount);
        hint.color = settings != null ? settings.paperTextColor : Color.black;
        hint.text = pages > 1
            ? $"{body.pageToDisplay} / {pages}　←→：ページ　Space：次へ　Esc：とじる"
            : "Space / Esc：とじる";
    }

    private Color Selected => settings != null ? settings.selectedColor : new Color(0.95f, 0.8f, 0.3f);
    private Color Normal => settings != null ? settings.normalColor : new Color(0.25f, 0.25f, 0.32f);
    private Color TextColor => settings != null ? settings.textColor : Color.white;

    //=====================================================================
    // 画面を作る
    //=====================================================================

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;    // メニュー画面（500）より手前
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        Stretch((RectTransform)root.transform);

        var dim = AddImage("Dim", root.transform, new Color(0, 0, 0, 0.6f));
        Stretch(dim.rectTransform);

        box = AddImage("Box", root.transform, new Color(0.08f, 0.08f, 0.12f, 0.96f));
        Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 640));

        title = AddText("Title", box.transform, 48, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -60), new Vector2(920, 70));

        picture = AddImage("Picture", box.transform, Color.white);
        Place(picture.rectTransform, new Vector2(0.5f, 1.0f), new Vector2(0, -190), new Vector2(360, 160));

        body = AddText("Body", box.transform, 34, TextAlignmentOptions.TopLeft);
        Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(880, 420));
        body.textWrappingMode = TextWrappingModes.Normal;

        row = new GameObject("Code", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(box.transform, false);
        Place(row, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(880, 220));
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24; h.childAlignment = TextAnchor.MiddleCenter; h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        list = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
        list.SetParent(box.transform, false);
        Place(list, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(800, 420));
        var v = list.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 10; v.childAlignment = TextAnchor.UpperCenter; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        hint = AddText("Hint", box.transform, 26, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(0.5f, 0.0f), new Vector2(0, 40), new Vector2(920, 50));

        root.SetActive(false);
    }

    private void BuildSlots()
    {
        foreach(Transform child in row) { Destroy(child.gameObject); }
        slotBoxes.Clear();
        slotTexts.Clear();
        for(int i = 0; i < values.Length; i++)
        {
            var slot = AddImage("Slot" + i, row, Normal);
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 110; le.preferredHeight = 150;
            var text = AddText("Text", slot.transform, 96, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            slotBoxes.Add(slot);
            slotTexts.Add(text);
        }
    }

    private void BuildRows(IList<string> labels, IList<Sprite> icons)
    {
        foreach(Transform child in list) { Destroy(child.gameObject); }
        rowBoxes.Clear();
        for(int i = 0; i < labels.Count; i++)
        {
            var rowBox = AddImage("Row" + i, list, Normal);
            var le = rowBox.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 64;

            Sprite icon = icons != null && i < icons.Count ? icons[i] : null;
            if(icon != null)
            {
                var img = AddImage("Icon", rowBox.transform, Color.white);
                img.sprite = icon;
                img.preserveAspect = true;
                Place(img.rectTransform, new Vector2(0.0f, 0.5f), new Vector2(42, 0), new Vector2(52, 52));
            }

            var text = AddText("Text", rowBox.transform, 34, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(icon != null ? 84 : 24, 0);
            text.text = labels[i];
            rowBoxes.Add(rowBox);
        }
        if(labels.Count == 0)
        {
            var empty = AddText("Empty", list, 32, TextAlignmentOptions.Center);
            empty.text = "（ありません）";
            var le = empty.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 64;
        }
    }

    private void ApplyFont()
    {
        TMP_FontAsset font = settings != null ? settings.font : null;
        if(font == null) { return; }
        foreach(TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) { t.font = font; }
    }

    private TextMeshProUGUI AddText(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if(settings != null && settings.font != null) { t.font = settings.font; }
        else
        {
            var s = Resources.Load<PuzzleUISettings>("PuzzleUI");
            if(s != null && s.font != null) { t.font = s.font; }
        }
        t.fontSize = size;
        t.alignment = align;
        t.color = TextColor;
        t.raycastTarget = false;
        return t;
    }

    private static Image AddImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
    }
}
