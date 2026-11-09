/*********************************************************************/
// * \file   PushBlockResetEventAction.cs
// * \brief  イベントで押せる箱を置いた位置に戻すアクション
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 押せる箱を元に戻す
/// </summary>
///
/// 例：箱が角に詰まって動かせなくなったときの「リセットのレバー」／ 部屋を出たら戻す
/// ・穴に落ちた箱はそのまま（穴は埋まったまま）
public class PushBlockResetEventAction : EventAction
{
    [SerializeField]
    [Tooltip("戻す箱。空ならシーンの全部の押せる箱")]
    private List<PushBlock> blocks = new List<PushBlock>();

    public override void Execute(GameObject gameObject)
    {
        var targets = blocks.Count > 0 ? blocks : new List<PushBlock>(PushBlock.All);
        foreach(PushBlock block in targets) { if(block != null) { block.ResetToHome(); } }
    }
}
