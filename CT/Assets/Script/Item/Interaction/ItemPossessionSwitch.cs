/*********************************************************************/
// * \file   ItemPossessionSwitch.cs
// * \brief  アイテムを持っている間だけオブジェクトを切り替えるクラス
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテムを持っている間だけオブジェクトを表示・非表示にするクラス
/// （ランタンを持っていると明かりがつく、鍵を持っていると扉が光る、など）
/// </summary>
///
/// ・条件をすべて満たしている間「満たしているとき」のオブジェクトを表示し、
///   満たしていない間は「満たしていないとき」のオブジェクトを表示する
/// ・所持品が変わった瞬間に切り替わる
/// ・このコンポーネント自身のオブジェクトは切り替え対象にしない（別の常に表示されているオブジェクトに付ける）
public class ItemPossessionSwitch : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("条件（すべて満たしたとき）")]

    [SerializeField]
    private List<ItemCondition> conditions = new List<ItemCondition>();

    [Header("切り替えるオブジェクト")]

    [SerializeField, Tooltip("条件を満たしている間だけ表示する")]
    private List<GameObject> activeWhenMet = new List<GameObject>();

    [SerializeField, Tooltip("条件を満たしていない間だけ表示する")]
    private List<GameObject> activeWhenNotMet = new List<GameObject>();

    private InventoryManager subscribed;

    //=====================================================================
    // 初期化
    //=====================================================================

    // InventoryManager はシーン読み込み後に自動生成されることがあるので、Start で登録する
    private void Start()
    {
        WarnIfSelfTarget();

        subscribed = InventoryManager.Instance;
        if(subscribed != null) { subscribed.OnChanged += Refresh; }

        Refresh();
    }

    private void OnDestroy()
    {
        if(subscribed != null) { subscribed.OnChanged -= Refresh; }
    }

    //=====================================================================
    // 切り替え
    //=====================================================================

    /// <summary>
    /// 所持状況に合わせて表示を切り替える
    /// </summary>
    public void Refresh()
    {
        bool isMet = ItemCondition.AllMet(conditions);

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

    private void WarnIfSelfTarget()
    {
        if(activeWhenMet.Contains(gameObject) || activeWhenNotMet.Contains(gameObject))
        {
            Debug.LogWarning($"{name}: 自分自身が切り替え対象に入っています。非表示になると切り替えが止まるため、別のオブジェクトに付けてください", this);
        }
    }
}
