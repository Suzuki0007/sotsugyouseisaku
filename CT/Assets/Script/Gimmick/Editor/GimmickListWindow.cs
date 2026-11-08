/*********************************************************************/
// * \file   GimmickListWindow.cs
// * \brief  仕掛けの一覧（Tools > 仕掛け > 仕掛けの一覧）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 開いているシーンの仕掛けの一覧
/// </summary>
///
/// ・仕掛けの状態（GimmickState）ごとに：種類・使えるキャラ・段階の数・つながっている先（配線）・セーブのキー
///   どこにもつながっていない状態は「つながっていない」と出す
/// ・2人の仕掛け・演出の部品（狭い通路・持ち上げる場所・合流の範囲・カメラの範囲・一言の範囲・即死の罠など）も種類ごとに並べる
/// ・再生中は今の値と「切り替え」「戻す」（細かい操作はデバッグウィンドウの「仕掛け」）
public class GimmickListWindow : EditorWindow
{
    private class Row
    {
        public GimmickState state;
        public string kind;
        public string user;
        public List<GimmickWiring.Edge> outs = new List<GimmickWiring.Edge>();
    }

    private readonly List<Row> rows = new List<Row>();
    private readonly List<(string kind, List<Component> items)> others = new List<(string, List<Component>)>();
    private Vector2 scroll;
    private string search = "";
    private bool onlyProblems;
    private bool dirty = true;

    [MenuItem("Tools/仕掛け/仕掛けの一覧", false, 290)]
    public static void Open()
    {
        var w = GetWindow<GimmickListWindow>("仕掛けの一覧");
        w.minSize = new Vector2(720, 300);
        w.dirty = true;
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += MarkDirty;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        dirty = true;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= MarkDirty;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
    }

    private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) { MarkDirty(); }

    private void MarkDirty() { dirty = true; Repaint(); }

    private void OnFocus() { dirty = true; }

    private void OnInspectorUpdate()
    {
        if(Application.isPlaying) { Repaint(); }
    }

    //=====================================================================
    // 集める
    //=====================================================================

    private void Collect()
    {
        dirty = false;
        rows.Clear();
        others.Clear();

        List<GimmickWiring.Edge> edges = GimmickWiring.Collect();
        foreach(GimmickState state in Object.FindObjectsByType<GimmickState>(FindObjectsInactive.Include))
        {
            if(state == null || EditorUtility.IsPersistent(state)) { continue; }
            var row = new Row { state = state, kind = KindOf(state.gameObject), user = UserOf(state.gameObject) };
            foreach(GimmickWiring.Edge e in edges) { if(e.from == state) { row.outs.Add(e); } }
            rows.Add(row);
        }
        rows.Sort((a, b) => string.CompareOrdinal(PathOf(a.state.transform), PathOf(b.state.transform)));

        AddOthers<CharacterBarrier>("狭い通路（キャラだけ通れる壁）");
        AddOthers<LiftPoint>("持ち上げる場所");
        AddOthers<PartyRejoinArea>("合流の範囲");
        AddOthers<CameraArea>("カメラの範囲");
        AddOthers<ConditionalVisible>("条件で見える物");
        AddOthers<PushBlock>("押せる箱");
        AddOthers<NotePickup>("メモ");
        AddOthers<ExamineSequence>("調べるたびに変わる");
        AddOthers<DeathTrap>("即死の罠");
        AddOthers<RetryPoint>("やり直す位置");
        AddOthers<BarkArea>("一言の範囲");
    }

    private void AddOthers<T>(string kind) where T : Component
    {
        var list = Object.FindObjectsByType<T>(FindObjectsInactive.Include).Where(c => c != null && !EditorUtility.IsPersistent(c)).Cast<Component>().ToList();
        if(list.Count == 0) { return; }
        list.Sort((a, b) => string.CompareOrdinal(PathOf(a.transform), PathOf(b.transform)));
        others.Add((kind, list));
    }

    private static string KindOf(GameObject go)
    {
        if(go.GetComponent<PuzzleCheck>() != null)  { return "答え合わせ"; }
        if(go.GetComponent<CodeLock>() != null)     { return "番号錠"; }
        if(go.GetComponent<FloorSwitch>() != null)  { return "床スイッチ"; }
        if(go.GetComponent<ItemSocket>() != null)   { return "置き台"; }
        if(go.GetComponent<GimmickHole>() != null)  { return "穴"; }
        if(go.GetComponent<BreakableWall>() != null) { return "壊せる壁"; }
        if(go.GetComponent<AttackSwitch>() != null) { return "攻撃で作動"; }
        if(go.GetComponent<GimmickSwitch>() != null) { return go.GetComponent<GimmickVisual>() != null ? "スイッチ（見た目つき）" : "調べるスイッチ"; }
        if(go.GetComponent<ExamineSequence>() != null) { return "調べた回数"; }
        return "状態だけ";
    }

    /// <summary> 使えるキャラ（部品の「使えるキャラ」の項目を探す） </summary>
    private static string UserOf(GameObject go)
    {
        foreach(MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
        {
            if(mb == null) { continue; }
            var so = new SerializedObject(mb);
            foreach(string name in new[] { "user", "passable", "breaker" })
            {
                SerializedProperty p = so.FindProperty(name);
                if(p != null && p.propertyType == SerializedPropertyType.Enum && p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length)
                {
                    return p.enumDisplayNames[p.enumValueIndex];
                }
            }
        }
        return "";
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        for(Transform p = t.parent; p != null; p = p.parent) { path = p.name + "/" + path; }
        return path;
    }

    //=====================================================================
    // 描く
    //=====================================================================

    private void OnGUI()
    {
        if(dirty) { Collect(); }

        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160));
            onlyProblems = GUILayout.Toggle(onlyProblems, "つながっていない物だけ", EditorStyles.toolbarButton, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("配線をいつも表示", EditorStyles.toolbarButton, GUILayout.Width(110))) { EditorApplication.ExecuteMenuItem("Tools/仕掛け/配線をいつも全部表示"); }
            if(GUILayout.Button("シーンチェック", EditorStyles.toolbarButton, GUILayout.Width(90))) { EditorApplication.ExecuteMenuItem("Tools/シーンチェック"); }
            if(GUILayout.Button("読み直す", EditorStyles.toolbarButton, GUILayout.Width(70))) { dirty = true; }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField($"仕掛けの状態（{rows.Count}）", EditorStyles.boldLabel);
        if(rows.Count == 0) { EditorGUILayout.HelpBox("このシーンに仕掛けの状態（GimmickState）はありません。Hierarchy 右クリック > 仕掛け から置けます。", MessageType.Info); }
        else { DrawHeader(); }

        foreach(Row row in rows)
        {
            if(row.state == null) { dirty = true; continue; }
            bool unused = row.outs.Count == 0 && !SelfActing(row.kind);
            if(onlyProblems && !unused) { continue; }
            if(!Matches(row)) { continue; }
            DrawRow(row, unused);
        }

        if(!onlyProblems)
        {
            foreach(var group in others)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"{group.kind}（{group.items.Count}）", EditorStyles.boldLabel);
                foreach(Component c in group.items)
                {
                    if(c == null) { dirty = true; continue; }
                    if(!string.IsNullOrEmpty(search) && !PathOf(c.transform).Contains(search)) { continue; }
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12);
                        if(GUILayout.Button(PathOf(c.transform), EditorStyles.linkLabel)) { Select(c.gameObject); }
                        if(!c.gameObject.activeInHierarchy) { GUILayout.Label("（非表示）", EditorStyles.miniLabel, GUILayout.Width(60)); }
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("つながっている先は、その状態を条件・入力に使っている物（Scene ビューの配線の矢印と同じ）。再生中の細かい操作はデバッグウィンドウの「仕掛け」。", EditorStyles.wordWrappedMiniLabel);
    }

    /// <summary> つながなくても、それだけで役目を果たす種類（壊れると通れる壁・埋まると渡れる穴） </summary>
    private static bool SelfActing(string kind) => kind == "壊せる壁" || kind == "穴";

    private bool Matches(Row row)
    {
        if(string.IsNullOrEmpty(search)) { return true; }
        if(PathOf(row.state.transform).Contains(search) || row.kind.Contains(search) || row.state.DisplayName.Contains(search)) { return true; }
        return row.outs.Any(e => e.to != null && (e.to.name.Contains(search) || e.label.Contains(search)));
    }

    private static void DrawHeader()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("名前", GUILayout.Width(170));
            GUILayout.Label("種類", GUILayout.Width(110));
            GUILayout.Label("使えるキャラ", GUILayout.Width(80));
            GUILayout.Label("段階", GUILayout.Width(32));
            GUILayout.Label(Application.isPlaying ? "今" : "最初", GUILayout.Width(70));
            GUILayout.Label("つながっている先");
        }
    }

    private void DrawRow(Row row, bool unused)
    {
        GimmickState s = row.state;
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button(new GUIContent(s.DisplayName, PathOf(s.transform) + "\nセーブ：" + s.SaveKey), EditorStyles.linkLabel, GUILayout.Width(170))) { Select(s.gameObject); }
            GUILayout.Label(row.kind, GUILayout.Width(110));
            GUILayout.Label(string.IsNullOrEmpty(row.user) ? "－" : row.user, GUILayout.Width(80));
            GUILayout.Label(s.StateCount.ToString(), GUILayout.Width(32));

            if(Application.isPlaying)
            {
                GUILayout.Label(s.ValueText + (s.Locked ? "（固定）" : ""), GUILayout.Width(70));
            }
            else
            {
                GUILayout.Label(s.StateCount == 2 ? (s.InitialValue != 0 ? "ON" : "OFF") : s.InitialValue.ToString(), GUILayout.Width(70));
            }

            if(row.outs.Count == 0 && SelfActing(row.kind))
            {
                GUILayout.Label("－（それだけで動く。つながなくてよい）", EditorStyles.miniLabel);
            }
            else if(unused)
            {
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1.0f, 0.65f, 0.3f) } };
                GUILayout.Label(row.kind == "答え合わせ" || row.kind == "番号錠" ? "つながっていない（扉などの条件に入れる）" : "つながっていない", style);
            }
            else
            {
                var labels = row.outs.Where(e => e.to != null).Select(e => e.to.name + "（" + e.label + "）").Distinct();
                GUILayout.Label(string.Join("、", labels), EditorStyles.wordWrappedLabel);
            }

            if(Application.isPlaying)
            {
                if(GUILayout.Button(s.StateCount == 2 ? "切り替え" : "次へ", EditorStyles.miniButton, GUILayout.Width(56))) { s.Toggle(true); }
                if(GUILayout.Button("戻す", EditorStyles.miniButton, GUILayout.Width(40))) { s.SetLocked(false); s.ResetValue(true); }
            }
        }
    }

    private static void Select(GameObject go)
    {
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        if(SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.FrameSelected(); }
    }
}
