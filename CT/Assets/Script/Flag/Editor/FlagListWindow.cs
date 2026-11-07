/*********************************************************************/
// * \file   FlagListWindow.cs
// * \brief  フラグ一覧ウィンドウ
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > フラグ > フラグ一覧
/// ・プロジェクト内の GameFlag をすべて表示する（ID・セーブするか・メモ）
/// ・再生中は現在の値を表示し、クリックで ON / OFF を切り替えられる（動作確認用）
/// ・ID が重なっているフラグを警告する
/// </summary>
public class FlagListWindow : EditorWindow
{
    private readonly List<GameFlag> flags = new List<GameFlag>();
    private readonly HashSet<string> duplicateIds = new HashSet<string>();
    private string search = "";
    private Vector2 scroll;

    [MenuItem("Tools/フラグ/フラグ一覧")]
    private static void Open()
    {
        GetWindow<FlagListWindow>("フラグ一覧");
    }

    [MenuItem("Tools/フラグ/新しいフラグを作成")]
    private static void CreateFlag()
    {
        const string folder = "Assets/Data/Flags";
        if(!AssetDatabase.IsValidFolder("Assets/Data")) { AssetDatabase.CreateFolder("Assets", "Data"); }
        if(!AssetDatabase.IsValidFolder(folder)) { AssetDatabase.CreateFolder("Assets/Data", "Flags"); }

        GameFlag flag = CreateInstance<GameFlag>();
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewFlag.asset");
        AssetDatabase.CreateAsset(flag, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = flag;
        EditorGUIUtility.PingObject(flag);
    }

    private void OnEnable()
    {
        Reload();
        EditorApplication.projectChanged += Reload;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= Reload;
    }

    // 再生中は値が変わるので描き直す
    private void OnInspectorUpdate()
    {
        if(Application.isPlaying) { Repaint(); }
    }

    private void Reload()
    {
        flags.Clear();
        duplicateIds.Clear();

        var seen = new HashSet<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:GameFlag"))
        {
            GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(AssetDatabase.GUIDToAssetPath(guid));
            if(flag == null) { continue; }

            flags.Add(flag);
            if(!seen.Add(flag.Id)) { duplicateIds.Add(flag.Id); }
        }

        flags.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        Repaint();
    }

    private void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if(GUILayout.Button("新規作成", EditorStyles.toolbarButton, GUILayout.Width(60))) { CreateFlag(); }
            if(GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(40))) { Reload(); }
        }

        if(duplicateIds.Count > 0)
        {
            EditorGUILayout.HelpBox("ID が重なっているフラグがあります（同じフラグとして扱われます）: " + string.Join(", ", duplicateIds), MessageType.Warning);
        }

        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("再生中は、ここで ON / OFF を切り替えて動作を確認できます。", MessageType.Info);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach(GameFlag flag in flags)
        {
            if(flag == null) { continue; }
            if(!string.IsNullOrEmpty(search) &&
               flag.Id.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
               (flag.Memo == null || flag.Memo.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            using(new EditorGUILayout.HorizontalScope())
            {
                if(Application.isPlaying)
                {
                    bool value = flag.Value;
                    bool next = GUILayout.Toggle(value, value ? "ON" : "OFF", EditorStyles.miniButton, GUILayout.Width(40));
                    if(next != value) { flag.Value = next; }
                }

                if(GUILayout.Button(flag.Id, EditorStyles.label, GUILayout.Width(180)))
                {
                    Selection.activeObject = flag;
                    EditorGUIUtility.PingObject(flag);
                }

                GUILayout.Label(flag.Saved ? "セーブ" : "一時", GUILayout.Width(40));
                GUILayout.Label(flag.Memo ?? "", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
