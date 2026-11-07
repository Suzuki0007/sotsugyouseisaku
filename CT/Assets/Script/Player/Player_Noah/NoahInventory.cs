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
        GetKey(null);
    }

    /// <summary>
    /// 鍵を手に入れる関数（アイテム画面に表示する鍵のデータを指定する）
    /// </summary>
    /// <param name="keyItem"> 鍵のアイテムデータ（null なら画面には出ない） </param>
    public void GetKey(ItemData keyItem)
    {
        InventoryManager.Instance.NoahData.hasKey = true;

        // アイテム画面に表示するため、所持アイテムにも追加する
        if(keyItem != null)
        {
            InventoryManager.Instance.AddItem(keyItem);
        }
        else
        {
            Debug.Log("鍵を手に入れた！");
        }
    }

    /// <summary>
    /// 鍵を1つ使う（アイテム欄からも消える）
    /// </summary>
    /// <returns> 鍵を持っていて使えたら true </returns>
    public bool UseKey()
    {
        return InventoryManager.Instance.ConsumeKey();
    }

    public bool HasKey()
    {
        // InventoryManagerのHasKeyプロパティの値を返す
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
