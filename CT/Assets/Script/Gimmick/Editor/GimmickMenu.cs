/*********************************************************************/
// * \file   GimmickMenu.cs
// * \brief  Hierarchy 右クリック > 仕掛け > …（スイッチ・扉・答え合わせを置く）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 仕掛けを置くメニュー
/// </summary>
///
/// ・スイッチを選んでから「条件で動く物」「答え合わせ」を作ると、選んだスイッチが条件・入力に入る
public static class GimmickMenu
{
    //=====================================================================
    // 調べるスイッチ
    //=====================================================================

    [MenuItem("GameObject/仕掛け/調べるスイッチ", false, 10)]
    private static void PlaceSwitch(MenuCommand command)
    {
        var go = new GameObject("スイッチ");

        // 見た目（丸）。差し替えてよい
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        sr.color = new Color(1.0f, 0.85f, 0.3f, 1.0f);
        sr.sortingOrder = 5;
        go.transform.localScale = Vector3.one * 0.6f;

        // 調べる範囲
        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.5f;

        go.AddComponent<GimmickState>();
        go.AddComponent<GimmickSwitch>();

        // ON / OFF で色を変えるイベント（ひな形。差し替えてよい）
        Place(go, command, "調べるスイッチ");
    }

    //=====================================================================
    // 条件で動く物（扉）
    //=====================================================================

    [MenuItem("GameObject/仕掛け/条件で動く物（扉）", false, 11)]
    private static void PlaceDoor(MenuCommand command)
    {
        if(!FirstCallThisClick()) { return; }
        List<GimmickState> selected = SelectedStates();

        var go = new GameObject("条件で開く扉");
        var reaction = go.AddComponent<GimmickReaction>();

        // 閉じた扉（通れない）。条件を満たすと隠れる
        var closed = new GameObject("閉じた扉");
        closed.transform.SetParent(go.transform, false);
        var sr = closed.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = new Color(0.45f, 0.3f, 0.2f, 1.0f);
        sr.sortingOrder = 4;
        closed.transform.localScale = new Vector3(1.0f, 2.0f, 1.0f);
        closed.AddComponent<BoxCollider2D>();

        // 開いたとき・閉じたときのイベント（音など）
        var opened = new GameObject("開いたとき");
        opened.transform.SetParent(go.transform, false);
        var openSequence = opened.AddComponent<EventSequence>();

        var so = new SerializedObject(reaction);
        SerializedProperty hide = so.FindProperty("hideWhenMet");
        hide.arraySize = 1;
        hide.GetArrayElementAtIndex(0).objectReferenceValue = closed;
        so.FindProperty("onMet").objectReferenceValue = openSequence;
        AddConditions(so.FindProperty("condition").FindPropertyRelative("gimmicks"), selected);
        so.ApplyModifiedPropertiesWithoutUndo();

        Place(go, selected.Count > 0 ? new MenuCommand(null) : command, "条件で動く物");
    }

    //=====================================================================
    // 答え合わせ
    //=====================================================================

    [MenuItem("GameObject/仕掛け/答え合わせ", false, 12)]
    private static void PlacePuzzle(MenuCommand command)
    {
        if(!FirstCallThisClick()) { return; }
        List<GimmickState> selected = SelectedStates();

        var go = new GameObject("答え合わせ");
        go.AddComponent<GimmickState>();
        var puzzle = go.AddComponent<PuzzleCheck>();

        var solved = new GameObject("正解したとき");
        solved.transform.SetParent(go.transform, false);
        var sequence = solved.AddComponent<EventSequence>();

        var so = new SerializedObject(puzzle);
        so.FindProperty("onSolved").objectReferenceValue = sequence;
        SerializedProperty inputs = so.FindProperty("inputs");
        foreach(GimmickState state in selected)
        {
            inputs.arraySize++;
            inputs.GetArrayElementAtIndex(inputs.arraySize - 1).objectReferenceValue = state;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Place(go, selected.Count > 0 ? new MenuCommand(null) : command, "答え合わせ");
    }

    //=====================================================================
    // 仕掛けの状態だけ
    //=====================================================================

    [MenuItem("GameObject/仕掛け/仕掛けの状態だけ（イベントで変える）", false, 13)]
    private static void PlaceState(MenuCommand command)
    {
        var go = new GameObject("仕掛けの状態");
        go.AddComponent<GimmickState>();
        Place(go, command, "仕掛けの状態");
    }

    //=====================================================================
    // 共通
    //=====================================================================

    private static double lastCreated = -1.0;

    /// <summary>
    /// 複数を選んで Hierarchy の右クリックから実行すると、選んだ数だけ呼ばれるので、1 回にまとめる
    /// </summary>
    public static bool FirstCallThisClick()
    {
        double now = EditorApplication.timeSinceStartup;
        if(now - lastCreated < 0.2) { return false; }
        lastCreated = now;
        return true;
    }

    /// <summary> 選んでいる物の仕掛けの状態（選んだ順） </summary>
    private static List<GimmickState> SelectedStates()
    {
        var list = new List<GimmickState>();
        foreach(GameObject go in Selection.gameObjects)
        {
            GimmickState state = go != null ? go.GetComponent<GimmickState>() : null;
            if(state != null && !list.Contains(state)) { list.Add(state); }
        }
        return list;
    }

    private static void AddConditions(SerializedProperty gimmicks, List<GimmickState> states)
    {
        foreach(GimmickState state in states)
        {
            gimmicks.arraySize++;
            SerializedProperty c = gimmicks.GetArrayElementAtIndex(gimmicks.arraySize - 1);
            c.FindPropertyRelative("target").objectReferenceValue = state;
            c.FindPropertyRelative("compare").enumValueIndex = (int)GimmickCompare.On;
            c.FindPropertyRelative("value").intValue = 1;
        }
    }

    private static void Place(GameObject go, MenuCommand command, string undoName)
    {
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x, pivot.y, 0.0f);
        }
        Undo.RegisterCreatedObjectUndo(go, undoName);
        Selection.activeGameObject = go;
    }
}
