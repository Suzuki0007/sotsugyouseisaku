/*********************************************************************/
// * \file   AttackPreviewAnchor.cs
// * \brief  攻撃のプレビューを出す位置（最後に選んだ敵・攻撃の発生源）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 攻撃のプレビューを出す位置を覚えておく
/// </summary>
///
/// ・敵（または攻撃の発生源）を選んでから攻撃データを選ぶと、その敵の位置に形が出る
/// ・選んだことがなければ、Scene ビューの真ん中
[InitializeOnLoad]
public static class AttackPreviewAnchor
{
    private static Transform last;

    static AttackPreviewAnchor()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        GameObject go = Selection.activeGameObject;
        if(go == null) { return; }

        PatternRunner runner = go.GetComponentInParent<PatternRunner>();
        if(runner != null) { last = runner.transform; }
    }

    /// <summary> プレビューの起点 </summary>
    public static Vector2 Position(SceneView view)
    {
        if(last != null) { return last.position; }
        return view != null ? (Vector2)view.pivot : Vector2.zero;
    }
}
