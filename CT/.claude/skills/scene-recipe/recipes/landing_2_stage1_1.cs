// 上陸 Stage1_1（砂漠）：着いたら塔の全景スチル → OP_02h → オートセーブ、塔の手前で扉の爪痕 → 最大の振動 → 背後から怪物 → 塔の入口まで逃げる（戦闘エリア）→ Stage1_2（ルート「上陸（Stage1_1）」・演出の Canvas を消して作る。F1_01 の範囲はオフ。先に landing_0_assets）
// @helpers
var scene = OpenSceneSafe("Assets/Scenes/Stage1_1.unity");
System.Func<string, UnityEngine.GameObject> Find = path => { var parts = path.Split('/'); foreach (var r in scene.GetRootGameObjects()) { if (r.name != parts[0]) continue; var t = r.transform; for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]); if (t != null) return t.gameObject; } return null; };
System.Func<string, SeData> Se = n => { foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:SeData")) { var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g); if (System.IO.Path.GetFileNameWithoutExtension(p) == n) return UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(p); } throw new System.Exception("no se " + n); };
System.Func<string, DialogueData> L = id => Dlg("Scenario_Landing", id);

// ---- 仮で決めた値（Inspector でも変えられる） ----
float shakeL = 0.32f, shakeLTime = 3.0f;                          // 振動（大）
var markY = 23.3f;                                                // 扉の爪痕に気づく帯（塔の階段の手前。鐘の言い伝えの帯 y 21.5 の少し先）
var enemyHome = new UnityEngine.Vector3(-115.2f, 18.4f, 0f);      // 怪物が止まる所（ノアの背後＝マップの手前。画面の下の端に見える）
var enemyFrom = new UnityEngine.Vector2(0f, -4.0f);               // そこへ下から出てくる
var restartAt = new UnityEngine.Vector3(-115.2f, 22.4f, 0f);      // 捕まったらここから（爪痕の帯のすぐ下）
var goalPos = new UnityEngine.Vector3(-115.55f, 27.6f, 0f);       // ゴール＝塔の階段の上（扉の手前）

{ var old = Find("上陸（Stage1_1）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
{ var old = Find("演出の画像（手紙・スチル）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
var root = NewGO(null, "上陸（Stage1_1）", new UnityEngine.Vector3(-115.2f, 20f, 0f)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

// 今の台本の塔の入口の会話（F1_01）は、扉の爪痕（OP_02i）に置き換えるのでオフ（消さない）
{ var f = Find("OP/OP_塔の入口（F1_01）"); if (f != null && f.activeSelf) { f.SetActive(false); Log.Append("F1_01 area off\n"); } }

// ---- 小道具 ----
System.Action<UnityEngine.GameObject, SeData> AddSe = (go, se) => { var a = go.AddComponent<SeEventAction>(); SetProp(a, "seOnEnter", se); SetProp(a, "playAtThisPosition", false); };
System.Action<UnityEngine.GameObject, float, float, bool> AddShake = (go, st, du, w) => { var a = go.AddComponent<ShakeEventAction>(); SetProp(a, "strength", st); SetProp(a, "duration", du); SetProp(a, "waitUntilDone", w); };
System.Action<UnityEngine.GameObject, GameFlag[]> AddFlags = (go, fs) => { var a = go.AddComponent<SetFlagEventAction>(); foreach (var f in fs) a.AddFlagForEditor(f, true); };
System.Action<UnityEngine.GameObject, UnityEngine.Object[], UnityEngine.Object[]> AddShowHide = (go, show, hide) => { var a = go.AddComponent<SetActiveEventAction>(); SetList(a, "show", show); SetList(a, "hide", hide); };
System.Action<UnityEngine.GameObject, int, float> AddFade = (go, type, dur) => { var a = go.AddComponent<ScreenFadeEventAction>(); SetProp(a, "type", type); SetProp(a, "duration", dur); };

// ---- スチルの Canvas（会話の枠より奥・暗転の幕より手前） ----
var fx = NewGO(null, "演出の画像（手紙・スチル）", UnityEngine.Vector3.zero); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fx, scene);
{ var c = fx.AddComponent<UnityEngine.Canvas>(); c.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; c.sortingOrder = 95;
  var cs = fx.AddComponent<UnityEngine.UI.CanvasScaler>(); cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new UnityEngine.Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f; }
var still = new UnityEngine.GameObject("塔の全景スチル（仮画像）", typeof(UnityEngine.RectTransform)); still.transform.SetParent(fx.transform, false);
{ var rt = (UnityEngine.RectTransform)still.transform; rt.anchorMin = UnityEngine.Vector2.zero; rt.anchorMax = UnityEngine.Vector2.one; rt.offsetMin = UnityEngine.Vector2.zero; rt.offsetMax = UnityEngine.Vector2.zero;
  var img = still.AddComponent<UnityEngine.UI.Image>(); img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Images/UI/Still/tower_still_placeholder_lowangle.png"); img.preserveAspect = true; }
still.SetActive(false);

// ---- E6：着いたとき（塔の全景スチル） ----
var arrive = NewGO(root.transform, "着いたとき（塔の全景スチル → OP_02h → オートセーブ）", new UnityEngine.Vector3(-115.2f, 11f, 0f));
var arriveSeq = MakeSeq(arrive, true);
AddWait(arrive, 0.6f);
AddFade(arrive, 0, 0.6f);                 // 暗くして
AddShowHide(arrive, new UnityEngine.Object[]{ still }, new UnityEngine.Object[0]);   // スチル（暗転の幕より手前）
AddWait(arrive, 1.5f);
AddTalk(arrive, L("OP_02h_TowerStill"));
AddShowHide(arrive, new UnityEngine.Object[0], new UnityEngine.Object[]{ still });   // スチルを閉じる（下は暗転の幕）
AddFade(arrive, 1, 1.0f);                 // マップへ
AddFlags(arrive, new[]{ Flag("Flag_Land_TowerStill") });
arrive.AddComponent<AutoSaveEventAction>();
{ var se = arrive.AddComponent<SceneStartEvent>(); var so = new UnityEditor.SerializedObject(se); so.FindProperty("sequence").objectReferenceValue = arriveSeq;
  var fl = so.FindProperty("condition.flags"); fl.arraySize = 1; fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = Flag("Flag_Land_TowerStill"); fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 1;
  so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- 怪物（隠しておく。戦闘エリアが始めると動く） ----
var enemyPrefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("Enemies/Enemy_Base");
var enemyGo = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
enemyGo.name = "砂漠の怪物（背後から出る・追いかける。攻撃では倒れない）";
enemyGo.transform.SetParent(root.transform, true); enemyGo.transform.position = enemyHome;
var enemy = enemyGo.GetComponent<Enemy>(); SetProp(enemy, "data", UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/EnemyData_Stage1_1_DesertChaser.asset")); SetProp(enemy, "saveDefeat", false);
enemyGo.SetActive(false);

// ---- 追跡（戦闘エリア） ----
var restartMark = NewGO(root.transform, "目印_やり直す位置（爪痕の帯のすぐ下）", restartAt);
var goal = NewGO(root.transform, "追跡のゴール（塔の階段の上）", goalPos);
var gcol = goal.AddComponent<UnityEngine.BoxCollider2D>(); gcol.isTrigger = true; gcol.size = new UnityEngine.Vector2(4.6f, 1.2f);
var battleGo = NewGO(root.transform, "追跡（戦闘エリア。塔の入口まで逃げる）", new UnityEngine.Vector3(-115.2f, 20f, 0f));
{ var b = battleGo.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(0.1f, 0.1f); }
var battle = battleGo.AddComponent<BattleArea>();
SetProp(battle, "start", 1); SetList(battle, "enemies", new UnityEngine.Object[]{ enemy }); SetProp(battle, "holdUntilStart", true);
SetProp(battle, "win", 3); SetProp(battle, "goal", gcol); SetProp(battle, "lose", 1); SetProp(battle, "catchDistance", 0.9f); SetProp(battle, "loseResult", 0); SetProp(battle, "restartGrace", 2.0f); SetProp(battle, "restartPoint", restartMark.transform);
SetProp(battle, "objective", "塔の入口へ逃げろ"); SetProp(battle, "startMessage", ""); SetProp(battle, "loseMessage", ""); SetProp(battle, "winMessage", "");
SetProp(battle, "endEnemies", 1); SetProp(battle, "once", true); SetProp(battle, "saveId", "Land_DesertChase");
// 勝ったとき（塔の入口に着いた）：Stage1_2 へ（正面の扉と同じ出現位置）
var win = NewGO(battleGo.transform, "勝ったとき（塔に入る → Stage1_2）", goalPos);
MakeSeq(win, true);
AddSe(win, Se("Se_scene_leave"));
{ var a = win.AddComponent<SceneChangeEventAction>(); SetProp(a, "sceneName", "Stage1_2"); SetProp(a, "spawnPointId", "FromStage1_1"); }
SetProp(battle, "onWin", win.GetComponent<EventSequence>());

// ---- E7・E8：扉の爪痕 → 最大の振動 → 背後から怪物 → 追跡（残骸を調べたかで OP_02j を分ける） ----
var area = NewGO(root.transform, "扉の爪痕（範囲に入ると気づく → 怪物 → 追跡）", new UnityEngine.Vector3(-115.2f, markY, 0f));
{ var b = area.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(14.4f, 1.0f); }
System.Func<string, string, EventSequence> Build = (n, talkId) => {
  var g = NewGO(area.transform, n, area.transform.position); var s = MakeSeq(g, true);
  AddTalk(g, L("OP_02i_TowerDoorMark"));
  AddFlags(g, new[]{ Flag("Flag_Land_DoorMark") });
  // 最大の振動：大きく揺れて大きな音（音を切っても揺れでわかる）
  AddSe(g, Se("Se_rumble_long"));
  AddShake(g, shakeL, shakeLTime, false);
  AddWait(g, 1.0f);
  { var a = g.AddComponent<FaceDirectionEventAction>(); SetProp(a, "who", 0); SetProp(a, "direction", 1); }   // ノアが振り向く（下）
  // 背後（マップの手前）から怪物
  AddShowHide(g, new UnityEngine.Object[]{ enemyGo }, new UnityEngine.Object[0]);
  { var a = g.AddComponent<EnemyCommandEventAction>(); SetList(a, "enemies", new UnityEngine.Object[]{ enemy }); SetProp(a, "command", 0); SetProp(a, "tintWhenStopped", false); }
  { var a = g.AddComponent<MoveObjectEventAction>(); SetProp(a, "target", enemyGo.transform); SetProp(a, "type", 0); SetProp(a, "offset", enemyFrom); SetProp(a, "duration", 1.4f); SetProp(a, "ease", 2); SetProp(a, "waitUntilDone", false); }
  // 会話の間も怪物を映したまま → 会話のあとノアへ戻す
  { var a = g.AddComponent<CameraLookEventAction>(); SetProp(a, "target", enemyGo.transform); SetProp(a, "moveTime", 0.6f); SetProp(a, "holdTime", 0.6f); SetProp(a, "returnAfter", false); }
  AddSe(g, Se("Se_monster_roar"));
  AddTalk(g, L(talkId));
  { var a = g.AddComponent<CameraLookEventAction>(); SetProp(a, "target", restartMark.transform); SetProp(a, "moveTime", 0.4f); SetProp(a, "holdTime", 0f); SetProp(a, "returnAfter", true); }
  { var a = g.AddComponent<BattleControlEventAction>(); SetProp(a, "battle", battle); SetProp(a, "command", 0); }
  { var a = g.AddComponent<BarkEventAction>(); var so = new UnityEditor.SerializedObject(a); var ln = so.FindProperty("lines"); ln.arraySize = 1; var e0 = ln.GetArrayElementAtIndex(0); e0.FindPropertyRelative("speaker").enumValueIndex = 2; e0.FindPropertyRelative("text").stringValue = "逃げなきゃ！"; e0.FindPropertyRelative("seconds").floatValue = 1.6f; so.FindProperty("waitUntilDone").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
  return s; };
var seqWreck = Build("残骸を調べたとき（OP_02j）", "OP_02j_Monster");
var seqNo = Build("残骸を調べていないとき（OP_02j_Monster_NoWreck）", "OP_02j_Monster_NoWreck");
{ var ii = area.AddComponent<ItemInteraction>(); var so = new UnityEditor.SerializedObject(ii);
  so.FindProperty("trigger").enumValueIndex = 1; so.FindProperty("saveId").stringValue = "Land_DoorMark"; so.FindProperty("saveCompleted").boolValue = true; so.FindProperty("promptLabel").stringValue = "";
  var rules = so.FindProperty("rules"); rules.arraySize = 2;
  var r0 = rules.GetArrayElementAtIndex(0); r0.FindPropertyRelative("label").stringValue = "砂浜の残骸をどれか調べた：「あの残骸と、同じ」あり"; r0.FindPropertyRelative("sequence").objectReferenceValue = seqWreck; r0.FindPropertyRelative("markCompleted").boolValue = true; r0.FindPropertyRelative("consumeItems").boolValue = false;
  var fc = r0.FindPropertyRelative("flagConditions"); fc.arraySize = 1; fc.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = Flag("Flag_Land_WreckAny"); fc.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 0;
  var r1 = rules.GetArrayElementAtIndex(1); r1.FindPropertyRelative("label").stringValue = "調べていない：その1行なし"; r1.FindPropertyRelative("sequence").objectReferenceValue = seqNo; r1.FindPropertyRelative("markCompleted").boolValue = true; r1.FindPropertyRelative("consumeItems").boolValue = false;
  r1.FindPropertyRelative("flagConditions").arraySize = 0; r1.FindPropertyRelative("conditions").arraySize = 0;
  so.ApplyModifiedPropertiesWithoutUndo(); }

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
return Log.ToString();
