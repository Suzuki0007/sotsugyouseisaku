/*********************************************************************/
// * \file   LightResetEventAction.cs
// * \brief  イベントの部品「光を戻す」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「光を戻す」（シーンの既定の光へ戻す）
/// </summary>
///
/// ・「光を変える」で変えた光をやめて、シーンの演出の既定（なければシーンの Global Light 2D）へなめらかに戻す
/// ・回想の中で使うと、回想の光へ戻る
public class LightResetEventAction : EventAction
{
    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("0 ならすぐに戻る")]
    private float seconds = 2.0f;

    [SerializeField, Label("変わり方")]
    private PresentationCurve curve = PresentationCurve.Smooth;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、戻り終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        PresentationDirector.ResetLightForEvent(seconds, curve);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        while(PresentationDirector.IsLightBlending) { yield return null; }
    }
}
