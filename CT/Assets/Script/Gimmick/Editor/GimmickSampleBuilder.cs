/*********************************************************************/
// * \file   GimmickSampleBuilder.cs
// * \brief  仕掛けのサンプルの部屋（プレハブ）を作る（Tools > 仕掛け > サンプルの部屋を作る）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 仕掛けのサンプルの部屋を作る
/// </summary>
///
/// ・Assets/Prefab/Gimmicks/Sample/ に、壁つきの小さな部屋をプレハブで作る。ステージの広い床に置いて遊べる
/// ・どれも Hierarchy 右クリック > 仕掛け のメニューと同じ部品を組み合わせただけ（中を開いて、つなぎ方の見本にする）
/// ・作り直すと上書きする（プレハブを直して使うときは、複製してから）
public static class GimmickSampleBuilder
{
    public const string Folder = "Assets/Prefab/Gimmicks/Sample";
    private const string NotePath = "Assets/Resources/Notes/Note_Sample_金庫の番号.asset";
    private const string StatueData = "Assets/Data/Enemies/Sample/EnemyData_Sample_像.asset";
    private const string EnemyBase = "Assets/Resources/Enemies/Enemy_Base.prefab";

    private static readonly Color WallColor = new Color(0.25f, 0.25f, 0.3f, 1.0f);

    [MenuItem("Tools/仕掛け/サンプルの部屋を作る", false, 340)]
    public static void BuildAllMenu()
    {
        List<string> made = BuildAll();
        EditorUtility.DisplayDialog("サンプルの部屋", made.Count + " 個のプレハブを作りました。\n" + Folder + "\n\nステージの広い床にドラッグして置いてください（入口は下、出口は上）。", "OK");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(Folder));
    }

    /// <summary> 全部作る（作ったプレハブのパス） </summary>
    public static List<string> BuildAll()
    {
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        PartyMenu.EnsureHintSettings();
        PuzzleMenu.EnsureSettings();

        var made = new List<string>
        {
            Save(BoxRoom()),
            Save(CandleRoom()),
            Save(CodeRoom()),
            Save(TwoSwitchRoom()),
            Save(PassageRoom()),
            Save(StatueRoom()),
        };
        AssetDatabase.SaveAssets();
        return made;
    }

    private static string Save(GameObject root)
    {
        string path = Folder + "/" + root.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return path;
    }

    //=====================================================================
    // 1. 押せる箱と床スイッチ
    //=====================================================================

    /// 箱を床スイッチまで押すと、上の扉が開く。穴は箱で埋めると渡れる
    private static GameObject BoxRoom()
    {
        GameObject room = Room("Sample_押せる箱と床スイッチ", 11, 9, out Transform parts);
        Memo(room, "箱を右へ2マス押して床スイッチに乗せると、上の扉が開く。\n左の細い通路の箱を上へ2マス押すと穴が埋まり、奥へ渡れる（穴の見本）。");

        GameObject floor = Put(GimmickMenu.BuildFloor(), parts, new Vector2(2.5f, 0.5f));
        Put(GimmickMenu.BuildBlock(), parts, new Vector2(0.5f, 0.5f)).name = "押せる箱";

        GameObject door = Put(GimmickMenu.BuildDoor(new List<GimmickState> { floor.GetComponent<GimmickState>() }, new Vector2(2.0f, 1.0f)), parts, new Vector2(0.0f, 4.0f));
        door.name = "扉（床スイッチが ON で開く）";
        SetBool(door.GetComponent<GimmickReaction>(), "latch", true);

        // 穴と、穴を埋める箱
        Put(GimmickMenu.BuildBlock(), parts, new Vector2(-4.0f, -1.5f)).name = "穴を埋める箱";
        GameObject hole = new GameObject("穴");
        GimmickMenu.Look(hole, GimmickMenu.Square, Vector2.one, new Color(0.08f, 0.08f, 0.1f, 1.0f), 1);
        hole.AddComponent<BoxCollider2D>().size = Vector2.one;
        hole.AddComponent<GimmickState>();
        hole.AddComponent<GimmickHole>();
        Put(hole, parts, new Vector2(-4.0f, 0.5f));
        // 穴の右は壁（左の細い通路は、穴を埋めないと奥へ行けない）
        Wall(room.transform, new Vector2(-3.0f, 1.5f), new Vector2(1.0f, 5.0f), "穴の横の壁");
        return room;
    }

    //=====================================================================
    // 2. 燭台の順番
    //=====================================================================

    /// 3つの燭台を「右 → 左 → 真ん中」の順に灯すと扉が開く。間違えると全部消える
    private static GameObject CandleRoom()
    {
        GameObject room = Room("Sample_燭台の順番", 11, 9, out Transform parts);
        Memo(room, "燭台を 右 → 左 → 真ん中 の順に灯すと扉が開く（答え合わせ：決めた順番）。\n間違えると全部消える。順番のヒントは、メモや一言・絵で出す。");

        GameObject left = Put(GimmickMenu.BuildBrazier(), parts, new Vector2(-3.0f, 1.0f)); left.name = "燭台（左）";
        GameObject mid = Put(GimmickMenu.BuildBrazier(), parts, new Vector2(0.0f, 1.0f)); mid.name = "燭台（真ん中）";
        GameObject right = Put(GimmickMenu.BuildBrazier(), parts, new Vector2(3.0f, 1.0f)); right.name = "燭台（右）";

        var order = new List<GimmickState> { right.GetComponent<GimmickState>(), left.GetComponent<GimmickState>(), mid.GetComponent<GimmickState>() };
        GameObject puzzle = Put(GimmickMenu.BuildPuzzle(order), parts, new Vector2(0.0f, -2.5f));
        puzzle.name = "答え合わせ（右・左・真ん中）";
        SetEnum(puzzle.GetComponent<PuzzleCheck>(), "mode", (int)PuzzleMode.Order);

        GameObject door = Put(GimmickMenu.BuildDoor(new List<GimmickState> { puzzle.GetComponent<GimmickState>() }, new Vector2(2.0f, 1.0f)), parts, new Vector2(0.0f, 4.0f));
        door.name = "扉（答え合わせが正解で開く）";
        return room;
    }

    //=====================================================================
    // 3. 番号錠とメモ
    //=====================================================================

    /// 机のメモを読むと番号がわかる。番号錠に入れると扉が開く
    private static GameObject CodeRoom()
    {
        GameObject room = Room("Sample_番号錠とメモ", 11, 9, out Transform parts);
        Memo(room, "左の机のメモ（Note_Sample_金庫の番号）を読み、右の番号錠に 0314 を入れると扉が開く。\nメモは Tab のメニューの「メモ」で読み返せる。");

        NoteData note = EnsureNote();
        var memo = new GameObject("メモ（机の上）");
        GimmickMenu.Look(memo, GimmickMenu.Square, new Vector2(0.45f, 0.55f), new Color(0.93f, 0.9f, 0.82f, 1.0f), 4);
        var memoArea = memo.AddComponent<CircleCollider2D>();
        memoArea.isTrigger = true;
        memoArea.radius = 0.8f;
        SetRef(memo.AddComponent<NotePickup>(), "note", note);
        Put(memo, parts, new Vector2(-3.0f, 1.0f));

        var code = new GameObject("番号錠");
        GimmickMenu.Look(code, GimmickMenu.Square, new Vector2(0.8f, 0.8f), new Color(0.5f, 0.55f, 0.6f, 1.0f), 4);
        var codeArea = code.AddComponent<CircleCollider2D>();
        codeArea.isTrigger = true;
        codeArea.radius = 1.0f;
        code.AddComponent<GimmickState>();
        var lockComp = code.AddComponent<CodeLock>();
        SetString(lockComp, "answer", "0314");
        SetString(lockComp, "title", "金庫の番号");
        Put(code, parts, new Vector2(3.0f, 1.0f));

        GameObject door = Put(GimmickMenu.BuildDoor(new List<GimmickState> { code.GetComponent<GimmickState>() }, new Vector2(2.0f, 1.0f)), parts, new Vector2(0.0f, 4.0f));
        door.name = "扉（番号錠が正解で開く）";
        return room;
    }

    private static NoteData EnsureNote()
    {
        var note = AssetDatabase.LoadAssetAtPath<NoteData>(NotePath);
        if(note != null) { return note; }

        System.IO.Directory.CreateDirectory(PuzzleMenu.NotesFolder);
        note = ScriptableObject.CreateInstance<NoteData>();
        AssetDatabase.CreateAsset(note, NotePath);
        var so = new SerializedObject(note);
        so.FindProperty("title").stringValue = "走り書き（サンプル）";
        so.FindProperty("body").stringValue = "金庫の番号を忘れないように書いておく。\n\n０３１４\n\n……あの子の誕生日だ。";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(note);
        return note;
    }

    //=====================================================================
    // 4. 同時押し
    //=====================================================================

    /// 離れた2つの床スイッチを同時に押す（F で相棒を片方に待たせ、もう片方へ）
    private static GameObject TwoSwitchRoom()
    {
        GameObject room = Room("Sample_同時押し", 13, 9, out Transform parts);
        Memo(room, "左右の床スイッチを同時に押すと扉が開く。\n片方に乗って F で相棒を待たせ、もう片方へ乗る（相棒は待っている間も床スイッチを押せる）。");

        GameObject a = Put(GimmickMenu.BuildFloor(), parts, new Vector2(-4.5f, 0.5f)); a.name = "床スイッチ（左）";
        GameObject b = Put(GimmickMenu.BuildFloor(), parts, new Vector2(4.5f, 0.5f)); b.name = "床スイッチ（右）";
        SetBool(a.GetComponent<FloorSwitch>(), "byBlock", false);
        SetBool(b.GetComponent<FloorSwitch>(), "byBlock", false);

        GameObject door = Put(GimmickMenu.BuildDoor(new List<GimmickState> { a.GetComponent<GimmickState>(), b.GetComponent<GimmickState>() }, new Vector2(2.0f, 1.0f)), parts, new Vector2(0.0f, 4.0f));
        door.name = "扉（左右が両方 ON で開く。一度開いたらずっと）";
        SetBool(door.GetComponent<GimmickReaction>(), "latch", true);
        return room;
    }

    //=====================================================================
    // 5. 狭い通路とお互いの扉
    //=====================================================================

    /// ノアだけが狭い通路を抜け、向こうのスイッチでシグルのための扉を開ける。
    /// 出口はシグルだけが壊せる壁。出口の先で2人がそろうと合流する
    private static GameObject PassageRoom()
    {
        GameObject room = Room("Sample_狭い通路とお互いの扉", 13, 9, out Transform parts, -3.5f, 3.5f);
        Memo(room, "真ん中の壁：下は狭い通路（ノアだけ）、上は扉。ノアが通路を抜けると相棒は待つ。\n右のスイッチで扉が開き、シグルを F で呼ぶ。出口の壊れる壁はシグルだけが壊せる。\n出口の先の「合流の範囲」に2人で入ると一緒に戻る。");

        // 真ん中の壁（通路と扉の間を残す）
        Wall(room.transform, new Vector2(0.0f, -2.5f), new Vector2(1.0f, 2.0f), "真ん中の壁（下）");
        Wall(room.transform, new Vector2(0.0f, 0.5f), new Vector2(1.0f, 2.0f), "真ん中の壁（中）");
        Wall(room.transform, new Vector2(0.0f, 3.0f), new Vector2(1.0f, 1.0f), "真ん中の壁（上）");

        var passage = new GameObject("狭い通路（ノアだけ）");
        GimmickMenu.Look(passage, GimmickMenu.Square, Vector2.one, new Color(0.3f, 0.3f, 0.45f, 0.6f), 2);
        passage.AddComponent<BoxCollider2D>().size = Vector2.one;
        passage.AddComponent<CharacterBarrier>();
        Put(passage, parts, new Vector2(0.0f, -1.0f));

        GameObject sw = Put(GimmickMenu.BuildSwitch(), parts, new Vector2(4.0f, -1.5f));
        sw.name = "スイッチ（真ん中の扉を開ける）";
        SetEnum(sw.GetComponent<GimmickSwitch>(), "action", (int)GimmickSwitchAction.TurnOn);

        GameObject door = Put(GimmickMenu.BuildDoor(new List<GimmickState> { sw.GetComponent<GimmickState>() }, new Vector2(1.0f, 1.0f)), parts, new Vector2(0.0f, 2.0f));
        door.name = "真ん中の扉（スイッチが ON で開く）";

        // 出口：シグルだけ壊せる壁
        var wall = new GameObject("壊せる壁（シグルだけ）");
        GimmickMenu.Look(wall, GimmickMenu.Square, new Vector2(2.0f, 1.0f), new Color(0.6f, 0.55f, 0.5f, 1.0f), 4);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(2.0f, 1.0f);
        wall.AddComponent<GimmickState>();
        wall.AddComponent<BreakableWall>();
        Put(wall, parts, new Vector2(3.5f, 4.0f));

        // 出口の先の合流の範囲
        var rejoin = new GameObject("合流の範囲（出口の先）");
        var box = rejoin.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.0f, 2.0f);
        var rejoinComp = rejoin.AddComponent<PartyRejoinArea>();
        SetBool(rejoinComp, "once", false);
        Put(rejoin, parts, new Vector2(3.5f, 5.5f));
        return room;
    }

    //=====================================================================
    // 6. 像の部屋
    //=====================================================================

    /// 部屋の入口でやり直す位置を記録。像は見ていない間に近づく。床の赤い罠に触れると入口からやり直し
    private static GameObject StatueRoom()
    {
        GameObject room = Room("Sample_像の部屋", 13, 11, out Transform parts);
        Memo(room, "入口に「やり直す位置」。奥の像（EnemyData_Sample_像）は背を向けている間だけ近づいてくる。\n赤い床は即死の罠（入口からやり直し）。上の扉は開いている（出口）。");

        var retry = new GameObject("やり直す位置（入口）");
        var rbox = retry.AddComponent<BoxCollider2D>();
        rbox.isTrigger = true;
        rbox.size = new Vector2(3.0f, 1.5f);
        var respawn = new GameObject("出る位置");
        respawn.transform.SetParent(retry.transform, false);
        SetRef(retry.AddComponent<RetryPoint>(), "respawn", respawn.transform);
        Put(retry, parts, new Vector2(0.0f, -3.75f));

        foreach(Vector2 p in new[] { new Vector2(-3.0f, -1.0f), new Vector2(2.0f, 0.0f), new Vector2(-1.0f, 2.0f) })
        {
            var trap = new GameObject("即死の罠");
            GimmickMenu.Look(trap, GimmickMenu.Square, new Vector2(1.6f, 1.6f), new Color(0.8f, 0.15f, 0.15f, 0.5f), 1);
            var tbox = trap.AddComponent<BoxCollider2D>();
            tbox.isTrigger = true;
            tbox.size = new Vector2(1.6f, 1.6f);
            trap.AddComponent<DeathTrap>();
            Put(trap, parts, p);
        }

        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBase);
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(StatueData);
        if(basePrefab != null && data != null)
        {
            var statue = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            statue.name = "像（見ていない間に近づく）";
            SetRef(statue.GetComponent<Enemy>(), "data", data);
            Put(statue, parts, new Vector2(4.0f, 3.0f));
        }
        else
        {
            Debug.LogWarning("[サンプルの部屋] 像のデータか Enemy_Base がないので、像を置きませんでした（Tools > 敵 > 共通プレハブとサンプルを作る）");
        }

        return room;
    }

    //=====================================================================
    // 部屋の共通
    //=====================================================================

    /// <summary>
    /// 壁に囲まれた部屋（幅・高さはマス。下の真ん中に入口、上の真ん中に出口〔2 マス〕をあける）
    /// </summary>
    private static GameObject Room(string name, int width, int height, out Transform parts, float entranceX = 0.0f, float exitX = 0.0f)
    {
        var root = new GameObject(name);
        var walls = new GameObject("壁");
        walls.transform.SetParent(root.transform, false);
        var p = new GameObject("仕掛け");
        p.transform.SetParent(root.transform, false);
        parts = p.transform;

        float w = width, h = height;
        GapWall(walls.transform, -h * 0.5f + 0.5f, w, entranceX, "下の壁");
        GapWall(walls.transform, h * 0.5f - 0.5f, w, exitX, "上の壁");
        Wall(walls.transform, new Vector2(-w * 0.5f + 0.5f, 0.0f), new Vector2(1.0f, h - 2.0f), "左の壁");
        Wall(walls.transform, new Vector2(w * 0.5f - 0.5f, 0.0f), new Vector2(1.0f, h - 2.0f), "右の壁");

        // 床（見た目だけ。ステージの床の上に置く）
        var floor = new GameObject("床（見た目）");
        floor.transform.SetParent(root.transform, false);
        GimmickMenu.Look(floor, GimmickMenu.Square, new Vector2(w - 2.0f, h - 2.0f), new Color(0.0f, 0.0f, 0.0f, 0.25f), 0);
        return root;
    }

    /// <summary> 横の壁（gapX を中心に 2 マスあける） </summary>
    private static void GapWall(Transform parent, float y, float w, float gapX, string name)
    {
        float left = -w * 0.5f, right = w * 0.5f;
        float a = gapX - 1.0f, b = gapX + 1.0f;
        if(a - left > 0.01f) { Wall(parent, new Vector2((left + a) * 0.5f, y), new Vector2(a - left, 1.0f), name + "（左）"); }
        if(right - b > 0.01f) { Wall(parent, new Vector2((b + right) * 0.5f, y), new Vector2(right - b, 1.0f), name + "（右）"); }
    }

    private static void Wall(Transform parent, Vector2 center, Vector2 size, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        GimmickMenu.Look(go, GimmickMenu.Square, size, WallColor, 2);
        go.AddComponent<BoxCollider2D>().size = size;
    }

    private static GameObject Put(GameObject go, Transform parent, Vector2 local)
    {
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        return go;
    }

    /// <summary> 部屋の説明（Inspector の「メモ」で読む。ゲームには出ない） </summary>
    private static void Memo(GameObject room, string text)
    {
        var memo = room.AddComponent<GimmickSampleMemo>();
        memo.text = text;
    }

    //=====================================================================
    // 値を入れる
    //=====================================================================

    private static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p == null) { Debug.LogWarning($"[サンプルの部屋] {target.GetType().Name}.{field} がない"); return; }
        p.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p == null) { Debug.LogWarning($"[サンプルの部屋] {target.GetType().Name}.{field} がない"); return; }
        p.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p == null) { Debug.LogWarning($"[サンプルの部屋] {target.GetType().Name}.{field} がない"); return; }
        p.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p == null) { Debug.LogWarning($"[サンプルの部屋] {target.GetType().Name}.{field} がない"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
