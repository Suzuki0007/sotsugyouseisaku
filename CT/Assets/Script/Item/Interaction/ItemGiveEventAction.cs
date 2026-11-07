/*********************************************************************/
// * \file   ItemGiveEventAction.cs
// * \brief  イベントでアイテムを渡すアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントでアイテムを渡すアクション
/// EventSequence の actions リストに追加して使う
/// </summary>
public class ItemGiveEventAction : EventAction
{
    [SerializeField, Tooltip("渡すアイテム")]
    private ItemData item;

    [SerializeField, Min(1), Tooltip("渡す個数")]
    private int count = 1;

    [SerializeField, Tooltip("渡したときに鳴らすSE（任意）")]
    private SeData se;

    public override void Execute(GameObject gameObject)
    {
        if(item == null) { Debug.LogWarning($"{name}: 渡すアイテムが設定されていません", this); return; }
        if(InventoryManager.Instance == null) { return; }

        InventoryManager.Instance.AddItem(item, Mathf.Max(1, count));
        SeManager.PlaySe(se, transform.position);
    }

    // 復元時は何もしない（アイテムは所持品として保存されている）
}
