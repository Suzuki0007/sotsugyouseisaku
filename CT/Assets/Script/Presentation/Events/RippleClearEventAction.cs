/*********************************************************************/
// * \file   RippleClearEventAction.cs
// * \brief  イベントの部品「波紋を消す」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「波紋を消す」
/// </summary>
///
/// ・残す ID の波紋を、秒数をかけて消す。ID が空なら、今ある波紋をすべて消す
/// ・消した波紋は、セーブからも消える
public class RippleClearEventAction : EventAction
{
    [SerializeField, Label("消す ID")]
    [Tooltip("部品「波紋を広げる」の「残す ID」。空ならすべて")]
    private string id = "";

    [SerializeField, Label("消える秒数", 0.0f)]
    private float seconds = 1.5f;

    [SerializeField, Label("消えるまで待つ")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        RippleSystem.Clear(id, seconds);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(waitUntilDone && seconds > 0.0f) { yield return new WaitForSeconds(seconds); }
    }
}
