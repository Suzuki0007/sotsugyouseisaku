/*********************************************************************/
// * \file   SignalMenu.cs
// * \brief  Hierarchy 右クリック > 合図 > 合図を受けたら（合図に反応するオブジェクトを作る）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 合図のメニュー
/// </summary>
public static class SignalMenu
{
    [MenuItem("GameObject/合図/合図を受けたら", false, 40)]
    private static void CreateListener(MenuCommand command)
    {
        var go = new GameObject("合図を受けたら");
        go.AddComponent<SignalListener>();

        // 反応したときに実行するイベントの入れ物（すぐ部品を足せるように）
        var seqGo = new GameObject("反応したとき");
        seqGo.transform.SetParent(go.transform, false);
        var seq = seqGo.AddComponent<EventSequence>();

        var so = new SerializedObject(go.GetComponent<SignalListener>());
        SerializedProperty list = so.FindProperty("reactions");
        list.arraySize = 1;
        SerializedProperty first = list.GetArrayElementAtIndex(0);
        first.FindPropertyRelative("label").stringValue = "反応1";
        first.FindPropertyRelative("sequence").objectReferenceValue = seq;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 p = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(p.x, p.y, 0.0f);
        }
        Undo.RegisterCreatedObjectUndo(go, "合図を受けたら");
        Selection.activeGameObject = go;
    }

    /// <summary> 標準の合図の置き場所 </summary>
    public const string StandardFolder = "Assets/Data/Signals/標準";

    /// <summary>
    /// 標準の合図のアセットを作る（あるものは ID とメモを合わせ直す。名前は変えない）
    /// </summary>
    [MenuItem("Tools/合図/標準の合図を作る（足りない分）")]
    public static void CreateStandardSignals()
    {
        int created = CreateStandardSignalsSilently(out int updated);
        EditorUtility.DisplayDialog("標準の合図", $"作った：{created} 個　合わせ直した：{updated} 個\n{StandardFolder}", "OK");
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(StandardFolder);
        if(folder != null) { EditorGUIUtility.PingObject(folder); }
    }

    /// <summary> 標準の合図のアセットを作る（ダイアログなし。作った数を返す） </summary>
    public static int CreateStandardSignalsSilently(out int updated)
    {
        updated = 0;
        if(!AssetDatabase.IsValidFolder("Assets/Data")) { AssetDatabase.CreateFolder("Assets", "Data"); }
        if(!AssetDatabase.IsValidFolder("Assets/Data/Signals")) { AssetDatabase.CreateFolder("Assets/Data", "Signals"); }
        if(!AssetDatabase.IsValidFolder(StandardFolder)) { AssetDatabase.CreateFolder("Assets/Data/Signals", "標準"); }

        // 今ある合図を ID で引けるように
        var existing = new System.Collections.Generic.Dictionary<string, GameSignal>();
        foreach(string guid in AssetDatabase.FindAssets("t:GameSignal"))
        {
            var s = AssetDatabase.LoadAssetAtPath<GameSignal>(AssetDatabase.GUIDToAssetPath(guid));
            if(s != null && !existing.ContainsKey(s.Id)) { existing[s.Id] = s; }
        }

        int created = 0;
        foreach(StandardSignals.Info info in StandardSignals.All)
        {
            if(existing.TryGetValue(info.id, out GameSignal found))
            {
                if(found.Memo != info.memo)
                {
                    found.Setup(info.id, info.memo);
                    EditorUtility.SetDirty(found);
                    updated++;
                }
                continue;
            }

            var asset = ScriptableObject.CreateInstance<GameSignal>();
            asset.Setup(info.id, info.memo);
            AssetDatabase.CreateAsset(asset, $"{StandardFolder}/{info.name}.asset");
            created++;
        }
        AssetDatabase.SaveAssets();
        return created;
    }
}
