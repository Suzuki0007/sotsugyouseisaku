/*********************************************************************/
// * \file   EnemyPattern.cs
// * \brief  敵の行動パターン（段階 → 行動の部品の並び）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行動の選び方
/// </summary>
public enum ActionOrder
{
    [InspectorName("上から順番に（最後まで行ったら最初から）")]
    Sequential = 0,

    [InspectorName("ランダム")]
    Random = 1,

    [InspectorName("重みつきランダム（重みが大きいほど選ばれやすい）")]
    Weighted = 2,
}

/// <summary>
/// 次の段階へ進む条件
/// </summary>
public enum PhaseExit
{
    [InspectorName("進まない（最後の段階）")]
    None = 0,

    [InspectorName("体力がこの割合以下になったら")]
    HealthBelow = 1,

    [InspectorName("この段階に入ってから○秒経ったら")]
    Time = 2,

    [InspectorName("条件を満たしたら")]
    Condition = 3,
}

/// <summary>
/// 行動パターンの段階（ボスの第1形態・第2形態など）
/// </summary>
[Serializable]
public class EnemyPhase
{
    [Label("段階の名前")]
    [Tooltip("デバッグ表示に出る名前（例：通常、怒り）")]
    public string name = "通常";

    [Label("行動の選び方")]
    public ActionOrder order = ActionOrder.Sequential;

    [SerializeReference]
    [Tooltip("この段階でする行動。「＋行動を追加」から選ぶ")]
    public List<EnemyAction> actions = new List<EnemyAction>();

    [Label("次の段階へ進む条件")]
    public PhaseExit exit = PhaseExit.None;

    [Label("体力の割合", 0.0f, 1.0f)]
    [Tooltip("0.5 なら体力が半分以下で次へ")]
    public float healthRatio = 0.5f;

    [Label("秒数", 0.0f)]
    public float seconds = 10.0f;

    [Label("条件")]
    [Tooltip("満たしたら次の段階へ（範囲の条件はシーンの物を指定できないので使えない）")]
    public GameCondition condition = new GameCondition();

    [Label("進むとき今の行動をやめる")]
    [Tooltip("オン：条件を満たした瞬間に、攻撃の途中でもやめて次の段階へ ／ オフ：今の行動が終わってから次へ")]
    public bool interrupt = true;

    [Header("この段階に入ったとき")]

    [Label("エフェクト")]
    public EffectData enterEffect;

    [Label("SE")]
    public SeData enterSe;

    [Label("アニメ")]
    [Tooltip("SpriteAnimData の動きの名前（1回流す）")]
    public string enterAnimation = "";

    [Label("止まる時間（秒）", 0.0f)]
    [Tooltip("段階が変わった直後に、何もしない時間（変身の演出など）")]
    public float enterPause = 0.0f;
}

/// <summary>
/// 敵の行動パターン
/// </summary>
///
/// ・段階のリスト。最初の段階から始まり、「次の段階へ進む条件」を満たすと次へ進む（戻らない）
/// ・各段階では、行動の部品（待つ・近づく・離れる・巡回・ランダムに歩く・攻撃する）を、選び方に従って繰り返す
/// ・同じパターンをいろいろな敵で使い回せる。巡回ポイントはシーンの敵（EnemyBrain）に持たせる
[CreateAssetMenu(menuName = "敵/行動パターン", fileName = "Pattern_")]
public class EnemyPattern : ScriptableObject
{
    [Tooltip("段階のリスト（上から順に進む）")]
    public List<EnemyPhase> phases = new List<EnemyPhase> { new EnemyPhase() };

    [Header("メモ")]

    [TextArea(2, 5)]
    [Tooltip("プランナー用のメモ（ゲームには影響しない）")]
    public string memo;
}
