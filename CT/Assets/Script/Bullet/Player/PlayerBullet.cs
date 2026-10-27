/*********************************************************************/
// * \file   PlayerBullet.cs
// * \brief  プレイヤーの弾クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// プレイヤーの弾クラス
/// </summary>
public class PlayerBullet : BulletBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    PlayerBulletPool bulletPool;

    //=====================================================================
    // 関数
    //=====================================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsReturned) return;

        if(collision.gameObject == shooter) return;

        if(collision.CompareTag("Enemies"))
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
    public void InitializePool(PlayerBulletPool pool)
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
