/*********************************************************************/
// * \file   MapReach.cs
// * \brief  今いる場所から、調べる物・出口・出現位置まで歩いて行けるかを確かめる（デバッグ用）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 届くかの確認（再生中。敵と相棒の道＝NavMesh2D の小さい体で調べる）
/// </summary>
///
/// ・対象：調べる物（Intaractable。範囲〔Is Trigger の Collider2D〕のあるもの）、出口（SceneChange）、出現位置（PlayerSpawnPoint）
/// ・対象の範囲のそばに床があり、そこまで道がつながっていれば「行ける」
/// ・壁・歩ける範囲・閉じた扉などを、今の状態で調べる（イベントで開く扉の先は「行けない」と出る）
/// ・デバッグウィンドウの「届くかの確認」と、テストの event.ps1 -Reach から使う
public static class MapReach
{
    public struct Entry
    {
        public string kind;
        public string name;
        public Vector2 position;
        public bool reachable;
        public string reason;
        public Object target;
    }

    /// <summary>
    /// from から各対象へ行けるか
    /// </summary>
    public static List<Entry> Check(Vector2 from)
    {
        var list = new List<Entry>();
        NavMesh2D nav = NavMesh2D.Instance;
        if(nav == null) { return list; }

        foreach(Intaractable it in Object.FindObjectsByType<Intaractable>())
        {
            Collider2D col = it.GetComponent<Collider2D>();
            if(col == null || !col.isTrigger || !col.enabled) { continue; }
            list.Add(Test(nav, from, "調べる物", it, col.bounds));
        }
        foreach(SceneChange exit in Object.FindObjectsByType<SceneChange>())
        {
            Collider2D col = exit.GetComponent<Collider2D>();
            if(col == null || !col.enabled) { continue; }
            list.Add(Test(nav, from, "出口", exit, col.bounds));
        }
        foreach(PlayerSpawnPoint spawn in Object.FindObjectsByType<PlayerSpawnPoint>())
        {
            list.Add(Test(nav, from, "出現位置", spawn, new Bounds(spawn.transform.position, Vector3.one * 0.2f)));
        }

        list.Sort((a, b) => a.reachable == b.reachable ? string.CompareOrdinal(a.name, b.name) : (a.reachable ? 1 : -1));
        return list;
    }

    private static Entry Test(NavMesh2D nav, Vector2 from, string kind, Component target, Bounds bounds)
    {
        var entry = new Entry { kind = kind, name = PathOf(target.transform), position = bounds.center, target = target };

        // 範囲の真ん中から、範囲の大きさ＋体の分だけ探して、いちばん近い床
        float search = Mathf.Max(bounds.extents.x, bounds.extents.y) + 0.6f;
        if(!nav.SamplePosition(bounds.center, search, EnemySize.Small, out Vector2 floor))
        {
            entry.reason = "そばに歩ける床がない（歩ける範囲の外・壁の中）";
            return entry;
        }

        if(!nav.SamplePosition(from, 1.5f, EnemySize.Small, out Vector2 start))
        {
            entry.reason = "今いる場所が歩ける床の上ではない";
            return entry;
        }

        entry.reachable = nav.IsReachable(start, floor, EnemySize.Small);
        if(!entry.reachable) { entry.reason = "道がつながっていない（壁・閉じた扉・歩ける範囲で分かれている）"; }
        return entry;
    }

    /// <summary> 結果を文にする（テスト用） </summary>
    public static string Report(Vector2 from)
    {
        List<Entry> list = Check(from);
        var sb = new StringBuilder();
        int ok = 0;
        foreach(Entry e in list) { if(e.reachable) { ok++; } }
        sb.Append($"from {from.x:0.0},{from.y:0.0}: reachable {ok}/{list.Count}\n");
        foreach(Entry e in list)
        {
            sb.Append(e.reachable ? "  OK " : "  NG ").Append(e.kind).Append(" ").Append(e.name)
              .Append($" ({e.position.x:0.0},{e.position.y:0.0})");
            if(!e.reachable) { sb.Append(" : ").Append(e.reason); }
            sb.Append("\n");
        }
        return sb.ToString();
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        for(Transform p = t.parent; p != null; p = p.parent) { path = p.name + "/" + path; }
        return path;
    }
}
