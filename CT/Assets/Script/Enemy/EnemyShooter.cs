/*********************************************************************/
// * \file   EnemyShooter.cs
// * \brief  敵の弾発射クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の弾発射クラス
/// </summary>
public class EnemyShooter : CharaBase
{
    //=====================================================================
    // 変数
    //===================================================================== 

    [SerializeField]
    private float shootInterval = 0f;

    [Header("エフェクト")]

    [SerializeField, Tooltip("弾を撃ったとき (右向きの絵なら撃った方向に回転して出る)")]
    private EffectData shotEffect;

    private GameObject player;

    private EnemyBulletPool enemyBulletPool;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public float ShootInterval => shootInterval;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        if(shootInterval <= 0) { Debug.LogError("弾の発射間隔が正しく設定されていません"); }
    }

    public void Shoot()
    {
        if(player == null || enemyBulletPool == null) { return; }

        // 死亡アニメ中など、コンポーネントが止められているときは撃たない
        if(!enabled) { return; }

        // プールから弾を取得する
        EnemyBullet bullet = enemyBulletPool.GetBullet(transform.position, Quaternion.identity);

        // プレイヤーの方向を計算する
        Vector2 direction = (player.transform.position - transform.position).normalized;

        // 弾に必要な情報を設定する
        bullet.SetDirection(direction);
        bullet.SetShooter(gameObject);

        EffectManager.Play(shotEffect, transform.position, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    /// <summary>
    /// 参照の初期化関数
    /// </summary>
    /// <param name="playerObject"></param>
    /// <param name="enemyBulletPool"></param>
    public void InitializeReference(GameObject playerObject, EnemyBulletPool enemyBulletPool)
    {
        if(playerObject == null || enemyBulletPool == null)
        {
            //Debug.Log("プレイヤーまたは弾のプールが設定されていません");
            return;
        }

        this.player = playerObject.transform.gameObject;
        this.enemyBulletPool = enemyBulletPool;

        if(player == null || enemyBulletPool == null)
        {
            //Debug.LogError("プレイヤーまたは弾のプールが設定されていません");
            enabled = false;
            return;
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetTargetPlayer(GameObject targetPlayer)
    {
        if(targetPlayer == null) { Debug.LogError("ターゲットプレイヤーの設定に失敗しました", this); }
        player = targetPlayer;
    }
}
