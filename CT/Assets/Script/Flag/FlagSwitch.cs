/*********************************************************************/
// * \file   FlagSwitch.cs
// * \brief  フラグの状態でオブジェクトを切り替えるクラス
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フラグ（と、必要ならアイテム）の状態でオブジェクトを表示・非表示にするクラス
/// （ボスを倒したら扉が消える、NPC と話したら橋がかかる、など）
/// </summary>
///
/// ・条件をすべて満たしている間「満たしているとき」のオブジェクトを表示し、
///   満たしていない間は「満たしていないとき」のオブジェクトを表示する
/// ・フラグや所持品が変わった瞬間に切り替わる。シーンを読み込み直しても、セーブされたフラグで切り替わる
/// ・このコンポーネント自身のオブジェクトは切り替え対象にしない（別の常に表示されているオブジェクトに付ける）
public class FlagSwitch : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("条件（すべて満たしたとき）")]

    [SerializeField]
    private List<FlagCondition> flagConditions = new List<FlagCondition>();

    [SerializeField, Tooltip("アイテムの条件も付けたいときだけ使う（任意）")]
    private List<ItemCondition> itemConditions = new List<ItemCondition>();

    [Header("切り替えるオブジェクト")]

    [SerializeField, Tooltip("条件を満たしている間だけ表示する")]
    private List<GameObject> activeWhenMet = new List<GameObject>();

    [SerializeField, Tooltip("条件を満たしていない間だけ表示する")]
    private List<GameObject> activeWhenNotMet = new List<GameObject>();

    private InventoryManager subscribedInventory;

    //=====================================================================
    // 初期化
    //=====================================================================

    // InventoryManager はシーン読み込み後に自動生成されることがあるので、Start で登録する
    private void Start()
    {
        if(activeWhenMet.Contains(gameObject) || activeWhenNotMet.Contains(gameObject))
        {
            Debug.LogWarning($"{name}: 自分自身が切り替え対象に入っています。非表示になると切り替えが止まるため、別のオブジェクトに付けてください", this);
        }

        GameFlags.OnChanged += Refresh;

        subscribedInventory = InventoryManager.Instance;
        if(subscribedInventory != null) { subscribedInventory.OnChanged += Refresh; }

        Refresh();
    }

    private void OnDestroy()
    {
        GameFlags.OnChanged -= Refresh;
        if(subscribedInventory != null) { subscribedInventory.OnChanged -= Refresh; }
    }

    //=====================================================================
    // 切り替え
    //=====================================================================

    /// <summary>
    /// 条件に合わせて表示を切り替える
    /// </summary>
    public void Refresh()
    {
        bool isMet = FlagCondition.AllMet(flagConditions) && ItemCondition.AllMet(itemConditions);

        SetAll(activeWhenMet, isMet);
        SetAll(activeWhenNotMet, !isMet);
    }

    private static void SetAll(List<GameObject> targets, bool active)
    {
        foreach(GameObject target in targets)
        {
            if(target != null && target.activeSelf != active) { target.SetActive(active); }
        }
    }
}
