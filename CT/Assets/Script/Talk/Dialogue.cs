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

    public GameObject dialoguePanel;
    public Text dialogueTextUI;

    [SerializeField]
    private string dialogueText;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        if(dialoguePanel == null) { return; }
        if(dialogueTextUI == null) { return; }

        dialoguePanel.SetActive(false);
    }

    //=====================================================================
    // インタラクション関数
    //=====================================================================

    protected override void Interact()
    {
        if(dialoguePanel == null) { return; }
        if(dialogueTextUI == null) { return; }

        dialogueTextUI.text = dialogueText;
        dialoguePanel.SetActive(true);
        Debug.Log("ダイアログが有効");
    }

    //=====================================================================
    // 衝突関数
    //=====================================================================

    /// <summary>
    /// OnCollisionEnter2D関数は、他のオブジェクトとの衝突が開始したときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionEnter2D(Collision2D other)
    {
        //if(other.gameObject.CompareTag("Player"))
        //{
        //    dialogueTextUI.text = dialogueText;
        //    dialoguePanel.SetActive(true);
        //    Debug.Log("ダイアログが有効");
        //}
    }

    /// <summary>
    /// OnCollisionExit2D関数は、他のオブジェクトとの衝突が終了したときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionExit2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            dialoguePanel.SetActive(false);
            Debug.Log("ダイアログが無効");
        }
    }
}
