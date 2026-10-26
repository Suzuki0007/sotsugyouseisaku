/*********************************************************************/
// * \file   EnemyBulletPool.cs
// * \brief  敵の弾のプールクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の弾のプールクラス
/// </summary>
public class EnemyBulletPool : BulletPoolBase<EnemyBullet>
{
    protected override void OnCreateBullet(EnemyBullet bullet)
    {
        base.OnCreateBullet(bullet);
        bullet.InitializePool(this);
    }

    protected override void OnBulletTaken(EnemyBullet bullet)
    {
        base.OnBulletTaken(bullet);
    }

    protected override void OnBulletReturned(EnemyBullet bullet)
    {
        base.OnBulletReturned(bullet);
    }
}
