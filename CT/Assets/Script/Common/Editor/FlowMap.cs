/*********************************************************************/
// * \file   FlowMap.cs
// * \brief  仕掛けの流れ図（何をすると何が開くか）と詰みの点検を作る
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 仕掛けの流れ図を作る（Tools > 手順書 > 仕掛けの流れ図を作り直す）
/// </summary>
///
/// ・Build Settings のシーンを全部読み、「調べる物・入ると動く範囲・答え合わせ・見た目の切り替え」が
///   どのフラグ・持ち物・仕掛けを条件にして、何を立てる・渡すかを、シーンごとの図（Mermaid）と表にする
/// ・出力：Docs/仕掛けの流れ/<シーン名>.md と README.md（直接直さない。作り直すと上書き）
/// ・点検：①条件に使っているのにどこからも ON にならないフラグ ②条件がそろわず動かない物（詰みの候補）
///   ③1つの出来事で一度に変わる物が多すぎる所（手順が分かりにくくなる）
/// ・コードで立てるフラグや、まだない部屋で立てる予定のフラグ（受け口）は Docs/仕掛けの流れ/_例外.tsv に書く
/// ・ゲームの順番（どのシーンを先に通るか）までは見ない近似。「詰みの候補」は確かめてから直す
public static class FlowMap
{
    //=====================================================================
    // 型
    //=====================================================================

    class Gate
    {
        public List<KeyValuePair<GameFlag, bool>> flags = new List<KeyValuePair<GameFlag, bool>>();
        public List<Object> items = new List<Object>();
        public List<Object> itemsNot = new List<Object>();   // 持っていないとき（届くかの判定には使わない）
        public List<GimmickState> gimmicks = new List<GimmickState>();

        public Gate Clone()
        {
            var g = new Gate();
            g.flags.AddRange(flags); g.items.AddRange(items); g.itemsNot.AddRange(itemsNot); g.gimmicks.AddRange(gimmicks);
            return g;
        }

        public bool IsEmpty => flags.Count == 0 && items.Count == 0 && itemsNot.Count == 0 && gimmicks.Count == 0;

        public string Key()
        {
            var sb = new StringBuilder();
            foreach(var f in flags) { if(f.Key != null) { sb.Append(f.Key.GetEntityId().ToString()).Append(f.Value ? "+" : "-").Append(','); } }
            foreach(var i in itemsNot) { if(i != null) { sb.Append('x').Append(i.GetEntityId().ToString()).Append(','); } }
            foreach(var i in items) { if(i != null) { sb.Append('i').Append(i.GetEntityId().ToString()).Append(','); } }
            foreach(var g in gimmicks) { if(g != null) { sb.Append('g').Append(g.GetEntityId().ToString()).Append(','); } }
            return sb.ToString();
        }
    }

    class Node
    {
        public int id;
        public string scene;
        public string label;
        public string kind;
        public GameObject go;
        public Gate gate = new Gate();
        public bool consumer;
        public bool reached;
        public List<KeyValuePair<GameFlag, bool>> setFlags = new List<KeyValuePair<GameFlag, bool>>();
        public List<Object> gives = new List<Object>();
        public List<Object> takes = new List<Object>();
        public List<Object> notes = new List<Object>();
        public List<GimmickState> gimmicksSet = new List<GimmickState>();
        public List<string> moves = new List<string>();
        public bool HasOutput => setFlags.Count + gives.Count + takes.Count + notes.Count + gimmicksSet.Count + moves.Count > 0;
    }

    //=====================================================================
    // 定数
    //=====================================================================

    const string OutDir = "Docs/仕掛けの流れ";
    const string IgnoreFile = OutDir + "/_例外.tsv";
    const int BurstThreshold = 5;      // 1つの出来事で、これ以上の物が一度に変わると注意

    //=====================================================================
    // 変数
    //=====================================================================

    static List<Node> nodes;
    static Dictionary<string, Node> nodeByKey;

    //=====================================================================
    // 入口
    //=====================================================================

    [MenuItem("Tools/手順書/仕掛けの流れ図を作り直す")]
    static void Menu()
    {
        string r = Build();
        EditorUtility.DisplayDialog("仕掛けの流れ図", r, "OK");
    }

    /// <summary>全部のシーンを読んで Docs/仕掛けの流れ/ に書き出す。要約を返す</summary>
    public static string Build()
    {
        if(EditorApplication.isPlaying) { return "再生中は作れません"; }
        nodes = new List<Node>();
        nodeByKey = new Dictionary<string, Node>();

        var opened = new List<Scene>();
        var sceneNames = new List<string>();
        try
        {
            foreach(var s in EditorBuildSettings.scenes)
            {
                if(!s.enabled) { continue; }
                Scene scene = SceneManager.GetSceneByPath(s.path);
                if(!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                sceneNames.Add(scene.name);
                ReadScene(scene);
            }
            return Analyze(sceneNames);
        }
        finally
        {
            foreach(var sc in opened) { EditorSceneManager.CloseScene(sc, true); }
            nodes = null; nodeByKey = null;
        }
    }

    //=====================================================================
    // シーンを読む
    //=====================================================================

    static void ReadScene(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(mb == null || mb is EventSequence) { continue; }
                if(mb.GetType().Assembly.GetName().Name != "Assembly-CSharp") { continue; }
                ReadComponent(scene.name, mb);
            }
        }
    }

    static void ReadComponent(string scene, MonoBehaviour mb)
    {
        Gate gate = new Gate();
        if(mb is GimmickState self) { gate.gimmicks.Add(self); }
        CollectGate(mb, gate);
        int before = nodes.Count;
        Walk(scene, mb, mb, gate, 0, "");

        // 流れを持たないが条件で変わる物（見た目の切り替えなど）
        if(nodes.Count == before && !gate.IsEmpty && !(mb is GimmickState))
        {
            Node n = GetNode(scene, mb, gate);
            n.consumer = true;
        }
    }

    // 条件（フラグ・持ち物・仕掛け）を集める。GameCondition の中も見る
    static void CollectGate(object o, Gate gate)
    {
        foreach(var f in Fields(o.GetType()))
        {
            object v = f.GetValue(o);
            if(v == null) { continue; }
            if(v is FlagCondition fc) { AddFlag(gate, fc); }
            else if(v is ItemCondition ic) { AddItem(gate, ic); }
            else if(v is GimmickCondition gc) { if(gc.target != null) { gate.gimmicks.Add(gc.target); } }
            else if(v is GameCondition g) { CollectGate(g, gate); }
            else if(v is IList list && !(v is string))
            {
                foreach(object e in list)
                {
                    if(e is FlagCondition efc) { AddFlag(gate, efc); }
                    else if(e is ItemCondition eic) { AddItem(gate, eic); }
                    else if(e is GimmickCondition egc) { if(egc.target != null) { gate.gimmicks.Add(egc.target); } }
                    else if(e is GameCondition eg) { CollectGate(eg, gate); }
                }
            }
        }
    }

    static void AddItem(Gate gate, ItemCondition ic)
    {
        if(ic.item == null) { return; }
        if(ic.notHave) { gate.itemsNot.Add(ic.item); } else { gate.items.Add(ic.item); }
    }

    static void AddFlag(Gate gate, FlagCondition fc)
    {
        if(fc.flag == null) { return; }
        gate.flags.Add(new KeyValuePair<GameFlag, bool>(fc.flag, fc.state == FlagCondition.State.On));
    }

    static bool IsConditionType(Type t)
    {
        return t == typeof(FlagCondition) || t == typeof(ItemCondition) || t == typeof(GimmickCondition) || t == typeof(GameCondition);
    }

    // 部品の中を歩いて、EventSequence（＝ここから動く流れ）を見つける
    static void Walk(string scene, MonoBehaviour owner, object o, Gate gate, int depth, string path)
    {
        if(depth > 5) { return; }
        foreach(var f in Fields(o.GetType()))
        {
            object v = f.GetValue(o);
            if(v == null) { continue; }
            Type t = f.FieldType;
            if(IsConditionType(t)) { continue; }

            if(v is EventSequence es) { AddSequence(scene, owner, gate, es, f.Name); continue; }
            if(depth == 0 && v is GameFlag flag) { DirectFlag(scene, owner, gate, f.Name, flag); continue; }
            if(depth == 0 && v is ScriptableObject so && IsItem(so) && IsPickup(owner)) { GetNode(scene, owner, gate).gives.Add(so); continue; }
            if(v is Object) { continue; }

            if(v is IList list && !(v is string))
            {
                foreach(object e in list)
                {
                    if(e == null) { continue; }
                    if(e is EventSequence ees) { AddSequence(scene, owner, gate, ees, f.Name); continue; }
                    if(e is Object || IsConditionType(e.GetType())) { continue; }
                    if(IsWalkable(e.GetType())) { WalkChild(scene, owner, e, gate, depth, path); }
                }
                continue;
            }
            if(IsWalkable(t)) { WalkChild(scene, owner, v, gate, depth, path); }
        }
    }

    static void WalkChild(string scene, MonoBehaviour owner, object child, Gate gate, int depth, string path)
    {
        Gate local = gate.Clone();
        CollectGate(child, local);
        Walk(scene, owner, child, local, depth + 1, path);
    }

    static bool IsWalkable(Type t)
    {
        if(t.IsPrimitive || t.IsEnum || t == typeof(string) || t.IsValueType) { return false; }
        if(typeof(Delegate).IsAssignableFrom(t)) { return false; }
        if(t.Namespace != null && t.Namespace.StartsWith("UnityEngine")) { return false; }
        return true;
    }

    static bool IsItem(ScriptableObject so) { return so.GetType().Name == "ItemData"; }
    static bool IsPickup(MonoBehaviour mb) { string n = mb.GetType().Name; return n.Contains("Pickup") || n.Contains("Treasure") || n == "KeyItem"; }

    // 部品が直接持つフラグ（戦闘に勝った・倒した・初めての説明 など）は「この部品が立てる」とみなす
    static void DirectFlag(string scene, MonoBehaviour owner, Gate gate, string fieldName, GameFlag flag)
    {
        string n = fieldName.ToLowerInvariant();
        if(n.Contains("joined") || n.Contains("condition") || n.Contains("require")) { return; }
        GetNode(scene, owner, gate).setFlags.Add(new KeyValuePair<GameFlag, bool>(flag, true));
    }

    static void AddSequence(string scene, MonoBehaviour owner, Gate gate, EventSequence es, string fieldName)
    {
        Node n = GetNode(scene, owner, gate);
        foreach(EventAction a in es.GetActions()) { ReadAction(n, a); }
        if(owner is PuzzleCheck && fieldName == "onSolved")
        {
            var st = owner.GetComponent<GimmickState>();
            if(st != null) { n.gimmicksSet.Add(st); }
        }
    }

    static void ReadAction(Node n, EventAction a)
    {
        if(a == null) { return; }
        string tn = a.GetType().Name;
        if(a is SetFlagEventAction)
        {
            var list = GetField(a, "flags") as IList;
            if(list == null) { return; }
            foreach(SetFlagEventAction.Entry e in list) { if(e != null && e.flag != null) { n.setFlags.Add(new KeyValuePair<GameFlag, bool>(e.flag, e.value)); } }
            return;
        }
        if(tn == "ItemGiveEventAction") { if(GetField(a, "item") is Object it) { n.gives.Add(it); } return; }
        if(tn == "ItemTakeEventAction") { if(GetField(a, "item") is Object it) { n.takes.Add(it); } return; }
        if(tn == "GimmickEventAction") { if(GetField(a, "target") is GimmickState g) { n.gimmicksSet.Add(g); } return; }
        if(tn == "SceneChangeEventAction") { if(GetField(a, "sceneName") is string s && s.Length > 0) { n.moves.Add(s); } return; }
        foreach(var f in Fields(a.GetType()))
        {
            object v = f.GetValue(a);
            if(v is GameFlag gf) { n.setFlags.Add(new KeyValuePair<GameFlag, bool>(gf, true)); }
            else if(tn == "NoteEventAction" && v is ScriptableObject so) { n.notes.Add(so); }
        }
    }

    static Node GetNode(string scene, MonoBehaviour owner, Gate gate)
    {
        string key = owner.GetEntityId() + "|" + gate.Key();
        if(nodeByKey.TryGetValue(key, out Node n)) { return n; }
        n = new Node { id = nodes.Count, scene = scene, go = owner.gameObject, gate = gate.Clone(), kind = KindOf(owner) };
        int same = nodes.Count(x => x.go == owner.gameObject && x.kind == n.kind);
        n.label = owner.gameObject.name + (same > 0 ? " #" + (same + 1) : "");
        nodes.Add(n);
        nodeByKey[key] = n;
        return n;
    }

    static string KindOf(MonoBehaviour mb)
    {
        switch(mb.GetType().Name)
        {
            case "ItemInteraction":
                object trig = GetField(mb, "trigger");
                return trig != null && trig.ToString() == "EnterArea" ? "入ると" : "調べる";
            case "EventZone": return "入ると";
            case "SceneStartEvent": return "着いたとき";
            case "PuzzleCheck": return "答え合わせ";
            case "GimmickState": return "状態が変わったとき";
            case "GimmickSwitch": return "スイッチ";
            case "GimmickReaction": return "見た目";
            case "ConditionalVisible": return "見た目";
            case "BattleArea": return "戦闘";
            case "TimpaniPeg": return "ペグ";
            case "SignalListener": return "合図を受けたら";
            default: return mb.GetType().Name;
        }
    }

    //=====================================================================
    // 点検と書き出し
    //=====================================================================

    static string Analyze(List<string> sceneNames)
    {
        var ignore = ReadIgnore();

        // 届くか（条件がそろうか）を、そろう物がなくなるまで繰り返して調べる
        var flagsOn = new HashSet<GameFlag>();
        foreach(var n in nodes) { foreach(var f in n.gate.flags) { if(f.Key != null && ignore.ContainsKey(f.Key.name)) { flagsOn.Add(f.Key); } } }
        var items = new HashSet<Object>();
        var gimmicks = new HashSet<GimmickState>();
        foreach(var g in Object.FindObjectsByType<GimmickState>(FindObjectsInactive.Include))
        {
            if(g.GetComponent<PuzzleCheck>() == null) { gimmicks.Add(g); }
        }
        bool changed = true;
        while(changed)
        {
            changed = false;
            foreach(var n in nodes)
            {
                if(n.reached) { continue; }
                if(!n.gate.flags.All(f => !f.Value || flagsOn.Contains(f.Key))) { continue; }
                if(!n.gate.items.All(i => items.Contains(i))) { continue; }
                if(!n.gate.gimmicks.All(g => gimmicks.Contains(g))) { continue; }
                n.reached = true; changed = true;
                foreach(var f in n.setFlags) { if(f.Value) { flagsOn.Add(f.Key); } }
                foreach(var i in n.gives) { items.Add(i); }
                foreach(var g in n.gimmicksSet) { gimmicks.Add(g); }
            }
        }

        // ① どこからも ON にならないフラグ
        var written = new HashSet<GameFlag>(nodes.SelectMany(n => n.setFlags).Where(f => f.Value).Select(f => f.Key));
        var neverOn = nodes.SelectMany(n => n.gate.flags).Where(f => f.Value && f.Key != null && !written.Contains(f.Key) && !ignore.ContainsKey(f.Key.name))
                           .Select(f => f.Key).Distinct().ToList();

        var issues = new Dictionary<string, List<string>>();
        foreach(string s in sceneNames) { issues[s] = new List<string>(); }
        foreach(var flag in neverOn)
        {
            foreach(var n in nodes.Where(n => n.gate.flags.Any(f => f.Key == flag && f.Value)).GroupBy(n => n.scene))
            {
                issues[n.Key].Add($"⚠ `{flag.name}` を条件に使っているが、どこからも ON にならない（{string.Join("・", n.Select(x => x.label).Distinct())}）。受け口なら `_例外.tsv` に書く");
            }
        }
        // ② 条件がそろわない物
        foreach(var n in nodes.Where(n => !n.reached))
        {
            var missing = n.gate.flags.Where(f => f.Value && !flagsOn.Contains(f.Key)).Select(f => "`" + f.Key.name + "`")
                .Concat(n.gate.items.Where(i => !items.Contains(i)).Select(i => "持ち物 " + i.name))
                .Concat(n.gate.gimmicks.Where(g => !gimmicks.Contains(g)).Select(g => "仕掛け " + g.name));
            issues[n.scene].Add($"⚠ **{n.label}**（{n.kind}）は条件がそろわない（詰みの候補）：{string.Join("・", missing)}");
        }
        // ③ 一度に変わる物が多い
        foreach(var n in nodes)
        {
            var on = new HashSet<GameFlag>(n.setFlags.Where(f => f.Value).Select(f => f.Key));
            var given = new HashSet<Object>(n.gives);
            if(on.Count == 0 && given.Count == 0) { continue; }
            // 新しく調べられる・動く物だけ数える（見た目が変わるだけの物は数えない。同じ親のスイッチ＝太鼓の並びなどは1つ）
            var touched = nodes.Where(m => m != n && m.scene == n.scene && !m.consumer && (m.gate.flags.Any(f => f.Value && on.Contains(f.Key)) || m.gate.items.Any(given.Contains)))
                               .Select(m => m.kind == "スイッチ" && m.go.transform.parent != null ? m.go.transform.parent.gameObject : m.go).Distinct().ToList();
            int count = touched.Count + n.gives.Count + n.notes.Count;
            if(count >= BurstThreshold)
            {
                issues[n.scene].Add($"ℹ **{n.label}**（{n.kind}）のあと、一度に {count} 個の物が変わる・手に入る（{string.Join("・", touched.Select(g => g.name).Concat(n.gives.Select(i => i.name)).Concat(n.notes.Select(i => i.name)))}）。手順が分かりにくくないか");
            }
        }

        // 書き出し
        Directory.CreateDirectory(OutDir);
        if(!File.Exists(IgnoreFile))
        {
            File.WriteAllText(IgnoreFile, "#フラグ（アセットの名前）\t理由（コードで立てる・まだない部屋で立てる予定 など）\n", new UTF8Encoding(false));
        }
        var index = new StringBuilder();
        index.AppendLine("# 仕掛けの流れ（自動）");
        index.AppendLine();
        index.AppendLine("Tools > 手順書 > 仕掛けの流れ図を作り直す（`FlowMap.Build()`）で作る。**直接直さない。**");
        index.AppendLine("シーンごとに「何をすると何が開くか」の図と表、点検の結果。ゲームの順番までは見ない近似なので、「詰みの候補」は確かめてから直す。");
        index.AppendLine("コードで立てるフラグ・まだない部屋で立てる予定のフラグ（受け口）は `_例外.tsv` に書くと、点検から外れる。");
        index.AppendLine();
        index.AppendLine("| シーン | 物 | 注意 |");
        index.AppendLine("|---|---|---|");
        int total = 0;
        foreach(string s in sceneNames)
        {
            var list = nodes.Where(n => n.scene == s).ToList();
            if(list.Count == 0) { continue; }
            File.WriteAllText(Path.Combine(OutDir, s + ".md"), SceneMarkdown(s, list, issues[s]), new UTF8Encoding(false));
            index.AppendLine($"| [{s}]({s}.md) | {list.Count} | {issues[s].Count} |");
            total += issues[s].Count;
        }
        index.AppendLine();
        index.AppendLine("## 注意（全部）");
        foreach(string s in sceneNames) { foreach(string i in issues[s]) { index.AppendLine($"- {s}：{i}"); } }
        if(total == 0) { index.AppendLine("- なし"); }
        File.WriteAllText(Path.Combine(OutDir, "README.md"), index.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();

        var summary = new StringBuilder();
        summary.Append($"{sceneNames.Count} シーン・{nodes.Count} 個の物、注意 {total} 件 → {OutDir}/README.md");
        foreach(string s in sceneNames) { foreach(string i in issues[s]) { summary.Append("\n").Append(s).Append(": ").Append(i); } }
        return summary.ToString();
    }

    static string SceneMarkdown(string scene, List<Node> list, List<string> issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {scene} の仕掛けの流れ（自動）");
        sb.AppendLine();
        sb.AppendLine("Tools > 手順書 > 仕掛けの流れ図を作り直す で作る。直接直さない。四角＝物（きっかけ）、丸＝フラグ、六角＝持ち物、平行四辺形＝仕掛けの状態。赤＝条件がそろわない。");
        sb.AppendLine();
        sb.AppendLine("## 注意");
        if(issues.Count == 0) { sb.AppendLine("- なし"); }
        foreach(string i in issues) { sb.AppendLine("- " + i); }
        sb.AppendLine();

        // 図
        var ids = new Dictionary<Object, string>();
        var defs = new StringBuilder();
        var edges = new StringBuilder();
        string Id(Object o, string prefix, string shapeOpen, string shapeClose, string text)
        {
            if(ids.TryGetValue(o, out string id)) { return id; }
            id = prefix + ids.Count;
            ids[o] = id;
            defs.AppendLine($"  {id}{shapeOpen}\"{Esc(text)}\"{shapeClose}");
            return id;
        }
        foreach(var n in list)
        {
            string nid = "n" + n.id;
            defs.AppendLine($"  {nid}[\"{Esc(n.label)}<br/>（{n.kind}）\"]" + (n.reached ? "" : ":::ng"));
            foreach(var f in n.gate.flags) { edges.AppendLine($"  {Id(f.Key, "f", "([", "])", f.Key.name)} {(f.Value ? "-->" : "-.->|OFF のとき|")} {nid}"); }
            foreach(var i in n.gate.items) { edges.AppendLine($"  {Id(i, "i", "{{", "}}", i.name)} -->|要る| {nid}"); }
            foreach(var i in n.gate.itemsNot) { edges.AppendLine($"  {Id(i, "i", "{{", "}}", i.name)} -.->|持っていないとき| {nid}"); }
            foreach(var g in n.gate.gimmicks) { if(g.gameObject != n.go) { edges.AppendLine($"  {Id(g, "g", "[/", "/]", g.name)} --> {nid}"); } }
            foreach(var f in n.setFlags) { edges.AppendLine($"  {nid} {(f.Value ? "-->|ON|" : "-.->|OFF|")} {Id(f.Key, "f", "([", "])", f.Key.name)}"); }
            foreach(var i in n.gives) { edges.AppendLine($"  {nid} -->|手に入る| {Id(i, "i", "{{", "}}", i.name)}"); }
            foreach(var i in n.takes) { edges.AppendLine($"  {nid} -.->|渡す| {Id(i, "i", "{{", "}}", i.name)}"); }
            foreach(var i in n.notes) { edges.AppendLine($"  {nid} -->|メモ| {Id(i, "m", "[[", "]]", i.name)}"); }
            foreach(var g in n.gimmicksSet) { edges.AppendLine($"  {nid} -->|変える| {Id(g, "g", "[/", "/]", g.name)}"); }
            foreach(string m in n.moves) { edges.AppendLine($"  {nid} --> mv{n.id}>\"→ {Esc(m)}\"]"); }
        }
        sb.AppendLine("## 図");
        sb.AppendLine("```mermaid");
        sb.AppendLine("flowchart LR");
        sb.AppendLine("  classDef ng fill:#fdd,stroke:#c00");
        sb.Append(defs);
        sb.Append(edges);
        sb.AppendLine("```");
        sb.AppendLine();

        // 表
        sb.AppendLine("## 表");
        sb.AppendLine("| 物 | きっかけ | 条件 | 結果 |");
        sb.AppendLine("|---|---|---|---|");
        foreach(var n in list)
        {
            var cond = n.gate.flags.Select(f => f.Key.name + (f.Value ? "" : " が OFF"))
                .Concat(n.gate.items.Select(i => "持ち物 " + i.name)).Concat(n.gate.itemsNot.Select(i => i.name + " を持っていない"))
                .Concat(n.gate.gimmicks.Where(g => g.gameObject != n.go).Select(g => "仕掛け " + g.name));
            var res = n.setFlags.Select(f => f.Key.name + (f.Value ? " ON" : " OFF"))
                .Concat(n.gives.Select(i => "手に入る " + i.name))
                .Concat(n.takes.Select(i => "渡す " + i.name))
                .Concat(n.notes.Select(i => "メモ " + i.name))
                .Concat(n.gimmicksSet.Select(g => "変える " + g.name))
                .Concat(n.moves.Select(m => "→ " + m));
            string c = string.Join("、", cond); string r = string.Join("、", res);
            sb.AppendLine($"| {(n.reached ? "" : "⚠ ")}{n.label} | {n.kind} | {(c.Length > 0 ? c : "なし")} | {(r.Length > 0 ? r : (n.consumer ? "（表示が変わる）" : "（会話・演出だけ）"))} |");
        }
        return sb.ToString();
    }

    static string Esc(string s) { return s.Replace("\"", "'").Replace("<", "‹").Replace(">", "›"); }

    static Dictionary<string, string> ReadIgnore()
    {
        var d = new Dictionary<string, string>();
        if(!File.Exists(IgnoreFile)) { return d; }
        foreach(string line in File.ReadAllLines(IgnoreFile, Encoding.UTF8))
        {
            if(line.StartsWith("#") || line.Trim().Length == 0) { continue; }
            var c = line.Split('\t');
            d[c[0].Trim()] = c.Length > 1 ? c[1] : "";
        }
        return d;
    }

    //=====================================================================
    // 反射の補助
    //=====================================================================

    static readonly Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();

    // Unity が保存する欄（public か [SerializeField] / [SerializeReference]）
    static FieldInfo[] Fields(Type t)
    {
        if(fieldCache.TryGetValue(t, out var cached)) { return cached; }
        var list = new List<FieldInfo>();
        for(Type c = t; c != null && c != typeof(MonoBehaviour) && c != typeof(object); c = c.BaseType)
        {
            foreach(var f in c.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if(f.IsNotSerialized) { continue; }
                if(!f.IsPublic && f.GetCustomAttribute<SerializeField>() == null && f.GetCustomAttribute<SerializeReference>() == null) { continue; }
                list.Add(f);
            }
        }
        var arr = list.ToArray();
        fieldCache[t] = arr;
        return arr;
    }

    static object GetField(object o, string name)
    {
        for(Type c = o.GetType(); c != null; c = c.BaseType)
        {
            var f = c.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if(f != null) { return f.GetValue(o); }
        }
        return null;
    }
}
