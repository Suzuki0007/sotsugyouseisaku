/*********************************************************************/
// * \file   NotePickup.cs
// * \brief  調べるとメモを読む（拾う）。メニューの「メモ」で読み返せる
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// メモを拾う
/// </summary>
///
/// ・調べると読む画面が出て、メモを持っていることになる（セーブ）。メニュー > メモ で読み返せる
/// ・「拾ったら消す」なら、読み終わったら見た目を消す（シーンに戻っても消えたまま）
///   オフなら、何度でも調べて読める（壁の落書き・掲示板）
public class NotePickup : GimmickInteractable
{
    [SerializeField, Label("メモ")]
    [Tooltip("Assets/Resources/Notes/ の NoteData")]
    private NoteData note;

    [SerializeField, Label("拾ったら消す")]
    private bool hideAfterRead = true;

    [SerializeField, Label("拾ったときの SE")]
    private SeData pickSe;

    [SerializeField, Label("読み終わったときのイベント")]
    [Tooltip("例：「……これは、金庫の番号？」の会話")]
    private EventSequence onRead;

    public NoteData Note => note;

    private void Start()
    {
        if(hideAfterRead && NoteBook.Has(note)) { gameObject.SetActive(false); }
    }

    protected override void OnInteract(GameObject user)
    {
        if(note == null) { return; }

        SeManager.PlaySe(pickSe, transform.position);
        NoteBook.Add(note);
        NoteBook.Read(note, () =>
        {
            if(onRead != null) { onRead.Play(gameObject); }
            if(hideAfterRead) { gameObject.SetActive(false); }
        });
    }
}
