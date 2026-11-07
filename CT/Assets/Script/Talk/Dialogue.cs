/*********************************************************************/
// * \file   Dialogue.cs
// * \brief  ダイアログクラス
// *
// * \author 成田悠真
/*********************************************************************/

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ダイアログクラス
/// </summary>
///
/// 調べたとき（またはDialogueEventActionから呼ばれたとき）に会話を表示する。
/// ・シーンにDialogueManagerがある場合：DialogueManagerで複数の文を表示する
///   （dialogueDataが未設定なら、dialogueTextを1文の会話として表示する）
/// ・シーンにDialogueManagerがない場合：旧方式（dialoguePanel + Text）で表示する
public class Dialogue : Intaractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("会話データ")]

    [Tooltip("表示する会話。未設定なら下のDialogue Textを1文として表示する")]
    [SerializeField]
    private DialogueData dialogueData;

    [Header("旧方式（DialogueManagerがないシーン用）")]

    [SerializeField]
    private string dialogueText;

    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private Text dialogueTextUI;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// 旧方式で表示するかどうか（シーンにDialogueManagerがない）
    /// </summary>
    public bool IsLegacy => DialogueManager.Instance == null;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        // 旧方式のパネルは非表示にしておく
        if(dialoguePanel != null) { dialoguePanel.SetActive(false); }
    }

    //=====================================================================
    // インタラクション関数
    //=====================================================================

    protected override void Interact()
    {
        Talk();
    }

    /// <summary>
    /// 会話を開始する
    /// </summary>
    /// <param name="finished"> 会話終了時に呼ばれる処理（DialogueManager使用時のみ） </param>
    public void Talk(Action finished = null)
    {
        if(IsLegacy)
        {
            ShowDialogue();
            return;
        }

        if(dialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueData, finished);
        }
        else if(!string.IsNullOrEmpty(dialogueText))
        {
            // 会話データ未設定なら、旧方式のテキストを1文の会話として表示する
            DialogueData.Line[] lines = { new DialogueData.Line("", dialogueText) };
            DialogueManager.Instance.StartDialogue(lines, finished);
        }
    }

    /// <summary>
    /// 会話を閉じる
    /// </summary>
    public void Close()
    {
        if(IsLegacy)
        {
            HideDialogue();
            return;
        }

        DialogueManager.Instance.ForceClose();
    }

    //=====================================================================
    // 旧方式の表示関数
    //=====================================================================

    /// <summary>
    /// ダイアログを表示する（旧方式）
    /// </summary>
    public void ShowDialogue()
    {
        if(dialoguePanel == null) { return; }
        if(dialogueTextUI == null) { return; }

        dialogueTextUI.text = dialogueText;
        dialoguePanel.SetActive(true);
    }

    /// <summary>
    /// ダイアログを非表示にする（旧方式）
    /// </summary>
    public void HideDialogue()
    {
        if(dialoguePanel == null) { return; }

        dialoguePanel.SetActive(false);
    }
}
