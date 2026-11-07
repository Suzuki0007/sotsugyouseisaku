/*********************************************************************/
// * \file   ItemSlotUI.cs
// * \brief  アイテム画面の一覧の1行
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// アイテム画面の一覧の1行
/// </summary>
///
/// マウスを乗せる・クリックすると、そのアイテムを選択する
public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("UI")]

    [SerializeField]
    private Image background;

    [Tooltip("アイコン（任意）")]
    [SerializeField]
    private Image icon;

    [SerializeField]
    private TMP_Text nameText;

    [Header("色")]

    [SerializeField]
    private Color normalColor = new Color(1f, 1f, 1f, 0.06f);

    [SerializeField]
    private Color selectedColor = new Color(1f, 0.85f, 0.4f, 0.35f);

    private ItemMenuUI owner;

    private int index;

    //=====================================================================
    // 設定
    //=====================================================================

    /// <summary>
    /// 表示するアイテムを設定する
    /// </summary>
    /// <param name="item"> 表示するアイテム </param>
    /// <param name="count"> 所持数 </param>
    /// <param name="slotIndex"> 一覧の何番目か </param>
    /// <param name="menu"> 選択を知らせる先 </param>
    public void Setup(ItemData item, int count, int slotIndex, ItemMenuUI menu)
    {
        owner = menu;
        index = slotIndex;

        if(nameText != null) { nameText.text = ItemMenuUI.FormatName(item, count); }

        if(icon != null)
        {
            icon.sprite = item.Icon;
            icon.preserveAspect = true;
            icon.enabled = item.Icon != null;
        }

        SetSelected(false);
    }

    /// <summary>
    /// 選択中の見た目にする
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if(background != null) { background.color = isSelected ? selectedColor : normalColor; }
    }

    //=====================================================================
    // マウス
    //=====================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(owner != null) { owner.Select(index); }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(owner != null) { owner.Select(index); }
    }
}
