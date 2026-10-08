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

    [SerializeField] private GameObject player;

    [SerializeField] private EnemyBullet bulletPrefab;

    //=====================================================================
    // 関数
    //=====================================================================

    void Start()
    {
        if(player == null) { Debug.LogError("プレイヤーが設定されていません"); }
        if(bulletPrefab == null) { Debug.LogError("弾のプレハブが設定されていません"); }
    }

    void Update()
    {
        // 一定間隔で弾を発射する
        if(Time.frameCount % 60 == 0)
        {
            Debug.Log("弾発射キーが押されてしまいそうになりたいでしょう");
            Shoot();
        }
    }

    public void Shoot()
    {
        // 弾を生成する
        EnemyBullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Vector2 direction = (player.transform.position - transform.position).normalized;
        bullet.SetDirection(direction);

        bullet.SetShooter(gameObject);
    }
}
