/*********************************************************************/
// * \file   EnemyBrain.cs
// * \brief  敵の行動パターンを実行する（敵の体がある場合）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の行動パターンを実行する
/// </summary>
///
/// ・共通プレハブ Enemy_Base に付いている。行動パターンは敵のデータ（EnemyData）のものを使う（ここで差し替えもできる）
/// ・巡回ポイントはシーンごとに違うので、ここ（シーンに置いた敵）に持たせる
/// ・段階の切り替え・行動の選択・待ち時間などは PatternRunner（見えない敵 AttackEmitter と共通）
[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class EnemyBrain : PatternRunner
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
    private int patrolIndex;
    private Vector2 home;

    //=====================================================================
    // PatternRunner
    //=====================================================================

    public override Enemy Enemy => enemy;

    public override bool IsAlive => enemy != null && !enemy.IsDead;

    protected override bool CanActSelf => enemy != null && !enemy.Stopped;

    protected override bool ShouldStart => wakeDistance <= 0.0f || DistanceToTarget <= wakeDistance;

    public override EnemySize Size => enemy != null ? enemy.Size : EnemySize.Medium;

    public override Vector2 HomePosition => home;

    public override string StatusText => Started ? base.StatusText : (Pattern != null ? "待機中（プレイヤーが近づくと動く）" : "行動パターンなし");

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        home = transform.position;
        if(enemy.Health != null) { enemy.Health.OnDied += OnDied; }

        Begin(overridePattern != null ? overridePattern : (enemy.Data != null ? enemy.Data.Pattern : null));
    }

    protected override void OnDestroy()
    {
        if(enemy != null && enemy.Health != null) { enemy.Health.OnDied -= OnDied; }
        base.OnDestroy();
    }

    private void OnDied()
    {
        End();

        // 呼び出した仲間を一緒に消す設定
        if(enemy.Data != null && enemy.Data.RemoveSummonedOnDeath) { RemoveSummoned(true); }
    }

    //=====================================================================
    // 巡回
    //=====================================================================

    public override int PatrolPointCount
    {
        get
        {
            int count = 0;
            foreach(Transform t in patrolPoints) { if(t != null) { count++; } }
            return count;
        }
    }

    public override Vector2 NextPatrolPoint()
    {
        for(int i = 0; i < patrolPoints.Count; i++)
        {
            Transform t = patrolPoints[patrolIndex % patrolPoints.Count];
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
            if(t != null) { return t.position; }
        }
        return transform.position;
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

        DrawActionRanges();
    }

    /// <summary>
    /// 行動の「この行動をする距離」（オレンジ）と「見える距離」（水色）を、最初の段階（再生中は今の段階）の分だけ円で出す
    /// </summary>
    private void DrawActionRanges()
    {
#if UNITY_EDITOR
        Enemy self = enemy != null ? enemy : GetComponent<Enemy>();
        EnemyPattern usePattern = Pattern != null ? Pattern : (overridePattern != null ? overridePattern : (self != null && self.Data != null ? self.Data.Pattern : null));
        if(usePattern == null || usePattern.phases.Count == 0) { return; }

        int index = Mathf.Clamp(Application.isPlaying ? PhaseIndex : 0, 0, usePattern.phases.Count - 1);
        EnemyPhase phase = usePattern.phases[index];
        if(phase == null || phase.actions == null) { return; }

        var drawn = new List<float>();
        Vector3 center = transform.position;
        foreach(EnemyAction action in phase.actions)
        {
            if(action == null) { continue; }
            string name = action.Summary;

            if(action.distanceCheck != DistanceCheck.Any)
            {
                DrawRange(center, action.distance, new Color(1.0f, 0.6f, 0.2f, 0.8f), $"{name}：{(action.distanceCheck == DistanceCheck.Closer ? "この内側で" : "この外側で")}", drawn);
            }
            if(action.sightCheck != SightCheck.Any)
            {
                DrawRange(center, action.sightRange, new Color(0.3f, 0.85f, 1.0f, 0.7f), $"{name}：見える距離", drawn);
            }
            if(action is ChaseAction chase)
            {
                DrawRange(center, chase.chaseSightRange, new Color(0.3f, 0.85f, 1.0f, 0.4f), "追いかけ中に見える距離", drawn);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private static void DrawRange(Vector3 center, float radius, Color color, string label, List<float> drawn)
    {
        if(radius <= 0.0f) { return; }

        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, radius);

        // 同じ半径の文字が重ならないよう、少しずつ下へずらす
        int same = 0;
        foreach(float r in drawn) { if(Mathf.Abs(r - radius) < 0.05f) { same++; } }
        drawn.Add(radius);

        var style = new GUIStyle(UnityEditor.EditorStyles.miniLabel);
        style.normal.textColor = color;
        UnityEditor.Handles.Label(center + new Vector3(radius * 0.71f, radius * 0.71f - same * 0.35f), label, style);
    }
#endif
}
