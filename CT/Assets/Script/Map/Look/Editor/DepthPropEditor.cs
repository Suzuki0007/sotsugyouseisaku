/*********************************************************************/
// * \file   DepthPropEditor.cs
// * \brief  奥行きのある物（DepthProp）の Inspector と、作る・作り直す処理（メニュー・レシピから）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 奥行きのある物の Inspector（当たり判定と境目を作り直す）と、物をくるんで奥行きのある物にする処理
/// </summary>
[CustomEditor(typeof(DepthProp))]
public class DepthPropEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if(GUILayout.Button("当たり判定と境目を作り直す"))
        {
            foreach(Object t in targets) { Rebuild((DepthProp)t, true); }
        }
        EditorGUILayout.HelpBox("赤い四角＝入れない足元。青い線より上のキャラは物の後ろ、下のキャラは前。数字を変えたら押してください（黄色い線が出ていたら作り直しが要ります）。", MessageType.Info);
    }

    //=====================================================================
    // メニュー（Hierarchy 右クリック > マップの見た目 > 奥行きのある物にする）
    //=====================================================================

    private static bool menuQueued;

    [MenuItem("GameObject/マップの見た目/奥行きのある物にする", false, 42)]
    private static void MenuWrap(MenuCommand cmd)
    {
        // 複数選んで右クリックすると 1 つずつ呼ばれるので、まとめて 1 回だけ（選んだ物を 1 つの「奥行きのある物」にする）
        if(menuQueued) { return; }
        menuQueued = true;
        EditorApplication.delayCall += () =>
        {
            menuQueued = false;
            var targets = new List<GameObject>(Selection.gameObjects);
            if(targets.Count == 0 && cmd.context is GameObject one) { targets.Add(one); }
            if(targets.Count == 0) { return; }
            DepthProp prop = Wrap(targets, null, true);
            if(prop != null)
            {
                Selection.activeObject = prop.gameObject;
                Debug.Log($"{prop.name}：奥行きのある物にしました（赤い四角＝入れない足元、青い線より上は後ろ）", prop);
            }
        };
    }

    [MenuItem("GameObject/マップの見た目/奥行きのある物にする", true)]
    private static bool MenuWrapValidate()
    {
        foreach(GameObject go in Selection.gameObjects) { if(go.GetComponentInChildren<SpriteRenderer>() != null) { return true; } }
        return false;
    }

    //=====================================================================
    // 作る・作り直す
    //=====================================================================

    /// <summary>
    /// 物（いくつでも）を足元の親でくるんで「奥行きのある物」にする。親の名前が空なら最初の物の名前から
    /// </summary>
    public static DepthProp Wrap(IList<GameObject> targets, string name, bool undo)
    {
        if(targets == null || targets.Count == 0) { return null; }
        GameObject first = targets[0];
        if(string.IsNullOrEmpty(name)) { name = first.name + "（奥行き）"; }

        var root = new GameObject(name);
        if(undo) { Undo.RegisterCreatedObjectUndo(root, "奥行きのある物にする"); }
        Transform parent = first.transform.parent;
        if(parent != null) { root.transform.SetParent(parent, false); }
        else { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, first.scene); }
        root.transform.SetSiblingIndex(first.transform.GetSiblingIndex());
        root.transform.position = first.transform.position;

        foreach(GameObject go in targets)
        {
            if(undo) { Undo.SetTransformParent(go.transform, root.transform, "奥行きのある物にする"); }
            else     { go.transform.SetParent(root.transform, true); }
        }

        var group = root.AddComponent<SortingGroup>();
        // 子の絵の並び順・層を引き継ぐ（キャラと同じ並び順にすると、上下の位置で前後が決まる）
        SpriteRenderer sr = first.GetComponentInChildren<SpriteRenderer>();
        if(sr != null)
        {
            group.sortingLayerID = sr.sortingLayerID;
            group.sortingOrder = sr.sortingOrder;
        }
        var prop = root.AddComponent<DepthProp>();
        Rebuild(prop, undo);
        return prop;
    }

    /// <summary>
    /// 足元の当たり判定（歩ける範囲の子）を作り直し、親の位置を前後の境目へ動かす（絵の位置は変えない）
    /// </summary>
    public static void Rebuild(DepthProp prop, bool undo)
    {
        if(!prop.TryGetFootprint(out Rect rect))
        {
            Debug.LogWarning($"{prop.name}：子に絵（SpriteRenderer）がないので作れません", prop);
            return;
        }

        // 親を境目の高さへ（子は世界の位置のまま）
        prop.TryGetSortLine(out float line);
        Transform t = prop.transform;
        var children = new List<Transform>();
        foreach(Transform c in t) { children.Add(c); }
        var world = new List<Vector3>();
        foreach(Transform c in children) { world.Add(c.position); }
        if(undo)
        {
            Undo.RecordObject(t, "当たり判定と境目を作り直す");
            foreach(Transform c in children) { Undo.RecordObject(c, "当たり判定と境目を作り直す"); }
        }
        t.position = new Vector3(rect.center.x, line, t.position.z);
        for(int i = 0; i < children.Count; i++) { children[i].position = world[i]; }

        // 足元の当たり判定
        if(prop.footprint != null)
        {
            if(undo) { Undo.DestroyObjectImmediate(prop.footprint); }
            else     { Object.DestroyImmediate(prop.footprint); }
        }
        WalkArea area = MapLookMenu.FindWalkAreaFor(rect.center);
        if(area == null)
        {
            Debug.LogWarning($"{prop.name}：シーンに歩ける範囲（WalkArea）がないので、当たり判定を作れません（前後の境目だけ決めました）", prop);
            return;
        }
        if(undo) { Undo.RecordObject(prop, "当たり判定と境目を作り直す"); }
        prop.footprint = MapLookMenu.AddFootprintRect(area, "歩けない 足元：" + prop.name, rect, undo);
        EditorUtility.SetDirty(prop);
    }
}
