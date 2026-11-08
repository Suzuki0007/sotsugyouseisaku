/*********************************************************************/
// * \file   EnemyListWindow.cs
// * \brief  敵の一覧（全部の敵のデータを表で比べて直す）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 敵の一覧（Tools > 敵 > 敵の一覧）
/// </summary>
///
/// ・プロジェクトの EnemyData を全部並べて、体力・速さなどをその場で直せる（元に戻すは Ctrl + Z）
/// ・行の名前を押すと、そのデータを選ぶ（Inspector で全部の項目が見られる）
/// ・「使っている攻撃」は行動パターンの中の攻撃。押すとその攻撃を選ぶ
public class EnemyListWindow : EditorWindow
{
    //=====================================================================
    // 列
    //=====================================================================

    private class Column
    {
        public string title;
        public string property;
        public float width;
        public string tooltip;
    }

    private static readonly Column[] Columns =
    {
        new Column { title = "名前",         property = "displayName",      width = 110 },
        new Column { title = "体力",         property = "maxHealth",        width = 45 },
        new Column { title = "速さ",         property = "moveSpeed",        width = 45, tooltip = "プレイヤーは 5" },
        new Column { title = "大きさ",       property = "size",             width = 70 },
        new Column { title = "重い",         property = "heavy",            width = 35, tooltip = "押されにくい" },
        new Column { title = "体当たり",     property = "contactDamage",    width = 55, tooltip = "ぶつかったときのダメージ" },
        new Column { title = "ひるむまで",   property = "staggerThreshold", width = 60, tooltip = "0 ならひるまない" },
        new Column { title = "行動パターン", property = "pattern",          width = 150 },
        new Column { title = "倒されたあと", property = "defeatStyle",      width = 110 },
        new Column { title = "メモ",         property = "memo",             width = 160 },
    };

    //=====================================================================
    // 変数
    //=====================================================================

    private readonly List<EnemyData> datas = new List<EnemyData>();
    private Vector2 scroll;
    private string search = "";
    private bool showSamples = true;
    private int sortColumn = -1;
    private bool sortDescending;

    //=====================================================================
    // 開く
    //=====================================================================

    [MenuItem("Tools/敵/敵の一覧", false, 210)]
    public static void Open()
    {
        var window = GetWindow<EnemyListWindow>("敵の一覧");
        window.minSize = new Vector2(600, 240);
        window.Reload();
    }

    private void OnEnable() { Reload(); }

    private void OnProjectChange() { Reload(); Repaint(); }

    private void Reload()
    {
        datas.Clear();
        foreach(string guid in AssetDatabase.FindAssets("t:EnemyData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
            if(data != null) { datas.Add(data); }
        }
        datas.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
    }

    //=====================================================================
    // 表示
    //=====================================================================

    private void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            showSamples = GUILayout.Toggle(showSamples, "サンプルも出す", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("新しく作る", EditorStyles.toolbarButton)) { CreateNew(); }
            if(GUILayout.Button("読み直す", EditorStyles.toolbarButton)) { Reload(); }
        }

        EditorGUILayout.HelpBox("値をその場で直せます（Ctrl + Z で元に戻せます）。左の名前を押すとデータを選びます。列の見出しを押すと並べ替えます。", MessageType.None);

        List<EnemyData> rows = Filtered();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawHeader();
        foreach(EnemyData data in rows) { DrawRow(data); }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField($"{rows.Count} 件（全部で {datas.Count} 件）", EditorStyles.miniLabel);
    }

    private List<EnemyData> Filtered()
    {
        var rows = new List<EnemyData>();
        foreach(EnemyData d in datas)
        {
            if(d == null) { continue; }
            string path = AssetDatabase.GetAssetPath(d);
            if(!showSamples && path.Contains("/Sample/")) { continue; }
            if(!string.IsNullOrEmpty(search) && d.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 && d.DisplayName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0) { continue; }
            rows.Add(d);
        }

        if(sortColumn >= 0)
        {
            string prop = Columns[sortColumn].property;
            rows.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(SortKey(a, prop), SortKey(b, prop));
                return sortDescending ? -c : c;
            });
        }
        return rows;
    }

    private static string SortKey(EnemyData data, string property)
    {
        SerializedProperty p = new SerializedObject(data).FindProperty(property);
        if(p == null) { return ""; }
        switch(p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString("D8");
            case SerializedPropertyType.Float:   return p.floatValue.ToString("0000.000");
            case SerializedPropertyType.Boolean: return p.boolValue ? "1" : "0";
            case SerializedPropertyType.Enum:    return p.enumValueIndex.ToString("D3");
            case SerializedPropertyType.ObjectReference: return p.objectReferenceValue != null ? p.objectReferenceValue.name : "";
            default: return p.stringValue ?? "";
        }
    }

    private void DrawHeader()
    {
        using(new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("データ", EditorStyles.boldLabel, GUILayout.Width(170));
            for(int i = 0; i < Columns.Length; i++)
            {
                string mark = sortColumn == i ? (sortDescending ? " ▼" : " ▲") : "";
                if(GUILayout.Button(new GUIContent(Columns[i].title + mark, Columns[i].tooltip), EditorStyles.boldLabel, GUILayout.Width(Columns[i].width)))
                {
                    if(sortColumn == i) { sortDescending = !sortDescending; } else { sortColumn = i; sortDescending = false; }
                }
            }
            GUILayout.Label("使っている攻撃", EditorStyles.boldLabel);
        }
    }

    private void DrawRow(EnemyData data)
    {
        var so = new SerializedObject(data);
        so.Update();

        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button(new GUIContent(data.name, AssetDatabase.GetAssetPath(data)), EditorStyles.linkLabel, GUILayout.Width(170)))
            {
                Selection.activeObject = data;
                EditorGUIUtility.PingObject(data);
            }

            foreach(Column column in Columns)
            {
                SerializedProperty p = so.FindProperty(column.property);
                if(p == null) { GUILayout.Label("-", GUILayout.Width(column.width)); continue; }
                DrawValue(p, column.width);
            }

            // 使っている攻撃（行動パターンの中）
            foreach(AttackData attack in DebugWindow.CollectAttacks(data.Pattern))
            {
                if(GUILayout.Button(attack.name, EditorStyles.miniButton, GUILayout.ExpandWidth(false))) { Selection.activeObject = attack; }
            }
        }

        if(so.ApplyModifiedProperties()) { EditorUtility.SetDirty(data); }
    }

    /// <summary>
    /// 値だけを描く（PropertyField だと見出し［Header］や項目名［Label］まで出て表が崩れるため）
    /// </summary>
    private static void DrawValue(SerializedProperty p, float width)
    {
        GUILayoutOption w = GUILayout.Width(width);
        switch(p.propertyType)
        {
            case SerializedPropertyType.Integer:
                p.intValue = EditorGUILayout.IntField(p.intValue, w);
                break;
            case SerializedPropertyType.Float:
                p.floatValue = EditorGUILayout.FloatField(p.floatValue, w);
                break;
            case SerializedPropertyType.Boolean:
                p.boolValue = EditorGUILayout.Toggle(p.boolValue, w);
                break;
            case SerializedPropertyType.Enum:
                p.enumValueIndex = EditorGUILayout.Popup(p.enumValueIndex, p.enumDisplayNames, w);
                break;
            case SerializedPropertyType.ObjectReference:
                p.objectReferenceValue = EditorGUILayout.ObjectField(p.objectReferenceValue, typeof(EnemyPattern), false, w);
                break;
            case SerializedPropertyType.String:
                p.stringValue = EditorGUILayout.TextField(p.stringValue, w);
                break;
            default:
                GUILayout.Label("-", w);
                break;
        }
    }

    /// <summary>
    /// 新しい敵のデータを作る（サンプルの敵を写して、Assets/Data/Enemies/ に置く）
    /// </summary>
    private void CreateNew()
    {
        string path = EditorUtility.SaveFilePanelInProject("新しい敵のデータ", "EnemyData_新しい敵", "asset", "保存する場所を選んでください", "Assets/Data/Enemies");
        if(string.IsNullOrEmpty(path)) { return; }

        var source = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Sample/EnemyData_Sample.asset");
        EnemyData data = source != null ? Instantiate(source) : CreateInstance<EnemyData>();
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        Reload();
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
    }
}
