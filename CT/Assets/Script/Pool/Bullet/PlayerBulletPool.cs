/*********************************************************************/
// * \file   PlayerBulletPool.cs
// * \brief  プレイヤーの弾プールクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// プレイヤーの弾プールクラス
/// </summary>
public class PlayerBulletPool : BulletPoolBase<PlayerBullet>
{
    //=====================================================================
    // 関数
    //=====================================================================

    protected override void OnCreateBullet(PlayerBullet bullet)
    {
        base.OnCreateBullet(bullet);
    }

    protected override void OnBulletTaken(PlayerBullet bullet)
    {
        base.OnBulletTaken(bullet);
    }

    protected override void OnBulletReturned(PlayerBullet bullet)
    {
        base.OnBulletReturned(bullet);
    }
}
