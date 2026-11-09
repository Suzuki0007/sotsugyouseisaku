/*********************************************************************/
// * \file   EnemyPart.cs
// * \brief  敵の部位（弱点・装甲・壊せる部位・独立する部位）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部位の種類
/// </summary>
public enum EnemyPartType
{
    [InspectorName("ふつうの部位")]
    Normal = 0,

    [InspectorName("弱点（当てるとよく効く・ひるみやすい）")]
    Weak = 1,

    [InspectorName("装甲（本体に効きにくい。壊すと効くようになる）")]
    Armor = 2,
}

/// <summary>
/// 敵の部位
/// </summary>
///
/// ・敵（Enemy）の子オブジェクトに、Collider2D（Is Trigger）と一緒に付ける（Hierarchy で敵を選んで右クリック > 敵 > 部位を追加）
/// ・ここに当たった攻撃は、倍率をかけて本体の体力を減らす（弱点なら 2 倍、装甲なら 0 倍＝効かない、など）
/// ・部位の体力を決めると「壊せる部位」になる。壊れたとき：消える・絵を変える・段階を変える・攻撃を使えなくする・イベント・
///   「独立する」（別の敵として動き出す。外れた腕が飛びかかってくる、など）
/// ・弱点は「見える条件」を決められる（シグルを操作しているときだけ光る、など）
/// ・本体と部位に同時に攻撃が当たったときは、部位で受ける（弱点 → 装甲 → ふつうの部位 → 本体 の順）
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(DamageReceiver))]
public class EnemyPart : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("部位の名前")]
    [Tooltip("見た目の変化（「この部位が壊れたら」）・デバッグ表示で使う名前（例：盾、右腕、コア）")]
    private string partName = "部位";

    [SerializeField, Label("種類")]
    private EnemyPartType type = EnemyPartType.Normal;

    [SerializeField, Label("本体へのダメージの倍率", 0.0f)]
    [Tooltip("ここに当たったとき、本体の体力を減らす倍率。弱点 2、装甲 0（効かない）〜 0.5、ふつう 1 が目安")]
    private float bodyMultiplier = 1.0f;

    [SerializeField, Label("ひるみやすさ", 0.0f)]
    [Tooltip("ここに当たったときのひるみの倍率（敵のデータの「ひるむまでのダメージ」にたまる量）。弱点は 2 など")]
    private float staggerMultiplier = 1.0f;

    [SerializeField]
    [Tooltip("この部位だけの効き方（攻撃の種類・キャラ・ゲージ判定ごとの倍率）。例：この装甲はゲージ攻撃でしか削れない")]
    private List<DamageRule> rules = new List<DamageRule>();

    [Header("壊せる部位")]

    [SerializeField, Label("部位の体力", 0)]
    [Tooltip("0 なら壊れない。この回数分（効き方をかけたダメージで）当てると壊れる")]
    private int partHealth = 0;

    [SerializeField, Label("壊れたら消える")]
    [Tooltip("オン：壊れたらこの部位は見えなくなり、当たらなくなる（装甲なら本体がむき出しになる）／オフ：残る（絵を変えるなど）")]
    private bool hideOnBreak = true;

    [SerializeField, Label("壊れたあとの絵")]
    [Tooltip("消えない設定のとき、壊れたらこの絵にする（空ならそのまま）")]
    private Sprite brokenSprite;

    [SerializeField, Label("壊れたら段階を変える", 0)]
    [Tooltip("本体の行動パターンの段階の番号（1 から）。0 なら変えない（例：盾を壊されたら「怒り」へ）")]
    private int changePhase = 0;

    [SerializeField]
    [Tooltip("壊れたら本体が使えなくなる攻撃（例：右腕を壊したら、なぎ払いをしなくなる）")]
    private List<AttackData> disableAttacks = new List<AttackData>();

    [SerializeField, Label("独立する")]
    [Tooltip("壊れたら、この敵のデータの敵として、部位の位置から動き出す（空なら独立しない）。例：外れた腕が飛びかかってくる")]
    private EnemyData independent;

    [SerializeField, Label("壊れたときのイベント")]
    private EventSequence onBreak;

    [SerializeField, Label("壊れたときのエフェクト")]
    private EffectData breakEffect;

    [SerializeField, Label("壊れたときの SE")]
    private SeData breakSe;

    [SerializeField, Label("当たったときのエフェクト")]
    [Tooltip("部位に攻撃が当たるたびに出す（装甲の「カキン」など）")]
    private EffectData hitEffect;

    [Header("見える条件（弱点など）")]

    [SerializeField, Label("見える条件")]
    [Tooltip("満たしている間だけ「目印」を出す（空ならいつも出す）。例：シグルを操作しているとき")]
    private GameCondition revealCondition = new GameCondition();

    [SerializeField, Label("目印")]
    [Tooltip("見える条件を満たしている間だけ表示するオブジェクト（光る輪など。子オブジェクトに置く）")]
    private GameObject highlight;

    [SerializeField, Label("見えている間だけ効く")]
    [Tooltip("オン：見える条件を満たしていない間は、本体へのダメージの倍率とひるみやすさが 1（ふつう）になる")]
    private bool effectiveOnlyWhenRevealed = false;

    private Enemy enemy;
    private DamageReceiver receiver;
    private int currentHealth;
    private bool broken;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string PartName => partName;
    public EnemyPartType Type => type;
    public bool Broken => broken;
    public bool Breakable => partHealth > 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => partHealth;
    public Enemy Owner => enemy;

    /// <summary> 見える条件を満たしているか </summary>
    public bool Revealed => revealCondition == null || revealCondition.IsEmpty || revealCondition.IsMet();

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        receiver = GetComponent<DamageReceiver>();
        receiver.Handler = Receive;
        receiver.Priority = type == EnemyPartType.Weak ? 3 : (type == EnemyPartType.Armor ? 2 : 1);
        currentHealth = partHealth;

        // 攻撃が当たるように、敵と同じタグ・レイヤーにする
        gameObject.tag = "Enemies";
        gameObject.layer = LayerMask.NameToLayer("Enemies");

        if(enemy == null) { Debug.LogWarning($"{name}: 親に敵（Enemy）がないので、部位として働きません", this); }
    }

    private void Update()
    {
        if(highlight != null)
        {
            bool show = !broken && Revealed;
            if(highlight.activeSelf != show) { highlight.SetActive(show); }
        }
    }

    //=====================================================================
    // 攻撃を受ける
    //=====================================================================

    private bool Receive(DamageInfo info)
    {
        if(enemy == null || enemy.IsDead || enemy.Health == null) { return false; }

        // この部位の効き方
        float partMultiplier = 1.0f;
        foreach(DamageRule rule in rules)
        {
            if(rule != null && rule.Matches(info)) { partMultiplier *= Mathf.Max(0.0f, rule.multiplier); }
        }
        int partDamage = partMultiplier <= 0.0f ? 0 : Mathf.Max(1, Mathf.FloorToInt(info.amount * partMultiplier + 0.5f));

        EffectManager.Play(hitEffect, info.point);

        // 本体への倍率は、当たった時点の状態で決める（装甲を壊した一撃も、装甲で受け止める）
        // 壊れて残っている部位は、ふつうの部位として扱う
        bool active = !broken && (!effectiveOnlyWhenRevealed || Revealed);

        // 部位の体力
        if(Breakable && !broken && partDamage > 0)
        {
            currentHealth -= partDamage;
            if(currentHealth <= 0) { Break(); }
        }

        // 本体へ
        float toBody = active ? bodyMultiplier : 1.0f;
        int bodyDamage = (partDamage <= 0 || toBody <= 0.0f) ? 0 : Mathf.Max(1, Mathf.FloorToInt(partDamage * toBody + 0.5f));

        DamageInfo forwarded = info.WithAmount(bodyDamage);
        forwarded.noStagger = true;
        enemy.Health.Damage(forwarded);

        // ひるみ（本体に渡す分では数えず、ここで数える）
        float stagger = active ? staggerMultiplier : 1.0f;
        if(partDamage > 0) { enemy.AddStagger(partDamage * stagger, info); }

        return true;
    }

    /// <summary>
    /// 壊す（体力が 0 になったとき。デバッグからも呼べる）
    /// </summary>
    public void Break()
    {
        if(broken) { return; }
        broken = true;
        currentHealth = 0;

        Vector2 position = transform.position;
        EffectManager.Play(breakEffect, position);
        SeManager.PlaySe(breakSe, position);

        if(enemy != null)
        {
            enemy.NotifyPartBroken(this);
            foreach(AttackData attack in disableAttacks) { enemy.DisableAttack(attack); }
            if(changePhase > 0 && enemy.Brain != null) { enemy.Brain.SetPhase(changePhase - 1); }
        }

        if(independent != null)
        {
            Enemy spawned = Enemy.Spawn(independent, position);
            if(spawned != null && enemy != null && enemy.Brain != null) { enemy.Brain.AddSummoned(spawned); }
        }

        if(onBreak != null) { onBreak.Play(gameObject); }

        if(hideOnBreak)
        {
            foreach(Collider2D col in GetComponentsInChildren<Collider2D>()) { col.enabled = false; }
            foreach(Renderer r in GetComponentsInChildren<Renderer>()) { r.enabled = false; }
        }
        else if(brokenSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if(sr != null) { sr.sprite = brokenSprite; }
        }
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if(col == null) { return; }

        switch(type)
        {
            case EnemyPartType.Weak:  Gizmos.color = new Color(1.0f, 0.9f, 0.1f, 0.5f); break;
            case EnemyPartType.Armor: Gizmos.color = new Color(0.5f, 0.7f, 1.0f, 0.5f); break;
            default:                  Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.4f); break;
        }
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
