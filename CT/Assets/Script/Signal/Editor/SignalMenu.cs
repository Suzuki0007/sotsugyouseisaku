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
}
