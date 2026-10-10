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
    private GameObject player;

    //[SerializeField] 
    //private EnemyBullet bulletPrefab;

    [SerializeField]
    private float shootInterval = 1f;

    [SerializeField]
    private EnemyBulletPool enemyBulletPool;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        if(player == null)          { Debug.LogError("プレイヤーが設定されていません"); }
        //if(bulletPrefab == null)    { Debug.LogError("弾のプレハブが設定されていません"); }
        if(shootInterval <= 0)      { Debug.LogError("弾の発射間隔が正しく設定されていません"); }
        if(enemyBulletPool == null)      { Debug.LogError("弾のプールが設定されていません"); }
    }

    void Update()
    {
        //// 一定間隔で弾を発射する
        //if(Time.frameCount % (int)(shootInterval * 60) == 0)
        //{
        //    Debug.Log("弾発射キーが押されてしまいそうになりたいでしょう");

        //    // 弾を発射する
        //    Shoot();
        //}
    }

    public void Shoot()
    {
        //// 弾を生成する
        //EnemyBullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        //Vector2 direction = (player.transform.position - transform.position).normalized;
        //bullet.SetDirection(direction);

        //bullet.SetShooter(gameObject);




        // プールから弾を取得する
        EnemyBullet bullet = enemyBulletPool.GetBullet(transform.position, Quaternion.identity);

        // プレイヤーの方向を計算する
        Vector2 direction = (player.transform.position - transform.position).normalized;

        // 弾に必要な情報を設定する
        bullet.SetDirection(direction);
        bullet.SetShooter(gameObject);
    }
}
