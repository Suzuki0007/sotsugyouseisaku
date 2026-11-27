/*********************************************************************/
// * \file   ScreenFrameEventAction.cs
// * \brief  イベントの部品「いつもの画面の縁」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「いつもの画面の縁」（かける・外す）
/// </summary>
///
/// ・いつもかける画面の縁（縁のぼかし・上下のグラデーション・周辺の暗さ）を、イベントの途中で外す・かけ直す
/// ・画面いっぱいの1枚絵・真っ白な場面など、縁がじゃまな所だけ外すときに。「シーンの既定に戻す」で元へ
/// ・色調の「画面の縁」「周辺を暗くする」は別（こちらで外しても、色調の分は残る）
/// ・ロードしたときは、セーブしたときの状態に戻る
public class ScreenFrameEventAction : EventAction
{
    public enum Mode
    {
        [InspectorName("外す")]
        Off = 0,

        [InspectorName("かける")]
        On = 1,

        [InspectorName("シーンの既定に戻す")]
        Reset = 2,
    }

    [SerializeField, Label("どうするか")]
    private Mode mode = Mode.Off;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("なめらかに変える秒数。0 ならすぐ")]
    private float seconds = 1.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        switch(mode)
        {
            case Mode.Off:   PresentationDirector.SetFrameForEvent(false, seconds); break;
            case Mode.On:    PresentationDirector.SetFrameForEvent(true, seconds); break;
            case Mode.Reset: PresentationDirector.ResetFrameForEvent(seconds); break;
        }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone || seconds <= 0.0f) { yield break; }
        float t = 0.0f;
        while(t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }
}
