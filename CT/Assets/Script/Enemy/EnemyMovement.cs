/*********************************************************************/
// * \file   EnemyMovement.cs
// * \brief  敵の移動クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の移動クラス
/// </summary>
public class EnemyMovement : CharaBase
{
    public void Move(float speed)
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}
