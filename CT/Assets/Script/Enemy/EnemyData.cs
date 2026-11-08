/*********************************************************************/
// * \file   EnemyData.cs
// * \brief  敵1種類分のデータ（体力・速さ・見た目・倒されたときの演出など）
// *
// * \author
/*********************************************************************/

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
}
