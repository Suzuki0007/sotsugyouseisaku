/*********************************************************************/
// * \file   StillCloseEventAction.cs
// * \brief  イベントの部品「1枚絵を閉じる」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「1枚絵を閉じる」
/// </summary>
///
/// ・「1枚絵を出す」（閉じ方＝部品で閉じる）で出した絵を、フェードで閉じる
/// ・絵が出ていなければ何もしない
public class StillCloseEventAction : EventAction
{
    [SerializeField, Label("閉じるまでの秒数", 0.0f)]
    [Tooltip("フェードで消す時間。0 ならすぐ")]
    private float seconds = 1.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、消え終わってから次の部品へ進む")]
    private bool waitUntilDone = true;

    public override void Execute(GameObject gameObject)
    {
        StillView.Close(seconds);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        while(StillView.IsFading) { yield return null; }
    }
}
