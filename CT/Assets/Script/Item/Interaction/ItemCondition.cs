/*********************************************************************/
// * \file   ItemCondition.cs
// * \brief  アイテムの所持条件
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテムの所持条件（「鍵を1個以上持っている」「ランタンを持っていない」など）
/// </summary>
///
/// ItemInteraction のルールや ItemPossessionSwitch で使う
[Serializable]
public class ItemCondition
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("判定するアイテム")]
    public ItemData item;

    [Tooltip("必要な個数（この個数以上持っていれば満たす）")]
    [Min(1)]
    public int count = 1;

    [Tooltip("チェックすると「持っていないとき」に満たす")]
    public bool notHave;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// 必要な個数（0以下が入っていても1として扱う）
    /// </summary>
    public int RequiredCount => Mathf.Max(1, count);

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 条件を満たしているか
    /// </summary>
    public bool IsMet()
    {
        // アイテム未設定の条件は無視する（常に満たす）
        if(item == null) { return true; }

        int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(item) : 0;
        bool hasEnough = have >= RequiredCount;

        return notHave ? !hasEnough : hasEnough;
    }

    /// <summary>
    /// すべての条件を満たしているか（条件が空なら常に満たす）
    /// </summary>
    public static bool AllMet(List<ItemCondition> conditions)
    {
        if(conditions == null) { return true; }

        foreach(ItemCondition condition in conditions)
        {
            if(condition != null && !condition.IsMet()) { return false; }
        }
        return true;
    }

    /// <summary>
    /// 条件のアイテムを消費する（「持っていないとき」の条件は消費しない）
    /// </summary>
    public static void ConsumeAll(List<ItemCondition> conditions)
    {
        if(conditions == null || InventoryManager.Instance == null) { return; }

        foreach(ItemCondition condition in conditions)
        {
            if(condition == null || condition.item == null || condition.notHave) { continue; }

            InventoryManager.Instance.RemoveItem(condition.item, condition.RequiredCount);
        }
    }
}
