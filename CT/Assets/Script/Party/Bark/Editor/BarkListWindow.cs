/*********************************************************************/
// * \file   BarkListWindow.cs
// * \brief  相棒の一言の一覧（Tools > 仕掛け > 相棒の一言 > 一言の一覧）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一言の一覧
/// </summary>
///
/// ・Resources/Barks/ の一言を表で見る・使う／使わないを切り替える・優先度などを直す・探す
/// ・再生中：今出せるか（出せない理由）、▶ で試しに出す、最近出した一言、一言を止める・出した記録を消す
/// ・CSV の取り込み・書き出し・ひな形
public class BarkListWindow : EditorWindow
{
    private List<BarkData> list = new List<BarkData>();
    private Vector2 scroll;
    private string search = "";
    private int triggerFilter = -1;
    private bool showLog = true;

    [MenuItem("Tools/仕掛け/相棒の一言/一言の一覧", false, 300)]
    public static void Open()
    {
        var w = GetWindow<BarkListWindow>("一言の一覧");
        w.minSize = new Vector2(760, 300);
        w.Refresh();
    }

    private void OnEnable() { Refresh(); }

    private void OnFocus() { Refresh(); }

    private void Refresh()
    {
        list = BarkCsv.LoadAll();
    }

    private void OnInspectorUpdate()
    {
        if(Application.isPlaying) { Repaint(); }
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawFilter();

        if(list.Count == 0)
        {
            EditorGUILayout.HelpBox("一言のデータがありません。「新しく作る」か「CSVから取り込む」で作ってください（Resources/Barks/ に置きます）。", MessageType.Info);
        }

        DrawHeader();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach(BarkData d in list)
        {
            if(d == null || !Visible(d)) { continue; }
            DrawRow(d);
        }
        EditorGUILayout.EndScrollView();

        DrawPlayPanel();
    }

    //=====================================================================
    // 上の段
    //=====================================================================

    private void DrawToolbar()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if(GUILayout.Button("新しく作る", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                BarkData d = BarkMenu.CreateData("Bark_新しい一言", triggerFilter >= 0 ? (BarkTrigger)triggerFilter : BarkTrigger.Manual);
                Selection.activeObject = d;
                Refresh();
            }
            if(GUILayout.Button("CSVから取り込む", EditorStyles.toolbarButton, GUILayout.Width(110))) { BarkCsv.ImportFromPanel(); Refresh(); }
            if(GUILayout.Button("CSVに書き出す", EditorStyles.toolbarButton, GUILayout.Width(100))) { BarkCsv.ExportAllFromPanel(); }
            if(GUILayout.Button("CSVのひな形", EditorStyles.toolbarButton, GUILayout.Width(90))) { BarkCsv.CreateTemplate(); }
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("一言の設定", EditorStyles.toolbarButton, GUILayout.Width(80))) { Selection.activeObject = BarkMenu.EnsureSettings(); }
            if(GUILayout.Button("読み直す", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                Refresh();
                if(Application.isPlaying) { Bark.Reload(); }
            }
        }
    }

    private void DrawFilter()
    {
        using(new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField("探す（名前・文）", search);

            var names = new List<string> { "すべてのきっかけ" };
            foreach(BarkTrigger t in System.Enum.GetValues(typeof(BarkTrigger))) { names.Add(BarkCsv.TriggerName(t)); }
            int index = EditorGUILayout.Popup(triggerFilter + 1, names.ToArray(), GUILayout.Width(150));
            triggerFilter = index - 1;
        }
    }

    private bool Visible(BarkData d)
    {
        if(triggerFilter >= 0 && (int)d.trigger != triggerFilter) { return false; }
        if(string.IsNullOrEmpty(search)) { return true; }
        if(d.name.Contains(search)) { return true; }
        foreach(BarkVariant v in d.variants)
        {
            if(v == null) { continue; }
            foreach(BarkLine l in v.lines) { if(l != null && l.text != null && l.text.Contains(search)) { return true; } }
        }
        return false;
    }

    //=====================================================================
    // 表
    //=====================================================================

    private void DrawHeader()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("使う", GUILayout.Width(30));
            GUILayout.Label("名前", GUILayout.Width(160));
            GUILayout.Label("きっかけ", GUILayout.Width(110));
            GUILayout.Label("優先度", GUILayout.Width(45));
            GUILayout.Label("確率", GUILayout.Width(40));
            GUILayout.Label("次まで", GUILayout.Width(45));
            GUILayout.Label("候補", GUILayout.Width(32));
            GUILayout.Label("最初の文");
            if(Application.isPlaying) { GUILayout.Label("今", GUILayout.Width(150)); }
        }
    }

    private void DrawRow(BarkData d)
    {
        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            bool active = EditorGUILayout.Toggle(d.active, GUILayout.Width(30));
            if(GUILayout.Button(d.name, EditorStyles.linkLabel, GUILayout.Width(160))) { Selection.activeObject = d; EditorGUIUtility.PingObject(d); }
            var trigger = (BarkTrigger)EditorGUILayout.Popup((int)d.trigger, TriggerNames(), GUILayout.Width(110));
            int priority = EditorGUILayout.IntField(d.priority, GUILayout.Width(45));
            float chance = EditorGUILayout.FloatField(d.chance, GUILayout.Width(40));
            float cooldown = EditorGUILayout.FloatField(d.cooldown, GUILayout.Width(45));
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(d, "一言を変える");
                d.active = active;
                d.trigger = trigger;
                d.priority = priority;
                d.chance = Mathf.Clamp01(chance);
                d.cooldown = Mathf.Max(0.0f, cooldown);
                EditorUtility.SetDirty(d);
            }

            GUILayout.Label(d.variants.Count.ToString(), GUILayout.Width(32));
            string first = FirstLine(d);
            GUILayout.Label(string.IsNullOrEmpty(first) ? "（文がない）" : first, GUILayout.MinWidth(120));

            if(Application.isPlaying)
            {
                string why = Bark.WhyNot(d);
                GUILayout.Label(string.IsNullOrEmpty(why) ? "出せる" : why, GUILayout.Width(118));
                if(GUILayout.Button("▶", GUILayout.Width(26)))
                {
                    GameObject current = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;
                    Bark.Play(d, BarkContext.Of(current), false);
                }
            }
        }
    }

    private static string[] triggerNames;

    private static string[] TriggerNames()
    {
        if(triggerNames == null)
        {
            var names = new List<string>();
            foreach(BarkTrigger t in System.Enum.GetValues(typeof(BarkTrigger))) { names.Add(BarkCsv.TriggerName(t)); }
            triggerNames = names.ToArray();
        }
        return triggerNames;
    }

    private static string FirstLine(BarkData d)
    {
        foreach(BarkVariant v in d.variants)
        {
            if(v == null) { continue; }
            foreach(BarkLine l in v.lines) { if(l != null && !string.IsNullOrEmpty(l.text)) { return l.text.Replace("\n", " "); } }
        }
        return "";
    }

    //=====================================================================
    // 再生中
    //=====================================================================

    private void DrawPlayPanel()
    {
        if(!Application.isPlaying) { return; }

        EditorGUILayout.Space(4);
        using(new EditorGUILayout.HorizontalScope())
        {
            Bark.Muted = GUILayout.Toggle(Bark.Muted, "一言を出さない", GUILayout.Width(110));
            if(GUILayout.Button("出している一言を止める", GUILayout.Width(150)))
            {
                BarkView v = BarkView.Find();
                if(v != null) { v.StopCurrent(true); }
            }
            if(GUILayout.Button("出した記録を消す", GUILayout.Width(120))) { Bark.ResetHistory(); }
            GUILayout.FlexibleSpace();
        }

        showLog = EditorGUILayout.Foldout(showLog, "最近出した一言（新しい順）", true);
        if(!showLog) { return; }
        foreach(string line in Bark.Log) { EditorGUILayout.LabelField(line); }
    }
}
