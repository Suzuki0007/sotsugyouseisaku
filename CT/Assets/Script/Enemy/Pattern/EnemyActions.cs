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
    public bool CanRun(PatternRunner brain)
    {
        if(distanceCheck == DistanceCheck.Closer  && brain.DistanceToTarget > distance)  { return false; }
        if(distanceCheck == DistanceCheck.Farther && brain.DistanceToTarget <= distance) { return false; }
        return CanRunExtra(brain);
    }

    protected virtual bool CanRunExtra(PatternRunner brain) => true;

    /// <summary> 行動する（コルーチン） </summary>
    public abstract IEnumerator Run(PatternRunner brain);

    /// <summary> 途中でやめられたときの片づけ </summary>
    public virtual void Cleanup(PatternRunner brain) { }
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

    public override IEnumerator Run(PatternRunner brain)
    {
        if(brain.Enemy != null) { brain.Enemy.Stop(); }
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

    public override IEnumerator Run(PatternRunner brain)
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

    public override void Cleanup(PatternRunner brain)
    {
        if(brain.Enemy != null) { brain.Enemy.SpeedMultiplier = 1.0f; }
    }

    // 移動は、敵の体があるときだけ（見えない敵では飛ばす）
    protected override bool CanRunExtra(PatternRunner brain) => brain.Enemy != null;
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

    public override IEnumerator Run(PatternRunner brain)
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

    public override void Cleanup(PatternRunner brain)
    {
        if(brain.Enemy != null) { brain.Enemy.SpeedMultiplier = 1.0f; }
    }

    // 移動は、敵の体があるときだけ（見えない敵では飛ばす）
    protected override bool CanRunExtra(PatternRunner brain) => brain.Enemy != null;
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

    public override IEnumerator Run(PatternRunner brain)
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

    public override void Cleanup(PatternRunner brain)
    {
        if(brain.Enemy != null) { brain.Enemy.SpeedMultiplier = 1.0f; }
    }

    // 移動は、敵の体があるときだけ（見えない敵では飛ばす）
    protected override bool CanRunExtra(PatternRunner brain) => brain.Enemy != null;
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

    public override IEnumerator Run(PatternRunner brain)
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

    public override void Cleanup(PatternRunner brain)
    {
        if(brain.Enemy != null) { brain.Enemy.SpeedMultiplier = 1.0f; }
    }

    // 移動は、敵の体があるときだけ（見えない敵では飛ばす）
    protected override bool CanRunExtra(PatternRunner brain) => brain.Enemy != null;
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

    protected override bool CanRunExtra(PatternRunner brain) => attack != null;

    public override IEnumerator Run(PatternRunner brain)
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

    public override void Cleanup(PatternRunner brain)
    {
        if(brain.CurrentAttack != null) { brain.CurrentAttack.Cleanup(); }
        brain.CurrentAttack = null;
    }
}

//=====================================================================
// 仲間を呼ぶ
//=====================================================================

/// <summary>
/// 仲間を呼ぶときの出る場所
/// </summary>
public enum SummonPlace
{
    [InspectorName("自分のまわり")]
    AroundSelf = 0,

    [InspectorName("プレイヤーのまわり")]
    AroundPlayer = 1,

    [InspectorName("自分のまわりに卵を置く（時間でかえる。壊せる）")]
    Egg = 2,
}

[Serializable, EnemyActionMenu("仲間を呼ぶ", 30)]
public class SummonAction : EnemyAction
{
    [Label("呼ぶ敵")]
    [Tooltip("出てくる敵のデータ（EnemyData）")]
    public EnemyData enemy;

    [Label("1回に呼ぶ数", 1)]
    public int count = 2;

    [Label("同時にいられる数", 1)]
    [Tooltip("呼び出した仲間がこの数以上いるときは、この行動を飛ばす")]
    public int maxAlive = 4;

    [Label("出る場所")]
    public SummonPlace place = SummonPlace.AroundSelf;

    [Label("距離", 0.0f)]
    [Tooltip("出る場所の中心からの距離（マス）")]
    public float radius = 2.5f;

    [Label("予告の時間（秒）", 0.0f)]
    [Tooltip("出る場所に丸を見せてから出す。0 ならすぐ出る")]
    public float telegraphTime = 0.8f;

    [Label("予告の色")]
    public Color telegraphColor = new Color(0.6f, 0.3f, 1.0f, 0.35f);

    [Label("卵がかえるまで（秒）", 0.1f)]
    public float hatchTime = 3.0f;

    [Label("卵の体力", 1)]
    [Tooltip("この回数分攻撃すると、かえる前に壊せる")]
    public int eggHealth = 2;

    [Label("卵の絵")]
    [Tooltip("空なら丸で表示")]
    public Sprite eggSprite;

    [Label("出たときのエフェクト")]
    public EffectData spawnEffect;

    [Label("出たときの SE")]
    public SeData spawnSe;

    [Label("呼ぶときの敵のアニメ")]
    public string animation = "Summon";

    public override string Summary => enemy != null ? $"仲間を呼ぶ（{enemy.DisplayName}×{count}）" : "仲間を呼ぶ（敵が空）";

    protected override bool CanRunExtra(PatternRunner brain) => enemy != null && brain.SummonedCount < maxAlive;

    public override IEnumerator Run(PatternRunner brain)
    {
        if(enemy == null) { yield break; }
        if(brain.Enemy != null) { brain.Enemy.Stop(); }
        brain.PlayAction(animation);

        // 出る場所を決める（歩ける場所だけ）
        Vector2 center = brain.Position;
        if(place == SummonPlace.AroundPlayer && brain.Target != null) { center = brain.Target.transform.position; }

        int n = Mathf.Min(Mathf.Max(1, count), Mathf.Max(0, maxAlive - brain.SummonedCount));
        var points = new System.Collections.Generic.List<Vector2>();
        NavMesh2D nav = NavMesh2D.Instance;
        for(int i = 0; i < n; i++)
        {
            float angle = 360.0f * i / n + UnityEngine.Random.Range(-20.0f, 20.0f);
            Vector2 p = center + (Vector2)(Quaternion.Euler(0.0f, 0.0f, angle) * Vector2.right) * radius;
            if(nav != null && !nav.SamplePosition(p, 1.5f, enemy.Size, out p)) { continue; }
            points.Add(p);
        }

        // 予告
        var views = new System.Collections.Generic.List<TelegraphView>();
        if(telegraphTime > 0.0f)
        {
            foreach(Vector2 p in points) { views.Add(TelegraphView.ShowCircle(p, 0.6f, telegraphColor)); }
            float elapsed = 0.0f;
            while(elapsed < telegraphTime)
            {
                if(brain.CanAct) { elapsed += Time.deltaTime; }
                foreach(TelegraphView v in views) { if(v != null) { v.SetProgress(elapsed / telegraphTime); } }
                yield return null;
            }
            foreach(TelegraphView v in views) { if(v != null) { v.Hide(); } }
        }

        // 出す
        foreach(Vector2 p in points)
        {
            EffectManager.Play(spawnEffect, p);
            SeManager.PlaySe(spawnSe, p);

            if(place == SummonPlace.Egg)
            {
                EnemyEgg.Create(enemy, p, hatchTime, eggHealth, eggSprite, brain, spawnEffect, spawnSe);
            }
            else
            {
                Enemy spawned = Enemy.Spawn(enemy, p);
                brain.AddSummoned(spawned);
            }
        }

        yield return brain.Wait(0.3f);
    }
}
