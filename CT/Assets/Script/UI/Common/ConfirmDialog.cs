/*********************************************************************/
// * \file   ConfirmDialog.cs
// * \brief  「はい／いいえ」の確認ダイアログ
// *
// * \author
/*********************************************************************/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 「はい／いいえ」の確認ダイアログ
/// </summary>
///
/// Show でメッセージと、はい・いいえのときの処理を渡して開く
/// Esc / Backspace は「いいえ」と同じ。初めに選ばれているのは「いいえ」（誤って決定しないように）
public class ConfirmDialog : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("表示・非表示を切り替えるオブジェクト（空ならこのオブジェクト）")]
    private GameObject root;

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private Button yesButton;

    [SerializeField]
    private Button noButton;

    private Action onYes;
    private Action onNo;
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
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(yesButton != null) { yesButton.onClick.AddListener(() => Answer(true)); }
        if(noButton != null)  { noButton.onClick.AddListener(() => Answer(false)); }

        // キーボードの ←→ で「はい」「いいえ」の間だけを行き来する
        // （自動のままだと、後ろにある画面のボタンが選ばれてしまう）
        if(yesButton != null && noButton != null)
        {
            yesButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = noButton, selectOnRight = noButton };
            noButton.navigation  = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = yesButton, selectOnRight = yesButton };
        }
    }

    private void Update()
    {
        if(!isOpen || Keyboard.current == null) { return; }

        if(Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            Answer(false);
        }
    }

    //=====================================================================
    // 開閉
    //=====================================================================

    /// <summary>
    /// 確認ダイアログを開く
    /// </summary>
    /// <param name="message"> 表示する文（TextMeshPro のタグが使える） </param>
    /// <param name="yes"> 「はい」のときの処理 </param>
    /// <param name="no"> 「いいえ」・Esc のときの処理（任意） </param>
    public void Show(string message, Action yes, Action no = null)
    {
        onYes = yes;
        onNo = no;
        isOpen = true;

        if(messageText != null) { messageText.text = message; }
        Root.SetActive(true);

        if(EventSystem.current != null && noButton != null) { EventSystem.current.SetSelectedGameObject(noButton.gameObject); }
    }

    /// <summary>
    /// 何もせずに閉じる
    /// </summary>
    public void Hide()
    {
        if(isOpen) { closedFrame = Time.frameCount; }
        isOpen = false;
        onYes = null;
        onNo = null;
        Root.SetActive(false);
    }

    private void Answer(bool yes)
    {
        if(!isOpen) { return; }

        UISound.Play(yes ? UISoundType.Decide : UISoundType.Cancel);

        Action callback = yes ? onYes : onNo;
        Hide();
        callback?.Invoke();
    }
}
