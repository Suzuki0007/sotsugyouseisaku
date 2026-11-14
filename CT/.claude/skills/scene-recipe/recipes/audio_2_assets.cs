// 音の素材（2026-11-14）：データ・プレハブの音の欄に入れる（何度実行しても同じ）
// 前提：audio_1_sedata を実行済み（SeData がある）。シーンの分は audio_3_scenes
var sb = new System.Text.StringBuilder();
System.Func<string, SeData> Se = n =>
{
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:SeData"))
    {
        var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == n) return UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(p);
    }
    sb.Append("!! no SeData " + n + "\n"); return null;
};
System.Action<UnityEngine.Object, string, UnityEngine.Object> Set = (target, field, value) =>
{
    if (target == null) { sb.Append("!! no target for " + field + "\n"); return; }
    var so = new UnityEditor.SerializedObject(target);
    var p = so.FindProperty(field);
    if (p == null) { sb.Append("!! " + target.name + " has no " + field + "\n"); return; }
    p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(target);
    sb.Append(target.name + "." + field + " = " + (value != null ? value.name : "null") + "\n");
};

// ---- プレイヤー ----
{
    string path = "Assets/Prefab/Players/Player_Noah.prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    var foot = root.GetComponent<FootstepSe>();
    { var so = new UnityEditor.SerializedObject(foot); so.FindProperty("playMode").enumValueIndex = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
    Set(foot, "footstepSe", Se("Se_Footstep_Noah"));
    Set(foot, "followerFootstepSe", Se("Se_Footstep_Noah_Follow"));
    var hs = root.GetComponent<HealthSe>(); if (hs == null) hs = root.AddComponent<HealthSe>();
    Set(hs, "damageSe", Se("Se_damage_noah"));
    var guard = root.GetComponent<NoahBulletDeleteAreaController>();
    Set(guard, "areaStartSe", Se("Se_guard_start"));
    Set(guard, "areaLoopSe", Se("Se_guard_loop"));
    Set(guard, "areaEndSe", Se("Se_guard_end"));
    Set(root.GetComponent<AreaAttackController>(), "attackSe", Se("Se_noah_attack_area"));
    var dead = root.GetComponent<NoahDead>();
    Set(dead, "gameOverLeaveSe", Se("Se_fall_glass"));
    Set(dead, "gameOverArriveSe", Se("Se_gameover_arrive"));
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
{
    string path = "Assets/Prefab/Players/Player_Sigle.prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    var foot = root.GetComponent<FootstepSe>();
    { var so = new UnityEditor.SerializedObject(foot); so.FindProperty("playMode").enumValueIndex = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
    Set(foot, "footstepSe", Se("Se_Footstep_Sigle"));
    Set(foot, "followerFootstepSe", Se("Se_Footstep_Sigle_Follow"));
    var hs = root.GetComponent<HealthSe>(); if (hs == null) hs = root.AddComponent<HealthSe>();
    Set(hs, "damageSe", Se("Se_damage_sigul"));
    Set(root.GetComponent<SigleShooter>(), "shotSe", Se("Se_bang_sigul_attack"));
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}

// ---- ペグ・すぐ効くアイテム ----
{
    string path = "Assets/Prefab/Gimmicks/Timpani/Peg.prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    var peg = root.GetComponent<TimpaniPeg>();
    Set(peg, "startSe", Se("Se_peg_start"));
    Set(peg, "successSe", Se("Se_peg_success_1"));
    Set(peg, "missSe", Se("Se_peg_miss"));
    Set(peg, "tightenedSe", Se("Se_peg_tightened"));
    var so = new UnityEditor.SerializedObject(peg);
    var steps = so.FindProperty("successSeSteps"); steps.arraySize = 3;
    for (int i = 0; i < 3; i++) steps.GetArrayElementAtIndex(i).objectReferenceValue = Se("Se_peg_success_" + (i + 1));
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
foreach (var (file, se) in new[] { ("Item_Heal", "Se_pickup_heal"), ("Item_GuardBoost", "Se_pickup_guard") })
{
    string path = $"Assets/Prefab/Gimmicks/Timpani/{file}.prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    Set(root.GetComponent<InstantPickup>(), "pickupSe", Se(se));
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}

// ---- 謎解きの見本の部屋（新しく作る仕掛けの見本にもなる） ----
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Gimmicks/Sample" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    foreach (var c in root.GetComponentsInChildren<GimmickSwitch>(true)) Set(c, "useSe", Se(c.name.Contains("燭台") ? "Se_candle_light" : "Se_switch"));
    foreach (var c in root.GetComponentsInChildren<FloorSwitch>(true)) Set(c, "pressSe", Se("Se_floor_switch"));
    foreach (var c in root.GetComponentsInChildren<PushBlock>(true)) Set(c, "pushSe", Se("Se_push_block"));
    foreach (var c in root.GetComponentsInChildren<GimmickReaction>(true)) Set(c, "moveSe", Se("Se_door_open"));
    foreach (var c in root.GetComponentsInChildren<BreakableWall>(true)) Set(c, "breakSe", Se("Se_wall_break"));
    foreach (var c in root.GetComponentsInChildren<CodeLock>(true)) { Set(c, "correctSe", Se("Se_codelock_correct")); Set(c, "wrongSe", Se("Se_codelock_wrong")); }
    foreach (var c in root.GetComponentsInChildren<PuzzleCheck>(true)) { Set(c, "solvedSe", Se("Se_codelock_correct")); Set(c, "wrongSe", Se("Se_codelock_wrong")); }
    foreach (var c in root.GetComponentsInChildren<NotePickup>(true)) Set(c, "pickSe", Se("Se_page_turn"));
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}

// ---- 謎解きの画面（番号を回す・ページをめくる） ----
{
    var s = UnityEngine.Resources.Load<PuzzleUISettings>("PuzzleUI");
    Set(s, "dialSe", Se("Se_codelock_tick"));
    Set(s, "pageSe", Se("Se_page_turn"));
}

// ---- 音楽生物の攻撃 ----
foreach (var (file, tele, act) in new[] {
    ("Attack_Timpani_Ring", "Se_warning_roll", "Se_ring_hit"),
    ("Attack_Timpani_Ring_Hard", "Se_warning_roll", "Se_ring_hit"),
    ("Attack_Timpani_Spiral", "Se_warning_roll", "Se_spiral_hits"),
    ("Attack_Timpani_Rock", "Se_rock_warning", "Se_rock_land") })
{
    var a = UnityEditor.AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/Data/Enemies/Timpani/{file}.asset");
    Set(a, "telegraphSe", Se(tele));
    Set(a, "activeSe", Se(act));
}

// ---- 敵のデータ（音楽生物は攻撃が効かない・おとなしくなる音は TimpaniUnit なので入れない） ----
{
    var chaser = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/EnemyData_Stage1_2_Chaser.asset");
    Set(chaser, "staggerSe", Se("Se_stagger"));
    Set(chaser, "deathSe", Se("Se_death"));
    Set(chaser, "moveLoopSe", Se("Se_monster_steps_loop"));
    foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/Data/Enemies/Sample" }))
    {
        var d = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
        Set(d, "staggerSe", Se("Se_stagger"));
        Set(d, "deathSe", Se("Se_death"));
    }
}

// ---- 会話：文字送り（ノア＝全体の音 A、シグル＝B） ----
{
    var sigle = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfile>("Assets/Data/Characters/Sigle.asset");
    Set(sigle, "typeSe", Se("Se_text_blip_B"));
    string path = "Assets/Prefab/UI/DialogueSystem.prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    Set(root.GetComponentInChildren<DialogueManager>(true), "typeSe", Se("Se_text_blip_A"));
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}

UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
