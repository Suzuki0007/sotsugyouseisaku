/*********************************************************************/
// * \file   WaitEventAction.cs
// * \brief  指定した秒数だけ待つアクション
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// 指定した秒数だけ待ってから次の部品へ進むアクション
/// （扉が消えてから一拍おいて会話、など）
/// </summary>
///
/// EventSequence を「順番に実行」にしたときだけ効果がある
public class WaitEventAction : EventAction
{
    [SerializeField, Tooltip("待つ秒数")]
    [Min(0f)]
    private float seconds = 1f;

    [SerializeField, Tooltip("ゲームの一時停止（Time Scale = 0）中も時間を進める")]
    private bool ignoreTimeScale = false;

    // 同時に実行するときは待てないので何もしない
    public override void Execute(GameObject gameObject) { }

    public override IEnumerator Run(GameObject gameObject)
    {
        if(seconds <= 0f) { yield break; }

        if(ignoreTimeScale) { yield return new WaitForSecondsRealtime(seconds); }
        else                { yield return new WaitForSeconds(seconds); }
    }
}
