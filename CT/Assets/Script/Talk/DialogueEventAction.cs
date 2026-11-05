/*********************************************************************/
// * \file   DialogueEventAction.cs
// * \brief  イベントでダイアログを表示するアクションのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// イベントでダイアログを表示するアクションのクラス
/// </summary>
public class DialogueEventAction : EventAction
{
    //=====================================================================
    // 列挙型
    //=====================================================================

    /// <summary>
    /// ダイアログを閉じるモード
    /// </summary>
    public enum DialogueCloseMode
    {
        Input,
        ExitArea
    }

    //=====================================================================
    // 変数
    //===================================================================== 

    [SerializeField]
    private Dialogue dialogue;

    [SerializeField]
    private DialogueCloseMode closeMode = DialogueCloseMode.ExitArea;

    [SerializeField]
    private Key closeKey = Key.E;

    private bool isShowing;

    //=====================================================================
    // 更新関数
    //=====================================================================

    private void Update()
    {
        if(!isShowing) { return; }
        if(closeMode != DialogueCloseMode.Input) { return; }
        if(Keyboard.current == null) { return; }

        // キーが押されたときにダイアログを閉じる
        if(Keyboard.current[closeKey].wasPressedThisFrame)
        {
            dialogue.HideDialogue();
        }
    }

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject player)
    {
        if(dialogue == null) { return; }

        dialogue.ShowDialogue();
        isShowing = true;
    }

    /// <summary>
    /// イベント範囲から出たときに呼び出す
    /// </summary>
    public override void OnExit(GameObject player)
    {
        if(closeMode != DialogueCloseMode.ExitArea) { return; }

        dialogue.HideDialogue();
        isShowing = false;
    }
}
