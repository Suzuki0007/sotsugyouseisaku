/*********************************************************************/
// * \file   SceneFootstepMenu.cs
// * \brief  Hierarchy 右クリック > 音 > 足音の設定（このシーン／範囲）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 足音の設定を置くメニュー
/// </summary>
public static class SceneFootstepMenu
{
    [MenuItem("GameObject/音/足音の設定（このシーン全体）", false, 41)]
    private static void CreateWhole(MenuCommand command)
    {
        Create(command, "足音の設定（シーン全体）", false);
    }

    [MenuItem("GameObject/音/足音の設定（範囲の中だけ）", false, 42)]
    private static void CreateArea(MenuCommand command)
    {
        Create(command, "足音の設定（範囲）", true);
    }

    private static void Create(MenuCommand command, string name, bool withArea)
    {
        var go = new GameObject(name);
        go.AddComponent<SceneFootstep>();
        if(withArea)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(8.0f, 6.0f);
        }

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 p = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(p.x, p.y, 0.0f);
        }
        Undo.RegisterCreatedObjectUndo(go, name);
        Selection.activeGameObject = go;
    }
}
