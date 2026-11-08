/*********************************************************************/
// * \file   AttackEmitter.cs
// * \brief  攻撃の発生源（体のない「見えない敵」。攻撃だけが襲ってくる場面に使う）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 攻撃の発生源（見えない敵）
/// </summary>
///
/// ・シーンに置いて、行動パターンを入れる。体がないので、行動のうち「待つ」「攻撃する」「仲間を呼ぶ」だけを使う（移動は飛ばす）
/// ・攻撃の起点が「敵の位置」なら、このオブジェクトの位置から出る。「プレイヤーの位置」なら、プレイヤーを狙って出る
///   （例：プレイヤーの足元から腕が突き出る ＝ 起点：プレイヤー、発動：下から突き出る、突き出る物：腕のプレハブ）
/// ・「動く条件」を満たしている間だけ動く（エリアにいる間・フラグが立つまで、など）
/// ・ギミックの「敵に命令」で、止める・動かす・段階を変える（攻撃を弱める）ことができる
/// ・会話中などは止まる（EnemyBlock）
public class AttackEmitter : PatternRunner
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("行動パターン")]
    [Tooltip("「待つ」「攻撃する」「仲間を呼ぶ」の並び。段階を使えば、ギミックを解くたびに弱くする（段階を変える）こともできる")]
    private EnemyPattern emitterPattern;

    [SerializeField, Label("最初から動く")]
    [Tooltip("オフにすると、ギミックの「敵に命令（動かす）」まで止まっている")]
    private bool startActive = true;

    [SerializeField, Label("動く条件")]
    [Tooltip("満たしている間だけ動く（空なら常に）。「プレイヤーがいる範囲」に Is Trigger の Collider2D を入れると、その中にいる間だけ攻撃してくる")]
    private GameCondition condition = new GameCondition();

    private bool stoppedByCommand;
    private bool conditionMet = true;

    //=====================================================================
    // PatternRunner
    //=====================================================================

    protected override bool CanActSelf => !stoppedByCommand && conditionMet;

    public override string StatusText
    {
        get
        {
            if(stoppedByCommand) { return "止まっている（命令）"; }
            if(!conditionMet) { return "止まっている（動く条件を満たしていない）"; }
            return base.StatusText;
        }
    }

    /// <summary> 命令で止められているか </summary>
    public bool Stopped => stoppedByCommand;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Start()
    {
        stoppedByCommand = !startActive;
        Begin(emitterPattern);
    }

    protected override void Update()
    {
        conditionMet = condition == null || condition.IsEmpty || condition.IsMet();

        // 止められた瞬間は、出している攻撃（予告など）をやめる
        if(!CanActSelf && CurrentAttack != null) { Interrupt(); }

        base.Update();
    }

    /// <summary>
    /// 止める・動かす（ギミックの命令から）
    /// </summary>
    public void SetStopped(bool stopped)
    {
        StopAllPauseRoutines();
        ApplyStopped(stopped);
    }

    private void ApplyStopped(bool stopped)
    {
        stoppedByCommand = stopped;
        if(stopped) { Interrupt(); }
    }

    /// <summary>
    /// 秒数だけ止める
    /// </summary>
    public void StopFor(float seconds)
    {
        StopAllPauseRoutines();
        ApplyStopped(true);
        pauseRoutine = StartCoroutine(ResumeAfter(seconds));
    }

    private Coroutine pauseRoutine;

    private void StopAllPauseRoutines()
    {
        if(pauseRoutine != null) { StopCoroutine(pauseRoutine); }
        pauseRoutine = null;
    }

    private System.Collections.IEnumerator ResumeAfter(float seconds)
    {
        float elapsed = 0.0f;
        while(elapsed < seconds)
        {
            if(!EnemyBlock.IsBlocked) { elapsed += Time.deltaTime; }
            yield return null;
        }
        pauseRoutine = null;
        ApplyStopped(false);
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1.0f, 0.3f, 0.8f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.8f);

        if(condition != null && condition.area != null)
        {
            Gizmos.color = new Color(1.0f, 0.3f, 0.8f, 0.15f);
            Bounds b = condition.area.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
