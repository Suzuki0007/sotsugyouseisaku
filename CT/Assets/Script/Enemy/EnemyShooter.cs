/*********************************************************************/
// * \file   EnemyShooter.cs
// * \brief  敵の弾発射クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 敵の弾発射クラス
/// </summary>
public class EnemyShooter : CharaBase
{
    //=====================================================================
    // 変数
    //===================================================================== 

    [SerializeField]
    private float shootInterval = 1f;

    private GameObject player;

    private EnemyBulletPool enemyBulletPool;

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

        // プールから弾を取得する
        EnemyBullet bullet = enemyBulletPool.GetBullet(transform.position, Quaternion.identity);

        // プレイヤーの方向を計算する
        Vector2 direction = (player.transform.position - transform.position).normalized;

        // 弾に必要な情報を設定する
        bullet.SetDirection(direction);
        bullet.SetShooter(gameObject);
    }

    public void InitializeReference(GameObject playerObject, EnemyBulletPool enemyBulletPool)
    {
        this.player = playerObject.transform.gameObject;
        this.enemyBulletPool = enemyBulletPool;

        if(player == null || enemyBulletPool == null)
        {
            Debug.LogError("プレイヤーまたは弾のプールが設定されていません");
            enabled = false;
            return;
        }
    }
}
