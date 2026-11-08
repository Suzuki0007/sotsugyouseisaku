/*********************************************************************/
// * \file   NoteBook.cs
// * \brief  持っているメモの記録と、読む・読み返す画面
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 持っているメモ
/// </summary>
///
/// ・持っているかはセーブの Note/<ID>（仮置き → 確定。ほかの物と同じ）
/// ・一覧は Resources/Notes/ の NoteData のうち、持っている物
public static class NoteBook
{
    private const string KeyPrefix = "Note/";

    /// <summary> 持っているか </summary>
    public static bool Has(NoteData note) => note != null && Save.Get(KeyPrefix + note.Id, false);

    /// <summary> 持っていることにする </summary>
    public static void Add(NoteData note)
    {
        if(note == null) { return; }
        Save.SetPending(KeyPrefix + note.Id, true);
    }

    /// <summary> 持っているメモ（並び順・名前の順） </summary>
    public static List<NoteData> Collected()
    {
        var list = new List<NoteData>();
        foreach(NoteData note in Resources.LoadAll<NoteData>("Notes")) { if(Has(note)) { list.Add(note); } }
        list.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.name, b.name));
        return list;
    }

    /// <summary> 1 つのメモを読む画面を出す </summary>
    public static void Read(NoteData note, Action onClose)
    {
        if(note == null) { onClose?.Invoke(); return; }
        PuzzleUI.OpenText(note.Title, note.Body, note.Picture, onClose);
    }

    /// <summary>
    /// 持っているメモの一覧を出す（メニューの「メモ」）。選ぶと読む → 閉じると一覧に戻る → もどると onClose
    /// </summary>
    public static void OpenList(Action onClose, int start = 0)
    {
        List<NoteData> notes = Collected();
        var labels = new List<string>();
        foreach(NoteData n in notes) { labels.Add(n.Title); }

        PuzzleUI.OpenChoice("メモ", labels, null, start, index =>
        {
            Read(notes[index], () => OpenList(onClose, index));
        }, onClose, notes.Count == 0 ? "まだメモはありません" : "");
    }
}
