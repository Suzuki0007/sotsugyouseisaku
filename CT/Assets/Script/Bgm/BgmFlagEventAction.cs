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
/// ・Flag（GameFlag アセット）を入れたらそれを使う。空なら Flag Name を使う（旧方式）
/// ・新しく作るときは SetFlagEventAction を使ってもよい（同じことができる）
/// </summary>
public class BgmFlagEventAction : EventAction
{
    [SerializeField, Tooltip("切り替えるフラグ（GameFlag アセット）。入れたら下の Flag Name は使わない")]
    private GameFlag flag;

    [SerializeField, Tooltip("旧方式のフラグ名 (例: Boss)")]
    private string flagName;

    [SerializeField, Tooltip("イベント発動時に設定する値")]
    private bool valueOnEnter = true;

    [SerializeField, Tooltip("エリアから出たときに元に戻す")]
    private bool revertOnExit = false;

    public override void Execute(GameObject gameObject)
    {
        SetValue(valueOnEnter);
    }

    public override void OnExit(GameObject gameObject)
    {
        if(!revertOnExit) { return; }
        SetValue(!valueOnEnter);
    }

    private void SetValue(bool value)
    {
        if(flag != null) { GameFlags.Set(flag, value); }
        else             { BgmFlags.Set(flagName, value); }
    }
}
