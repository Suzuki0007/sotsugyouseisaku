/*********************************************************************/
// * \file   DamageInfo.cs
// * \brief  攻撃の情報（量・種類・攻撃したキャラ・当たった位置など）
// *
// * \author
/*********************************************************************/

using UnityEngine;

//=====================================================================
// 型
//=====================================================================

/// <summary>
/// 攻撃の種類。敵ごとの効き方・ギミックの反応などで使う
/// </summary>
/// 値はセーブ・アセットに保存されるので、途中に挿入せず末尾に足すこと（番号を変えない）
public enum DamageKind
{
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
/// プレイヤーのキャラ
/// </summary>
public enum PlayerCharacter
{
    [InspectorName("なし（プレイヤー以外）")]
    None = 0,

    [InspectorName("ノア")]
    Noah = 1,

    [InspectorName("シグル")]
    Sigle = 2,
}

/// <summary>
/// ゲージ攻撃の判定の結果（ゲージ攻撃以外は None）
/// </summary>
public enum GaugeJudge
{
    [InspectorName("なし")]
    None = 0,

    [InspectorName("ミス")]
    Miss = 1,

    [InspectorName("グッド")]
    Good = 2,

    [InspectorName("パーフェクト")]
    Perfect = 3,
}

/// <summary>
/// 攻撃の情報
/// </summary>
///
/// Health.Damage(int) は今までどおり使える（種類「その他」として扱う）
/// 誰の・どの攻撃かで反応を変えたいときは Health.Damage(DamageInfo) を使う
public struct DamageInfo
{
    //=====================================================================
    // 変数
    //=====================================================================

    /// <summary> ダメージ量 </summary>
    public int amount;

    /// <summary> 攻撃の種類 </summary>
    public DamageKind kind;

    /// <summary> 攻撃したオブジェクト（弾なら撃った人。分からなければ null） </summary>
    public GameObject attacker;

    /// <summary> 攻撃したプレイヤーのキャラ（プレイヤー以外なら None） </summary>
    public PlayerCharacter character;

    /// <summary> 当たった位置 </summary>
    public Vector2 point;

    /// <summary> ゲージ攻撃の判定の結果 </summary>
    public GaugeJudge judge;

    /// <summary> ひるみを数えない（部位が自分でひるみを数えてから本体に渡すとき、二重に数えないため） </summary>
    public bool noStagger;

    //=====================================================================
    // 作成
    //=====================================================================

    /// <summary>
    /// 量だけの攻撃（種類「その他」）
    /// </summary>
    public static DamageInfo Simple(int amount)
    {
        return new DamageInfo { amount = amount, kind = DamageKind.Other };
    }

    /// <summary>
    /// 攻撃の情報を作る（攻撃したキャラは attacker から自動で決める）
    /// </summary>
    public static DamageInfo Create(int amount, DamageKind kind, GameObject attacker, Vector2 point)
    {
        return new DamageInfo
        {
            amount = amount,
            kind = kind,
            attacker = attacker,
            character = PlayerCharacters.Of(attacker),
            point = point,
            judge = GaugeJudge.None,
        };
    }

    /// <summary>
    /// 量だけを変えたコピー
    /// </summary>
    public DamageInfo WithAmount(int newAmount)
    {
        DamageInfo copy = this;
        copy.amount = newAmount;
        return copy;
    }
}

/// <summary>
/// プレイヤーのキャラの判定
/// </summary>
public static class PlayerCharacters
{
    /// <summary>
    /// そのオブジェクトがどのキャラか（ノア・シグル以外は None）
    /// </summary>
    public static PlayerCharacter Of(GameObject target)
    {
        if(target == null) { return PlayerCharacter.None; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return PlayerCharacter.None; }

        if(target == manager.Noah)  { return PlayerCharacter.Noah; }
        if(target == manager.Sigle) { return PlayerCharacter.Sigle; }
        return PlayerCharacter.None;
    }

    /// <summary>
    /// 今操作しているキャラ
    /// </summary>
    public static PlayerCharacter Current
    {
        get
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            return manager != null ? Of(manager.CurrentPlayer) : PlayerCharacter.None;
        }
    }

    /// <summary>
    /// キャラのオブジェクト（いなければ null）
    /// </summary>
    public static GameObject Get(PlayerCharacter character)
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return null; }

        switch(character)
        {
            case PlayerCharacter.Noah:  return manager.Noah;
            case PlayerCharacter.Sigle: return manager.Sigle;
            default:                    return null;
        }
    }

    /// <summary>
    /// パーティーの体力（2人で共有しているのでノアの Health）
    /// </summary>
    public static Health PartyHealth
    {
        get
        {
            GameObject noah = Get(PlayerCharacter.Noah);
            return noah != null ? noah.GetComponent<Health>() : null;
        }
    }
}
