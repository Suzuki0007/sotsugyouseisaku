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

    [Tooltip("アイテム画面に表示する鍵のデータ。未設定なら Resources/Items にある ID「key」の ItemData を使う")]
    [SerializeField]
    private ItemData keyItemData;

    [Tooltip("拾ったことを記録するためのID。空ならヒエラルキー上の名前を使う（同じシーンで重複しないようにする）")]
    [SerializeField]
    private string pickupId;

    [Header("SE")]

    [SerializeField, Tooltip("鍵を拾ったとき")]
    private SeData pickupSe;

    // 拾ったことを記録するセーブ用の名前
    private string saveKey;

    //====================================================================================
    // 初期化
    //====================================================================================

    private void Awake()
    {
        saveKey = ItemPickup.MakeSaveKey(gameObject, pickupId);
    }

    private void Start()
    {
        // 拾ったことがあるなら消す（鍵は複数持てるので、持っていても拾っていなければ出す）
        if(Save.Get(saveKey, false))
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

        ItemData keyItem = keyItemData != null ? keyItemData : ItemData.Find(ItemData.KeyItemId);
        noahInventory.GetKey(keyItem);

        // 拾ったことを仮置きで記録（チェックポイント・シーン遷移で確定）
        Save.SetPending(saveKey, true);

        // オブジェクトを消す前に鳴らす (SeManager側で鳴るので消えても途切れない)
        SeManager.PlaySe(pickupSe, transform.position);

        gameObject.SetActive(false);

        Destroy(gameObject);

        //Debug.Log("鍵を拾いました");
    }
}
