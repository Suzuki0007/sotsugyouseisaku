/*********************************************************************/
// * \file   NoteData.cs
// * \brief  メモ・手紙・日記などの読み物（Assets/Resources/Notes/ に置く）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// メモ
/// </summary>
///
/// ・拾う（NotePickup）・イベントでもらう（NoteEventAction）と、その場で読む画面が出て、メニューの「メモ」でいつでも読み返せる
/// ・**Assets/Resources/Notes/ に置く**（メニューの一覧に出すため）
/// ・持っているかはセーブされる（Note/<ID>）
[CreateAssetMenu(menuName = "仕掛け/メモ", fileName = "Note_")]
public class NoteData : ScriptableObject
{
    [Label("ID")]
    [Tooltip("セーブ用の名前。空ならファイル名")]
    [SerializeField] private string id = "";

    [Label("見出し")]
    [SerializeField] private string title = "メモ";

    [Tooltip("本文。長いときはページに分かれる（←→ でめくる）")]
    [TextArea(6, 20)]
    [SerializeField] private string body = "";

    [Label("絵")]
    [Tooltip("本文の上に出す絵（なくてもよい）")]
    [SerializeField] private Sprite picture;

    [Label("並び順")]
    [Tooltip("メニューの一覧での順番（小さいほど上）。同じなら拾った順ではなく名前の順")]
    [SerializeField] private int order = 0;

    [Label("メモ（自分用）")]
    [TextArea(1, 3)]
    [SerializeField] private string memo = "";

    public string Id => string.IsNullOrEmpty(id) ? name : id;
    public string Title => string.IsNullOrEmpty(title) ? name : title;
    public string Body => body;
    public Sprite Picture => picture;
    public int Order => order;
}
