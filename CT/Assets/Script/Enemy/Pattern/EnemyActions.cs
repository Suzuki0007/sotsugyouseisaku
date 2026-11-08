/*********************************************************************/
// * \file   EnemyActions.cs
// * \brief  敵の行動の部品（待つ・近づく・離れる・巡回・ランダムに歩く・攻撃する）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using UnityEngine;

//=====================================================================
// 共通
//=====================================================================

/// <summary>
/// 行動の部品の、追加メニューでの名前（EnemyPattern の「＋行動を追加」に出る）
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EnemyActionMenuAttribute : Attribute
{
    public readonly string path;
    public readonly int order;

    public EnemyActionMenuAttribute(string path, int order = 0)
    {
        this.path = path;
        this.order = order;
    }
}

/// <summary>
/// 距離の条件
/// </summary>
public enum DistanceCheck
{
    [InspectorName("問わない")]
    Any = 0,

    [InspectorName("プレイヤーがこの距離より近いとき")]
    Closer = 1,

    [InspectorName("プレイヤーがこの距離より遠いとき")]
    Farther = 2,
}

/// <summary>
/// 行動の部品の基底
/// </summary>
///
/// ・新しい部品を作るときはこれを継承し、[Serializable] と [EnemyActionMenu("分類/名前")] を付ける
/// ・Run はコルーチン。待つときは brain.Wait / brain.MoveUntil を使う（止められている間は時間が進まない）
/// ・途中でやめられたときの片づけは Cleanup に書く
/// ・クラス名を変えるとデータが消える（SerializeReference のため）。変えるときは [UnityEngine.Scripting.APIUpdating.MovedFrom] を付ける
[Serializable]
public abstract class EnemyAction
{
    [Label("この行動をする距離")]
    [Tooltip("プレイヤーとの距離で、この行動をするかを決める（満たさないときは飛ばす）")]
    public DistanceCheck distanceCheck = DistanceCheck.Any;

    [Label("距離", 0.0f)]
    public float distance = 4.0f;

    [Label("重み", 0.0f)]
    [Tooltip("選び方が「重みつきランダム」のときの選ばれやすさ")]
    public float weight = 1.0f;

    /// <summary> 一覧に出す説明（例：「近づく（1.5 まで）」） </summary>
    public abstract string Summary { get; }

    /// <summary>
    /// 今この行動をしてよいか
    /// </summary>
    public bool CanRun(EnemyBrain brain)
    {
        if(distanceCheck == DistanceCheck.Closer  && brain.DistanceToTarget > distance)  { return false; }
        if(distanceCheck == DistanceCheck.Farther && brain.DistanceToTarget <= distance) { return false; }
        return CanRunExtra(brain);
    }

    protected virtual bool CanRunExtra(EnemyBrain brain) => true;

    /// <summary> 行動する（コルーチン） </summary>
    public abstract IEnumerator Run(EnemyBrain brain);

    /// <summary> 途中でやめられたときの片づけ </summary>
    public virtual void Cleanup(EnemyBrain brain) { }
}

//=====================================================================
// 待つ
//=====================================================================

[Serializable, EnemyActionMenu("待つ", 0)]
public class WaitAction : EnemyAction
{
    [Label("最短（秒）", 0.0f)]
    public float minSeconds = 1.0f;

    [Label("最長（秒）", 0.0f)]
    [Tooltip("最短と最長の間でランダムに待つ（同じなら決まった時間）")]
    public float maxSeconds = 1.0f;

    public override string Summary => Mathf.Approximately(minSeconds, maxSeconds) ? $"待つ（{minSeconds} 秒）" : $"待つ（{minSeconds}〜{maxSeconds} 秒）";

    public override IEnumerator Run(EnemyBrain brain)
    {
        brain.Enemy.Stop();
        float seconds = UnityEngine.Random.Range(Mathf.Min(minSeconds, maxSeconds), Mathf.Max(minSeconds, maxSeconds));
        yield return brain.Wait(seconds);
    }
}

//=====================================================================
// 近づく
//=====================================================================

[Serializable, EnemyActionMenu("移動/近づく", 10)]
public class ApproachAction : EnemyAction
{
    [Label("ここまで近づく", 0.0f)]
    [Tooltip("プレイヤーとの距離がこれ以下になったら終わる")]
    public float stopDistance = 1.5f;

    [Label("最長の時間（秒）", 0.1f)]
    [Tooltip("この時間が経ったら、着いていなくても終わる")]
    public float maxSeconds = 3.0f;

    [Label("速さの倍率", 0.1f)]
    public float speedMultiplier = 1.0f;

    public override string Summary => $"近づく（{stopDistance} まで、最長 {maxSeconds} 秒）";

    public override IEnumerator Run(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = speedMultiplier;
        yield return brain.MoveUntil(() =>
        {
            GameObject target = brain.Enemy.Target;
            if(target == null) { return true; }
            if(brain.DistanceToTarget <= stopDistance) { return true; }
            brain.Enemy.MoveTo(target.transform.position, stopDistance * 0.9f);
            return false;
        }, maxSeconds);
        brain.Enemy.SpeedMultiplier = 1.0f;
        brain.Enemy.Stop();
    }

    public override void Cleanup(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = 1.0f;
    }
}

//=====================================================================
// 離れる
//=====================================================================

[Serializable, EnemyActionMenu("移動/離れる", 11)]
public class RetreatAction : EnemyAction
{
    [Label("ここまで離れる", 0.5f)]
    [Tooltip("プレイヤーとの距離がこれ以上になったら終わる")]
    public float keepDistance = 5.0f;

    [Label("最長の時間（秒）", 0.1f)]
    public float maxSeconds = 2.0f;

    [Label("速さの倍率", 0.1f)]
    public float speedMultiplier = 1.0f;

    public override string Summary => $"離れる（{keepDistance} まで、最長 {maxSeconds} 秒）";

    public override IEnumerator Run(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = speedMultiplier;

        // 行き止まりに逃げ込まないよう、歩ける場所の中からプレイヤーから遠い所を選ぶ
        Vector2? goal = brain.FindRetreatPoint(keepDistance);
        if(goal.HasValue)
        {
            brain.Enemy.MoveTo(goal.Value, 0.2f);
            yield return brain.MoveUntil(() => brain.DistanceToTarget >= keepDistance || !brain.Enemy.IsMoving, maxSeconds);
        }
        else
        {
            yield return brain.Wait(0.3f);
        }

        brain.Enemy.SpeedMultiplier = 1.0f;
        brain.Enemy.Stop();
    }

    public override void Cleanup(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = 1.0f;
    }
}

//=====================================================================
// 巡回
//=====================================================================

[Serializable, EnemyActionMenu("移動/巡回する", 12)]
public class PatrolAction : EnemyAction
{
    [Label("全部のポイントを回る")]
    [Tooltip("オン：巡回ポイントを全部回ってから次の行動へ ／ オフ：次のポイントへ1つ進んだら次の行動へ")]
    public bool allPoints = false;

    [Label("ポイントで待つ（秒）", 0.0f)]
    public float waitAtPoint = 0.5f;

    [Label("速さの倍率", 0.1f)]
    public float speedMultiplier = 1.0f;

    public override string Summary => allPoints ? "巡回する（全部のポイント）" : "巡回する（次のポイントへ）";

    public override IEnumerator Run(EnemyBrain brain)
    {
        int count = brain.PatrolPointCount;
        if(count == 0)
        {
            // 巡回ポイントがないときは少し待つ（シーンチェックで知らせる）
            yield return brain.Wait(0.5f);
            yield break;
        }

        brain.Enemy.SpeedMultiplier = speedMultiplier;

        int steps = allPoints ? count : 1;
        for(int i = 0; i < steps; i++)
        {
            Vector2 point = brain.NextPatrolPoint();
            brain.Enemy.MoveTo(point, 0.15f);
            yield return brain.MoveUntil(() => !brain.Enemy.IsMoving, 10.0f);
            brain.Enemy.Stop();
            yield return brain.Wait(waitAtPoint);
        }

        brain.Enemy.SpeedMultiplier = 1.0f;
    }

    public override void Cleanup(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = 1.0f;
    }
}

//=====================================================================
// ランダムに歩く
//=====================================================================

[Serializable, EnemyActionMenu("移動/ランダムに歩く", 13)]
public class WanderAction : EnemyAction
{
    [Label("歩き回る範囲（半径）", 0.5f)]
    [Tooltip("最初にいた場所から、この半径の中を歩く")]
    public float radius = 3.0f;

    [Label("歩く回数", 1)]
    public int count = 1;

    [Label("着いたら待つ（秒）", 0.0f)]
    public float waitAtPoint = 0.5f;

    [Label("速さの倍率", 0.1f)]
    public float speedMultiplier = 0.7f;

    public override string Summary => $"ランダムに歩く（半径 {radius}、{count} 回）";

    public override IEnumerator Run(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = speedMultiplier;

        for(int i = 0; i < Mathf.Max(1, count); i++)
        {
            Vector2? point = brain.FindRandomPoint(brain.HomePosition, radius);
            if(point.HasValue)
            {
                brain.Enemy.MoveTo(point.Value, 0.15f);
                yield return brain.MoveUntil(() => !brain.Enemy.IsMoving, 6.0f);
                brain.Enemy.Stop();
            }
            yield return brain.Wait(waitAtPoint);
        }

        brain.Enemy.SpeedMultiplier = 1.0f;
    }

    public override void Cleanup(EnemyBrain brain)
    {
        brain.Enemy.SpeedMultiplier = 1.0f;
    }
}

//=====================================================================
// 攻撃する
//=====================================================================

[Serializable, EnemyActionMenu("攻撃する", 20)]
public class AttackAction : EnemyAction
{
    [Label("攻撃")]
    [Tooltip("AttackData（予告 → 発動 → 後隙）")]
    public AttackData attack;

    [Label("回数", 1)]
    [Tooltip("続けて何回出すか")]
    public int repeat = 1;

    [Label("間隔（秒）", 0.0f)]
    [Tooltip("続けて出すときの、後隙のあとの待ち時間")]
    public float interval = 0.0f;

    public override string Summary => attack != null ? $"攻撃する（{attack.name}{(repeat > 1 ? $" ×{repeat}" : "")}）" : "攻撃する（攻撃が空）";

    protected override bool CanRunExtra(EnemyBrain brain) => attack != null;

    public override IEnumerator Run(EnemyBrain brain)
    {
        if(attack == null) { yield break; }

        for(int i = 0; i < Mathf.Max(1, repeat); i++)
        {
            var execution = new AttackExecution(brain, attack);
            brain.CurrentAttack = execution;
            yield return execution.Run();
            brain.CurrentAttack = null;

            if(i < repeat - 1) { yield return brain.Wait(interval); }
        }
    }

    public override void Cleanup(EnemyBrain brain)
    {
        if(brain.CurrentAttack != null) { brain.CurrentAttack.Cleanup(); }
        brain.CurrentAttack = null;
    }
}
