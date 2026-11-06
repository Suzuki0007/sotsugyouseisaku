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
    //====================================================================================
    // 変数
    //====================================================================================

    [SerializeField]
    private NoahInventory noahInventory;

    [Header("SE")]

    [SerializeField, Tooltip("鍵を拾ったとき")]
    private SeData pickupSe;

    //====================================================================================
    // 初期化
    //====================================================================================

    private void Start()
    {
        if(noahInventory != null && noahInventory.HasKey())
        {
            gameObject.SetActive(false);
        }
    }

    //===================================================================================
    // インタラクション関数
    //===================================================================================

    /// <summary>
    /// Interact関数は、プレイヤーが鍵を拾うときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        if(noahInventory == null){ return; }
        if(noahInventory.HasKey()) { return; }

        noahInventory.GetKey();

        // オブジェクトを消す前に鳴らす (SeManager側で鳴るので消えても途切れない)
        SeManager.PlaySe(pickupSe, transform.position);

        gameObject.SetActive(false);

        Destroy(gameObject);

        //Debug.Log("鍵を拾いました");
    }
}
