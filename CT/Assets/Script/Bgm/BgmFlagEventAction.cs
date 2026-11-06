/*********************************************************************/
// * \file   BgmFlagEventAction.cs
// * \brief  イベントでBGMフラグを切り替えるアクションのクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント (EventZone → EventSequence) でBGMフラグを切り替えるアクション
/// BgmConditionFlag と組み合わせて使う
/// </summary>
public class BgmFlagEventAction : EventAction
{
    [SerializeField, Tooltip("フラグ名 (例: Boss)")]
    private string flagName;

    [SerializeField, Tooltip("イベント発動時に設定する値")]
    private bool valueOnEnter = true;

    [SerializeField, Tooltip("エリアから出たときに元に戻す")]
    private bool revertOnExit = false;

    public override void Execute(GameObject gameObject)
    {
        BgmFlags.Set(flagName, valueOnEnter);
    }

    public override void OnExit(GameObject gameObject)
    {
        if(!revertOnExit) { return; }
        BgmFlags.Set(flagName, !valueOnEnter);
    }
}
