/*********************************************************************/
// * \file   DialogueEventAction.cs
// * \brief  イベントでダイアログを表示するアクションのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using System.Collections;
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

    [SerializeField, Label("会話データ")]
    [Tooltip("出す会話（Assets/Data/Dialogue/ の DialogueData）。ここに入れれば、下の Dialogue（子の「会話」オブジェクト）は要りません")]
    private DialogueData dialogueData;

    [SerializeField]
    [Tooltip("（前の作り方）会話を持つ Dialogue。上の「会話データ」が空のときだけ使う")]
    private Dialogue dialogue;

    [SerializeField]
    private DialogueCloseMode closeMode = DialogueCloseMode.ExitArea;

    [Tooltip("順番に実行するとき、会話を閉じるまで次の部品を待たせる")]
    [SerializeField]
    private bool waitUntilClosed = true;

    [Tooltip("旧方式（DialogueManagerがないシーン）で閉じるキー")]
    [SerializeField]
    private Key closeKey = Key.E;

    // 旧方式で表示中かどうか
    private bool isShowingLegacy;

    // 旧方式で表示したフレーム（同じフレームで閉じないため）
    private int shownFrame = -1;

    // 会話データを直接出して、まだ閉じていないか（エリアから出たときに閉じるため）
    private bool talkingDirect;

    /// <summary> 直接入れた会話データ（エディタの表示用。前の作り方の Dialogue の中身はエディタ側で読む） </summary>
    public DialogueData DirectData => dialogueData;

    /// <summary> 前の作り方の Dialogue（エディタの表示用） </summary>
    public Dialogue LegacyDialogue => dialogue;

    /// <summary> 会話が設定されているか </summary>
    public bool HasDialogue => dialogueData != null || dialogue != null;

    /// <summary> エディタ・レシピ用：会話データを入れる </summary>
    public void SetDialogueData(DialogueData data) { dialogueData = data; }

    // 会話データを直接出すか（DialogueManager があるシーンだけ）
    private bool UseDirect => dialogueData != null && DialogueManager.Instance != null;

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
        if(UseDirect)
        {
            talkingDirect = true;
            DialogueManager.Instance.StartDialogue(dialogueData, () => talkingDirect = false);
            return;
        }
        if(dialogueData != null && dialogue == null)
        {
            Debug.LogWarning($"{name}: このシーンには DialogueManager（会話の画面）がないので、会話データを出せません", this);
            return;
        }
        if(dialogue == null) { return; }

        dialogue.Talk();

        if(dialogue.IsLegacy)
        {
            isShowingLegacy = true;
            shownFrame = Time.frameCount;
        }
    }

    /// <summary>
    /// 順番に実行するとき：会話を閉じるまで待つ
    /// </summary>
    public override IEnumerator Run(GameObject player)
    {
        // 待たない設定・旧方式（閉じたことが分からない）なら、表示だけしてすぐ次へ
        if(!waitUntilClosed || (!UseDirect && (dialogue == null || dialogue.IsLegacy)))
        {
            Execute(player);
            yield break;
        }

        // 別の会話が表示中なら、閉じるまで待ってから始める
        while(DialogueManager.Instance != null && DialogueManager.Instance.IsTalking) { yield return null; }
        if(DialogueManager.Instance == null) { yield break; }

        bool closed = false;
        if(UseDirect)
        {
            talkingDirect = true;
            DialogueManager.Instance.StartDialogue(dialogueData, () => { closed = true; talkingDirect = false; });
        }
        else
        {
            dialogue.Talk(() => closed = true);
        }

        // 会話データが空などで始まらなかったときは待たない
        if(!DialogueManager.Instance.IsTalking) { yield break; }

        while(!closed) { yield return null; }
    }

    /// <summary>
    /// イベント範囲から出たときに呼び出す
    /// </summary>
    public override void OnExit(GameObject player)
    {
        if(closeMode != DialogueCloseMode.ExitArea) { return; }
        if(talkingDirect)
        {
            if(DialogueManager.Instance != null) { DialogueManager.Instance.ForceClose(); }
            talkingDirect = false;
            return;
        }
        if(dialogue == null) { return; }

        dialogue.Close();
        isShowingLegacy = false;
    }
}
