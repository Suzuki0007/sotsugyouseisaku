/*********************************************************************/
// * \file   StillShowEventAction.cs
// * \brief  イベントの部品「1枚絵を出す」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「1枚絵を出す」（イラストを1枚、画面に出す）
/// </summary>
///
/// ・フェードで出し、止めたまま／ゆっくり寄る／ゆっくり引く／横・縦に流す
/// ・閉じ方：キーで閉じる（会話の送りと同じキー。案内が出る）／時間で閉じる／「1枚絵を閉じる」で閉じる（その間に会話を出せる）
/// ・出す範囲はマップの範囲（中央 4:3）か画面全体（16:9）。会話の枠・立ち絵は絵の上に出る
/// ・色調・光・天候の影響を受けない。シーンを移ると消える。ロードしたときは出さない
public class StillShowEventAction : EventAction
{
    public enum CloseMode
    {
        [InspectorName("キーで閉じる")]
        Key = 0,

        [InspectorName("時間で閉じる")]
        Time = 1,

        [InspectorName("部品「1枚絵を閉じる」で閉じる")]
        ByAction = 2,
    }

    [SerializeField, Label("絵")]
    [Tooltip("出すイラスト（スプライト）")]
    private Sprite sprite;

    [SerializeField, Label("出すまでの秒数", 0.0f)]
    [Tooltip("フェードで出す時間。0 ならすぐ")]
    private float fadeSeconds = 1.5f;

    [Header("動き")]

    [SerializeField, Label("動き")]
    private StillView.Motion motion = StillView.Motion.None;

    [SerializeField, Label("動きの秒数", 0.0f)]
    [Tooltip("動きが始まってから終わるまで")]
    private float motionSeconds = 10.0f;

    [SerializeField, Label("大きさ（始め）", 0.5f, 3.0f)]
    [Tooltip("ゆっくり寄る・引くの始めの大きさ（1＝範囲にぴったり）")]
    private float scaleFrom = 1.0f;

    [SerializeField, Label("大きさ（終わり）", 0.5f, 3.0f)]
    [Tooltip("ゆっくり寄る・引くの終わりの大きさ（寄るなら 1.15 くらい）")]
    private float scaleTo = 1.15f;

    [SerializeField, Label("位置（始め）", -1.0f, 1.0f)]
    [Tooltip("流すときの始めの位置（-1＝左端・下端、0＝真ん中、1＝右端・上端を見せる）。絵が範囲より大きいときだけ動く")]
    private float panFrom = -1.0f;

    [SerializeField, Label("位置（終わり）", -1.0f, 1.0f)]
    private float panTo = 1.0f;

    [Header("閉じ方")]

    [SerializeField, Label("閉じ方")]
    private CloseMode closeMode = CloseMode.Key;

    [SerializeField, Label("出しておく秒数", 0.0f)]
    [Tooltip("「時間で閉じる」のとき、出し終わってから閉じるまで")]
    private float showSeconds = 5.0f;

    [SerializeField, Label("閉じるまでの秒数", 0.0f)]
    [Tooltip("キー・時間で閉じるときのフェード")]
    private float closeSeconds = 1.0f;

    [Header("見せ方")]

    [SerializeField, Label("出す範囲")]
    private StillView.Area area = StillView.Area.Map;

    [SerializeField, Label("絵の合わせ方")]
    [Tooltip("流すときは「範囲を埋める」（はみ出た分だけ動く）")]
    private StillView.Fit fit = StillView.Fit.Cover;

    [SerializeField, Label("背景の色")]
    [Tooltip("絵の外側の色")]
    private StillView.Background background = StillView.Background.Black;

    [SerializeField, Label("上下の帯を出しておく")]
    [Tooltip("シネマ演出の帯を最初から出しておく（会話が始まっても帯が動かない。CinemaStill と同じ）")]
    private bool holdCinemaBars = true;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("キー・時間：閉じるまで次へ進まない。部品で閉じる：出し終わるまで待つ")]
    private bool waitUntilDone = true;

    public Sprite Sprite => sprite;
    public CloseMode Close => closeMode;

    public override void Execute(GameObject gameObject)
    {
        StillView.Show(new StillView.Request
        {
            sprite = sprite,
            fadeSeconds = fadeSeconds,
            motion = motion,
            motionSeconds = motionSeconds,
            // 見上げるは始め・終わりの大きさをそのまま（寄ったまま少し引く、など）
            scaleFrom = motion == StillView.Motion.LookUp ? scaleFrom : (motion == StillView.Motion.ZoomOut ? Mathf.Max(scaleFrom, scaleTo) : Mathf.Min(scaleFrom, scaleTo)),
            scaleTo = motion == StillView.Motion.LookUp ? scaleTo : (motion == StillView.Motion.ZoomOut ? Mathf.Min(scaleFrom, scaleTo) : Mathf.Max(scaleFrom, scaleTo)),
            panFrom = panFrom,
            panTo = panTo,
            area = area,
            background = background,
            fit = fit,
            holdCinemaBars = holdCinemaBars,
        });
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);

        switch(closeMode)
        {
            case CloseMode.Key:
                if(!waitUntilDone) { StillView.RunDetached(KeyThenClose()); yield break; }
                yield return KeyThenClose();
                break;

            case CloseMode.Time:
                if(!waitUntilDone) { StillView.RunDetached(TimeThenClose()); yield break; }
                yield return TimeThenClose();
                break;

            default:
                if(waitUntilDone) { while(StillView.IsFading) { yield return null; } }
                break;
        }
    }

    private IEnumerator KeyThenClose()
    {
        while(StillView.IsFading) { yield return null; }
        yield return StillView.WaitForKey();
        StillView.Close(closeSeconds);
        while(StillView.IsFading) { yield return null; }
        yield return null;
    }

    private IEnumerator TimeThenClose()
    {
        while(StillView.IsFading) { yield return null; }
        float t = 0.0f;
        while(t < showSeconds && StillView.IsShowing) { t += Time.deltaTime; yield return null; }
        StillView.Close(closeSeconds);
        while(StillView.IsFading) { yield return null; }
    }
}
