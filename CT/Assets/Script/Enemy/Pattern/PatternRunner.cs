/*********************************************************************/
// * \file   PatternRunner.cs
// * \brief  行動パターンを実行する共通部分（敵の EnemyBrain と、見えない敵の AttackEmitter が使う）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行動パターンを実行する共通部分
/// </summary>
///
/// ・段階の切り替え、行動の選択、待ち時間（止められている間は数えない）、途中でやめる（Interrupt）
/// ・敵の体がない（AttackEmitter）ときは Enemy が null。移動の行動は飛ばし、攻撃はこのオブジェクトの位置から出す
/// ・派生クラスは Pattern・CanActSelf・IsAlive などを決める
public abstract class PatternRunner : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    protected EnemyPattern pattern;

    private int phaseIndex;
    private float phaseTime;
    private int sequentialIndex;
    private bool started;
    private bool phaseChangePending;
    private int forcedPhase = -1;

    private Coroutine loop;
    private Coroutine actionRoutine;
    private EnemyAction currentAction;
    private EnemyAction forcedAction;

    private EnemyBulletPool bulletPool;

    // 呼び出した仲間（同時に出せる数の管理・親が倒れたら消すため）
    private readonly List<Enemy> summoned = new List<Enemy>();

    //=====================================================================
    // 派生クラスで決めること
    //=====================================================================

    /// <summary> 敵の体（なければ null。見えない敵など） </summary>
    public virtual Enemy Enemy => null;

    /// <summary> 生きているか（倒れたらループを終わる） </summary>
    public virtual bool IsAlive => true;

    /// <summary> このオブジェクトだけの「止まる」条件（命令で止められた・範囲の外など） </summary>
    protected virtual bool CanActSelf => true;

    /// <summary> 動き出してよいか（プレイヤーが近づいたら、など） </summary>
    protected virtual bool ShouldStart => true;

    /// <summary> 敵の大きさ（経路探索に使う） </summary>
    public virtual EnemySize Size => EnemySize.Medium;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public EnemyPattern Pattern => pattern;

    public int PhaseIndex => phaseIndex;

    public bool Started => started;

    /// <summary> 実行中の攻撃（途中でやめるときの片づけ用） </summary>
    public AttackExecution CurrentAttack { get; set; }

    /// <summary> 会話中・イベント中なども止まらない（イベントの部品から出す攻撃など） </summary>
    public bool IgnoreEnemyBlock { get; set; }

    /// <summary> 動ける状態か（会話中・止められている・倒れている、のどれでもない） </summary>
    public bool CanAct => IsAlive && CanActSelf && (IgnoreEnemyBlock || !EnemyBlock.IsBlocked);

    /// <summary> 攻撃を出す位置 </summary>
    public Vector2 Position => transform.position;

    /// <summary> 向いている方向（敵の体があればその向き、なければこのオブジェクトの右） </summary>
    public virtual Vector2 Facing => Enemy != null ? Enemy.Facing : (Vector2)transform.right;

    /// <summary> 狙う相手（操作中のキャラ） </summary>
    public GameObject Target
    {
        get
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            return manager != null ? manager.CurrentPlayer : null;
        }
    }

    /// <summary> 狙う相手までの距離（いなければとても大きい値） </summary>
    public float DistanceToTarget
    {
        get
        {
            GameObject target = Target;
            return target != null ? Vector2.Distance(transform.position, target.transform.position) : float.MaxValue;
        }
    }

    /// <summary> 攻撃した人として記録するオブジェクト </summary>
    public GameObject Attacker => gameObject;

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
    public virtual string StatusText
    {
        get
        {
            if(pattern == null) { return currentAction != null ? $"行動パターンなし：{currentAction.Summary}" : "行動パターンなし"; }
            if(!started) { return "待機中"; }
            if(!CanActSelf) { return "止まっている"; }

            string phase = phaseIndex < pattern.phases.Count ? pattern.phases[phaseIndex].name : "";
            string action = currentAction != null ? currentAction.Summary : "-";
            return $"{phase}：{action}";
        }
    }

    //=====================================================================
    // 開始・終了
    //=====================================================================

    /// <summary>
    /// 行動パターンを始める（派生クラスの Start から呼ぶ）
    /// </summary>
    protected void Begin(EnemyPattern usePattern)
    {
        pattern = usePattern;
        if(pattern != null && pattern.phases.Count > 0 && loop == null)
        {
            loop = StartCoroutine(Loop());
        }
    }

    /// <summary>
    /// 行動パターンを終える（倒れた・消える）
    /// </summary>
    protected void End()
    {
        if(loop != null) { StopCoroutine(loop); }
        loop = null;
        StopCurrentAction();
    }

    protected virtual void OnDisable()
    {
        StopCurrentAction();
    }

    protected virtual void OnDestroy()
    {
        StopCurrentAction();
    }

    //=====================================================================
    // 段階の切り替え
    //=====================================================================

    protected virtual void Update()
    {
        if(pattern == null || !started || !IsAlive) { return; }
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
                Health health = Enemy != null ? Enemy.Health : GetComponent<Health>();
                return health != null && health.MaxHealth > 0 && (float)health.CurrentHealth / health.MaxHealth <= phase.healthRatio;

            case PhaseExit.Time:
                return phaseTime >= phase.seconds;

            case PhaseExit.Condition:
                return phase.condition != null && !phase.condition.IsEmpty && phase.condition.IsMet();
        }
        return false;
    }

    /// <summary>
    /// 段階を指定して切り替える（ギミックの命令など。番号は 0 から）。今の行動はやめる
    /// </summary>
    public void SetPhase(int index)
    {
        if(pattern == null || pattern.phases.Count == 0) { return; }

        forcedPhase = Mathf.Clamp(index, 0, pattern.phases.Count - 1);
        phaseChangePending = true;
        Interrupt();
    }

    private IEnumerator EnterNextPhase()
    {
        phaseChangePending = false;
        phaseIndex = forcedPhase >= 0 ? forcedPhase : Mathf.Min(phaseIndex + 1, pattern.phases.Count - 1);
        forcedPhase = -1;
        phaseTime = 0.0f;
        sequentialIndex = 0;

        EnemyPhase phase = pattern.phases[phaseIndex];
        EffectManager.Play(phase.enterEffect, transform.position);
        SeManager.PlaySe(phase.enterSe, transform.position);
        PlayAction(phase.enterAnimation);
        if(Enemy != null) { Enemy.Stop(); }

        yield return Wait(phase.enterPause);
    }

    //=====================================================================
    // 行動のループ
    //=====================================================================

    private IEnumerator Loop()
    {
        while(!ShouldStart) { yield return new WaitForSeconds(0.2f); }
        started = true;

        while(IsAlive)
        {
            if(phaseChangePending) { yield return EnterNextPhase(); }

            if(!CanAct)
            {
                yield return null;
                continue;
            }

            EnemyPhase phase = pattern.phases[phaseIndex];
            // デバッグなどで「次はこれ」と指定された行動があれば、条件に関係なくそれをする
            EnemyAction action = forcedAction != null ? forcedAction : PickAction(phase);
            forcedAction = null;
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
    /// 今の行動を途中でやめる（段階の切り替え・命令・ひるみなど）。次の行動はループが選ぶ
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

        if(Enemy != null)
        {
            Enemy.ClearOverrideVelocity();
            Enemy.SpeedMultiplier = 1.0f;
            Enemy.Stop();
        }
    }

    //=====================================================================
    // 行動の部品から使う関数
    //=====================================================================

    /// <summary>
    /// 秒数だけ待つ（止められている間は数えない）
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
    /// 1回だけの動き（構え・攻撃など）を流す（敵の体があるときだけ）
    /// </summary>
    public void PlayAction(string clip)
    {
        if(Enemy != null) { Enemy.PlayAction(clip); }
    }

    /// <summary>
    /// 1つの攻撃を出す（行動パターンの外から。ギミックのダメージエリアなど）
    /// </summary>
    public Coroutine RunAttack(AttackData attack)
    {
        if(attack == null) { return null; }

        var execution = new AttackExecution(this, attack);
        CurrentAttack = execution;
        return StartCoroutine(RunAttackRoutine(execution));
    }

    private IEnumerator RunAttackRoutine(AttackExecution execution)
    {
        yield return execution.Run();
        if(CurrentAttack == execution) { CurrentAttack = null; }
    }

    /// <summary>
    /// 今の行動をやめて、すぐにこの攻撃を出させる（デバッグウィンドウの「この攻撃を出させる」）
    /// </summary>
    /// ・行動パターンが動いていれば、次の行動として割り込む（終わったらいつもの行動に戻る）
    /// ・行動パターンがない（動いていない）ときは、その場で1回だけ出す
    public void ForceAttack(AttackData attack)
    {
        if(attack == null || !IsAlive) { return; }

        var action = new AttackAction { attack = attack };
        if(loop != null && started)
        {
            forcedAction = action;
            Interrupt();
            return;
        }

        StopCurrentAction();
        currentAction = action;
        actionRoutine = StartCoroutine(RunForcedAction(action));
    }

    private IEnumerator RunForcedAction(EnemyAction action)
    {
        yield return action.Run(this);
        if(currentAction == action) { currentAction = null; }
        actionRoutine = null;
    }

    //=====================================================================
    // 仲間を呼ぶ
    //=====================================================================

    /// <summary> 今いる、呼び出した仲間の数 </summary>
    public int SummonedCount
    {
        get
        {
            summoned.RemoveAll(e => e == null || e.IsDead);
            return summoned.Count;
        }
    }

    public void AddSummoned(Enemy enemy)
    {
        if(enemy != null) { summoned.Add(enemy); }
    }

    /// <summary>
    /// 呼び出した仲間を消す（倒されたときに一緒に消す設定のとき）
    /// </summary>
    protected void RemoveSummoned(bool killWithEffect)
    {
        foreach(Enemy e in summoned)
        {
            if(e == null || e.IsDead) { continue; }
            if(killWithEffect) { e.Kill(); }
            else               { Destroy(e.gameObject); }
        }
        summoned.Clear();
    }

    //=====================================================================
    // 場所を探す
    //=====================================================================

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
            if(nav.SamplePosition(candidate, 0.5f, Size, out Vector2 result)) { return result; }
        }
        return null;
    }

    /// <summary>
    /// プレイヤーから離れられる、歩ける場所を選ぶ（行き止まりに逃げ込まないよう、たどり着ける所だけ）
    /// </summary>
    public Vector2? FindRetreatPoint(float keepDistance)
    {
        GameObject target = Target;
        if(target == null) { return null; }

        Vector2 me = transform.position;
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
                if(!nav.SamplePosition(candidate, 1.0f, Size, out candidate)) { continue; }
                if(!nav.IsReachable(me, candidate, Size)) { continue; }
            }

            float score = Vector2.Distance(candidate, player) - Mathf.Abs(angle) * 0.01f;
            if(score > bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    //=====================================================================
    // 視界
    //=====================================================================

    /// <summary> 最後にプレイヤーを見た場所（見たことがなければ null） </summary>
    public Vector2? LastSeenPosition { get; private set; }

    /// <summary>
    /// プレイヤーが見えるか（range 以内で、間に壁がない）。見えたら「最後に見た場所」を更新する
    /// </summary>
    public bool CanSeeTarget(float range)
    {
        GameObject target = Target;
        if(target == null) { return false; }

        Vector2 from = transform.position;
        Vector2 to = target.transform.position;
        if(Vector2.Distance(from, to) > range) { return false; }

        // 間に壁（動かない当たり判定）があれば見えない。敵だけ入れないエリアなどは見通せる
        foreach(RaycastHit2D hit in Physics2D.LinecastAll(from, to))
        {
            if(hit.collider != null && NavMesh2D.IsObstacle(hit.collider)) { return false; }
        }

        LastSeenPosition = to;
        return true;
    }

    /// <summary> 最後に見た場所を忘れる（探し終わった・やり直し） </summary>
    public void ForgetLastSeen()
    {
        LastSeenPosition = null;
    }

    // 巡回（体のある敵だけ。EnemyBrain が上書きする）
    public virtual int PatrolPointCount => 0;
    public virtual Vector2 NextPatrolPoint() => transform.position;
    public virtual Vector2 HomePosition => transform.position;
}
