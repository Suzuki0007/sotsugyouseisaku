/*********************************************************************/
// * \file   KeyItem.cs
// * \brief  キーのアイテムクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キーのアイテムクラス
/// </summary>
public class KeyItem : Intaractable
{
    /// <summary>
    /// Interact関数は、プレイヤーが鍵を拾うときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        NoahInventory noahInventory = player.GetComponent<NoahInventory>();
        if(noahInventory == null){ return; }

        noahInventory.GetKey();

        gameObject.SetActive(false);

        //Debug.Log("鍵を拾いました");
    }
}
