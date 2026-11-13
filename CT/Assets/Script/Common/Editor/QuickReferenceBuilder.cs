/*********************************************************************/
// * \file   QuickReferenceBuilder.cs
// * \brief  早見表（自動）を作る：コード（クラスの説明・Inspector の項目名・Tooltip・メニュー）から Docs/早見表（自動）.md を作り直す
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
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 早見表（自動）を作る
/// </summary>
///
/// ・Tools > 手順書 > 早見表を作り直す。コードから作るので、項目名やメニューを変えたら押し直すだけで最新になる
/// ・載せるもの：メニュー（Hierarchy 右クリック・Project の Create・Tools）、イベントの部品、コンポーネント（[Label] の項目があるもの）、
///   データ（CreateAssetMenu）、標準の合図
/// ・説明はクラスの /// &lt;summary&gt; と、その下の「・」の行。項目は [Label] の名前と [Tooltip]
/// ・手で書く手順書（プランナー向け作業手順書）とは別のファイル。こちらは直接直さない（押し直すと消える）
public static class QuickReferenceBuilder
{
    /// <summary> 書き出す先（プロジェクトのフォルダから） </summary>
    public const string OutputPath = "Docs/早見表（自動）.md";

    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static Dictionary<Type, string> scriptPaths;

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Tools/手順書/早見表を作り直す")]
    private static void BuildMenu()
    {
        string path = Build();
        EditorUtility.DisplayDialog("早見表", "作り直しました。\n" + path, "OK");
    }

    /// <summary>
    /// 早見表を作り直す（書いたファイルのフルパスを返す）
    /// </summary>
    public static string Build()
    {
        scriptPaths = CollectScriptPaths();

        var sb = new StringBuilder();
        sb.AppendLine("# 早見表（自動）");
        sb.AppendLine();
        sb.AppendLine("> **このファイルはコードから自動で作っています。直接直さないでください**（押し直すと消えます）。");
        sb.AppendLine("> 作り直す：Unity のメニュー **Tools > 手順書 > 早見表を作り直す**。作業の手順は `Docs/プランナー向け作業手順書.md`。");
        sb.AppendLine($"> 作った日：{DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("- [1. メニュー](#1-メニュー)");
        sb.AppendLine("- [2. イベントの部品](#2-イベントの部品)");
        sb.AppendLine("- [3. コンポーネント（Inspector の項目）](#3-コンポーネントinspector-の項目)");
        sb.AppendLine("- [4. データ（Project の Create）](#4-データproject-の-create)");
        sb.AppendLine("- [5. 標準の合図](#5-標準の合図)");
        sb.AppendLine();

        WriteMenus(sb);
        WriteEventActions(sb);
        WriteComponents(sb);
        WriteData(sb);
        WriteSignals(sb);

        string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, sb.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        return full;
    }

    //=====================================================================
    // 1. メニュー
    //=====================================================================

    private static void WriteMenus(StringBuilder sb)
    {
        var items = new List<(string path, int priority)>();
        foreach(MethodInfo m in TypeCache.GetMethodsWithAttribute<MenuItem>())
        {
            if(!IsProjectAssembly(m.DeclaringType)) { continue; }
            foreach(MenuItem attr in m.GetCustomAttributes<MenuItem>())
            {
                if(attr.validate) { continue; }
                string p = attr.menuItem;
                // ショートカットの書き方（ %#&_ ）を外す
                p = Regex.Replace(p, @"\s+[%#&_^][^/]*$", "");
                items.Add((p, attr.priority));
            }
        }

        sb.AppendLine("## 1. メニュー");
        sb.AppendLine();

        sb.AppendLine("### Hierarchy で右クリック");
        sb.AppendLine("| メニュー |");
        sb.AppendLine("|---|");
        foreach(var i in items.Where(i => i.path.StartsWith("GameObject/")).OrderBy(i => i.path, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {Escape(i.path.Substring("GameObject/".Length).Replace("/", " > "))} |");
        }
        sb.AppendLine();

        sb.AppendLine("### Project で右クリック > Create");
        sb.AppendLine("| メニュー | 作る物 |");
        sb.AppendLine("|---|---|");
        foreach(var (type, attr) in CreateMenus().OrderBy(c => c.attr.menuName, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {Escape(attr.menuName.Replace("/", " > "))} | {Escape(FirstSummaryLine(type))} |");
        }
        foreach(var i in items.Where(i => i.path.StartsWith("Assets/")).OrderBy(i => i.path, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {Escape(i.path.Substring("Assets/".Length).Replace("/", " > "))}（右クリック） | |");
        }
        sb.AppendLine();

        sb.AppendLine("### メニューバー > Tools");
        sb.AppendLine("| メニュー |");
        sb.AppendLine("|---|");
        foreach(var i in items.Where(i => i.path.StartsWith("Tools/")).OrderBy(i => i.path, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {Escape(i.path.Substring("Tools/".Length).Replace("/", " > "))} |");
        }
        sb.AppendLine();
    }

    private static IEnumerable<(Type type, CreateAssetMenuAttribute attr)> CreateMenus()
    {
        foreach(Type t in TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>())
        {
            if(!IsProjectAssembly(t)) { continue; }
            var attr = t.GetCustomAttribute<CreateAssetMenuAttribute>();
            if(attr == null || string.IsNullOrEmpty(attr.menuName)) { continue; }
            yield return (t, attr);
        }
    }

    //=====================================================================
    // 2. イベントの部品
    //=====================================================================

    private static void WriteEventActions(StringBuilder sb)
    {
        sb.AppendLine("## 2. イベントの部品");
        sb.AppendLine();
        sb.AppendLine("EventSequence の「＋ 部品を追加」で選ぶ（分類 > 名前）。上から順に実行される。");
        sb.AppendLine();

        foreach(var group in EventActionCatalog.AllTypes().GroupBy(t => EventActionCatalog.CategoryOf(t)).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"### {group.Key}");
            sb.AppendLine();
            foreach(Type t in group.OrderBy(t => EventActionCatalog.NameOf(t), StringComparer.Ordinal))
            {
                WriteType(sb, EventActionCatalog.NameOf(t), t);
            }
        }
    }

    //=====================================================================
    // 3. コンポーネント
    //=====================================================================

    private static void WriteComponents(StringBuilder sb)
    {
        sb.AppendLine("## 3. コンポーネント（Inspector の項目）");
        sb.AppendLine();
        sb.AppendLine("Inspector に日本語の項目名がある部品（フォルダごと）。Add Component の検索にはクラス名（英語）を入れる。");
        sb.AppendLine();

        var types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .Where(t => IsProjectAssembly(t) && !t.IsAbstract && !typeof(EventAction).IsAssignableFrom(t) && HasLabels(t))
            .ToList();

        foreach(var group in types.GroupBy(FolderOf).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"### {group.Key}");
            sb.AppendLine();
            foreach(Type t in group.OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                var menu = t.GetCustomAttribute<AddComponentMenu>();
                string title = menu != null && !string.IsNullOrEmpty(menu.componentMenu) ? $"{LastPart(menu.componentMenu)}（{t.Name}）" : t.Name;
                WriteType(sb, title, t);
            }
        }
    }

    //=====================================================================
    // 4. データ
    //=====================================================================

    private static void WriteData(StringBuilder sb)
    {
        sb.AppendLine("## 4. データ（Project の Create）");
        sb.AppendLine();
        foreach(var (type, attr) in CreateMenus().Where(c => HasLabels(c.type) || !string.IsNullOrEmpty(FirstSummaryLine(c.type))).OrderBy(c => c.attr.menuName, StringComparer.Ordinal))
        {
            WriteType(sb, $"{LastPart(attr.menuName)}（{type.Name}。Create > {attr.menuName.Replace("/", " > ")}）", type);
        }
    }

    //=====================================================================
    // 5. 標準の合図
    //=====================================================================

    private static void WriteSignals(StringBuilder sb)
    {
        sb.AppendLine("## 5. 標準の合図");
        sb.AppendLine();
        sb.AppendLine("ゲームが自動で出す合図。アセットは `Assets/Data/Signals/標準/`（Tools > 合図 > 標準の合図を作る）。使い方は `Docs/合図の使い方.md`。");
        sb.AppendLine();
        sb.AppendLine("| 合図 | ID | いつ出るか |");
        sb.AppendLine("|---|---|---|");
        foreach(StandardSignals.Info info in StandardSignals.All)
        {
            sb.AppendLine($"| {Escape(info.name)} | `{info.id}` | {Escape(info.memo)} |");
        }
        sb.AppendLine();
    }

    //=====================================================================
    // 1つの型
    //=====================================================================

    private static void WriteType(StringBuilder sb, string title, Type t)
    {
        sb.AppendLine($"#### {title}");
        List<string> summary = SummaryLines(t);
        if(summary.Count > 0)
        {
            sb.AppendLine(summary[0]);
            foreach(string s in summary.Skip(1)) { sb.AppendLine("- " + s); }
        }
        else { sb.AppendLine($"`{t.Name}`"); }

        var fields = LabelFields(t, 0);
        if(fields.Count > 0)
        {
            sb.AppendLine();
            foreach(string f in fields) { sb.AppendLine(f); }
        }
        sb.AppendLine();
    }

    /// <summary> [Label] の付いた項目を「- **名前**：Tooltip」の行に（中の型の項目は字下げ） </summary>
    private static List<string> LabelFields(Type t, int depth)
    {
        var lines = new List<string>();
        if(depth > 1) { return lines; }
        string indent = new string(' ', depth * 2);

        // 土台のクラスの項目も（上から順に）
        var chain = new List<Type>();
        for(Type c = t; c != null && IsProjectAssembly(c); c = c.BaseType) { chain.Insert(0, c); }

        foreach(Type c in chain)
        {
            foreach(FieldInfo f in c.GetFields(InstanceFields))
            {
                if(!f.IsPublic && f.GetCustomAttribute<SerializeField>() == null && f.GetCustomAttribute<SerializeReference>() == null) { continue; }
                if(f.GetCustomAttribute<NonSerializedAttribute>() != null || f.GetCustomAttribute<HideInInspector>() != null) { continue; }

                var label = f.GetCustomAttribute<LabelAttribute>();
                var tooltip = f.GetCustomAttribute<TooltipAttribute>();
                string name = label != null ? label.label : ObjectNames.NicifyVariableName(f.Name);
                Type inner = ElementType(f.FieldType);
                bool nested = inner != null && IsProjectAssembly(inner) && inner.GetCustomAttribute<SerializableAttribute>() != null && !typeof(UnityEngine.Object).IsAssignableFrom(inner);

                if(label == null && tooltip == null && !nested) { continue; }

                string tip = tooltip != null ? "：" + OneLine(tooltip.tooltip) : "";
                lines.Add($"{indent}- **{Escape(name)}**{tip}");
                if(nested) { lines.AddRange(LabelFields(inner, depth + 1)); }
            }
        }
        return lines;
    }

    private static Type ElementType(Type t)
    {
        if(t.IsArray) { return t.GetElementType(); }
        if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) { return t.GetGenericArguments()[0]; }
        if(t.IsClass && t != typeof(string)) { return t; }
        return null;
    }

    private static bool HasLabels(Type t)
    {
        for(Type c = t; c != null && IsProjectAssembly(c); c = c.BaseType)
        {
            foreach(FieldInfo f in c.GetFields(InstanceFields)) { if(f.GetCustomAttribute<LabelAttribute>() != null) { return true; } }
        }
        return false;
    }

    //=====================================================================
    // クラスの説明（ソースの /// コメント）
    //=====================================================================

    private static Dictionary<Type, string> CollectScriptPaths()
    {
        var map = new Dictionary<Type, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Script" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
            Type c = ms != null ? ms.GetClass() : null;
            if(c != null && !map.ContainsKey(c)) { map.Add(c, p); }
        }
        return map;
    }

    private static string PathOf(Type t)
    {
        if(scriptPaths != null && scriptPaths.TryGetValue(t, out string p)) { return p; }
        return null;
    }

    private static string FolderOf(Type t)
    {
        string p = PathOf(t);
        if(string.IsNullOrEmpty(p)) { return "（その他）"; }
        string dir = Path.GetDirectoryName(p).Replace('\\', '/');
        return dir.StartsWith("Assets/Script/") ? dir.Substring("Assets/Script/".Length) : dir;
    }

    private static string FirstSummaryLine(Type t)
    {
        List<string> s = SummaryLines(t);
        return s.Count > 0 ? s[0] : "";
    }

    /// <summary> クラスの上の /// &lt;summary&gt; の文と、その下の「・」の行 </summary>
    private static List<string> SummaryLines(Type t)
    {
        var result = new List<string>();
        string path = PathOf(t);
        if(string.IsNullOrEmpty(path) || !File.Exists(path)) { return result; }

        string[] lines = File.ReadAllLines(path);
        int classLine = -1;
        var re = new Regex(@"\b(class|struct)\s+" + Regex.Escape(t.Name) + @"\b");
        for(int i = 0; i < lines.Length; i++)
        {
            string s = lines[i].TrimStart();
            if(s.StartsWith("//")) { continue; }
            if(re.IsMatch(lines[i])) { classLine = i; break; }
        }
        if(classLine < 0) { return result; }

        // クラスの上の「///」と「[...]」の行をさかのぼる
        int start = classLine;
        while(start - 1 >= 0)
        {
            string s = lines[start - 1].Trim();
            if(s.StartsWith("///") || s.StartsWith("[")) { start--; } else { break; }
        }

        // 1行目＝見出し（summary の最初の行）、ほかは1行ずつ（「・」で始まる行は新しい行、それ以外は前の行の続き）
        string headline = null;
        bool started = false;
        for(int i = start; i < classLine; i++)
        {
            string s = lines[i].Trim();
            if(!s.StartsWith("///")) { continue; }
            s = s.Substring(3).Trim().Replace("<summary>", "").Replace("</summary>", "").Trim();
            if(s.Length == 0 || s.StartsWith("<")) { continue; }
            s = Escape(Unxml(s));

            if(headline == null) { headline = s; continue; }
            bool bullet = s.StartsWith("・");
            if(bullet) { s = s.Substring(1).Trim(); }
            if(bullet || !started || EndsSentence(result[result.Count - 1])) { result.Add(s); started = true; }
            else { result[result.Count - 1] += " " + s; }
        }
        if(headline != null) { result.Insert(0, headline); }
        return result;
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static bool IsProjectAssembly(Type t)
    {
        if(t == null) { return false; }
        string a = t.Assembly.GetName().Name;
        return a == "Assembly-CSharp" || a == "Assembly-CSharp-Editor";
    }

    private static string LastPart(string menu)
    {
        int i = menu.LastIndexOf('/');
        return i >= 0 ? menu.Substring(i + 1) : menu;
    }

    private static string OneLine(string s) => Escape(s.Replace("\r", "").Replace("\n", " / "));

    private static bool EndsSentence(string s) => s.EndsWith("。") || s.EndsWith(":") || s.EndsWith("：") || s.EndsWith(")") || s.EndsWith("）");

    private static string Unxml(string s) => s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");

    private static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "｜");
}
