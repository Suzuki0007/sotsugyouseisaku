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

    [InspectorName("上から落ちる（予告が影になる）")]
    Drop = 3,

    [InspectorName("下から突き出る")]
    Rise = 4,

    [InspectorName("設置して起爆（爆弾・罠）")]
    Place = 5,
}

/// <summary>
/// ひるみで攻撃をやめるか
/// </summary>
public enum AttackCancel
{
    [InspectorName("予告中だけ（発動したら最後まで出す）")]
    DuringTelegraph = 0,

    [InspectorName("発動中も（いつでもやめる）")]
    Always = 1,

    [InspectorName("できない（スーパーアーマー）")]
    Never = 2,
}

/// <summary>
/// 弾の撃ち方
/// </summary>
public enum BulletPattern
{
    [InspectorName("狙い撃ち（扇状に広げられる）")]
    Aimed = 0,

    [InspectorName("全方位（ぐるっと一周）")]
    Circle = 1,

    [InspectorName("うず巻き（撃つたびに回る）")]
    Spiral = 2,

    [InspectorName("ばらまき（ランダムな方向）")]
    Random = 3,
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
/// ・上から落ちる＝予告が影になり、最後に物が落ちてくる ／ 下から突き出る＝発動で地面から物が出る（残せる）
///   ／ 設置して起爆＝その場に物を置き、時間・触れる・攻撃されるで爆発（置いたあと、敵は次の行動に移る）
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

    [Label("ひるみでキャンセルできる")]
    [Tooltip("敵がひるんだとき、この攻撃を途中でやめるか（後隙ではいつでもひるむ）。できないときは、攻撃が終わってからひるむ")]
    public AttackCancel cancel = AttackCancel.DuringTelegraph;

    [Label("出し続ける時間（秒）", 0.1f)]
    [Tooltip("「出し続ける」のときの長さ")]
    public float sustainTime = 1.5f;

    [Label("当たる間隔（秒）", 0.05f)]
    [Tooltip("「出し続ける」のとき、同じ相手に何秒ごとに当たるか")]
    public float hitInterval = 0.5f;

    [Label("回る速さ（度/秒）")]
    [Tooltip("「出し続ける」の間、範囲を回転させる（ビームのなぎ払い）。0 なら回らない。マイナスで逆回り")]
    public float rotateSpeed = 0.0f;

    [Label("弾の撃ち方")]
    public BulletPattern bulletPattern = BulletPattern.Aimed;

    [Label("撃つ弾の数", 1)]
    [Tooltip("1回に同時に撃つ数（狙い撃ち：扇状 ／ 全方位・うず巻き：一周に並べる ／ ばらまき：ランダム）")]
    public int bulletCount = 1;

    [Label("弾の広がり（度）", 0.0f, 360.0f)]
    [Tooltip("狙い撃ち・ばらまきのときの、端から端までの角度")]
    public float bulletSpread = 30.0f;

    [Label("撃つたびに回す角度（度）")]
    [Tooltip("うず巻きのとき、1回撃つごとに向きを回す角度")]
    public float bulletSpiralStep = 15.0f;

    [Label("連射の回数", 1)]
    [Tooltip("上の弾を何回撃つか")]
    public int bulletVolleys = 1;

    [Label("連射の間隔（秒）", 0.0f)]
    public float bulletVolleyInterval = 0.3f;

    [Label("弾の速さ", 0.0f)]
    [Tooltip("1秒に進む距離。0 なら弾のプレハブのまま")]
    public float bulletSpeed = 0.0f;

    [Label("弾消しで消せる")]
    [Tooltip("オフにすると、ノアの弾消し（Q）で消えない弾になる")]
    public bool bulletDeletable = true;

    [Label("落ちてくる物（プレハブ）")]
    [Tooltip("「上から落ちる」のとき、上から降ってくる見た目（岩など）。空なら予告（影）と着地のエフェクトだけ")]
    public GameObject dropObject;

    [Label("落ちる高さ", 0.0f)]
    [Tooltip("どれだけ上から落ちてくるか（マス）")]
    public float dropHeight = 6.0f;

    [Label("落ちる時間（秒）", 0.05f)]
    [Tooltip("予告の最後のこの時間で落ちてくる")]
    public float dropTime = 0.35f;

    [Label("突き出る物（プレハブ）")]
    [Tooltip("「下から突き出る」のとき、地面から出てくる見た目（とげ・腕など）。当たり判定（Collider2D）が付いていれば、残っている間は壁になる")]
    public GameObject riseObject;

    [Label("残る時間（秒）", 0.0f)]
    [Tooltip("突き出た物が残る時間。0 ならすぐ引っ込む")]
    public float riseLinger = 1.0f;

    [Label("残っている間も当たる")]
    [Tooltip("オン：残っている間、範囲に入ると「当たる間隔」ごとに当たる")]
    public bool riseHitWhileLingering = false;

    [Label("置く物（プレハブ）")]
    [Tooltip("「設置して起爆」のとき、置かれる見た目（爆弾など）。空なら小さな丸")]
    public GameObject placeObject;

    [Label("起爆までの時間（秒）", 0.0f)]
    [Tooltip("置いてから爆発するまで。0 なら時間では爆発しない（触れる・攻撃されるで爆発）")]
    public float fuseTime = 3.0f;

    [Label("プレイヤーが触れたら爆発")]
    public bool detonateOnTouch = true;

    [Label("攻撃されたら爆発")]
    [Tooltip("ノアやシグルの攻撃が当たったら爆発する（敵を巻き込む使い方も）")]
    public bool detonateOnAttacked = false;

    [Label("起爆まで範囲を見せる")]
    [Tooltip("置いてから爆発するまで、爆発の範囲を表示する")]
    public bool showFuseRange = true;

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

    [Label("発動したときの画面の揺れ（マス）", 0.0f)]
    [Tooltip("地響き・落石などで画面を揺らす大きさ（0.1 で小さく、0.3 で大きく）。0 なら揺らさない")]
    public float activeShake = 0.0f;

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
