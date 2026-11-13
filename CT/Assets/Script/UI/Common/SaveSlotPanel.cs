/*********************************************************************/
// * \file   SaveSlotPanel.cs
// * \brief  セーブデータ選択画面（はじめから・つづきから・ロード・セーブで共通）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// セーブデータ選択画面
/// </summary>
///
/// Open(使い方, 決まったときの処理, 戻ったときの処理) で開く
/// ・NewGame  ：オートセーブ枠だけを出す。データがあれば上書きの確認
/// ・Continue ：オートセーブ枠＋手動の枠のうち、データのあるものだけ選べる。確認なし
/// ・Load     ：オートセーブ枠＋手動の枠のうち、データのあるものだけ選べる。「ロードしますか？」の確認
/// ・Save     ：手動の枠だけ（オートセーブ枠には手動でセーブしない）。使用中なら上書きの確認
/// オートセーブ枠の行は、1つ目のスロットのボタンを複製して一番上に足す（番号は Save.AutoSlot）
/// Esc / Backspace・「もどる」で戻る。ゲームを止めていても（Time.timeScale = 0）操作できる
public class SaveSlotPanel : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum Purpose
    {
        NewGame,
        Continue,
        Load,
        Save,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("表示・非表示を切り替えるオブジェクト（空ならこのオブジェクト）")]
    private GameObject root;

    [SerializeField, Tooltip("「つづきから：読み込むデータを選んでください」などを出す欄")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("スロットのボタン（上から スロット1, 2, 3 …）")]
    private List<SaveSlotButton> slotButtons = new List<SaveSlotButton>();

    [SerializeField]
    private Button backButton;

    [SerializeField, Tooltip("上書き・ロードの確認に使うダイアログ")]
    private ConfirmDialog confirm;

    [Header("SE（任意。空なら共通の UI 効果音：Resources/UISounds）")]

    [SerializeField]
    private SeData decideSe;

    [SerializeField]
    private SeData cancelSe;

    private Purpose purpose;
    private SaveSlotButton autoButton;
    private Action<int> onDecided;
    private Action onCancel;
    private bool isOpen;
    private int closedFrame = -1;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool IsOpen => isOpen;

    /// <summary>
    /// 開いている、または閉じた直後のフレームか（同じ Esc で親の画面まで閉じないように使う）
    /// </summary>
    public bool IsOpenOrJustClosed => isOpen || Time.frameCount == closedFrame;

    private GameObject Root => root != null ? root : gameObject;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        if(backButton != null) { backButton.onClick.AddListener(Cancel); }
    }

    private void Update()
    {
        if(!isOpen || Keyboard.current == null) { return; }
        if(confirm != null && confirm.IsOpenOrJustClosed) { return; }

        if(Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            Cancel();
        }
    }

    //=====================================================================
    // 開閉
    //=====================================================================

    /// <summary>
    /// 開く
    /// </summary>
    /// <param name="newPurpose"> 使い方 </param>
    /// <param name="decided"> スロットが決まったとき（確認があれば「はい」のあと）の処理 </param>
    /// <param name="cancel"> 戻ったときの処理 </param>
    public void Open(Purpose newPurpose, Action<int> decided, Action cancel)
    {
        purpose = newPurpose;
        onDecided = decided;
        onCancel = cancel;
        isOpen = true;

        Root.SetActive(true);
        if(confirm != null) { confirm.Hide(); }

        if(titleText != null) { titleText.text = TitleFor(purpose); }

        Refresh();
        SelectDefault();
    }

    /// <summary>
    /// 何もせずに閉じる
    /// </summary>
    public void Close()
    {
        if(isOpen) { closedFrame = Time.frameCount; }
        isOpen = false;
        if(confirm != null) { confirm.Hide(); }
        Root.SetActive(false);
    }

    /// <summary>
    /// スロットの表示を今のファイルの内容で更新する
    /// </summary>
    public void Refresh()
    {
        // オートセーブ枠（セーブ以外で出す）
        EnsureAutoButton();
        if(autoButton != null)
        {
            bool showAuto = purpose != Purpose.Save;
            autoButton.gameObject.SetActive(showAuto);
            if(showAuto)
            {
                SaveSlotInfo info = SaveSlotInfo.Load(Save.AutoSlot);
                bool selectable = purpose == Purpose.NewGame || info.Exists;
                bool isCurrent = purpose == Purpose.Load && Save.CurrentSlot == Save.AutoSlot;
                autoButton.Show(info, PlaceNameTable.GetName(info.SceneName), selectable, isCurrent);
                autoButton.SetOnClick(() => OnSlotClicked(Save.AutoSlot));
            }
        }

        // 手動の枠（はじめからでは出さない）
        for(int i = 0; i < slotButtons.Count; i++)
        {
            SaveSlotButton button = slotButtons[i];
            if(button == null) { continue; }

            int slot = i + 1;
            bool inRange = slot <= Save.SlotCount && purpose != Purpose.NewGame;
            button.gameObject.SetActive(inRange);
            if(!inRange) { continue; }

            SaveSlotInfo info = SaveSlotInfo.Load(slot);
            bool selectable = purpose == Purpose.Save || info.Exists;
            bool isCurrent = (purpose == Purpose.Save || purpose == Purpose.Load) && Save.CurrentSlot == slot;
            button.Show(info, PlaceNameTable.GetName(info.SceneName), selectable, isCurrent);
            button.SetOnClick(() => OnSlotClicked(slot));
        }
    }

    /// <summary>
    /// オートセーブ枠の行を作る（1つ目のスロットのボタンを複製して、一番上に置く）
    /// </summary>
    private void EnsureAutoButton()
    {
        if(autoButton != null || slotButtons.Count == 0 || slotButtons[0] == null) { return; }

        SaveSlotButton source = slotButtons[0];
        autoButton = Instantiate(source, source.transform.parent);
        autoButton.name = "SlotAuto";
        autoButton.transform.SetSiblingIndex(source.transform.GetSiblingIndex());

        // 行が1つ増えて1行が低くなり、文字がはみ出すので、すき間を詰めて並びの高さを広げる
        var list = source.transform.parent as RectTransform;
        var layout = list != null ? list.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() : null;
        if(layout != null)
        {
            const float RowHeight = 165.0f;
            layout.spacing = Mathf.Min(layout.spacing, 12.0f);
            int rows = slotButtons.Count + 1;
            float need = rows * RowHeight + (rows - 1) * layout.spacing + layout.padding.vertical;
            if(list.rect.height < need) { list.sizeDelta += new Vector2(0.0f, need - list.rect.height); }
        }
    }

    /// <summary>
    /// どれかのセーブデータ（オートセーブ枠か手動の枠）があるか
    /// </summary>
    public static bool AnySaveExists()
    {
        if(Save.SlotExists(Save.AutoSlot)) { return true; }
        for(int slot = 1; slot <= Save.SlotCount; slot++) { if(Save.SlotExists(slot)) { return true; } }
        return false;
    }

    //=====================================================================
    // 選択
    //=====================================================================

    private void OnSlotClicked(int slot)
    {
        if(!isOpen || SceneFader.IsBusy) { return; }
        if(confirm != null && confirm.IsOpen) { return; }

        UISound.Play(decideSe, UISoundType.Decide);

        string message = ConfirmMessage(purpose, slot);
        if(message == null || confirm == null)
        {
            Decide(slot);
            return;
        }

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        confirm.Show(message,
            () => Decide(slot),
            () =>
            {
                // 「いいえ」の音は確認ダイアログが鳴らす
                if(EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(selected); }
            });
    }

    private void Decide(int slot)
    {
        Action<int> callback = onDecided;
        callback?.Invoke(slot);
    }

    private void Cancel()
    {
        if(!isOpen) { return; }

        UISound.Play(cancelSe, UISoundType.Cancel);
        Action callback = onCancel;
        Close();
        callback?.Invoke();
    }

    //=====================================================================
    // 文言
    //=====================================================================

    private static string TitleFor(Purpose p)
    {
        switch(p)
        {
            case Purpose.NewGame:  return "はじめから：記録はオートセーブに残ります";
            case Purpose.Continue: return "つづきから：読み込むデータを選んでください";
            case Purpose.Load:     return "ロード：読み込むデータを選んでください";
            case Purpose.Save:     return "セーブ：保存するスロットを選んでください";
        }
        return "";
    }

    /// <summary>
    /// 確認の文（確認しないなら null）
    /// </summary>
    private static string ConfirmMessage(Purpose p, int slot)
    {
        bool exists = Save.SlotExists(slot);
        string name = slot == Save.AutoSlot ? "オートセーブ" : $"スロット {slot}";
        switch(p)
        {
            case Purpose.NewGame:
                return exists ? $"{name}のデータを上書きして\nはじめから始めますか？\n<size=80%>（手動でセーブしたスロット 1〜{Save.SlotCount} は残ります）</size>" : null;

            case Purpose.Load:
                return $"{name}のデータをロードしますか？\n<size=80%>（最後にセーブしたあとの進行は失われます）</size>";

            case Purpose.Save:
                if(!exists) { return null; }
                return slot == Save.CurrentSlot
                    ? $"スロット {slot} にセーブしますか？"
                    : $"スロット {slot} のデータに上書きしてセーブしますか？\n<size=80%>（上書きしたデータは元に戻せません）</size>";
        }
        return null;
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private void SelectDefault()
    {
        if(EventSystem.current == null) { return; }

        GameObject target = null;

        // セーブ・ロードでは、プレイ中のスロットを最初に選ぶ
        if((purpose == Purpose.Save || purpose == Purpose.Load) && Save.HasSlot)
        {
            SaveSlotButton current = null;
            if(Save.CurrentSlot == Save.AutoSlot) { current = autoButton; }
            else if(Save.CurrentSlot >= 1 && Save.CurrentSlot - 1 < slotButtons.Count) { current = slotButtons[Save.CurrentSlot - 1]; }
            if(current != null && current.gameObject.activeSelf && current.Button.interactable) { target = current.gameObject; }
        }

        if(target == null)
        {
            var ordered = new List<SaveSlotButton>();
            if(autoButton != null) { ordered.Add(autoButton); }
            ordered.AddRange(slotButtons);
            foreach(SaveSlotButton b in ordered)
            {
                if(b != null && b.gameObject.activeSelf && b.Button.interactable) { target = b.gameObject; break; }
            }
        }
        if(target == null && backButton != null) { target = backButton.gameObject; }

        EventSystem.current.SetSelectedGameObject(target);
    }
}
