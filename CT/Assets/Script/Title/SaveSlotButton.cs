/*********************************************************************/
// * \file   SaveSlotButton.cs
// * \brief  タイトル画面のセーブスロット1つ分のボタン
// *
// * \author
/*********************************************************************/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面のセーブスロット1つ分のボタン（スロット番号・場所・プレイ時間・保存日時を表示する）
/// </summary>
[RequireComponent(typeof(Button))]
public class SaveSlotButton : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("「スロット1」などを出す欄")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("場所・プレイ時間・保存日時を出す欄")]
    private TMP_Text detailText;

    private Button button;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public Button Button => button != null ? button : (button = GetComponent<Button>());

    public SaveSlotInfo Info { get; private set; }

    //=====================================================================
    // 表示
    //=====================================================================

    /// <summary>
    /// スロットの内容を表示する
    /// </summary>
    /// <param name="info"> スロットの概要 </param>
    /// <param name="placeName"> 場所の表示名 </param>
    /// <param name="selectable"> 選べるか（つづきからで空のスロットは選べない） </param>
    /// <param name="isCurrent"> 今プレイ中のスロットか（「プレイ中」と表示する） </param>
    public void Show(SaveSlotInfo info, string placeName, bool selectable, bool isCurrent = false)
    {
        Info = info;

        if(titleText != null) { titleText.text = isCurrent ? $"スロット {info.Slot}　<size=75%><color=#9fd0ff>プレイ中</color></size>" : $"スロット {info.Slot}"; }

        if(detailText != null)
        {
            if(!info.Exists)
            {
                detailText.text = "データなし";
            }
            else
            {
                string saved = info.SavedAt.HasValue ? info.SavedAt.Value.ToString("yyyy/MM/dd HH:mm") : "-";
                detailText.text = $"{placeName}　プレイ時間 {SaveSlotInfo.FormatPlayTime(info.PlayTime)}\n保存日時 {saved}";
            }
        }

        Button.interactable = selectable;
    }

    /// <summary>
    /// 押されたときの処理を設定する（前の設定は消す）
    /// </summary>
    public void SetOnClick(Action onClick)
    {
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => onClick?.Invoke());
    }
}
