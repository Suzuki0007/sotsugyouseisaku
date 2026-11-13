/*********************************************************************/
// * \file   ShakeEventAction.cs
// * \brief  イベントの部品「画面を揺らす」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「画面を揺らす」（地響き・衝撃・頭痛など）
/// </summary>
///
/// ・マップの画面だけが揺れる（会話・HP などの UI は揺れない）
/// ・音と一緒に使うときは、この部品の前に SeEventAction を置く
public class ShakeEventAction : EventAction
{
    [SerializeField, Label("揺れの大きさ（マス）", 0.0f)]
    [Tooltip("0.1 で小さく、0.3 くらいで大きく揺れる")]
    private float strength = 0.2f;

    [SerializeField, Label("揺れる時間（秒）", 0.0f)]
    [Tooltip("だんだん弱くなって止まる")]
    private float duration = 0.6f;

    [SerializeField, Label("揺れの速さ", 1.0f)]
    [Tooltip("大きいほど細かく速く揺れる（ふつうは 25）")]
    private float frequency = 25.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、揺れが止まってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        CameraShake.Shake(strength, duration, frequency);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(waitUntilDone) { yield return new WaitForSeconds(duration); }
    }
}
