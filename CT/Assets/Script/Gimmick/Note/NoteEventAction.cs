/*********************************************************************/
// * \file   NoteEventAction.cs
// * \brief  イベントでメモを渡す（読む画面を出す）アクション
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// メモを渡す
/// </summary>
///
/// 例：NPC から手紙をもらう ／ 宝箱からメモが出てくる ／ 本棚を調べると日記が読める
/// ・「順番に実行」の EventSequence では、読み終わる（閉じる）まで次の部品を待つ
public class NoteEventAction : EventAction
{
    [SerializeField, Label("メモ")]
    private NoteData note;

    [SerializeField, Label("その場で読む")]
    [Tooltip("オフ：持っていることにするだけ（メニューで読める）")]
    private bool show = true;

    public NoteData Note => note;

    public override void Execute(GameObject gameObject)
    {
        NoteBook.Add(note);
        if(show) { NoteBook.Read(note, null); }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        NoteBook.Add(note);
        if(!show || note == null) { yield break; }

        bool closed = false;
        NoteBook.Read(note, () => closed = true);
        while(!closed) { yield return null; }
    }
}
