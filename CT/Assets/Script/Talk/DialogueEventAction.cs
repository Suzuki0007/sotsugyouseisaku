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
    ///
    /// Input    : 送りキーで最後の文まで進めると閉じる
    /// ExitArea : 送りキーでも閉じるが、エリアから出たときにも強制的に閉じる
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

    [Tooltip("旧方式（DialogueManagerがないシーン）で閉じるキー")]
    [SerializeField]
    private Key closeKey = Key.E;

    // 旧方式で表示中かどうか
    private bool isShowingLegacy;

    // 旧方式で表示したフレーム（同じフレームで閉じないため）
    private int shownFrame = -1;

    //=====================================================================
    // 更新関数
    //=====================================================================

    private void Update()
    {
        // DialogueManagerがあるシーンでは、送り・閉じる処理はDialogueManagerが行う
        if(!isShowingLegacy) { return; }
        if(closeMode != DialogueCloseMode.Input) { return; }
        if(Keyboard.current == null) { return; }
        if(Time.frameCount == shownFrame) { return; }

        // キーが押されたときにダイアログを閉じる
        if(Keyboard.current[closeKey].wasPressedThisFrame)
        {
            dialogue.HideDialogue();
            isShowingLegacy = false;
        }
    }

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject player)
    {
        if(dialogue == null) { return; }

        dialogue.Talk();

        if(dialogue.IsLegacy)
        {
            isShowingLegacy = true;
            shownFrame = Time.frameCount;
        }
    }

    /// <summary>
    /// イベント範囲から出たときに呼び出す
    /// </summary>
    public override void OnExit(GameObject player)
    {
        if(closeMode != DialogueCloseMode.ExitArea) { return; }
        if(dialogue == null) { return; }

        dialogue.Close();
        isShowingLegacy = false;
    }
}
