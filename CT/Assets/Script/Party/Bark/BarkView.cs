/*********************************************************************/
// * \file   BarkView.cs
// * \brief  相棒の一言の吹き出し（シーンごとに自動で作られる）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 一言の吹き出し
/// </summary>
///
/// ・話すキャラの頭の上に、ゲームを止めずに出す。文はすぐに全部出し、決めた秒数で消える
/// ・掛け合いは1行ずつ順番に出す
/// ・話すキャラが画面の外にいるときは、画面の下に名前つきで出す（設定でオフにできる）
/// ・会話・メニュー・謎解きの画面・暗転が始まったら、出している一言はやめる
public class BarkView : MonoBehaviour
{
    private static BarkView instance;

    private GameObject root;
    private SpriteRenderer background;
    private SpriteRenderer accent;
    private TextMeshPro text;

    private Coroutine routine;
    private GameObject speaker;
    private bool offscreen;
    private float alpha;
    private Color speakerColor;
    private bool fromEvent;

    /// <summary> 一言を出している（出す前の間も含む）か </summary>
    public bool Busy => routine != null;

    /// <summary> 出している一言の優先度 </summary>
    public int CurrentPriority { get; private set; }

    /// <summary> 出しているデータ（その場で決めた行なら null） </summary>
    public BarkData CurrentData { get; private set; }

    /// <summary> 今の吹き出しの文（デバッグ・確認用） </summary>
    public string CurrentText => root != null && root.activeSelf ? text.text : "";

    /// <summary> 今の吹き出しが画面の下（画面の外のキャラ）か </summary>
    public bool CurrentOffscreen => offscreen;

    public static BarkView Find() => instance;

    public static BarkView Get()
    {
        if(instance == null)
        {
            var go = new GameObject("相棒の一言（自動で作られる）");
            instance = go.AddComponent<BarkView>();
        }
        return instance;
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }

    //=====================================================================
    // 出す
    //=====================================================================

    /// <summary> 一言を出す（今の一言はやめる） </summary>
    public void Play(BarkData data, List<BarkLine> lines, BarkContext context, int priority, bool isEvent)
    {
        StopCurrent(false);
        CurrentData = data;
        CurrentPriority = priority;
        fromEvent = isEvent;
        routine = StartCoroutine(Run(data, new List<BarkLine>(lines), context));
    }

    /// <summary> 出している一言をやめる </summary>
    public void StopCurrent(bool notify)
    {
        if(routine != null) { StopCoroutine(routine); routine = null; }
        Hide();
        CurrentData = null;
        CurrentPriority = int.MinValue;
        if(notify) { Bark.NotifyFinished(); }
    }

    private IEnumerator Run(BarkData data, List<BarkLine> lines, BarkContext context)
    {
        // 出すまでの間
        float delay = data != null ? data.delay : 0.0f;
        if(delay > 0.0f) { yield return new WaitForSeconds(delay); }

        // 操作が止まっている間（会話・イベントなど）は少し待つ。長く続いたら出さない
        float waited = 0.0f;
        while(Blocked())
        {
            waited += Time.unscaledDeltaTime;
            if(waited > 4.0f) { Finish(); yield break; }
            yield return null;
        }

        for(int i = 0; i < lines.Count; i++)
        {
            BarkLine line = lines[i];
            if(line == null || string.IsNullOrEmpty(line.text)) { continue; }

            speaker = Bark.ResolveSpeaker(line.speaker, context);
            if(speaker == null) { continue; }

            SeManager.PlaySe(line.se, speaker.transform.position);
            Show(line.text);

            BarkSettings s = Bark.Settings;
            float seconds = Bark.SecondsOf(line);
            float t = 0.0f;
            while(t < seconds + s.fadeSeconds)
            {
                if(Interrupted()) { Finish(); yield break; }
                t += Time.deltaTime;
                alpha = s.fadeSeconds > 0.0f ? Mathf.Clamp01((seconds + s.fadeSeconds - t) / s.fadeSeconds) : 1.0f;
                Place();
                yield return null;
            }
            Hide();

            if(i < lines.Count - 1 && line.gap > 0.0f) { yield return new WaitForSeconds(line.gap); }
        }
        Finish();
    }

    private void Finish()
    {
        routine = null;
        Hide();
        CurrentData = null;
        CurrentPriority = int.MinValue;
        Bark.NotifyFinished();
    }

    /// <summary> まだ出さずに待つか（自動の一言は操作が止まっている間、イベントの一言は会話などの間） </summary>
    private bool Blocked() => fromEvent ? Interrupting() : InputBlock.IsBlocked;

    /// <summary> 出している一言をやめるか </summary>
    private bool Interrupted() => Interrupting() || !Bark.CanSpeak();

    private static bool Interrupting() =>
        DialogueManager.IsBusy || SceneFader.IsBusy || GameMenu.IsBusy || ItemMenuUI.IsBusy || PuzzleUI.IsBusy;

    //=====================================================================
    // 吹き出し
    //=====================================================================

    private void Show(string message)
    {
        BarkSettings s = Bark.Settings;
        Build(s);

        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        bool noah = m != null && speaker == m.Noah;
        speakerColor = noah ? s.noahColor : s.sigleColor;
        string speakerName = noah ? s.noahName : s.sigleName;

        offscreen = !IsVisible(speaker.transform.position + Vector3.up * s.height);
        if(offscreen && !s.showOffscreen) { speaker = null; return; }

        string body = offscreen ? "<color=#" + ColorUtility.ToHtmlStringRGB(speakerColor) + ">" + speakerName + "</color>：" + message : message;
        text.text = body;

        // 文の大きさに合わせて吹き出しを広げる
        Vector2 size = text.GetPreferredValues(body, s.maxWidth, 0.0f);
        size.x = Mathf.Min(size.x, s.maxWidth);
        text.rectTransform.sizeDelta = size;
        Vector2 box = size + s.padding * 2.0f;
        ScaleTo(background, box);
        ScaleTo(accent, new Vector2(box.x, 0.05f));
        accent.transform.localPosition = new Vector3(0.0f, -box.y * 0.5f, 0.0f);

        alpha = 1.0f;
        root.SetActive(true);
        Place();
    }

    private void Hide()
    {
        if(root != null) { root.SetActive(false); }
    }

    private void Place()
    {
        if(root == null || !root.activeSelf) { return; }
        if(speaker == null) { Hide(); return; }

        BarkSettings s = Bark.Settings;
        Camera cam = Camera.main;
        Vector3 pos;
        if(offscreen && cam != null)
        {
            pos = cam.ViewportToWorldPoint(new Vector3(0.5f, s.offscreenY, 0.0f));
        }
        else
        {
            pos = speaker.transform.position + Vector3.up * s.height;
            // 画面の端からはみ出さないように寄せる
            if(cam != null && cam.orthographic)
            {
                float halfW = cam.orthographicSize * cam.aspect;
                float w = background.transform.localScale.x * (background.sprite != null ? background.sprite.bounds.size.x : 1.0f) * 0.5f;
                float left = cam.transform.position.x - halfW + w + 0.1f;
                float right = cam.transform.position.x + halfW - w - 0.1f;
                if(left < right) { pos.x = Mathf.Clamp(pos.x, left, right); }
            }
        }
        pos.z = 0.0f;
        root.transform.position = pos;

        Color bg = s.backgroundColor; bg.a *= alpha;
        background.color = bg;
        Color ac = speakerColor; ac.a *= alpha;
        accent.color = ac;
        Color tc = s.textColor; tc.a *= alpha;
        text.color = tc;
    }

    private static bool IsVisible(Vector3 world)
    {
        Camera cam = Camera.main;
        if(cam == null) { return true; }
        Vector3 v = cam.WorldToViewportPoint(world);
        return v.x >= 0.0f && v.x <= 1.0f && v.y >= 0.0f && v.y <= 1.0f;
    }

    private void Build(BarkSettings s)
    {
        if(root != null) { return; }

        root = new GameObject("吹き出し");
        root.transform.SetParent(transform, false);

        var bg = new GameObject("背景");
        bg.transform.SetParent(root.transform, false);
        background = bg.AddComponent<SpriteRenderer>();
        background.sprite = s.background != null ? s.background : WhiteSprite();
        background.sortingOrder = s.sortingOrder;

        var line = new GameObject("縁");
        line.transform.SetParent(root.transform, false);
        accent = line.AddComponent<SpriteRenderer>();
        accent.sprite = WhiteSprite();
        accent.sortingOrder = s.sortingOrder + 1;

        var textGo = new GameObject("文字");
        textGo.transform.SetParent(root.transform, false);
        text = textGo.AddComponent<TextMeshPro>();
        if(s.font != null) { text.font = s.font; }
        text.fontSize = s.fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = true;
        text.sortingOrder = s.sortingOrder + 2;

        root.SetActive(false);
    }

    private static void ScaleTo(SpriteRenderer r, Vector2 size)
    {
        Vector2 native = r.sprite != null ? (Vector2)r.sprite.bounds.size : Vector2.one;
        r.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, native.x), size.y / Mathf.Max(0.01f, native.y), 1.0f);
    }

    private static Sprite white;

    private static Sprite WhiteSprite()
    {
        if(white == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        return white;
    }
}
