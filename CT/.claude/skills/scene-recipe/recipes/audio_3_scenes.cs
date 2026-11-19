// 音の素材（2026-11-14）：シーンの音の欄・BGM（何度実行しても同じ。足す部品は名前で見つけて重ねない）
// 対象：Title・Stage1_1・Stage1_2・Stage_1_Boss・SceneGameOver を順に開いて入れ、保存する（最後に元のシーンを開き直す）
// 前提：audio_1_sedata・audio_2_assets を実行済み
var sb = new System.Text.StringBuilder();
var startPath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! a scene has unsaved changes: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;

System.Func<string, SeData> Se = n =>
{
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:SeData"))
    {
        var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == n) return UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(p);
    }
    sb.Append("!! no SeData " + n + "\n"); return null;
};
System.Func<string, UnityEngine.AudioClip> Bgm = n =>
{
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:AudioClip", new[] { "Assets/BGM" }))
        return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    sb.Append("!! no BGM " + n + "\n"); return null;
};
System.Action<UnityEngine.Object, string, UnityEngine.Object> Set = (target, field, value) =>
{
    if (target == null) { sb.Append("!! no target for " + field + "\n"); return; }
    var so = new UnityEditor.SerializedObject(target);
    var p = so.FindProperty(field);
    if (p == null) { sb.Append("!! " + target.name + " has no " + field + "\n"); return; }
    p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  " + target.name + "." + field + " = " + (value != null ? value.name : "null") + "\n");
};
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
    sb.Append("!! not found " + path + "\n"); return null;
};
// EventSequence の n 番目（1 から）の部品
System.Func<UnityEngine.GameObject, int, EventAction> ActionAt = (go, n) =>
{
    if (go == null) return null;
    var acts = go.GetComponent<EventSequence>().GetActions();
    return n - 1 < acts.Count ? acts[n - 1] : null;
};
// SeEventAction を流れの index 番目（0 から）に足す。同じ音の SeEventAction がもうあれば何もしない
System.Action<UnityEngine.GameObject, int, SeData> InsertSe = (go, index, se) =>
{
    if (go == null || se == null) return;
    var seq = go.GetComponent<EventSequence>();
    foreach (var a in seq.GetActions())
        if (a is SeEventAction s && new UnityEditor.SerializedObject(s).FindProperty("seOnEnter").objectReferenceValue == se) { sb.Append("  (already) " + se.name + " in " + go.name + "\n"); return; }
    var add = go.AddComponent<SeEventAction>();
    var aso = new UnityEditor.SerializedObject(add);
    aso.FindProperty("seOnEnter").objectReferenceValue = se;
    aso.FindProperty("playAtThisPosition").boolValue = false;
    aso.ApplyModifiedPropertiesWithoutUndo();
    var so = new UnityEditor.SerializedObject(seq);
    var list = so.FindProperty("actions");
    if (list.arraySize == 0)
    {
        // 部品の並び（同じオブジェクトの上から）を、明示の一覧に写してから足す
        var own = seq.GetComponents<EventAction>();
        foreach (var a in own) { if (a == add) continue; list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = a; }
    }
    list.InsertArrayElementAtIndex(System.Math.Min(index, list.arraySize));
    list.GetArrayElementAtIndex(System.Math.Min(index, list.arraySize - 1)).objectReferenceValue = add;
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  + " + se.name + " at " + (index + 1) + " in " + go.name + "\n");
};
// BGM：基本の曲と、フラグで切り替える曲（条件の部品は SceneBgm の子「条件_<名前>」）
System.Action<SceneBgm, UnityEngine.AudioClip, float> SetDefaultBgm = (bgm, clip, vol) =>
{
    var so = new UnityEditor.SerializedObject(bgm);
    so.FindProperty("defaultClip").objectReferenceValue = clip;
    so.FindProperty("defaultVolume").floatValue = vol;
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  BGM default = " + (clip != null ? clip.name : "null") + " vol " + vol + "\n");
};
System.Action<SceneBgm, string, GameFlag, UnityEngine.AudioClip, float> AddFlagRule = (bgm, label, flag, clip, vol) =>
{
    var holderT = bgm.transform.Find("条件_" + label);
    var holder = holderT != null ? holderT.gameObject : new UnityEngine.GameObject("条件_" + label);
    holder.transform.SetParent(bgm.transform, false);
    var cond = holder.GetComponent<BgmConditionFlag>(); if (cond == null) cond = holder.AddComponent<BgmConditionFlag>();
    Set(cond, "flag", flag);
    var so = new UnityEditor.SerializedObject(bgm);
    var rules = so.FindProperty("rules");
    UnityEditor.SerializedProperty r = null;
    for (int i = 0; i < rules.arraySize; i++) if (rules.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue == label) r = rules.GetArrayElementAtIndex(i);
    if (r == null) { rules.arraySize++; r = rules.GetArrayElementAtIndex(rules.arraySize - 1); }
    r.FindPropertyRelative("label").stringValue = label;
    r.FindPropertyRelative("condition").objectReferenceValue = cond;
    r.FindPropertyRelative("clip").objectReferenceValue = clip;
    r.FindPropertyRelative("volume").floatValue = vol;
    r.FindPropertyRelative("priority").intValue = 10;
    r.FindPropertyRelative("fadeTime").floatValue = 0.8f;
    r.FindPropertyRelative("latch").boolValue = false;
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  BGM rule " + label + " = " + (clip != null ? clip.name : "null") + " when " + (flag != null ? flag.name : "null") + "\n");
};
System.Func<string, string, GameFlag> FlagAsset = (file, memo) =>
{
    string path = "Assets/Data/Flags/" + file + ".asset";
    var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(path);
    if (f != null) return f;
    f = UnityEngine.ScriptableObject.CreateInstance<GameFlag>();
    UnityEditor.AssetDatabase.CreateAsset(f, path);
    var so = new UnityEditor.SerializedObject(f);
    so.FindProperty("id").stringValue = file.Substring("Flag_".Length);
    so.FindProperty("memo").stringValue = memo;
    so.FindProperty("saved").boolValue = false;
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("  new flag " + file + "\n");
    return f;
};
System.Action<UnityEngine.SceneManagement.Scene> PartySe = scene =>
{
    foreach (var m in UnityEngine.Object.FindObjectsByType<PlayerSwitchManager>(UnityEngine.FindObjectsInactive.Include))
    {
        if (m.gameObject.scene != scene) continue;
        Set(m, "switchSe", Se("Se_party_switch")); Set(m, "waitSe", Se("Se_party_wait"));
        Set(m, "followSe", Se("Se_party_follow")); Set(m, "cannotSe", Se("Se_party_cannot"));
    }
};
System.Action<UnityEngine.SceneManagement.Scene> ExitSe = scene =>
{
    var go = Find(scene, "OtherSystem/SceneChange");
    if (go != null) Set(go.GetComponent<SceneChange>(), "leaveSe", Se("Se_scene_leave"));
};
System.Action<UnityEngine.SceneManagement.Scene> KeySe = scene =>
{
    foreach (var root in scene.GetRootGameObjects())
    {
        foreach (var k in root.GetComponentsInChildren<KeyItem>(true)) Set(k, "pickupSe", Se("Se_item_pickup"));
        foreach (var p in root.GetComponentsInChildren<ItemPickup>(true)) Set(p, "pickupSe", Se("Se_item_pickup"));
    }
};
System.Action<UnityEngine.SceneManagement.Scene> Save = scene =>
{
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    sb.Append("saved " + scene.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene) + "\n");
};
System.Func<string, UnityEngine.SceneManagement.Scene> Open = path =>
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    sb.Append("== " + s.name + "\n");
    return s;
};

// ---- Title ----
{
    var s = Open("Assets/Scenes/Title.unity");
    SetDefaultBgm(Find(s, "System").GetComponent<SceneBgm>(), Bgm("BGM-01_title"), 0.8f);
    Save(s);
}
// ---- Stage1_1：OP の鐘・2人の操作・鍵・出口 ----
{
    var s = Open("Assets/Scenes/Stage1_1.unity");
    InsertSe(Find(s, "OP/OP_途中（鐘の言い伝え）"), 0, Se("Se_bell_clocktower"));
    PartySe(s); KeySe(s); ExitSe(s);
    Save(s);
}
// ---- Stage1_2：着いたとき・ランタン・追跡・助け・扉・BGM ----
{
    var s = Open("Assets/Scenes/Stage1_2.unity");
    var arrive = Find(s, "Stage1_2_イベント/着いたとき（頭痛 → 暗い → ランタンを取ろう）");
    Set(ActionAt(arrive, 2), "seOnEnter", Se("Se_kiiin"));

    var lantern = Find(s, "Stage1_2_イベント/ランタン（調べると拾う）");
    Set(ActionAt(lantern, 2), "se", Se("Se_lantern_pickup"));          // アイテムを渡す
    Set(ActionAt(lantern, 5), "seOnEnter", Se("Se_rumble_short"));      // F1_02d また音と揺れ
    Set(ActionAt(lantern, 16), "seOnEnter", Se("Se_dosun_monster_fall")); // 化け物が降ってきた
    InsertSe(lantern, 18, Se("Se_monster_roar"));                       // 揺れ・待つのあと、F1_03 の前にうなり声

    // 2026-11-19：助けの場面は合流しない形（シグルが扉Aを壊して書斎へ）に変わった。8 番目＝敵を倒すバン、22 番目＝扉A を壊す音
    var rescue = Find(s, "Stage1_2_イベント/追跡（戦闘エリア）/勝ったとき（追い詰められる → 助けられる → シグルが扉Aを壊して先へ）");
    Set(ActionAt(rescue, 8), "seOnEnter", Se("Se_bang_rescue_scene"));
    Set(ActionAt(rescue, 22), "seOnEnter", Se("Se_door_force_open"));

    var door = Find(s, "Stage1_2_イベント/正面の扉（合流後に調べると Stage_1_Boss へ）");
    Set(ActionAt(door, 3), "seOnEnter", Se("Se_door_force_open"));

    // 追跡中だけ追跡の曲（戦闘エリアの戦闘中フラグ）
    var chaseFlag = FlagAsset("Flag_1_2_Chase", "Stage1_2 の追跡（戦闘エリア）の最中 ON（戦闘エリアの戦闘中フラグ）。追跡の BGM に使う。セーブしない");
    var chase = Find(s, "Stage1_2_イベント/追跡（戦闘エリア）");
    if (chase != null) Set(chase.GetComponent<BattleArea>(), "battleFlag", chaseFlag);
    var bgm = Find(s, "BGMset").GetComponent<SceneBgm>();
    SetDefaultBgm(bgm, Bgm("BGM-03_tower1_explore_loop"), 0.7f);
    AddFlagRule(bgm, "追跡", chaseFlag, Bgm("BGM-04_chase_full"), 0.8f);

    var dm = Find(s, "DialogueManajer");
    if (dm != null) Set(dm.GetComponentInChildren<DialogueManager>(true), "typeSe", Se("Se_text_blip_A"));
    PartySe(s); KeySe(s); ExitSe(s);
    Save(s);
}
// ---- Stage_1_Boss：着いたとき・守りのゲージ・調律・BGM ----
{
    var s = Open("Assets/Scenes/Stage_1_Boss.unity");
    Set(ActionAt(Find(s, "ティンパニ部屋/着いたとき（戦闘前の会話）"), 3), "seOnEnter", Se("Se_rumble_long"));
    var gauge = Find(s, "ティンパニ部屋/守りのゲージ");
    if (gauge != null) { Set(gauge.GetComponent<GuardGauge>(), "emptySe", Se("Se_guard_empty")); Set(gauge.GetComponent<GuardGauge>(), "readySe", Se("Se_guard_ready")); }
    foreach (var u in UnityEngine.Object.FindObjectsByType<TimpaniUnit>(UnityEngine.FindObjectsInactive.Include)) Set(u, "calmSe", Se("Se_calm"));

    var inBattle = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Timpani_InBattle.asset");
    var bgm = Find(s, "BGMset").GetComponent<SceneBgm>();
    SetDefaultBgm(bgm, Bgm("BGM-03_tower1_explore_loop"), 0.7f);
    AddFlagRule(bgm, "ティンパニ戦", inBattle, Bgm("BGM-02_timpani_battle_loop"), 0.8f);
    PartySe(s); KeySe(s); ExitSe(s);
    Save(s);
}
// ---- SceneGameOver：BGM・出るときの音 ----
{
    var s = Open("Assets/Scenes/SceneGameOver.unity");
    SceneBgm bgm = null;
    foreach (var r in s.GetRootGameObjects()) { bgm = r.GetComponentInChildren<SceneBgm>(true); if (bgm != null) break; }
    if (bgm == null) { var go = new UnityEngine.GameObject("BGMset"); bgm = go.AddComponent<SceneBgm>(); sb.Append("  + BGMset\n"); }
    SetDefaultBgm(bgm, Bgm("BGM-05_gameover_loop"), 0.7f);
    { var so = new UnityEditor.SerializedObject(bgm); so.FindProperty("startFadeInTime").floatValue = 2.5f; so.FindProperty("previousFadeOutTime").floatValue = 0.3f; so.ApplyModifiedPropertiesWithoutUndo(); }
    var scene = Find(s, "Scene");
    if (scene != null) Set(scene.GetComponent<SceneChange>(), "leaveSe", Se("Se_scene_leave"));
    Save(s);
}

UnityEditor.AssetDatabase.SaveAssets();
if (!string.IsNullOrEmpty(startPath)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(startPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
