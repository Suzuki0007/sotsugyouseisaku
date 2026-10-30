/*********************************************************************/
// * \file   SigleShooter.cs
// * \brief  シグルの弾発射クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// シグルの弾発射クラス
/// </summary>
public class SigleShooter : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private PlayerBulletPool bulletPool;

    //=====================================================================
    // 初期化
    //=====================================================================

    public void InitializeReference(PlayerBulletPool bulletPool)
    {
        this.bulletPool = bulletPool;
        if(this.bulletPool == null)
        {
            Debug.LogError($"{name}: PlayerBulletPoolが設定されていません");
            return;
        }
    }

    //=====================================================================
    // 発射関数
    //=====================================================================

    /// <summary>
    /// 弾発射関数
    /// </summary>
    /// <param name="mouseClickPosition"> マウスクリック位置（ワールド座標） </param>
    public void Shoot(Vector2 mouseClickPosition)
    {
        if(bulletPool == null) { return; }

        // マウスクリック位置から弾の方向を計算する
        Vector2 direction = (mouseClickPosition - (Vector2)transform.position).normalized;
        if(direction.sqrMagnitude <= 0f) { return; }

        PlayerBullet bullet = bulletPool.GetBullet(transform.position, Quaternion.identity);
        if(bullet == null) { return; }

        // 弾に必要な情報を設定する
        bullet.SetShooter(gameObject);
        bullet.SetDirection(direction);
    }
}
