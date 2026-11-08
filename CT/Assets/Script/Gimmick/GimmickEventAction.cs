/*********************************************************************/
// * \file   GimmickEventAction.cs
// * \brief  イベントで仕掛けの状態を変える・答え合わせをするアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントでの仕掛けの操作
/// </summary>
public enum GimmickCommand
{
    [InspectorName("ON にする")]
    TurnOn = 0,

    [InspectorName("OFF にする")]
    TurnOff = 1,

    [InspectorName("ON / OFF を切り替える")]
    Toggle = 2,

    [InspectorName("次の段階へ")]
    Advance = 3,

    [InspectorName("値にする")]
    SetValue = 4,

    [InspectorName("最初の状態に戻す")]
    Reset = 5,

    [InspectorName("固定する（動かせなくする）")]
    Lock = 6,

    [InspectorName("固定を外す")]
    Unlock = 7,

    [InspectorName("答え合わせをする（答え合わせの仕掛けに）")]
    Judge = 8,

    [InspectorName("入力を全部戻す（答え合わせの仕掛けに）")]
    ResetPuzzle = 9,
}

/// <summary>
/// イベント（EventSequence）で仕掛けを操作する
/// </summary>
///
/// 例：会話で「レバーを引いた」→ 仕掛けを ON ／ アイテムをはめたら（ItemInteraction）→ 仕掛けを ON
///     「決定」のレバーで答え合わせ ／ ギミックのリセットボタン
/// ・固定されていても変える（イベントからの操作は強制）
/// ・セーブから戻ったときは何もしない（仕掛けの状態は、仕掛け自身がセーブから戻す）
public class GimmickEventAction : EventAction
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("仕掛け")]
    [Tooltip("シーンに置いた「仕掛けの状態」（GimmickState）。答え合わせの操作なら答え合わせ（PuzzleCheck）の付いた物")]
    private GimmickState target;

    [SerializeField, Label("操作")]
    private GimmickCommand command = GimmickCommand.TurnOn;

    [SerializeField, Label("値", 0)]
    [Tooltip("「値にする」のときの値")]
    private int value = 1;

    public GimmickState Target => target;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        if(target == null) { return; }

        PuzzleCheck puzzle = target.GetComponent<PuzzleCheck>();
        switch(command)
        {
            case GimmickCommand.TurnOn:   target.TurnOn(true); break;
            case GimmickCommand.TurnOff:  target.TurnOff(true); break;
            case GimmickCommand.Toggle:   target.Toggle(true); break;
            case GimmickCommand.Advance:  target.Advance(true); break;
            case GimmickCommand.SetValue: target.Set(value, true); break;
            case GimmickCommand.Reset:    target.ResetValue(true); break;
            case GimmickCommand.Lock:     target.SetLocked(true); break;
            case GimmickCommand.Unlock:   target.SetLocked(false); break;
            case GimmickCommand.Judge:
                if(puzzle != null) { puzzle.Judge(); }
                else { Debug.LogWarning($"{name}: 「答え合わせをする」の相手に答え合わせ（PuzzleCheck）がありません", this); }
                break;
            case GimmickCommand.ResetPuzzle:
                if(puzzle != null) { puzzle.ResetInputs(); }
                break;
        }
    }
}
