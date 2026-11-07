/*********************************************************************/
// * \file   FlagCreator.cs
// * \brief  フラグ関連のひな形を作るエディタ拡張
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// Hierarchy で右クリック → フラグ から、ひな形を作る
///   ・フラグで切り替え … FlagSwitch
///   ・入るとフラグを設定するエリア … BoxCollider2D ＋ EventZone ＋ EventSequence ＋ SetFlagEventAction
/// </summary>
public static class FlagCreator
{
    private const string MenuRoot = "GameObject/フラグ/";

    [MenuItem(MenuRoot + "フラグで切り替え", false, 10)]
    private static void CreateSwitch(MenuCommand command)
    {
        GameObject root = new GameObject("FlagSwitch");
        root.AddComponent<FlagSwitch>();

        Finish(root, command);
    }

    [MenuItem(MenuRoot + "入るとフラグを設定するエリア", false, 11)]
    private static void CreateZone(MenuCommand command)
    {
        GameObject root = new GameObject("FlagZone");

        BoxCollider2D area = root.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(2f, 2f);

        // EventZone は EventSequence を自動で付ける
        EventZone zone = root.AddComponent<EventZone>();
        EventSequence sequence = root.GetComponent<EventSequence>();

        SetFlagEventAction action = root.AddComponent<SetFlagEventAction>();
        action.AddFlagForEditor(null, true);
        sequence.AddAction(action);

        var serialized = new SerializedObject(zone);
        serialized.FindProperty("eventSequence").objectReferenceValue = sequence;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Finish(root, command);
    }

    /// <summary>
    /// 右クリックしたオブジェクトの子にする（なければシーンビューの中央に置く）、Undo 登録して選択する
    /// </summary>
    private static void Finish(GameObject root, MenuCommand command)
    {
        GameObject parent = command.context as GameObject;
        if(parent != null)
        {
            GameObjectUtility.SetParentAndAlign(root, parent);
        }
        else if(SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            root.transform.position = new Vector3(pivot.x, pivot.y, 0f);
        }

        Undo.RegisterCreatedObjectUndo(root, $"Create {root.name}");
        Selection.activeObject = root;
    }
}
