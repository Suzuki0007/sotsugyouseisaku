/*********************************************************************/
// * \file   EnemyBrain.cs
// * \brief  敵の行動パターンを実行する（段階の切り替え・行動の選択・待ち時間）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の行動パターンを実行する
/// </summary>
///
/// ・共通プレハブ Enemy_Base に付いている。行動パターンは敵のデータ（EnemyData）のものを使う（ここで差し替えもできる）
/// ・巡回ポイントはシーンごとに違うので、ここ（シーンに置いた敵）に持たせる
/// ・敵が止められている間（会話中・Paused など）は、待ち時間も攻撃も進まない
/// ・段階が変わったとき、行動を途中でやめるときは Interrupt（ひるみなど、ほかの仕組みからも呼べる）
[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("行動パターンを差し替える")]
    [Tooltip("空なら、敵のデータに入っている行動パターンを使う。この敵だけ違う動きにしたいときに入れる")]
    private EnemyPattern overridePattern;

    [SerializeField]
    [Tooltip("巡回ポイント（「巡回する」行動で、上から順に回る）。空のオブジェクトを置いて入れる")]
    private List<Transform> patrolPoints = new List<Transform>();

    [SerializeField, Label("動き出す距離", 0.0f)]
    [Tooltip("プレイヤーがこの距離まで近づいたら動き出す。0 なら最初から動く")]
    private float wakeDistance = 0.0f;

    private Enemy enemy;
    private EnemyPattern pattern;

    private int phaseIndex;
    private float phaseTime;
    private int sequentialIndex;
    private int patrolIndex;
    private bool awake;
    private bool phaseChangePending;

    private Coroutine loop;
    private Coroutine actionRoutine;
    private EnemyAction currentAction;

    private EnemyBulletPool bulletPool;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public Enemy Enemy => enemy;

    public EnemyPattern Pattern => pattern;

    /// <summary> 今の段階の番号（0 から） </summary>
    public int PhaseIndex => phaseIndex;

    /// <summary> 最初にいた場所（「ランダムに歩く」の中心） </summary>
    public Vector2 HomePosition { get; private set; }

    /// <summary> 実行中の攻撃（途中でやめるときの片づけ用） </summary>
    public AttackExecution CurrentAttack { get; set; }

    public bool CanAct => enemy != null && enemy.CanAct;

    /// <summary> 狙う相手までの距離（いなければとても大きい値） </summary>
    public float DistanceToTarget
    {
        get
        {
            GameObject target = enemy.Target;
            return target != null ? Vector2.Distance(enemy.transform.position, target.transform.position) : float.MaxValue;
        }
    }

    public int PatrolPointCount
    {
        get
        {
            int count = 0;
            foreach(Transform t in patrolPoints) { if(t != null) { count++; } }
            return count;
        }
    }

    /// <summary> シーンの敵の弾のプール（弾を撃つ攻撃で使う） </summary>
    public EnemyBulletPool BulletPool
    {
        get
        {
            if(bulletPool == null) { bulletPool = FindAnyObjectByType<EnemyBulletPool>(); }
            return bulletPool;
        }
    }

    /// <summary> デバッグ表示用の今の状態 </summary>
    public string StatusText
    {
        get
        {
            if(pattern == null) { return "行動パターンなし"; }
            if(!awake) { return "待機中（プレイヤーが近づくと動く）"; }

            string phase = phaseIndex < pattern.phases.Count ? pattern.phases[phaseIndex].name : "";
            string action = currentAction != null ? currentAction.Summary : "-";
            return $"{phase}：{action}";
        }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        HomePosition = transform.position;
        pattern = overridePattern != null ? overridePattern : (enemy.Data != null ? enemy.Data.Pattern : null);

        if(enemy.Health != null) { enemy.Health.OnDied += OnDied; }

        if(pattern != null && pattern.phases.Count > 0)
        {
            loop = StartCoroutine(Loop());
        }
    }

    private void OnDestroy()
    {
        if(enemy != null && enemy.Health != null) { enemy.Health.OnDied -= OnDied; }
        StopCurrentAction();
    }

    private void OnDisable()
    {
        StopCurrentAction();
    }

    private void OnDied()
    {
        if(loop != null) { StopCoroutine(loop); }
        loop = null;
        StopCurrentAction();
    }

    //=====================================================================
    // 段階の切り替え
    //=====================================================================

    private void Update()
    {
        if(pattern == null || !awake || enemy.IsDead) { return; }
        if(CanAct) { phaseTime += Time.deltaTime; }

        if(phaseChangePending) { return; }
        if(phaseIndex >= pattern.phases.Count - 1) { return; }

        EnemyPhase phase = pattern.phases[phaseIndex];
        if(!IsExitMet(phase)) { return; }

        // 次の段階へ。今の行動をやめるなら、ここでやめる（ループが次の段階の行動を選ぶ）
        phaseChangePending = true;
        if(phase.interrupt) { Interrupt(); }
    }

    private bool IsExitMet(EnemyPhase phase)
    {
        switch(phase.exit)
        {
            case PhaseExit.HealthBelow:
                Health health = enemy.Health;
                return health != null && health.MaxHealth > 0 && (float)health.CurrentHealth / health.MaxHealth <= phase.healthRatio;

            case PhaseExit.Time:
                return phaseTime >= phase.seconds;

            case PhaseExit.Condition:
                return phase.condition != null && !phase.condition.IsEmpty && phase.condition.IsMet();
        }
        return false;
    }

    private IEnumerator EnterNextPhase()
    {
        phaseChangePending = false;
        phaseIndex = Mathf.Min(phaseIndex + 1, pattern.phases.Count - 1);
        phaseTime = 0.0f;
        sequentialIndex = 0;

        EnemyPhase phase = pattern.phases[phaseIndex];
        EffectManager.Play(phase.enterEffect, transform.position);
        SeManager.PlaySe(phase.enterSe, transform.position);
        enemy.PlayAction(phase.enterAnimation);
        enemy.Stop();

        yield return Wait(phase.enterPause);
    }

    //=====================================================================
    // 行動のループ
    //=====================================================================

    private IEnumerator Loop()
    {
        // 動き出す距離まで待つ
        while(!awake)
        {
            awake = wakeDistance <= 0.0f || DistanceToTarget <= wakeDistance;
            if(!awake) { yield return new WaitForSeconds(0.2f); }
        }

        while(!enemy.IsDead)
        {
            if(phaseChangePending) { yield return EnterNextPhase(); }

            EnemyPhase phase = pattern.phases[phaseIndex];
            EnemyAction action = PickAction(phase);
            if(action == null)
            {
                yield return Wait(0.5f);
                continue;
            }

            currentAction = action;
            bool finished = false;
            actionRoutine = StartCoroutine(RunAction(action, () => finished = true));
            while(!finished && actionRoutine != null) { yield return null; }
            actionRoutine = null;
            currentAction = null;

            // 何もせずに一瞬で終わる行動が続いて固まらないように、1フレーム待つ
            yield return null;
        }
    }

    private IEnumerator RunAction(EnemyAction action, Action done)
    {
        yield return action.Run(this);
        done();
    }

    /// <summary>
    /// 今の段階から、次にする行動を選ぶ（できるものがなければ null）
    /// </summary>
    private EnemyAction PickAction(EnemyPhase phase)
    {
        List<EnemyAction> actions = phase.actions;
        if(actions == null || actions.Count == 0) { return null; }

        switch(phase.order)
        {
            case ActionOrder.Sequential:
                for(int i = 0; i < actions.Count; i++)
                {
                    EnemyAction a = actions[(sequentialIndex + i) % actions.Count];
                    if(a != null && a.CanRun(this))
                    {
                        sequentialIndex = (sequentialIndex + i + 1) % actions.Count;
                        return a;
                    }
                }
                return null;

            case ActionOrder.Random:
            {
                var candidates = new List<EnemyAction>();
                foreach(EnemyAction a in actions) { if(a != null && a.CanRun(this)) { candidates.Add(a); } }
                return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
            }

            default:
            {
                float total = 0.0f;
                foreach(EnemyAction a in actions) { if(a != null && a.CanRun(this)) { total += Mathf.Max(0.0f, a.weight); } }
                if(total <= 0.0f) { return null; }

                float pick = UnityEngine.Random.Range(0.0f, total);
                foreach(EnemyAction a in actions)
                {
                    if(a == null || !a.CanRun(this)) { continue; }
                    pick -= Mathf.Max(0.0f, a.weight);
                    if(pick <= 0.0f) { return a; }
                }
                return null;
            }
        }
    }

    /// <summary>
    /// 今の行動を途中でやめる（段階の切り替え・ひるみなど）。次の行動はループが選ぶ
    /// </summary>
    public void Interrupt()
    {
        StopCurrentAction();
    }

    private void StopCurrentAction()
    {
        if(actionRoutine != null) { StopCoroutine(actionRoutine); }
        actionRoutine = null;

        if(currentAction != null) { currentAction.Cleanup(this); }
        currentAction = null;

        if(CurrentAttack != null) { CurrentAttack.Cleanup(); }
        CurrentAttack = null;

        if(enemy != null)
        {
            enemy.ClearOverrideVelocity();
            enemy.SpeedMultiplier = 1.0f;
            enemy.Stop();
        }
    }

    //=====================================================================
    // 行動の部品から使う関数
    //=====================================================================

    /// <summary>
    /// 秒数だけ待つ（敵が止められている間は数えない）
    /// </summary>
    public IEnumerator Wait(float seconds)
    {
        float elapsed = 0.0f;
        while(elapsed < seconds)
        {
            if(CanAct) { elapsed += Time.deltaTime; }
            yield return null;
        }
    }

    /// <summary>
    /// done が true を返すか、maxSeconds が経つまで毎フレーム待つ（止められている間は数えない）
    /// </summary>
    public IEnumerator MoveUntil(Func<bool> done, float maxSeconds)
    {
        float elapsed = 0.0f;
        while(elapsed < maxSeconds)
        {
            if(CanAct)
            {
                if(done()) { yield break; }
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
    }

    /// <summary>
    /// 次の巡回ポイント（順番に回る）
    /// </summary>
    public Vector2 NextPatrolPoint()
    {
        for(int i = 0; i < patrolPoints.Count; i++)
        {
            Transform t = patrolPoints[patrolIndex % patrolPoints.Count];
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
            if(t != null) { return t.position; }
        }
        return transform.position;
    }

    /// <summary>
    /// center のまわりの、歩ける場所をランダムに選ぶ
    /// </summary>
    public Vector2? FindRandomPoint(Vector2 center, float radius)
    {
        NavMesh2D nav = NavMesh2D.Instance;
        for(int i = 0; i < 8; i++)
        {
            Vector2 candidate = center + UnityEngine.Random.insideUnitCircle * radius;
            if(nav == null) { return candidate; }
            if(nav.SamplePosition(candidate, 0.5f, enemy.Size, out Vector2 result)) { return result; }
        }
        return null;
    }

    /// <summary>
    /// プレイヤーから離れられる、歩ける場所を選ぶ（行き止まりに逃げ込まないよう、たどり着ける所だけ）
    /// </summary>
    public Vector2? FindRetreatPoint(float keepDistance)
    {
        GameObject target = enemy.Target;
        if(target == null) { return null; }

        Vector2 me = enemy.transform.position;
        Vector2 player = target.transform.position;
        Vector2 away = (me - player).sqrMagnitude > 0.0001f ? (me - player).normalized : UnityEngine.Random.insideUnitCircle.normalized;

        NavMesh2D nav = NavMesh2D.Instance;
        Vector2? best = null;
        float bestScore = float.MinValue;

        for(int i = 0; i < 12; i++)
        {
            // プレイヤーと反対向きを中心に、左右 ±120 度の中から探す
            float angle = Mathf.Lerp(-120.0f, 120.0f, i / 11.0f);
            Vector2 dir = Quaternion.Euler(0.0f, 0.0f, angle) * away;
            Vector2 candidate = me + dir * keepDistance;

            if(nav != null)
            {
                if(!nav.SamplePosition(candidate, 1.0f, enemy.Size, out candidate)) { continue; }
                if(!nav.IsReachable(me, candidate, enemy.Size)) { continue; }
            }

            float score = Vector2.Distance(candidate, player) - Mathf.Abs(angle) * 0.01f;
            if(score > bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmosSelected()
    {
        // 巡回ポイントのつながりと、動き出す距離
        Gizmos.color = new Color(0.3f, 1.0f, 0.4f, 0.9f);
        Vector3? prev = null;
        Vector3? first = null;
        foreach(Transform t in patrolPoints)
        {
            if(t == null) { continue; }
            Gizmos.DrawWireSphere(t.position, 0.25f);
            if(prev.HasValue) { Gizmos.DrawLine(prev.Value, t.position); }
            if(!first.HasValue) { first = t.position; }
            prev = t.position;
        }
        if(prev.HasValue && first.HasValue && PatrolPointCount > 2) { Gizmos.DrawLine(prev.Value, first.Value); }

        if(wakeDistance > 0.0f)
        {
            Gizmos.color = new Color(1.0f, 1.0f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, wakeDistance);
        }
    }
}
