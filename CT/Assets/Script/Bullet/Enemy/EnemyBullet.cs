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

        // 弾消しエリアに入った場合は「消えたとき」のエフェクトにする
        if(collision.GetComponent<BulletDeleteArea>() != null)
        {
            Vanish();
            return;
        }

        // 味方（ほかの敵）には当たらない
        if(collision.CompareTag("Enemies")) return;

        bool isTarget = collision.CompareTag("Player");

        // イベントの範囲・調べる範囲・シーン遷移などのトリガーは通り抜ける（弾どうしは今まで通りぶつかる）
        if(!isTarget && IgnoresTrigger(collision)) return;
        if(isTarget)
        {
            Health health = collision.GetComponent<Health>();

            if(health != null)
            {
                health.Damage(BulletDamage);
            }
        }

        PlayHitEffect(collision, isTarget);
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
