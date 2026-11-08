/*********************************************************************/
// * \file   EnemyData.cs
// * \brief  敵1種類分のデータ（体力・速さ・見た目・倒されたときの演出など）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵のデータ
/// </summary>
///
/// ・敵を増やすときは、既存の EnemyData を複製（Ctrl + D）して数値と絵を変える
/// ・シーンに置いた敵（共通プレハブ Enemy_Base）の Enemy の「敵のデータ」に入れて使う
/// 行動パターン・攻撃・弱点・倒した後の処理は、これからの段階で足していく
[CreateAssetMenu(menuName = "敵/敵のデータ", fileName = "EnemyData_")]
public class EnemyData : ScriptableObject
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("基本")]

    [SerializeField, Label("名前")]
    [Tooltip("一覧やデバッグで表示する名前")]
    private string displayName = "敵";

    [SerializeField, Label("最大の体力", 1)]
    [Tooltip("この回数分ダメージを受けると倒れる（ノアの範囲攻撃は1回で1ダメージ）")]
    private int maxHealth = 3;

    [SerializeField, Label("移動の速さ", 0.0f)]
    [Tooltip("1秒に進む距離（マス）。プレイヤーは 5")]
    private float moveSpeed = 2.0f;

    [SerializeField, Label("大きさ")]
    [Tooltip("通れる道の幅が変わる。大きい敵は細い通路を通れない")]
    private EnemySize size = EnemySize.Medium;

    [SerializeField, Label("押されにくい")]
    [Tooltip("オン：プレイヤーがぶつかっても押されない ／ オフ：押される")]
    private bool heavy = true;

    [Header("行動")]

    [SerializeField, Label("行動パターン")]
    [Tooltip("EnemyPattern（いつ・どこへ動き、いつ攻撃するか）。空ならその場に立っているだけ")]
    private EnemyPattern pattern;

    [Header("受けるダメージの効き方")]

    [SerializeField]
    [Tooltip("攻撃の種類・攻撃したキャラごとの倍率。当てはまるものを全部かける。0 なら効かない（空ならすべて等倍）。\n例：「ノアのゲージ攻撃 ×2」「シグルの弾 ×0（効かない）」")]
    private List<DamageRule> damageRules = new List<DamageRule>();

    [Header("体当たり")]

    [SerializeField, Label("ぶつかったときのダメージ", 0)]
    [Tooltip("プレイヤーに触れたときのダメージ。0 なら触れても何もない")]
    private int contactDamage = 1;

    [SerializeField, Label("ダメージの間隔（秒）", 0.1f)]
    [Tooltip("触れ続けているとき、何秒ごとにダメージを与えるか")]
    private float contactInterval = 1.0f;

    [Header("見た目")]

    [SerializeField, Label("アニメーション")]
    [Tooltip("SpriteAnimData。Idle（待機）・Walk（移動）・Damage（ダメージ）・Dead（倒れる）の名前の動きを自動で使う。空なら下の絵のまま")]
    private SpriteAnimData animation;

    [SerializeField, Label("絵（アニメがないとき）")]
    [Tooltip("アニメーションが空のときに表示する絵。空ならプレハブの絵のまま")]
    private Sprite sprite;

    [Header("倒されたとき")]

    [SerializeField, Label("エフェクト")]
    [Tooltip("消える瞬間に出す（死亡アニメがあれば流し終わってから）")]
    private EffectData deathEffect;

    [SerializeField, Label("SE")]
    [Tooltip("倒された瞬間に鳴らす")]
    private SeData deathSe;

    [SerializeField, Label("死亡アニメの名前")]
    [Tooltip("アニメーションにこの名前の動きがあれば、流し終わってから消える")]
    private string deathAnimation = "Dead";

    [Header("メモ")]

    [SerializeField, TextArea(2, 5)]
    [Tooltip("プランナー用のメモ（ゲームには影響しない）")]
    private string memo;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public float MoveSpeed => Mathf.Max(0.0f, moveSpeed);
    public EnemySize Size => size;
    public bool Heavy => heavy;
    public int ContactDamage => Mathf.Max(0, contactDamage);
    public float ContactInterval => Mathf.Max(0.1f, contactInterval);
    public SpriteAnimData Animation => animation;
    public Sprite Sprite => sprite;
    public EffectData DeathEffect => deathEffect;
    public SeData DeathSe => deathSe;
    public string DeathAnimation => deathAnimation;
    public EnemyPattern Pattern => pattern;
    public IReadOnlyList<DamageRule> DamageRules => damageRules;

    /// <summary>
    /// 効き方の設定をかけたダメージ（倍率が 0 なら 0。0 より大きければ最低 1）
    /// </summary>
    public int ApplyDamageRules(DamageInfo info)
    {
        float multiplier = 1.0f;
        foreach(DamageRule rule in damageRules)
        {
            if(rule != null && rule.Matches(info)) { multiplier *= Mathf.Max(0.0f, rule.multiplier); }
        }

        if(multiplier <= 0.0f) { return 0; }
        return Mathf.Max(1, Mathf.FloorToInt(info.amount * multiplier + 0.5f));
    }
}

/// <summary>
/// 攻撃の種類の条件（「すべて」を含む）
/// </summary>
/// 値は DamageKind と同じ番号にする（DamageKind を足したら、ここにも同じ番号で足す）
public enum DamageKindFilter
{
    [InspectorName("すべての攻撃")]
    Any = -1,

    [InspectorName("その他")]
    Other = 0,

    [InspectorName("ノアの範囲攻撃（Space）")]
    AreaAttack = 1,

    [InspectorName("ノアのゲージ攻撃（R）")]
    GaugeAttack = 2,

    [InspectorName("シグルの弾")]
    PlayerBullet = 3,

    [InspectorName("敵の弾")]
    EnemyBullet = 10,

    [InspectorName("敵の攻撃")]
    EnemyAttack = 11,

    [InspectorName("体当たり")]
    Contact = 12,

    [InspectorName("爆発")]
    Explosion = 20,

    [InspectorName("ギミック")]
    Gimmick = 21,
}

/// <summary>
/// 攻撃したキャラの条件
/// </summary>
public enum CharacterFilter
{
    [InspectorName("問わない")]
    Any = 0,

    [InspectorName("ノア")]
    Noah = 1,

    [InspectorName("シグル")]
    Sigle = 2,

    [InspectorName("プレイヤー以外（敵・ギミックなど）")]
    NotPlayer = 3,
}

/// <summary>
/// 受けるダメージの効き方（1行分）
/// </summary>
[Serializable]
public class DamageRule
{
    [Label("攻撃の種類")]
    public DamageKindFilter kind = DamageKindFilter.Any;

    [Label("攻撃したキャラ")]
    public CharacterFilter character = CharacterFilter.Any;

    [Label("倍率", 0.0f)]
    [Tooltip("2 なら2倍、0.5 なら半分、0 なら効かない")]
    public float multiplier = 1.0f;

    [Label("ゲージの判定")]
    [Tooltip("ゲージ攻撃のとき、判定の結果で絞る（「なし」なら問わない）")]
    public GaugeJudge judge = GaugeJudge.None;

    public bool Matches(DamageInfo info)
    {
        if(kind != DamageKindFilter.Any && (int)kind != (int)info.kind) { return false; }

        switch(character)
        {
            case CharacterFilter.Noah:      if(info.character != PlayerCharacter.Noah)  { return false; } break;
            case CharacterFilter.Sigle:     if(info.character != PlayerCharacter.Sigle) { return false; } break;
            case CharacterFilter.NotPlayer: if(info.character != PlayerCharacter.None)  { return false; } break;
        }

        if(judge != GaugeJudge.None && info.judge != judge) { return false; }
        return true;
    }
}
