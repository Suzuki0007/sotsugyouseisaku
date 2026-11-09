/*********************************************************************/
// * \file   GimmickWiring.cs
// * \brief  仕掛けの配線（どのスイッチが何につながっているか）を Scene ビューに線で出す
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 仕掛けの配線の表示
/// </summary>
///
/// ・仕掛けの状態（GimmickState）を参照している物（扉の条件・答え合わせの入力・イベントの部品・攻撃で作動するスイッチ など）へ、
///   スイッチから矢印を引く。選んでいる物につながる線だけを出す（Tools > 仕掛け > 配線をいつも全部表示 で全部）
/// ・再生中は、ON の仕掛けからの線が黄色になる
/// ・参照は、シーンのコンポーネントの中の GimmickState への参照をすべて探す（新しい部品を足しても自動で出る）
[InitializeOnLoad]
public static class GimmickWiring
{
    public struct Edge
    {
        public GimmickState from;
        public Component to;
        public string label;
    }

    private const string AlwaysKey = "CT.GimmickWiring.Always";
    private const string MenuPath = "Tools/仕掛け/配線をいつも全部表示";

    private static readonly List<Edge> edges = new List<Edge>();
    private static bool dirty = true;

    static GimmickWiring()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += MarkDirty;
        Selection.selectionChanged += () => { MarkDirty(); SceneView.RepaintAll(); };
        Undo.undoRedoPerformed += MarkDirty;
        ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream stream) => MarkDirty();
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) => MarkDirty();
    }

    private static void MarkDirty() { dirty = true; }

    private static bool Always
    {
        get => EditorPrefs.GetBool(AlwaysKey, false);
        set => EditorPrefs.SetBool(AlwaysKey, value);
    }

    [MenuItem(MenuPath, false, 300)]
    private static void ToggleAlways()
    {
        Always = !Always;
        SceneView.RepaintAll();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleAlwaysValidate()
    {
        Menu.SetChecked(MenuPath, Always);
        return true;
    }

    //=====================================================================
    // 集める
    //=====================================================================

    /// <summary>
    /// 開いているシーンの配線を全部集める
    /// </summary>
    public static List<Edge> Collect()
    {
        var result = new List<Edge>();
        foreach(MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if(mb == null || mb is GimmickState) { continue; }
            if(EditorUtility.IsPersistent(mb)) { continue; }

            var so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();
            while(it.Next(true))
            {
                if(it.propertyType != SerializedPropertyType.ObjectReference) { continue; }
                if(!(it.objectReferenceValue is GimmickState state)) { continue; }

                // 自分のオブジェクトの状態（スイッチ自身の状態など）は線を引かない
                if(state.gameObject == mb.gameObject) { continue; }

                result.Add(new Edge { from = state, to = mb, label = LabelOf(mb, so, it) });
            }
        }
        return result;
    }

    private static string LabelOf(MonoBehaviour mb, SerializedObject so, SerializedProperty prop)
    {
        string path = prop.propertyPath;

        // GameCondition の中の「仕掛け」の条件なら、比べ方まで出す
        if(path.EndsWith(".target") && path.Contains("gimmicks.Array.data"))
        {
            string parent = path.Substring(0, path.Length - ".target".Length);
            SerializedProperty compare = so.FindProperty(parent + ".compare");
            SerializedProperty value = so.FindProperty(parent + ".value");
            string condition = compare != null ? CompareText((GimmickCompare)compare.enumValueIndex, value != null ? value.intValue : 0) : "";
            return $"{Kind(mb)}：{condition}";
        }
        return Kind(mb);
    }

    private static string CompareText(GimmickCompare compare, int value)
    {
        switch(compare)
        {
            case GimmickCompare.On:       return "ON のとき";
            case GimmickCompare.Off:      return "OFF のとき";
            case GimmickCompare.Equal:    return $"{value} のとき";
            case GimmickCompare.NotEqual: return $"{value} でないとき";
            case GimmickCompare.AtLeast:  return $"{value} 以上";
            default:                      return $"{value} 以下";
        }
    }

    private static string Kind(MonoBehaviour mb)
    {
        switch(mb)
        {
            case GimmickReaction _:    return "条件で動く";
            case PuzzleCheck _:        return "答え合わせの入力";
            case GimmickEventAction _: return "イベントで操作";
            case AttackSwitch _:       return "攻撃で作動";
            case GimmickSwitch _:      return "使える条件";
            default:                   return mb.GetType().Name;
        }
    }

    //=====================================================================
    // 描く
    //=====================================================================

    private static void OnSceneGUI(SceneView view)
    {
        if(Event.current.type != EventType.Repaint) { return; }

        if(dirty)
        {
            dirty = false;
            edges.Clear();
            edges.AddRange(Collect());
        }
        if(edges.Count == 0) { return; }

        bool always = Always;
        var labelStyle = new GUIStyle(EditorStyles.miniLabel);

        foreach(Edge e in edges)
        {
            if(e.from == null || e.to == null) { continue; }
            if(!always && !IsSelected(e.from.gameObject) && !IsSelected(e.to.gameObject)) { continue; }

            Vector3 a = e.from.transform.position;
            Vector3 b = e.to.transform.position;

            bool on = Application.isPlaying && e.from.IsOn;
            Color color = on ? new Color(1.0f, 0.9f, 0.2f, 0.95f) : new Color(0.4f, 0.85f, 1.0f, 0.9f);
            Handles.color = color;
            Handles.DrawAAPolyLine(3.0f, a, b);

            // 矢印の先（線の 85% の位置に三角）
            Vector3 dir = (b - a);
            if(dir.sqrMagnitude > 0.01f)
            {
                Vector3 tip = Vector3.Lerp(a, b, 0.85f);
                Vector3 n = dir.normalized;
                Vector3 side = new Vector3(-n.y, n.x, 0.0f) * 0.18f;
                Handles.DrawAAConvexPolygon(tip, tip - n * 0.35f + side, tip - n * 0.35f - side);
            }

            labelStyle.normal.textColor = color;
            string state = Application.isPlaying ? $"［{e.from.ValueText}］" : "";
            Handles.Label(Vector3.Lerp(a, b, 0.5f), $"{e.from.DisplayName}{state} → {e.label}", labelStyle);
        }
    }

    private static bool IsSelected(GameObject go)
    {
        foreach(GameObject selected in Selection.gameObjects)
        {
            if(selected == null) { continue; }
            if(go == selected || go.transform.IsChildOf(selected.transform)) { return true; }
        }
        return false;
    }
}
