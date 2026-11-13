/*********************************************************************/
// * \file   RoomMenu.cs
// * \brief  Hierarchy 右クリック > 部屋 > 普通の部屋／戦闘の部屋／謎解きの部屋（中身をそろえて作る）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 部屋の型を作るメニュー
/// </summary>
///
/// ・部屋の絵（SpriteRenderer）を選んでから作ると、その絵に合わせて作る（歩ける範囲・カメラの範囲の大きさ、マスクを重ねる絵）
///   選んでいなければシーンビューの真ん中に 16×12 の大きさで作る
/// ・どの部屋にも：Room・歩ける範囲（仮の四角。マスク画像から作り直す）・カメラの範囲・出現位置・着いたとき（非表示。使うなら表示）
/// ・戦闘の部屋：戦闘エリア（入ったら始まる・敵を全部倒すと勝ち・1回だけ）・やり直す位置・勝ったとき（最後にオートセーブ）
/// ・謎解きの部屋：スイッチ2つ → 答え合わせ → 扉（正解で開く。開いたときの最後にオートセーブ）
/// ・プレハブではなく作る処理なので、シーンの物への参照が切れない（作ったあとは自由に直してよい）
public static class RoomMenu
{
    [MenuItem("GameObject/部屋/普通の部屋", false, 30)]
    private static void CreateNormal(MenuCommand command) { Create(RoomKind.Normal, command); }

    [MenuItem("GameObject/部屋/戦闘の部屋", false, 31)]
    private static void CreateBattle(MenuCommand command) { Create(RoomKind.Battle, command); }

    [MenuItem("GameObject/部屋/謎解きの部屋", false, 32)]
    private static void CreatePuzzle(MenuCommand command) { Create(RoomKind.Puzzle, command); }

    private static int lastFrame = -1;

    private static void Create(RoomKind kind, MenuCommand command)
    {
        // 複数選んでいると1つずつ呼ばれるので、1回だけ作る
        if(lastFrame == Time.frameCount && command.context != null) { return; }
        lastFrame = Time.frameCount;

        SpriteRenderer image = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<SpriteRenderer>() : null;
        GameObject room = Build(kind, image, null);
        Undo.RegisterCreatedObjectUndo(room, "部屋を作る");
        Selection.activeGameObject = room;
    }

    /// <summary>
    /// 部屋を作る（レシピからも使う）
    /// </summary>
    /// <param name="image"> 部屋の絵（null ならシーンビューの真ん中に 16×12） </param>
    /// <param name="id"> 部屋の ID（null なら自動） </param>
    public static GameObject Build(RoomKind kind, SpriteRenderer image, string id)
    {
        Vector3 center;
        Vector2 size;
        if(image != null && image.sprite != null)
        {
            center = image.bounds.center;
            size = image.bounds.size;
        }
        else
        {
            center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            size = new Vector2(16.0f, 12.0f);
        }
        center.z = 0.0f;

        string kindName = kind == RoomKind.Battle ? "戦闘の部屋" : kind == RoomKind.Puzzle ? "謎解きの部屋" : "普通の部屋";
        if(string.IsNullOrEmpty(id)) { id = "Room_" + System.Guid.NewGuid().ToString("N").Substring(0, 6); }

        var root = new GameObject($"部屋_{kindName}（{id}）");
        root.transform.position = center;
        root.AddComponent<Room>().Setup(kindName, id, kind, image);

        // ---- 歩ける範囲（仮の四角。マスク画像から作り直す） ----
        var walk = Child(root, "歩ける範囲", center);
        var walkArea = walk.AddComponent<WalkArea>();
        Set(walkArea, "maskTarget", image);
        var floor = Child(walk, "床（仮の四角。マスク画像から作ったら消す）", center);
        var floorBox = floor.AddComponent<BoxCollider2D>();
        floorBox.isTrigger = true;
        floorBox.size = size * 0.8f;

        // ---- カメラの範囲（部屋の絵と同じ大きさ） ----
        var cam = Child(root, "カメラの範囲", center);
        var camBox = cam.AddComponent<BoxCollider2D>();
        camBox.isTrigger = true;
        camBox.size = size;
        cam.AddComponent<CameraArea>();

        // ---- 出現位置（下のほう） ----
        Vector3 entry = center + new Vector3(0.0f, -size.y * 0.3f, 0.0f);
        var spawn = Child(root, $"出現位置（{id}_Entrance）", entry);
        Set(spawn.AddComponent<PlayerSpawnPoint>(), "spawnPointId", id + "_Entrance");

        // ---- 着いたとき（非表示。使うなら表示して、条件と中身を決める） ----
        var arrive = Child(root, "着いたとき（使うなら表示する）", center);
        var arriveSeq = arrive.AddComponent<EventSequence>();
        Set(arriveSeq, "lockPlayer", true);
        AddWait(arrive, 0.3f);
        var start = arrive.AddComponent<SceneStartEvent>();
        Set(start, "sequence", arriveSeq);
        arrive.SetActive(false);

        if(kind == RoomKind.Battle) { BuildBattle(root, center, size, entry, id); }
        if(kind == RoomKind.Puzzle) { BuildPuzzle(root, center, size); }

        return root;
    }

    //=====================================================================
    // 戦闘の部屋
    //=====================================================================

    private static void BuildBattle(GameObject root, Vector3 center, Vector2 size, Vector3 entry, string id)
    {
        var go = Child(root, "戦闘エリア", center);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size * 0.6f;
        var battle = go.AddComponent<BattleArea>();

        var restart = Child(go, "やり直す位置", entry);

        var onStart = Child(go, "始まったとき", center).AddComponent<EventSequence>();
        var winGo = Child(go, "勝ったとき", center);
        var onWin = winGo.AddComponent<EventSequence>();
        Set(onWin, "lockPlayer", true);
        AddWait(winGo, 0.5f);
        winGo.AddComponent<AutoSaveEventAction>();
        var onLose = Child(go, "負けたとき", center).AddComponent<EventSequence>();

        var so = new SerializedObject(battle);
        so.FindProperty("start").enumValueIndex = (int)BattleStart.EnterArea;
        so.FindProperty("win").enumValueIndex = 1;   // 敵を全部倒す
        so.FindProperty("restartPoint").objectReferenceValue = restart.transform;
        so.FindProperty("onStart").objectReferenceValue = onStart;
        so.FindProperty("onWin").objectReferenceValue = onWin;
        so.FindProperty("onLose").objectReferenceValue = onLose;
        so.FindProperty("objective").stringValue = "敵を全部倒せ";
        so.FindProperty("once").boolValue = true;
        so.FindProperty("saveId").stringValue = id + "_Battle";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    //=====================================================================
    // 謎解きの部屋
    //=====================================================================

    private static void BuildPuzzle(GameObject root, Vector3 center, Vector2 size)
    {
        var states = new List<GimmickState>();
        for(int i = 0; i < 2; i++)
        {
            GameObject sw = GimmickMenu.BuildSwitch();
            sw.name = $"スイッチ{i + 1}";
            sw.transform.SetParent(root.transform, false);
            sw.transform.position = center + new Vector3((i == 0 ? -0.25f : 0.25f) * size.x, 0.0f, 0.0f);
            states.Add(sw.GetComponent<GimmickState>());
        }

        GameObject puzzle = GimmickMenu.BuildPuzzle(states);
        puzzle.transform.SetParent(root.transform, false);
        puzzle.transform.position = center;

        // 正解したとき：ひと呼吸（効果音・会話などはここに足す）
        Transform solved = puzzle.transform.Find("正解したとき");
        if(solved != null) { AddWait(solved.gameObject, 0.3f); }

        var puzzleState = new List<GimmickState> { puzzle.GetComponent<GimmickState>() };
        GameObject door = GimmickMenu.BuildDoor(puzzleState, new Vector2(1.5f, 1.0f));
        door.name = "扉（正解で開く）";
        door.transform.SetParent(root.transform, false);
        door.transform.position = center + new Vector3(0.0f, size.y * 0.3f, 0.0f);

        // 扉が開くと通り道が変わるので、開いたときの最後にオートセーブ
        Transform opened = door.transform.Find("開いたとき");
        if(opened != null) { opened.gameObject.AddComponent<AutoSaveEventAction>(); }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static GameObject Child(GameObject parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = position;
        return go;
    }

    private static void AddWait(GameObject go, float seconds)
    {
        Set(go.AddComponent<WaitEventAction>(), "seconds", seconds);
    }

    private static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p == null) { Debug.LogWarning($"部屋を作る：{target.GetType().Name} に {field} がありません"); return; }
        switch(value)
        {
            case bool b:   p.boolValue = b; break;
            case float f:  p.floatValue = f; break;
            case string s: p.stringValue = s; break;
            default:       p.objectReferenceValue = value as Object; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
