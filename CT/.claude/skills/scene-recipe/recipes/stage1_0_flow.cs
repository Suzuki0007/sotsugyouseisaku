// Stage1_0（砂浜）を最初のマップにする（2026-11-19 ユーザーの指示「OP 後、Stage1_0 の船の上から始まり、奥まで進むと Stage1_1 へ」）
// ・Stage1_0：OP_はじめ を有効（OP の途中・塔の入口は Stage1_1 の物を使うのでオフのまま）、船の上に出現位置 Start_Boat、
//             奥（マップの上の端）に Stage1_1 への出口（ぶつかると移る）、コピー元の塔の扉（Stage1_2 行き）はオフ
// ・Stage1_1：下の端に出現位置 FromStage1_0
// ・Title：はじめから＝Stage1_0 の Start_Boat
// ・Stage_1_3 → Stage_1_Boss の名前の変更に合わせる：Stage1_2 の正面の扉の行き先、場所の名前の表
// 何度実行しても同じ（名前で見つけて作り直さない）。各シーンを保存する
var sb = new System.Text.StringBuilder();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! unsaved scene: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

System.Func<UnityEngine.SceneManagement.Scene, string, UnityEngine.GameObject> Find = (scene, path) =>
{
    string[] parts = path.Split('/');
    foreach (var r in scene.GetRootGameObjects())
    {
        if (r.name != parts[0]) continue;
        var t = r.transform;
        for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
        if (t != null) return t.gameObject;
    }
    return null;
};
System.Func<UnityEngine.SceneManagement.Scene, string, UnityEngine.Transform, string, UnityEngine.Vector3, UnityEngine.GameObject> Spawn = (scene, name, parent, id, pos) =>
{
    UnityEngine.GameObject go = null;
    foreach (var r in scene.GetRootGameObjects()) foreach (var sp in r.GetComponentsInChildren<PlayerSpawnPoint>(true)) if (sp.SpawnPointId == id) go = sp.gameObject;
    if (go == null)
    {
        go = new UnityEngine.GameObject(name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        if (parent != null) go.transform.SetParent(parent, true);
        go.AddComponent<PlayerSpawnPoint>();
        sb.Append("  + spawn " + id + "\n");
    }
    go.transform.position = pos;
    var so = new UnityEditor.SerializedObject(go.GetComponent<PlayerSpawnPoint>());
    so.FindProperty("spawnPointId").stringValue = id;
    so.ApplyModifiedPropertiesWithoutUndo();
    return go;
};
System.Action<UnityEngine.SceneManagement.Scene> Save = scene =>
{
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    sb.Append("saved " + scene.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene) + "\n");
};

// ---- Stage1_0 ----
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_0.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    sb.Append("== Stage1_0\n");
    var op = Find(s, "OP/OP_はじめ（暗転からマップへ）");
    if (op != null) { op.SetActive(true); sb.Append("  OP_はじめ on\n"); } else sb.Append("  !! no OP_はじめ\n");

    // 船の上（船の絵の真ん中）
    var boat = Find(s, "sunahama_hune_0");
    var bpos = boat != null ? boat.GetComponent<UnityEngine.SpriteRenderer>().bounds.center : new UnityEngine.Vector3(-115.6f, 18.0f, 0);
    var spRoot = Find(s, "PlayerSpawnPoint");
    Spawn(s, "出現位置_船の上（はじめから）", spRoot != null ? spRoot.transform.parent : null, "Start_Boat", new UnityEngine.Vector3(bpos.x, bpos.y, 0));

    // 奥の出口：マップの上の端の横いっぱい（ぶつかると Stage1_1 の下の端へ）
    var map = Find(s, "Grid/soto_yuka").GetComponent<UnityEngine.SpriteRenderer>().bounds;
    var exit = Find(s, "MapEntity/出口_Stage1_1へ（奥）");
    if (exit == null)
    {
        exit = new UnityEngine.GameObject("出口_Stage1_1へ（奥）");
        exit.transform.SetParent(Find(s, "MapEntity").transform, false);
        exit.AddComponent<UnityEngine.BoxCollider2D>();
        exit.AddComponent<SceneChange>();
        sb.Append("  + exit\n");
    }
    exit.transform.position = new UnityEngine.Vector3(map.center.x, map.max.y - 0.35f, 0);
    var box = exit.GetComponent<UnityEngine.BoxCollider2D>(); box.isTrigger = false; box.size = new UnityEngine.Vector2(map.size.x, 0.5f);
    var sco = new UnityEditor.SerializedObject(exit.GetComponent<SceneChange>());
    sco.FindProperty("sceneName").stringValue = "Stage1_1";
    sco.FindProperty("spawnPointId").stringValue = "FromStage1_0";
    sco.FindProperty("leaveSe").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>("Assets/SE/SE-26_SaveAndScene/Se_scene_leave.asset");
    sco.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  exit at y=" + exit.transform.position.y.ToString("0.00") + " width=" + map.size.x.ToString("0.0") + "\n");

    // コピー元の塔の扉（Stage1_2 行き）はオフ
    var door = Find(s, "MapEntity/OB_Door");
    if (door != null && door.activeSelf) { door.SetActive(false); sb.Append("  OB_Door (to Stage1_2) off\n"); }
    Save(s);
}

// ---- Stage1_1：下の端の入口 ----
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_1.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    sb.Append("== Stage1_1\n");
    var mapGo = default(UnityEngine.GameObject);
    foreach (var r in s.GetRootGameObjects()) foreach (var mb in r.GetComponentsInChildren<MapBounds>(true)) mapGo = mb.gameObject;
    var map = mapGo.GetComponent<UnityEngine.SpriteRenderer>().bounds;
    var spRoot = Find(s, "PlayerSpawnPoint");
    Spawn(s, "出現位置_Stage1_0から（下）", spRoot != null ? spRoot.transform.parent : null, "FromStage1_0", new UnityEngine.Vector3(map.center.x, map.min.y + 1.0f, 0));
    Save(s);
}

// ---- Stage1_2：正面の扉の行き先（Stage_1_3 → Stage_1_Boss） ----
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_2.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    sb.Append("== Stage1_2\n");
    foreach (var r in s.GetRootGameObjects())
    foreach (var a in r.GetComponentsInChildren<SceneChangeEventAction>(true))
    {
        var so = new UnityEditor.SerializedObject(a);
        var p = so.FindProperty("sceneName");
        if (p.stringValue == "Stage_1_3") { p.stringValue = "Stage_1_Boss"; so.ApplyModifiedPropertiesWithoutUndo(); sb.Append("  door → Stage_1_Boss (" + a.gameObject.name + ")\n"); }
    }
    Save(s);
}

// ---- Title：はじめから ----
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    sb.Append("== Title\n");
    foreach (var r in s.GetRootGameObjects())
    foreach (var tm in r.GetComponentsInChildren<TitleMenu>(true))
    {
        var so = new UnityEditor.SerializedObject(tm);
        so.FindProperty("firstScene").stringValue = "Stage1_0";
        so.FindProperty("firstSpawnPointId").stringValue = "Start_Boat";
        so.ApplyModifiedPropertiesWithoutUndo();
        sb.Append("  firstScene = Stage1_0 / Start_Boat\n");
    }
    Save(s);
}

// ---- 場所の名前の表（セーブ画面） ----
{
    var path = "Assets/Resources/PlaceNames.asset";
    var text = System.IO.File.ReadAllText(path);
    bool changed = false;
    if (text.Contains("scene: Stage_1_3")) { text = text.Replace("scene: Stage_1_3", "scene: Stage_1_Boss"); changed = true; sb.Append("PlaceNames: Stage_1_3 → Stage_1_Boss\n"); }
    if (!text.Contains("scene: Stage1_0"))
    {
        // 一番上に足す（表示名は「ステージ1-0」。正式な地名は表で直す）
        text = text.Replace("  entries:\n", "  entries:\n  - scene: Stage1_0\n    displayName: \"\\u30B9\\u30C6\\u30FC\\u30B81-0\"\n");
        changed = true; sb.Append("PlaceNames: + Stage1_0\n");
    }
    if (changed) { System.IO.File.WriteAllText(path, text); UnityEditor.AssetDatabase.ImportAsset(path); }
}

if (!string.IsNullOrEmpty(start)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
