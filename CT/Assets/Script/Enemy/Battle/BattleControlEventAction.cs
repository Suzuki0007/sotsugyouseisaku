/*********************************************************************/
// * \file   BattleControlEventAction.cs
// * \brief  イベントの部品「戦闘エリアを操作」（始める・勝ったことにする・やめる）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 戦闘エリアへの命令
/// </summary>
public enum BattleCommand
{
    [InspectorName("始める")]
    Begin = 0,

    [InspectorName("勝ったことにする")]
    Win = 1,

    [InspectorName("やめる（勝ち負けなし）")]
    Cancel = 2,
}

/// <summary>
/// イベントの部品「戦闘エリアを操作」
/// </summary>
///
/// ・会話のあとで追跡を始める（戦闘エリアの始まり方を「イベントの部品から」にしておく）
/// ・ギミックを解いたら勝ちにする、など
public class BattleControlEventAction : EventAction
{
    [SerializeField, Label("戦闘エリア")]
    private BattleArea battle;

    [SerializeField, Label("命令")]
    private BattleCommand command = BattleCommand.Begin;

    public override void Execute(GameObject gameObject)
    {
        if(battle == null) { return; }

        switch(command)
        {
            case BattleCommand.Begin:  battle.Begin(); break;
            case BattleCommand.Win:    battle.ForceWin(); break;
            case BattleCommand.Cancel: battle.Cancel(); break;
        }
    }
}
