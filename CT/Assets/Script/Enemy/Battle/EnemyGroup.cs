/*********************************************************************/
// * \file   EnemyGroup.cs
// * \brief  敵のグループ（全滅・○体倒したらイベント、撃破の順番）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グループが作動するきっかけ
/// </summary>
public enum GroupTrigger
{
    [InspectorName("全部倒したら")]
    AllDefeated = 0,

    [InspectorName("○体倒したら")]
    Count = 1,
}

/// <summary>
/// 順番でない敵の扱い
/// </summary>
public enum WrongOrderMode
{
    [InspectorName("無敵（バリアが見える）")]
    Barrier = 0,

    [InspectorName("倒しても○秒で復活する")]
    Revive = 1,

    [InspectorName("攻撃したら全員回復する")]
    Penalty = 2,
}

/// <summary>
/// 敵のグループ
/// </summary>
///
/// ・シーンに置いて、グループの敵を「メンバー」に入れる（Hierarchy 右クリック > 敵 > グループ）
/// ・全部（または○体）倒したら、イベントを実行・フラグを ON。「1回だけ」ならセーブする
/// ・「撃破の順番」をオンにすると、メンバーを上から順にしか倒せない。順番でない敵は、無敵（バリア）／倒しても復活／攻撃したら全員回復
/// ・次に倒す敵の頭の上に目印（下向きの三角）を出せる
public class EnemyGroup : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("グループの敵（シーンに置いた敵）。「撃破の順番」がオンなら、上から順に倒す")]
    private List<Enemy> members = new List<Enemy>();

    [Header("倒したら")]

    [SerializeField, Label("きっかけ")]
    private GroupTrigger trigger = GroupTrigger.AllDefeated;

    [SerializeField, Label("倒す数", 1)]
    [Tooltip("「○体倒したら」のときの数")]
    private int count = 1;

    [SerializeField, Label("実行するイベント")]
    private EventSequence onDefeated;

    [SerializeField, Label("ON にするフラグ")]
    private GameFlag flag;

    [SerializeField, Label("1回だけ（セーブする）")]
    private bool once = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する")]
    private string saveId = "";

    [Header("撃破の順番")]

    [SerializeField, Label("順番に倒す")]
    [Tooltip("オン：メンバーを上から順にしか倒せない")]
    private bool useOrder = false;

    [SerializeField, Label("順番でない敵は")]
    private WrongOrderMode wrongMode = WrongOrderMode.Barrier;

    [SerializeField, Label("復活までの秒数", 0.1f)]
    [Tooltip("「倒しても○秒で復活する」のとき。体力 1 で止まって動けなくなり、この秒数で全回復する")]
    private float reviveSeconds = 3.0f;

    [SerializeField, Label("次に倒す敵に目印を出す")]
    private bool markNext = true;

    [SerializeField, Label("目印の色")]
    private Color markerColor = new Color(1.0f, 0.9f, 0.2f, 0.9f);

    [SerializeField, Label("バリアの色")]
    private Color barrierColor = new Color(0.4f, 0.8f, 1.0f, 0.35f);

    [SerializeField, Label("全員回復したときのエフェクト")]
    [Tooltip("「攻撃したら全員回復する」で、順番を間違えたときに、それぞれの敵に出す")]
    private EffectData penaltyEffect;

    //=====================================================================
    // 状態
    //=====================================================================

    private bool done;
    private string saveKey;
    private TelegraphView marker;
    private readonly Dictionary<Enemy, TelegraphView> barriers = new Dictionary<Enemy, TelegraphView>();
    private readonly HashSet<Enemy> reviving = new HashSet<Enemy>();

    /// <summary> 作動したか </summary>
    public bool Done => done;

    /// <summary> 倒した数（消えた敵も倒したとみなす） </summary>
    public int DefeatedCount
    {
        get
        {
            int n = 0;
            foreach(Enemy e in members) { if(e == null || e.IsDead) { n++; } }
            return n;
        }
    }

    public int MemberCount => members.Count;

    /// <summary> 次に倒す敵（順番に倒すとき。いなければ null） </summary>
    public Enemy NextTarget
    {
        get
        {
            foreach(Enemy e in members) { if(e != null && !e.IsDead) { return e; } }
            return null;
        }
    }

    /// <summary> デバッグ表示用 </summary>
    public string StatusText
    {
        get
        {
            string text = done ? "作動した" : $"{DefeatedCount}/{(trigger == GroupTrigger.Count ? Mathf.Min(count, members.Count) : members.Count)} 体";
            if(useOrder && !done && NextTarget != null) { text += $"　次：{NextTarget.name}"; }
            return text;
        }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        saveKey = MakeSaveKey(gameObject, saveId);
        if(once && Save.Get(saveKey, false))
        {
            done = true;
            if(onDefeated != null) { onDefeated.Restore(gameObject); }
            return;
        }

        if(!useOrder) { return; }

        foreach(Enemy enemy in members)
        {
            if(enemy == null || enemy.Health == null) { continue; }
            Enemy captured = enemy;
            Health health = enemy.Health;

            // 順番でない敵は、倒れない（体力 1 で止まる）。バリアのときは無敵なので、ここまで来ない
            var before = health.DamageFilter;
            health.DamageFilter = info =>
            {
                if(before != null) { info = before(info); }
                if(!done && captured != NextTarget && wrongMode != WrongOrderMode.Barrier && info.amount >= health.CurrentHealth)
                {
                    info = info.WithAmount(Mathf.Max(0, health.CurrentHealth - 1));
                }
                return info;
            };

            health.OnDamagedInfo += info => OnMemberDamaged(captured);
        }
    }

    private void OnDisable()
    {
        ClearViews();
        foreach(Enemy e in members) { if(e != null && e.Health != null) { e.Health.Invincible = false; } }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(done) { return; }

        int defeated = DefeatedCount;
        bool met = trigger == GroupTrigger.AllDefeated ? members.Count > 0 && defeated >= members.Count : defeated >= Mathf.Min(count, members.Count);
        if(met)
        {
            Activate();
            return;
        }

        if(useOrder) { UpdateOrder(); }
    }

    private void Activate()
    {
        done = true;
        ClearViews();
        foreach(Enemy e in members) { if(e != null && e.Health != null) { e.Health.Invincible = false; } }

        if(flag != null) { flag.Value = true; }
        if(once) { Save.SetPending(saveKey, true); }
        if(onDefeated != null) { onDefeated.Play(gameObject); }
    }

    /// <summary>
    /// 撃破の順番：次の敵以外を守る・目印を出す
    /// </summary>
    private void UpdateOrder()
    {
        Enemy next = NextTarget;

        foreach(Enemy enemy in members)
        {
            if(enemy == null || enemy.IsDead)
            {
                if(enemy != null) { RemoveBarrier(enemy); }
                continue;
            }

            bool protectedNow = enemy != next && wrongMode == WrongOrderMode.Barrier;
            if(enemy.Health != null) { enemy.Health.Invincible = protectedNow; }

            if(protectedNow)
            {
                if(!barriers.TryGetValue(enemy, out TelegraphView view) || view == null)
                {
                    view = TelegraphView.ShowCircle(enemy.transform.position, enemy.BodyRadius + 0.35f, barrierColor, true, 6);
                    barriers[enemy] = view;
                }
                view.SetPose(enemy.transform.position, Vector2.right);
            }
            else
            {
                RemoveBarrier(enemy);
            }
        }

        // 次に倒す敵の頭の上に、下向きの三角
        if(markNext && next != null)
        {
            Vector2 top = MarkerPosition(next);
            if(marker == null)
            {
                var shape = new AttackShape { type = AttackShapeType.Sector, radius = 0.45f, angle = 50.0f };
                marker = TelegraphView.ShowShape(shape, top, Vector2.down, markerColor, true, 200);
            }
            float bob = Mathf.Sin(Time.time * 5.0f) * 0.1f;
            marker.SetPose(top + Vector2.up * bob, Vector2.down);
        }
        else if(marker != null)
        {
            marker.Hide();
            marker = null;
        }
    }

    private static Vector2 MarkerPosition(Enemy enemy)
    {
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        float top = sr != null ? sr.bounds.max.y : enemy.transform.position.y + 1.0f;
        return new Vector2(enemy.transform.position.x, top + 0.7f);
    }

    /// <summary>
    /// 順番でない敵がダメージを受けたとき（復活・全員回復）
    /// </summary>
    private void OnMemberDamaged(Enemy enemy)
    {
        if(done || enemy == NextTarget) { return; }

        if(wrongMode == WrongOrderMode.Penalty)
        {
            foreach(Enemy e in members)
            {
                if(e == null || e.IsDead || e.Health == null) { continue; }
                e.Health.Heal(e.Health.MaxHealth);
                EffectManager.Play(penaltyEffect, e.transform.position);
            }
        }
        else if(wrongMode == WrongOrderMode.Revive && enemy.Health != null && enemy.Health.CurrentHealth <= 1 && !reviving.Contains(enemy))
        {
            StartCoroutine(Revive(enemy));
        }
    }

    private IEnumerator Revive(Enemy enemy)
    {
        reviving.Add(enemy);
        enemy.Stagger(reviveSeconds);

        float elapsed = 0.0f;
        while(elapsed < reviveSeconds && enemy != null && !enemy.IsDead)
        {
            if(!EnemyBlock.IsBlocked) { elapsed += Time.deltaTime; }
            yield return null;
        }

        if(enemy != null && !enemy.IsDead && enemy.Health != null) { enemy.Health.Heal(enemy.Health.MaxHealth); }
        reviving.Remove(enemy);
    }

    //=====================================================================
    // 表示の片づけ
    //=====================================================================

    private void RemoveBarrier(Enemy enemy)
    {
        if(barriers.TryGetValue(enemy, out TelegraphView view) && view != null) { view.Hide(); }
        barriers.Remove(enemy);
    }

    private void ClearViews()
    {
        foreach(TelegraphView view in barriers.Values) { if(view != null) { view.Hide(); } }
        barriers.Clear();
        if(marker != null) { marker.Hide(); }
        marker = null;
    }

    public static string MakeSaveKey(GameObject target, string id)
    {
        string name = id;
        if(string.IsNullOrEmpty(name))
        {
            name = target.name;
            for(Transform p = target.transform.parent; p != null; p = p.parent) { name = p.name + "/" + name; }
        }
        return "Group/" + target.scene.name + "/" + name;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.9f, 0.2f, 0.8f);
        Vector3? prev = null;
        foreach(Enemy e in members)
        {
            if(e == null) { continue; }
            Gizmos.DrawLine(transform.position, e.transform.position);
            if(useOrder && prev.HasValue) { Gizmos.DrawLine(prev.Value, e.transform.position); }
            prev = e.transform.position;
        }
    }
}
