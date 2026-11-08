/*********************************************************************/
// * \file   AttackData.cs
// * \brief  敵の攻撃1つ分のデータ（予告 → 発動 → 後隙）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 攻撃の起点（どこから出るか）
/// </summary>
public enum AttackOrigin
{
    [InspectorName("敵の位置")]
    Enemy = 0,

    [InspectorName("プレイヤーの位置（予告を出した時点で決める）")]
    PlayerAtStart = 1,

    [InspectorName("プレイヤーの位置（予告の間は追いかける）")]
    PlayerTracking = 2,
}

/// <summary>
/// 攻撃の向き
/// </summary>
public enum AttackAim
{
    [InspectorName("プレイヤーの方向")]
    Player = 0,

    [InspectorName("敵の向いている方向")]
    Facing = 1,

    [InspectorName("決まった角度（右が 0、上が 90）")]
    Fixed = 2,
}

/// <summary>
/// 発動のしかた
/// </summary>
public enum AttackActivation
{
    [InspectorName("一瞬（その場で出る）")]
    Instant = 0,

    [InspectorName("出し続ける（ビームなど）")]
    Sustained = 1,

    [InspectorName("弾を撃つ")]
    Bullet = 2,
}

/// <summary>
/// 攻撃中の敵自身の動き
/// </summary>
public enum AttackMotion
{
    [InspectorName("その場で止まる")]
    Stay = 0,

    [InspectorName("突進する（予告の長方形を走る）")]
    Charge = 1,
}

/// <summary>
/// 攻撃が当たる相手
/// </summary>
public enum AttackTargets
{
    [InspectorName("プレイヤー")]
    Player = 0,

    [InspectorName("敵（ほかの敵にも当たる）")]
    Enemies = 1,

    [InspectorName("両方")]
    Both = 2,
}

/// <summary>
/// 予告の描き方
/// </summary>
public enum TelegraphStyle
{
    [InspectorName("塗りと枠")]
    FillAndOutline = 0,

    [InspectorName("塗りだけ")]
    Fill = 1,

    [InspectorName("枠だけ")]
    Outline = 2,
}

/// <summary>
/// 敵の攻撃のデータ
/// </summary>
///
/// ・攻撃は「予告（範囲を見せる）→ 発動（当たり判定が出る）→ 後隙（反撃できる時間）」の順に進む
/// ・同じ攻撃を、いろいろな敵の行動パターンで使い回せる
/// ・組み合わせの例：ビーム＝敵の位置×長方形×出し続ける、突進＝敵の位置×長方形×一瞬×突進、
///   足元の爆発＝プレイヤーの位置（予告時）×円×一瞬、なぎ払い＝敵の位置×扇形×一瞬
/// 落下・突き出る・設置などの発動のしかたは次の段階で足す
[CreateAssetMenu(menuName = "敵/攻撃", fileName = "Attack_")]
public class AttackData : ScriptableObject
{
    //=====================================================================
    // 予告
    //=====================================================================

    [Header("予告（範囲を先に見せる）")]

    [Label("予告の時間（秒）", 0.0f)]
    [Tooltip("範囲を見せてから発動するまでの時間。0 なら予告なしですぐ発動")]
    public float telegraphTime = 0.8f;

    [Label("範囲を表示する")]
    [Tooltip("オフにすると、時間は待つが範囲は見せない（見えない攻撃）")]
    public bool showTelegraph = true;

    [Label("描き方")]
    public TelegraphStyle telegraphStyle = TelegraphStyle.FillAndOutline;

    [Label("色")]
    [Tooltip("予告の色。透明度（A）で薄さを調整する")]
    public Color telegraphColor = new Color(1.0f, 0.15f, 0.1f, 0.35f);

    [Label("満ちていく表示")]
    [Tooltip("発動までの残り時間に合わせて、内側から色が満ちていく（そろそろ来る、が分かる）")]
    public bool telegraphFill = true;

    [Label("点滅させる")]
    [Tooltip("発動の直前（最後の 30%）に点滅させる")]
    public bool telegraphBlink = false;

    [Label("描く順番")]
    [Tooltip("大きいほど手前に描かれる。地面 0、建物 1〜2、弾 2、物 5、キャラ 99〜100。既定の 3 は「床の上・物やキャラの下」")]
    public int telegraphSortingOrder = 3;

    [Label("暗い所でも見える")]
    [Tooltip("オン：暗いステージでも明るさが変わらない（おすすめ）／オフ：周りと同じように暗くなる")]
    public bool telegraphUnlit = true;

    [Label("予告の SE")]
    public SeData telegraphSe;

    [Label("予告中の敵のアニメ")]
    [Tooltip("SpriteAnimData の動きの名前（構えるなど）。なければ何もしない")]
    public string telegraphAnimation = "Windup";

    //=====================================================================
    // 出る場所と形
    //=====================================================================

    [Header("出る場所と形")]

    [Label("起点")]
    [Tooltip("攻撃の範囲を置く場所")]
    public AttackOrigin origin = AttackOrigin.Enemy;

    [Label("向き")]
    public AttackAim aim = AttackAim.Player;

    [Label("決まった角度", -360.0f, 360.0f)]
    [Tooltip("向きが「決まった角度」のときの角度（度）")]
    public float fixedAngle = 0.0f;

    [Label("予告の間は向きを合わせ続ける")]
    [Tooltip("オン：予告の間はプレイヤーの方へ向き直し続け、発動の瞬間に向きが決まる ／ オフ：予告を出した時点の向きのまま（避けやすい）")]
    public bool trackDuringTelegraph = false;

    [Label("前にずらす距離")]
    [Tooltip("起点を向きの方向へずらす（マス）。敵の体の外から出したいときなど")]
    public float forwardOffset = 0.0f;

    [Label("範囲の形")]
    [Tooltip("当たり判定と予告の形（どちらも同じ形）")]
    public AttackShape shape = new AttackShape();

    [Label("長方形の長さを壁で止める")]
    [Tooltip("ビーム・突進で、途中に壁があればそこまでにする（予告も短くなる）")]
    public bool stopAtWall = true;

    //=====================================================================
    // 発動
    //=====================================================================

    [Header("発動")]

    [Label("発動のしかた")]
    public AttackActivation activation = AttackActivation.Instant;

    [Label("ダメージ", 0)]
    public int damage = 1;

    [Label("当たる相手")]
    public AttackTargets targets = AttackTargets.Player;

    [Label("ノアには効かない")]
    [Tooltip("オンなら、この攻撃はノアをすり抜ける（キャラを切り替えて避ける遊びに）")]
    public bool ignoreNoah = false;

    [Label("シグルには効かない")]
    public bool ignoreSigle = false;

    [Label("攻撃の種類")]
    [Tooltip("当たった相手から見た攻撃の種類（敵どうしで当たるときの効き方などに使う）")]
    public DamageKind damageKind = DamageKind.EnemyAttack;

    [Label("出し続ける時間（秒）", 0.1f)]
    [Tooltip("「出し続ける」のときの長さ")]
    public float sustainTime = 1.5f;

    [Label("当たる間隔（秒）", 0.05f)]
    [Tooltip("「出し続ける」のとき、同じ相手に何秒ごとに当たるか")]
    public float hitInterval = 0.5f;

    [Label("回る速さ（度/秒）")]
    [Tooltip("「出し続ける」の間、範囲を回転させる（ビームのなぎ払い）。0 なら回らない。マイナスで逆回り")]
    public float rotateSpeed = 0.0f;

    [Label("撃つ弾の数", 1)]
    [Tooltip("「弾を撃つ」のとき。同時に扇状に撃つ数（1 なら1発）")]
    public int bulletCount = 1;

    [Label("弾の広がり（度）", 0.0f, 360.0f)]
    [Tooltip("複数の弾を撃つときの、端から端までの角度")]
    public float bulletSpread = 30.0f;

    [Label("連射の回数", 1)]
    [Tooltip("上の弾を何回撃つか")]
    public int bulletVolleys = 1;

    [Label("連射の間隔（秒）", 0.0f)]
    public float bulletVolleyInterval = 0.3f;

    [Label("敵自身の動き")]
    public AttackMotion motion = AttackMotion.Stay;

    [Label("突進の速さ", 0.1f)]
    [Tooltip("1秒に進む距離（マス）。プレイヤーは 5")]
    public float chargeSpeed = 12.0f;

    [Label("壁にぶつかったら気絶（秒）", 0.0f)]
    [Tooltip("突進が壁で止まったとき、この秒数だけ動けなくなる（反撃のチャンス）。0 なら気絶しない")]
    public float chargeStunTime = 0.0f;

    [Label("発動のエフェクト")]
    [Tooltip("範囲の真ん中に出す（右向きの絵なら攻撃の向きに回る）")]
    public EffectData activeEffect;

    [Label("発動の SE")]
    public SeData activeSe;

    [Label("発動中の敵のアニメ")]
    public string activeAnimation = "Attack";

    //=====================================================================
    // 後隙
    //=====================================================================

    [Header("後隙（攻撃のあと、動けない時間）")]

    [Label("後隙（秒）", 0.0f)]
    [Tooltip("攻撃が終わってから次の行動までの時間。長いほど反撃しやすい")]
    public float recoveryTime = 0.5f;

    //=====================================================================
    // メモ
    //=====================================================================

    [Header("メモ")]

    [TextArea(2, 5)]
    [Tooltip("プランナー用のメモ（ゲームには影響しない）")]
    public string memo;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary> このキャラには効かないか </summary>
    public bool Ignores(PlayerCharacter character)
    {
        return (character == PlayerCharacter.Noah && ignoreNoah) || (character == PlayerCharacter.Sigle && ignoreSigle);
    }

    public bool HitsPlayer => targets == AttackTargets.Player || targets == AttackTargets.Both;
    public bool HitsEnemies => targets == AttackTargets.Enemies || targets == AttackTargets.Both;
}
