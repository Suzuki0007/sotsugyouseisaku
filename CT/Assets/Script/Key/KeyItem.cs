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
    [SerializeField] private NoahInventory noahInventory;

    private void Start()
    {
        if(noahInventory != null && noahInventory.HasKey())
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Interact関数は、プレイヤーが鍵を拾うときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        if(noahInventory == null){ return; }
        if(noahInventory.HasKey()) { return; }

        noahInventory.GetKey();

        gameObject.SetActive(false);

        //Debug.Log("鍵を拾いました");
    }
}
