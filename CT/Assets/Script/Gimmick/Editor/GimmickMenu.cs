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
        Place(BuildSwitch(), command, "調べるスイッチ");
    }

    /// <summary> 調べるスイッチを作る（サンプルの部屋からも使う） </summary>
    public static GameObject BuildSwitch()
    {
        var go = new GameObject("スイッチ");

        // 見た目（丸）。差し替えてよい
        Look(go, Knob, new Vector2(0.6f, 0.6f), new Color(1.0f, 0.85f, 0.3f, 1.0f), 5);

        // 調べる範囲
        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.0f;

        go.AddComponent<GimmickState>();
        go.AddComponent<GimmickSwitch>();

        return go;
    }

    //=====================================================================
    // 条件で動く物（扉）
    //=====================================================================

    [MenuItem("GameObject/仕掛け/条件で動く物（扉）", false, 11)]
    private static void PlaceDoor(MenuCommand command)
    {
        if(!FirstCallThisClick()) { return; }
        List<GimmickState> selected = SelectedStates();
        GameObject go = BuildDoor(selected, new Vector2(1.0f, 2.0f));
        Place(go, selected.Count > 0 ? new MenuCommand(null) : command, "条件で動く物");
    }

    /// <summary> 条件で開く扉を作る（条件：渡した状態がすべて ON） </summary>
    public static GameObject BuildDoor(List<GimmickState> selected, Vector2 size)
    {
        var go = new GameObject("条件で開く扉");
        var reaction = go.AddComponent<GimmickReaction>();

        // 閉じた扉（通れない）。条件を満たすと隠れる
        var closed = new GameObject("閉じた扉");
        closed.transform.SetParent(go.transform, false);
        Look(closed, Square, size, new Color(0.45f, 0.3f, 0.2f, 1.0f), 4);
        closed.AddComponent<BoxCollider2D>().size = size;

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
        return go;
    }

    //=====================================================================
    // 答え合わせ
    //=====================================================================

    [MenuItem("GameObject/仕掛け/答え合わせ", false, 12)]
    private static void PlacePuzzle(MenuCommand command)
    {
        if(!FirstCallThisClick()) { return; }
        List<GimmickState> selected = SelectedStates();
        GameObject go = BuildPuzzle(selected);
        Place(go, selected.Count > 0 ? new MenuCommand(null) : command, "答え合わせ");
    }

    /// <summary> 答え合わせを作る（入力：渡した状態） </summary>
    public static GameObject BuildPuzzle(List<GimmickState> selected)
    {
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
        return go;
    }

    //=====================================================================
    // 床スイッチ
    //=====================================================================

    [MenuItem("GameObject/仕掛け/床スイッチ", false, 20)]
    private static void PlaceFloor(MenuCommand command)
    {
        Place(BuildFloor(), command, "床スイッチ");
    }

    /// <summary> 床スイッチを作る </summary>
    public static GameObject BuildFloor()
    {
        var go = new GameObject("床スイッチ");
        SpriteRenderer floorSr = Look(go, Square, new Vector2(0.9f, 0.9f), new Color(0.55f, 0.55f, 0.6f, 1.0f), 1);

        var area = go.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(0.9f, 0.9f);

        var state = go.AddComponent<GimmickState>();
        go.AddComponent<FloorSwitch>();

        // 押されている間は色を変える（ひな形。絵に差し替えてよい）
        var visual = go.AddComponent<GimmickVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("target").objectReferenceValue = floorSr;
        SerializedProperty looks = so.FindProperty("looks");
        looks.arraySize = 1;
        SerializedProperty on = looks.GetArrayElementAtIndex(0);
        on.FindPropertyRelative("value").intValue = 1;
        on.FindPropertyRelative("useColor").boolValue = true;
        on.FindPropertyRelative("color").colorValue = new Color(0.4f, 0.9f, 0.5f, 1.0f);
        so.ApplyModifiedPropertiesWithoutUndo();
        _ = state;
        return go;
    }

    //=====================================================================
    // 押せる箱・穴
    //=====================================================================

    [MenuItem("GameObject/仕掛け/押せる箱", false, 21)]
    private static void PlaceBlock(MenuCommand command)
    {
        GameObject go = BuildBlock();
        Place(go, command, "押せる箱");
        SnapToGrid(go);
    }

    /// <summary> 押せる箱を作る </summary>
    public static GameObject BuildBlock()
    {
        var go = new GameObject("押せる箱");
        Look(go, Square, new Vector2(0.95f, 0.95f), new Color(0.65f, 0.45f, 0.25f, 1.0f), 5);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.95f, 0.95f);
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<PushBlock>();
        return go;
    }

    [MenuItem("GameObject/仕掛け/穴（箱で埋まる）", false, 22)]
    private static void PlaceHole(MenuCommand command)
    {
        var go = new GameObject("穴");
        Look(go, Square, new Vector2(1.0f, 1.0f), new Color(0.08f, 0.08f, 0.1f, 1.0f), 1);

        go.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 1.0f);    // 通れない（埋まると消える）
        go.AddComponent<GimmickState>();
        go.AddComponent<GimmickHole>();

        Place(go, command, "穴");
        SnapToGrid(go);
    }

    //=====================================================================
    // 置き台・燭台
    //=====================================================================

    [MenuItem("GameObject/仕掛け/置き台（アイテムをはめる）", false, 23)]
    private static void PlaceSocket(MenuCommand command)
    {
        var go = new GameObject("置き台");
        Look(go, Square, new Vector2(0.8f, 0.8f), new Color(0.6f, 0.6f, 0.65f, 1.0f), 4);

        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.0f;

        // はめた物の絵
        var placed = new GameObject("はめた物");
        placed.transform.SetParent(go.transform, false);
        var placedSr = placed.AddComponent<SpriteRenderer>();
        placedSr.sortingOrder = 5;
        placedSr.enabled = false;

        go.AddComponent<GimmickState>();
        var socket = go.AddComponent<ItemSocket>();
        var so = new SerializedObject(socket);
        so.FindProperty("placedRenderer").objectReferenceValue = placedSr;
        so.ApplyModifiedPropertiesWithoutUndo();

        Place(go, command, "置き台");
    }

    [MenuItem("GameObject/仕掛け/燭台（調べて灯す・明かりつき）", false, 24)]
    private static void PlaceBrazier(MenuCommand command)
    {
        Place(BuildBrazier(), command, "燭台");
    }

    /// <summary> 燭台（調べて灯す・明かりつき）を作る </summary>
    public static GameObject BuildBrazier()
    {
        var go = new GameObject("燭台");
        Look(go, Knob, new Vector2(0.5f, 0.5f), new Color(0.35f, 0.3f, 0.3f, 1.0f), 5);

        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.2f;

        go.AddComponent<GimmickState>();
        var sw = go.AddComponent<GimmickSwitch>();
        var swSo = new SerializedObject(sw);
        swSo.FindProperty("action").enumValueIndex = (int)GimmickSwitchAction.TurnOn;
        swSo.ApplyModifiedPropertiesWithoutUndo();

        // 火（ON のときだけ表示）と明かり
        var fire = new GameObject("火");
        fire.transform.SetParent(go.transform, false);
        fire.transform.localPosition = new Vector3(0.0f, 0.35f, 0.0f);
        Look(fire, Knob, new Vector2(0.4f, 0.4f), new Color(1.0f, 0.6f, 0.15f, 1.0f), 6);
        var light = fire.AddComponent<LightSource>();

        var visual = go.AddComponent<GimmickVisual>();
        var so = new SerializedObject(visual);
        SerializedProperty looks = so.FindProperty("looks");
        looks.arraySize = 1;
        SerializedProperty on = looks.GetArrayElementAtIndex(0);
        on.FindPropertyRelative("value").intValue = 1;
        SerializedProperty show = on.FindPropertyRelative("show");
        show.arraySize = 1;
        show.GetArrayElementAtIndex(0).objectReferenceValue = fire;
        SerializedProperty enable = on.FindPropertyRelative("enable");
        enable.arraySize = 1;
        enable.GetArrayElementAtIndex(0).objectReferenceValue = light;
        so.ApplyModifiedPropertiesWithoutUndo();
        fire.SetActive(false);
        return go;
    }

    public const string Knob = "UI/Skin/Knob.psd";
    public const string Square = "UI/Skin/UISprite.psd";

    /// <summary>
    /// 仮の見た目（Unity 付属の絵を、決めた大きさ〔マス〕で表示する。絵は差し替えてよい）
    /// 付属の絵は 0.3 マスほどしかないので、Sliced にして大きさを決める
    /// </summary>
    public static SpriteRenderer Look(GameObject go, string builtin, Vector2 size, Color color, int order)
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtin);

        // 付属の絵は Sliced にすると警告が出る（Full Rect でない）ので、子の「見た目」を拡大する
        // （当たり判定は親に付けるので、拡大の影響を受けない。絵を差し替えるときは「見た目」の Sprite と大きさを変える）
        var child = new GameObject("見た目");
        child.transform.SetParent(go.transform, false);
        Vector2 native = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
        child.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, native.x), size.y / Mathf.Max(0.01f, native.y), 1.0f);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    public static void SnapToGrid(GameObject go)
    {
        Vector3 p = go.transform.position;
        go.transform.position = new Vector3(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f, p.z);
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

    public static void Place(GameObject go, MenuCommand command, string undoName)
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
