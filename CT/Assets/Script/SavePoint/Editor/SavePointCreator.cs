/*********************************************************************/
// * \file   SavePointCreator.cs
// * \brief  セーブポイントのひな形を作るエディタ拡張
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// Hierarchy で右クリック → セーブポイント
/// 仮の見た目（青い丸）・調べる範囲（BoxCollider2D、Is Trigger）・SavePoint をまとめて作る
/// 見た目は SpriteRenderer の Sprite を差し替える
/// </summary>
public static class SavePointCreator
{
    [MenuItem("GameObject/セーブポイント", false, 12)]
    private static void Create(MenuCommand command)
    {
        GameObject go = CreateAt(Vector3.zero);

        GameObject parent = command.context as GameObject;
        if(parent != null)
        {
            GameObjectUtility.SetParentAndAlign(go, parent);
        }
        else if(SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x, pivot.y, 0f);
        }

        Undo.RegisterCreatedObjectUndo(go, "Create SavePoint");
        Selection.activeObject = go;
    }

    /// <summary>
    /// セーブポイントを作る（スクリプトからも使う）
    /// </summary>
    public static GameObject CreateAt(Vector3 position)
    {
        var go = new GameObject("SavePoint");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = new Color(0.45f, 0.8f, 1f, 1f);
        sr.sortingOrder = 50;
        go.transform.localScale = new Vector3(4f, 4f, 1f);   // 仮の画像は小さいので拡大（約 0.8 マス）

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.4f, 0.4f);                  // 拡大後に約 1.6 マスの調べる範囲

        go.AddComponent<SavePoint>();
        return go;
    }
}
