/*********************************************************************/
// * \file   Dialogue.cs
// * \brief  ダイアログクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ダイアログクラス
/// </summary>
public class Dialogue : Intaractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private string dialogueText;

    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private Text dialogueTextUI;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        // ダイアログを非表示にする
        HideDialogue();
    }

    //=====================================================================
    // インタラクション関数
    //=====================================================================

    protected override void Interact()
    {
        // ダイアログを表示する
        ShowDialogue();
    }

    /// <summary>
    /// ダイアログを表示する
    /// </summary>
    public void ShowDialogue()
    {
        if(dialoguePanel == null) { return; }
        if(dialogueTextUI == null) { return; }

        dialogueTextUI.text = dialogueText;
        dialoguePanel.SetActive(true);

        Debug.Log("ダイアログが有効");
    }

    /// <summary>
    /// ダイアログを非表示にする
    /// </summary>
    public void HideDialogue()
    {
        if(dialoguePanel == null) { return; }

        dialoguePanel.SetActive(false);

        Debug.Log("ダイアログが無効");
    }
}
