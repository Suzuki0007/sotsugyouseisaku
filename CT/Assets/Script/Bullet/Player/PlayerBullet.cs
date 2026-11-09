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

        // 味方（ノア・シグル）には当たらない
        if(collision.CompareTag("Player")) return;

        // 敵と、攻撃で作動するスイッチなど（攻撃を受ける部品）に当たる
        bool isTarget = collision.CompareTag("Enemies") || collision.GetComponent<DamageReceiver>() != null;

        // イベントの範囲・調べる範囲・シーン遷移などのトリガーは通り抜ける（弾どうしは今まで通りぶつかる）
        if(!isTarget && IgnoresTrigger(collision)) return;
        if(isTarget)
        {
            // DamageReceiver（部位など）があればそれ、なければ Health に当てる
            DamageUtility.Apply(collision, DamageInfo.Create(BulletDamage, DamageKind.PlayerBullet, shooter, transform.position));
        }

        PlayHitEffect(collision, isTarget);
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
