/*********************************************************************/
// * \file   ItemTakeEventAction.cs
// * \brief  イベントでアイテムを消費するアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントでアイテムを消費する（取り上げる）アクション
/// EventSequence の actions リストに追加して使う
/// </summary>
///
/// ItemInteraction のルールの「条件のアイテムを消費する」とは別に、
/// 条件にないアイテムを取り上げたいときに使う
public class ItemTakeEventAction : EventAction
{
    [SerializeField, Tooltip("消費するアイテム")]
    private ItemData item;

    [SerializeField, Min(1), Tooltip("消費する個数")]
    private int count = 1;

    [SerializeField, Tooltip("足りないときは、持っている分だけ消費する")]
    private bool takeAvailable = true;

    public override void Execute(GameObject gameObject)
    {
        if(item == null) { Debug.LogWarning($"{name}: 消費するアイテムが設定されていません", this); return; }
        if(InventoryManager.Instance == null) { return; }

        int amount = Mathf.Max(1, count);
        if(takeAvailable) { amount = Mathf.Min(amount, InventoryManager.Instance.GetCount(item)); }
        if(amount <= 0) { return; }

        InventoryManager.Instance.RemoveItem(item, amount);
    }

    // 復元時は何もしない（所持品の変化は保存されている）
}
