/*********************************************************************/
// * \file   NoahInventory.cs
// * \brief  ノアのインベントリクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// ノアのインベントリクラス
/// </summary>
public class NoahInventory : CharaBase
{
    public bool hasKey = false;

    /// <summary>
    /// 鍵を手に入れる関数
    /// </summary>
    public void GetKey()
    {
        hasKey = true;

        Debug.Log("鍵を手に入れた！");
    }
}
