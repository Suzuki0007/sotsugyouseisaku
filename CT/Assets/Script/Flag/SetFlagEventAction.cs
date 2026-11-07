/*********************************************************************/
// * \file   SetFlagEventAction.cs
// * \brief  イベントでフラグを ON / OFF するアクション
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベント（EventSequence）でフラグを ON / OFF するアクション
/// </summary>
///
/// 例：NPC と話したら「NPCと話した」を ON にする、ボス部屋に入ったら「ボス戦中」を ON にする
public class SetFlagEventAction : EventAction
{
    //=====================================================================
    // 型
    //=====================================================================

    [Serializable]
    public class Entry
    {
        [Tooltip("設定するフラグ")]
        public GameFlag flag;

        [Tooltip("チェックで ON、外すと OFF にする")]
        public bool value = true;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("設定するフラグの一覧")]
    private List<Entry> flags = new List<Entry>();

    [SerializeField, Tooltip("エリアから出たときに元に戻す（EventZone で使うとき用。ボス部屋の中だけ、など）")]
    private bool revertOnExit = false;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        foreach(Entry entry in flags)
        {
            if(entry != null) { GameFlags.Set(entry.flag, entry.value); }
        }
    }

    public override void OnExit(GameObject gameObject)
    {
        if(!revertOnExit) { return; }

        foreach(Entry entry in flags)
        {
            if(entry != null) { GameFlags.Set(entry.flag, !entry.value); }
        }
    }

    /// <summary>
    /// 完了済みの ItemInteraction を復元するときも、フラグを実行後の値にそろえる
    /// （セーブするフラグはすでにその値なので何も変わらない）
    /// </summary>
    public override void Restore(GameObject gameObject)
    {
        if(revertOnExit) { return; }

        Execute(gameObject);
    }

    /// <summary>
    /// フラグを1つ追加する（エディタのひな形作成用）
    /// </summary>
    public void AddFlagForEditor(GameFlag flag, bool value)
    {
        flags.Add(new Entry { flag = flag, value = value });
    }
}
