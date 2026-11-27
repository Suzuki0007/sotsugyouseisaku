/*********************************************************************/
// * \file   SubtitleEventAction.cs
// * \brief  イベントの部品「字幕」（シネマ演出の下の帯に文を出す）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「字幕」（シネマ演出の下の帯に、セリフを白い文字で出す）
/// </summary>
///
/// ・部品「シネマ演出」の「始める」のあとに並べる（帯がなくても出るが、黒い帯の上で読みやすい色にしてある）
/// ・会話データ（DialogueData）を入れると、その文を上から順に出す（CSV で作った会話がそのまま使える。立ち絵は出ない）
/// ・送り方：キーで送る（E・Space・Enter）／秒数で次へ
/// ・字幕の形：映画（名前・帯なしで、中央より少し下に大きな太字。新しく足すとこちら）／帯（下の帯に名前つき）
/// ・ロードしても字幕は戻らない
public class SubtitleEventAction : EventAction
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum Advance
    {
        [InspectorName("キーで送る（E・Space・Enter）")]
        Key = 0,

        [InspectorName("秒数で次へ")]
        Seconds = 1,
    }

    /// <summary> 字幕の形（番号で保存されるので末尾にだけ足す） </summary>
    public enum Style
    {
        [InspectorName("帯（下の帯に名前つき。会話と同じ形）")]
        Band = 0,

        [InspectorName("映画（中央より少し下に大きな太字。名前なし）")]
        Movie = 1,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    // 前からある部品は「帯」のまま（Reset は新しく足したときだけ呼ばれ、「映画」になる）
    [SerializeField, Label("字幕の形")]
    [Tooltip("映画：名前も帯もなく、文字だけを画面の中央より少し下に大きな太字で出す（見た目は Resources/Presentation/SubtitleStyle）／帯：下の帯に名前つきで出す（会話と同じ形）")]
    private Style style = Style.Band;

    [SerializeField, Label("会話データ")]
    [Tooltip("入れると、この会話の文を上から順に字幕で出す（下の「字幕の文」は使わない）。空なら「字幕の文」を出す")]
    private DialogueData dialogueData;

    [SerializeField, Label("話し手")]
    [Tooltip("「字幕の文」の話し手（名前が文の前にキャラの色で出る）。空なら名前なし（ナレーション）")]
    private CharacterProfile speaker;

    [SerializeField, Label("名前の上書き")]
    [Tooltip("話し手の名前の代わりに出す名前（例：？？？）。空なら話し手の名前")]
    private string speakerName;

    // TextArea は [Label] と一緒に使えない（表示の属性は1つしか効かない）ので1行の欄にしている
    [SerializeField, Label("字幕の文")]
    [Tooltip("下の帯に出す文。2行まで（1行は約 35 文字。字の大きさは固定なので、3行目は帯からはみ出す）")]
    private string text;

    [SerializeField, Label("送り方")]
    [Tooltip("キーで送る：E・Space・Enter を押すまで待つ／秒数で次へ：下の秒数で次の文・次の部品へ")]
    private Advance advance = Advance.Key;

    [SerializeField, Label("出しておく秒数", 0.0f)]
    [Tooltip("送り方が「秒数で次へ」のとき、1つの文を出しておく秒数")]
    private float seconds = 2.5f;

    [SerializeField, Label("最後に消す")]
    [Tooltip("オン：最後の文を送ったら字幕を消す／オフ：出したまま次の部品へ（「シネマ演出」の「終える」で消える）")]
    private bool hideAtEnd = true;

    // 字幕が出てくる・消える時間
    private const float FadeTime = 0.2f;

    // キーで送るときの案内（会話の枠の初期値と同じ文）
    private const string KeyHint = "<b>E</b> / <b>Space</b> / <b>Enter</b>　次へ";

    //=====================================================================
    // アクション関数
    //=====================================================================

    private void Reset()
    {
        // 新しく足した字幕は映画の形（11-27 の打ち合わせ：飾らず文字だけを大きく）
        style = Style.Movie;
    }

    public override void Execute(GameObject gameObject)
    {
        // すべて同時に実行のときは、最初の文を出すだけ
        CinemaMode cinema = CinemaMode.Get();
        if(cinema == null) { return; }

        if(dialogueData != null && dialogueData.Lines.Length > 0) { ShowLine(cinema, dialogueData.Lines[0]); }
        else { ShowText(cinema, text); }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        CinemaMode cinema = CinemaMode.Get();
        if(cinema == null) { yield break; }

        try
        {
            if(dialogueData != null && dialogueData.Lines.Length > 0)
            {
                foreach(DialogueData.Line line in dialogueData.Lines)
                {
                    if(line == null) { continue; }
                    ShowLine(cinema, line);
                    yield return Wait(cinema);
                }
            }
            else
            {
                ShowText(cinema, text);
                yield return Wait(cinema);
            }
        }
        finally
        {
            // イベントが途中で止められても、キー待ちが残らないようにする
            CinemaMode.CancelWait();
            if(cinema != null) { cinema.SetKeyHint(null); cinema.SetNextMark(false); }
        }

        if(hideAtEnd && cinema != null)
        {
            if(style == Style.Movie) { cinema.HideMovieSubtitle(MovieFadeTime); }
            else { cinema.HideSubtitle(FadeTime); }
        }
    }

    // 映画の字幕はゆっくり出して消す
    private const float MovieFadeTime = 0.4f;

    private void ShowText(CinemaMode cinema, string body)
    {
        if(style == Style.Movie) { cinema.ShowMovieSubtitle(body, MovieFadeTime); }
        else { cinema.ShowSubtitle(SpeakerName(), SpeakerColor(), body, FadeTime); }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private void ShowLine(CinemaMode cinema, DialogueData.Line line)
    {
        if(style == Style.Movie) { cinema.ShowMovieSubtitle(line.text, MovieFadeTime); return; }
        Color color = line.character != null ? line.character.NameColor : Color.white;
        cinema.ShowSubtitle(line.DisplayName, color, line.text, FadeTime);
    }

    private IEnumerator Wait(CinemaMode cinema)
    {
        if(advance == Advance.Key)
        {
            // 会話と同じく、▼ とキーの案内を出して待つ
            cinema.SetNextMark(true);
            cinema.SetKeyHint(KeyHint);
            yield return CinemaMode.WaitForAdvanceKey();
            if(cinema != null) { cinema.SetNextMark(false); }
            yield break;
        }

        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private string SpeakerName()
    {
        if(!string.IsNullOrEmpty(speakerName)) { return speakerName; }
        return speaker != null ? speaker.DisplayName : "";
    }

    private Color SpeakerColor()
    {
        return speaker != null ? speaker.NameColor : Color.white;
    }

    /// <summary> 出す文がないか（シーンチェック用） </summary>
    public bool IsEmpty => (dialogueData == null || dialogueData.Lines.Length == 0) && string.IsNullOrWhiteSpace(text);
}
