/*********************************************************************/
// * \file   EnemyDataSpawner.cs
// * \brief  敵のスポナー（新しい作りの敵を、敵のデータから出す）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// どの敵を出すかの選び方
/// </summary>
public enum SpawnPick
{
    [InspectorName("上から順番に")]
    Sequential = 0,

    [InspectorName("ランダム（重みつき）")]
    Random = 1,
}

/// <summary>
/// 出す敵1種類
/// </summary>
[Serializable]
public class SpawnEntry
{
    [Label("敵")]
    public EnemyData enemy;

    [Label("重み", 0.0f)]
    [Tooltip("選び方が「ランダム」のときの出やすさ")]
    public float weight = 1.0f;
}

/// <summary>
/// 敵のスポナー（新しい作り）
/// </summary>
///
/// ・敵のデータを入れるだけで、条件を満たしている間、間隔をあけて敵を出す（Hierarchy 右クリック > 敵 > スポナー）
/// ・何種類でも入れられる（順番・ランダム）。同時にいる数・合計で出す数を決められる
/// ・会話中などで敵が止まっている間は、時間が進まない
/// ・古い EnemySpawner（プレハブを出す）はそのまま使える
public class EnemyDataSpawner : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("出す敵（敵のデータ）。何種類でも入れられる")]
    private List<SpawnEntry> enemies = new List<SpawnEntry>();

    [SerializeField, Label("選び方")]
    private SpawnPick pick = SpawnPick.Sequential;

    [Header("出し方")]

    [SerializeField, Label("同時にいる数", 1)]
    [Tooltip("このスポナーから出た敵が、この数より少ないときだけ次を出す")]
    private int maxAlive = 1;

    [SerializeField, Label("合計で出す数", 0)]
    [Tooltip("この数を出したら終わり。0 なら何体でも出す")]
    private int totalCount = 0;

    [SerializeField, Label("1回に出す数", 1)]
    [Tooltip("同時にいる数を超えない分だけ出す")]
    private int perSpawn = 1;

    [SerializeField, Label("最初に出すまで（秒）", 0.0f)]
    [Tooltip("出し始める条件を満たしてから、最初の敵を出すまで")]
    private float firstDelay = 0.0f;

    [SerializeField, Label("次を出すまで（秒）", 0.0f)]
    [Tooltip("敵が倒されて空きができてから、次を出すまで")]
    private float interval = 5.0f;

    [Header("出す場所")]

    [SerializeField]
    [Tooltip("出る場所（空のオブジェクト）。空ならこのスポナーの位置。複数あれば順番に使う")]
    private List<Transform> points = new List<Transform>();

    [SerializeField, Label("ばらつき（半径）", 0.0f)]
    [Tooltip("出る場所から、この半径の中のどこかに出す（歩ける場所だけ）")]
    private float scatter = 0.0f;

    [SerializeField, Label("プレイヤーから離す距離", 0.0f)]
    [Tooltip("プレイヤーがこの距離より近い出る場所は使わない（目の前に急に出ないように）。0 なら気にしない")]
    private float minPlayerDistance = 0.0f;

    [Header("出し始める条件")]

    [SerializeField, Label("条件")]
    [Tooltip("満たしている間だけ出す（空なら最初から）。「プレイヤーがいる範囲」に Is Trigger の Collider2D を入れると、その中にいる間だけ出す")]
    private GameCondition condition = new GameCondition();

    [SerializeField, Label("条件を満たさなくなったら敵を消す")]
    [Tooltip("オン：範囲から出たら、このスポナーから出た敵を消す（数は戻らない）")]
    private bool removeWhenConditionLost = false;

    [Header("演出")]

    [SerializeField, Label("予告の時間（秒）", 0.0f)]
    [Tooltip("出る場所に丸を見せてから出す。0 ならすぐ出る")]
    private float telegraphTime = 0.6f;

    [SerializeField, Label("予告の色")]
    private Color telegraphColor = new Color(0.6f, 0.3f, 1.0f, 0.35f);

    [SerializeField, Label("出たときのエフェクト")]
    private EffectData spawnEffect;

    [SerializeField, Label("出たときの SE")]
    private SeData spawnSe;

    [Header("全部出して全部倒したら")]

    [SerializeField, Label("実行するイベント")]
    [Tooltip("「合計で出す数」を全部出して、全部倒したときに実行する（合計が 0 のときは使わない）")]
    private EventSequence onCleared;

    [SerializeField, Label("ON にするフラグ")]
    private GameFlag clearedFlag;

    //=====================================================================
    // 状態
    //=====================================================================

    private readonly List<Enemy> alive = new List<Enemy>();
    private readonly List<PendingSpawn> pending = new List<PendingSpawn>();
    private int spawned;
    private int sequentialIndex;
    private int pointIndex;
    private float timer;
    private bool waitingFirst = true;
    private bool cleared;

    private class PendingSpawn
    {
        public EnemyData data;
        public Vector2 position;
        public TelegraphView view;
        public float elapsed;
    }

    /// <summary> 出した数 </summary>
    public int SpawnedCount => spawned;

    /// <summary> 今いる、このスポナーから出た敵の数 </summary>
    public int AliveCount { get { alive.RemoveAll(e => e == null || e.IsDead); return alive.Count; } }

    /// <summary> 全部出し終えたか（合計が 0 なら false） </summary>
    public bool Finished => totalCount > 0 && spawned >= totalCount;

    /// <summary> デバッグ用の状態 </summary>
    public string StatusText
    {
        get
        {
            string total = totalCount > 0 ? $"{spawned}/{totalCount}" : $"{spawned}";
            string state = cleared ? "全部倒した" : (!ConditionMet ? "条件を満たしていない" : (Finished ? "出し終えた" : $"次まで {Mathf.Max(0.0f, CurrentWait - timer):0.0} 秒"));
            return $"出した数 {total}　今いる {AliveCount}/{maxAlive}　{state}";
        }
    }

    private bool ConditionMet => condition == null || condition.IsEmpty || condition.IsMet();

    private float CurrentWait => waitingFirst ? firstDelay : interval;

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(EnemyBlock.IsBlocked) { return; }

        UpdatePending();

        if(!ConditionMet)
        {
            if(removeWhenConditionLost) { RemoveAlive(); }
            CancelPending();
            return;
        }

        CheckCleared();
        if(Finished) { return; }

        int room = maxAlive - AliveCount - pending.Count;
        if(room <= 0) { return; }

        timer += Time.deltaTime;
        if(timer < CurrentWait) { return; }

        timer = 0.0f;
        waitingFirst = false;

        int n = Mathf.Min(Mathf.Max(1, perSpawn), room);
        if(totalCount > 0) { n = Mathf.Min(n, totalCount - spawned - pending.Count); }
        for(int i = 0; i < n; i++) { StartSpawn(); }
    }

    /// <summary>
    /// 1体出す（予告があれば予告から）
    /// </summary>
    private void StartSpawn()
    {
        EnemyData data = PickEnemy();
        if(data == null) { return; }
        if(!FindPosition(data, out Vector2 position)) { return; }

        var p = new PendingSpawn { data = data, position = position };
        if(telegraphTime > 0.0f) { p.view = TelegraphView.ShowCircle(position, data.Size == EnemySize.Large ? 1.2f : (data.Size == EnemySize.Small ? 0.45f : 0.7f), telegraphColor); }
        pending.Add(p);
        UpdatePending();
    }

    private void UpdatePending()
    {
        for(int i = pending.Count - 1; i >= 0; i--)
        {
            PendingSpawn p = pending[i];
            p.elapsed += Time.deltaTime;
            if(p.view != null) { p.view.SetProgress(telegraphTime > 0.0f ? p.elapsed / telegraphTime : 1.0f); }
            if(p.elapsed < telegraphTime) { continue; }

            if(p.view != null) { p.view.Hide(); }
            pending.RemoveAt(i);

            EffectManager.Play(spawnEffect, p.position);
            SeManager.PlaySe(spawnSe, p.position);
            Enemy e = Enemy.Spawn(p.data, p.position);
            if(e != null) { alive.Add(e); }
            spawned++;
        }
    }

    private void CancelPending()
    {
        foreach(PendingSpawn p in pending) { if(p.view != null) { p.view.Hide(); } }
        pending.Clear();
    }

    private void RemoveAlive()
    {
        foreach(Enemy e in alive) { if(e != null && !e.IsDead) { e.Remove(); } }
        alive.Clear();
    }

    private void CheckCleared()
    {
        if(cleared || !Finished || pending.Count > 0 || AliveCount > 0) { return; }

        cleared = true;
        if(clearedFlag != null) { clearedFlag.Value = true; }
        if(onCleared != null) { onCleared.Play(gameObject); }
    }

    //=====================================================================
    // 選ぶ
    //=====================================================================

    private EnemyData PickEnemy()
    {
        var valid = enemies.FindAll(e => e != null && e.enemy != null);
        if(valid.Count == 0) { return null; }

        if(pick == SpawnPick.Sequential)
        {
            EnemyData d = valid[sequentialIndex % valid.Count].enemy;
            sequentialIndex++;
            return d;
        }

        float total = 0.0f;
        foreach(SpawnEntry e in valid) { total += Mathf.Max(0.0f, e.weight); }
        if(total <= 0.0f) { return valid[0].enemy; }

        float r = UnityEngine.Random.Range(0.0f, total);
        foreach(SpawnEntry e in valid)
        {
            r -= Mathf.Max(0.0f, e.weight);
            if(r <= 0.0f) { return e.enemy; }
        }
        return valid[valid.Count - 1].enemy;
    }

    /// <summary>
    /// 出る場所を決める（プレイヤーに近すぎる場所は飛ばす・歩ける場所に合わせる）
    /// </summary>
    private bool FindPosition(EnemyData data, out Vector2 position)
    {
        var candidates = new List<Vector2>();
        foreach(Transform t in points) { if(t != null) { candidates.Add(t.position); } }
        if(candidates.Count == 0) { candidates.Add(transform.position); }

        GameObject player = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;
        NavMesh2D nav = NavMesh2D.Instance;

        for(int i = 0; i < candidates.Count; i++)
        {
            Vector2 basePos = candidates[(pointIndex + i) % candidates.Count];
            if(player != null && minPlayerDistance > 0.0f && Vector2.Distance(basePos, player.transform.position) < minPlayerDistance) { continue; }

            Vector2 p = basePos + (scatter > 0.0f ? UnityEngine.Random.insideUnitCircle * scatter : Vector2.zero);
            if(nav != null && !nav.SamplePosition(p, 1.5f, data.Size, out p)) { continue; }

            pointIndex = (pointIndex + i + 1) % candidates.Count;
            position = p;
            return true;
        }

        position = transform.position;
        return false;
    }

    private void OnDisable()
    {
        CancelPending();
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.4f, 1.0f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.6f, 0.0f));

        foreach(Transform t in points)
        {
            if(t == null) { continue; }
            Gizmos.DrawLine(transform.position, t.position);
            Gizmos.DrawWireSphere(t.position, Mathf.Max(0.25f, scatter));
        }
        if(points.Count == 0 && scatter > 0.0f) { Gizmos.DrawWireSphere(transform.position, scatter); }

        if(condition != null && condition.area != null)
        {
            Gizmos.color = new Color(0.7f, 0.4f, 1.0f, 0.12f);
            Bounds b = condition.area.bounds;
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
