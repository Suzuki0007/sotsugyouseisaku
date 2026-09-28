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
    /// <summary>
    /// 鍵を手に入れる関数
    /// </summary>
    public void GetKey()
    {
        InventoryManager.Instance.NoahData.hasKey = true;

        Debug.Log("鍵を手に入れた！");
    }

    public bool HasKey()
    {
        // InventoryManagerのHasKeyプロパティの値を返す
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
