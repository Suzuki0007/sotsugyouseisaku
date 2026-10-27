/*********************************************************************/
// * \file   EnemyBullet.cs
// * \brief  敵の弾丸クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の弾丸クラス
/// </summary>
public class EnemyBullet : BulletBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private EnemyBulletPool bulletPool;

    //=====================================================================
    // 関数
    //=====================================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsReturned) return;

        if(collision.gameObject == shooter) return;

        if(collision.CompareTag("Player"))
        {
            Health health = collision.GetComponent<Health>();

            if(health != null)
            {
                health.Damage(BulletDamage);
            }
        }

        ReturnToPool();
    }

    /// <summary>
    /// プールの初期化
    /// </summary>
    /// <param name="pool"></param>
    public void InitializePool(EnemyBulletPool pool)
    {
        bulletPool = pool;
    }

    public override void ReturnToPool()
    {
        if(isReturned) return;
        isReturned = true;

        if(bulletPool != null)
        {
            bulletPool.ReturnBullet(this);
        }
        else
        {
            Debug.LogWarning($"{name}: 弾のプールが設定されていません", this);

            gameObject.SetActive(false);
        }
    }
}
