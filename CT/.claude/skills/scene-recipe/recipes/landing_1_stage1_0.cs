// 上陸 Stage1_0（砂浜）：OP_はじめ を「暗転 → 小舟の上で手紙 → 岸に着く → 上陸・振動（小）」に作り直し、歩ける範囲・残骸3つ・足跡・振動（中）→ 砂漠 を置く（ルート「上陸（Stage1_0）」を消して作る。先に landing_0_assets）
// @helpers
var scene = OpenSceneSafe("Assets/Scenes/Stage1_0.unity");
System.Func<string, UnityEngine.GameObject> Find = path => { var parts = path.Split('/'); foreach (var r in scene.GetRootGameObjects()) { if (r.name != parts[0]) continue; var t = r.transform; for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]); if (t != null) return t.gameObject; } return null; };
System.Func<string, SeData> Se = n => { foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:SeData")) { var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g); if (System.IO.Path.GetFileNameWithoutExtension(p) == n) return UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(p); } throw new System.Exception("no se " + n); };
System.Func<string, DialogueData> L = id => Dlg("Scenario_Landing", id);

// ---- 仮で決めた値（Inspector でも変えられる。ここを変えて流し直してもよい） ----
var seaBoatPos   = new UnityEngine.Vector3(-115.6f, 12.0f, 0f); // 海の上の小舟
var shoreBoatPos = new UnityEngine.Vector3(-115.6f, 18.0f, 0f); // 岸に着いた小舟（今の小舟の位置）
float rideTime = 4.0f;   // 岸に着くまで（秒）
float stepUp = 3.2f;     // 小舟から降りる距離（マス）。波の上に立たないように砂の上まで
float shakeS = 0.08f, shakeSTime = 1.0f;   // 振動（小）
float shakeM = 0.16f, shakeMTime = 2.0f;   // 振動（中）
float rhythm = 0.65f;                       // 振動（中）の音の間隔（秒）

var letterRead = Flag("Flag_Land_LetterRead"); var opDone = Flag("Flag_OP_Done");

// ---- 前に作った物を消す ----
{ var old = Find("上陸（Stage1_0）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
{ var old = Find("OP/OP_はじめ（暗転からマップへ）"); if (old != null) { UnityEngine.Object.DestroyImmediate(old); Log.Append("removed old OP_はじめ\n"); } }
{ var old = Find("OP/OP_はじめ（暗転 → 小舟の上で手紙 → 上陸）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
{ var old = Find("演出の画像（手紙・スチル）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
var root = NewGO(null, "上陸（Stage1_0）", new UnityEngine.Vector3(-115.1f, 24f, 0f)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

// ---- 海の当たり判定（手で置いた箱。海・岸の両側）をオフ：歩ける範囲で同じことをする（小舟が海の上を進めるように） ----
foreach (var b in Find("Grid/soto_yuka").GetComponents<UnityEngine.BoxCollider2D>()) { if (b.enabled) { b.enabled = false; Log.Append("soto_yuka box off ").Append(b.offset).Append(b.size).Append("\n"); } }

// ---- 小舟：岸の小舟（今の物）と、海の上の小舟（コピー） ----
var shoreBoat = Find("sunahama_hune_0"); shoreBoat.transform.position = shoreBoatPos;
var seaBoat = UnityEngine.Object.Instantiate(shoreBoat, root.transform); seaBoat.name = "小舟（海の上。上陸するまで）"; seaBoat.transform.position = seaBoatPos; seaBoat.SetActive(true);

// ---- 出現位置：はじめから＝海の上の小舟の上 ----
foreach (var sp in UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(UnityEngine.FindObjectsInactive.Include))
  if (sp.SpawnPointId == "Start_Boat") { sp.transform.position = seaBoatPos + new UnityEngine.Vector3(0.4f, 0.4f, 0f); Log.Append("Start_Boat ").Append(sp.transform.position).Append("\n"); }
// シーンを直接再生したときの位置（PlayerPartySpawner の既定）も小舟の上に
{ var spn = Find("Spawner/PlayerPartySpawner/NoahSpaenPoint"); if (spn != null) spn.transform.position = seaBoatPos + new UnityEngine.Vector3(0.4f, 0.4f, 0f);
  var sps = Find("Spawner/PlayerPartySpawner/SigleSpawnPoint"); if (sps != null) sps.transform.position = seaBoatPos + new UnityEngine.Vector3(-0.4f, 0.4f, 0f); }

// ---- 歩ける範囲 ----
var walk = NewGO(root.transform, "歩ける範囲（砂浜だけ）", root.transform.position);
System.Func<string, float, float, float, float, bool, UnityEngine.GameObject> Box = (n, x, y, w, h, exclude) => { var g = NewGO(walk.transform, n, new UnityEngine.Vector3(x, y, 0f)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(w, h); if (exclude) g.AddComponent<WalkAreaExclude>(); return g; };
// 砂浜：波（y 22 くらいまで寄せてくる）の少し下から奥の端まで
Box("砂浜（仮の四角。本番はマスク画像から）", -115.1f, 29.3f, 14.2f, 18.0f, false);
var boatLane = Box("小舟の通り道（上陸するまで）", -115.4f, 15.8f, 2.4f, 11.0f, false);
var boatBlock = Box("小舟（上陸したあと。歩けない）", -115.6f, 17.9f, 1.6f, 3.2f, true);
Box("残骸1（歩けない）", -120.7f, 26.8f, 2.0f, 1.2f, true);
Box("残骸2（歩けない）", -110.8f, 25.8f, 1.6f, 1.2f, true);
Box("残骸3（歩けない）", -109.6f, 30.8f, 2.2f, 1.2f, true);
walk.AddComponent<WalkArea>();
// 上陸の前後で切り替える（海の小舟・通り道 ⇔ 岸の小舟・歩けない形）
{ var fs = walk.AddComponent<FlagSwitch>(); var so = new UnityEditor.SerializedObject(fs);
  var fl = so.FindProperty("flagConditions"); fl.arraySize = 1; fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = letterRead; fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 0;
  var on = so.FindProperty("activeWhenMet"); on.arraySize = 2; on.GetArrayElementAtIndex(0).objectReferenceValue = boatBlock; on.GetArrayElementAtIndex(1).objectReferenceValue = shoreBoat;
  var off = so.FindProperty("activeWhenNotMet"); off.arraySize = 2; off.GetArrayElementAtIndex(0).objectReferenceValue = boatLane; off.GetArrayElementAtIndex(1).objectReferenceValue = seaBoat;
  so.ApplyModifiedPropertiesWithoutUndo(); }
shoreBoat.SetActive(false); boatBlock.SetActive(false); // エディタでは上陸前の見た目（再生中は FlagSwitch が決める）

// ---- 手紙の紙（仮。会話の枠より奥・暗転の幕より手前） ----
var fx = NewGO(null, "演出の画像（手紙・スチル）", UnityEngine.Vector3.zero); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fx, scene);
{ var c = fx.AddComponent<UnityEngine.Canvas>(); c.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; c.sortingOrder = 95;
  var cs = fx.AddComponent<UnityEngine.UI.CanvasScaler>(); cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new UnityEngine.Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f; }
var paper = new UnityEngine.GameObject("手紙の紙（仮）", typeof(UnityEngine.RectTransform)); paper.transform.SetParent(fx.transform, false);
{ var rt = (UnityEngine.RectTransform)paper.transform; rt.sizeDelta = new UnityEngine.Vector2(760, 480); rt.anchoredPosition = new UnityEngine.Vector2(260, 130); // 立ち絵（左）と会話の枠（下）にかからない所
  var img = paper.AddComponent<UnityEngine.UI.Image>(); img.color = new UnityEngine.Color(0.90f, 0.85f, 0.72f);
  var tgo = new UnityEngine.GameObject("文面", typeof(UnityEngine.RectTransform)); tgo.transform.SetParent(paper.transform, false);
  var trt = (UnityEngine.RectTransform)tgo.transform; trt.anchorMin = UnityEngine.Vector2.zero; trt.anchorMax = UnityEngine.Vector2.one; trt.offsetMin = new UnityEngine.Vector2(60, 60); trt.offsetMax = new UnityEngine.Vector2(-60, -60);
  var tmp = tgo.AddComponent<TMPro.TextMeshProUGUI>(); tmp.font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
  tmp.text = "塔で待っている"; tmp.fontSize = 72; tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.color = new UnityEngine.Color(0.25f, 0.18f, 0.12f); }
paper.SetActive(false);

// ---- 部品を足す小道具 ----
System.Action<UnityEngine.GameObject, SeData> AddSe = (go, se) => { var a = go.AddComponent<SeEventAction>(); SetProp(a, "seOnEnter", se); SetProp(a, "playAtThisPosition", false); };
System.Action<UnityEngine.GameObject, float, float, bool> AddShake = (go, st, du, w) => { var a = go.AddComponent<ShakeEventAction>(); SetProp(a, "strength", st); SetProp(a, "duration", du); SetProp(a, "waitUntilDone", w); };
System.Action<UnityEngine.GameObject, GameFlag[]> AddFlags = (go, fs) => { var a = go.AddComponent<SetFlagEventAction>(); foreach (var f in fs) a.AddFlagForEditor(f, true); };
System.Action<UnityEngine.GameObject, UnityEngine.Object[], UnityEngine.Object[]> AddShowHide = (go, show, hide) => { var a = go.AddComponent<SetActiveEventAction>(); SetList(a, "show", show); SetList(a, "hide", hide); };
// エリアに入る／調べる → 流れ（1回だけなら完了を記録）
System.Func<string, UnityEngine.Vector3, UnityEngine.Vector2, bool, int, string, bool, UnityEngine.GameObject> Area = (n, pos, size, circle, trigger, saveId, once) => {
  var g = NewGO(root.transform, n, pos);
  if (circle) { var c = g.AddComponent<UnityEngine.CircleCollider2D>(); c.isTrigger = true; c.radius = size.x; } else { var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = size; }
  var ii = g.AddComponent<ItemInteraction>(); var seq = MakeSeq(g, true);
  var so = new UnityEditor.SerializedObject(ii); so.FindProperty("trigger").enumValueIndex = trigger; so.FindProperty("saveId").stringValue = saveId; so.FindProperty("saveCompleted").boolValue = once;
  so.FindProperty("promptLabel").stringValue = trigger == 0 ? "調べる" : "";
  var rules = so.FindProperty("rules"); rules.arraySize = 1; var r = rules.GetArrayElementAtIndex(0);
  r.FindPropertyRelative("label").stringValue = once ? "1回だけ" : "何度でも"; r.FindPropertyRelative("sequence").objectReferenceValue = seq; r.FindPropertyRelative("markCompleted").boolValue = once; r.FindPropertyRelative("consumeItems").boolValue = false;
  so.ApplyModifiedPropertiesWithoutUndo(); return g; };

// ---- E1・E2：OP_はじめ（暗転 → 記憶の声 → 小舟の上で手紙 → 岸に着く → 上陸・振動（小）） ----
var op = NewGO(Find("OP").transform, "OP_はじめ（暗転 → 小舟の上で手紙 → 上陸）", seaBoatPos);
op.transform.SetAsFirstSibling();
var opSeq = MakeSeq(op, true);
AddWait(op, 1.0f);
AddTalk(op, Dlg("Scenario_F1", "F1_00_Prologue"));
AddWait(op, 0.8f);
AddTalk(op, Dlg("Scenario_OP", "OP_01_Monologue"));
{ var a = op.AddComponent<ScreenFadeEventAction>(); SetProp(a, "type", 1); SetProp(a, "duration", 1.5f); }
AddWait(op, 0.8f);
AddShowHide(op, new UnityEngine.Object[]{ paper }, new UnityEngine.Object[0]);
AddTalk(op, L("OP_02a_Letter"));
AddShowHide(op, new UnityEngine.Object[0], new UnityEngine.Object[]{ paper });
{ var a = op.AddComponent<NoteEventAction>(); SetProp(a, "note", UnityEditor.AssetDatabase.LoadAssetAtPath<NoteData>("Assets/Resources/Notes/Landing/Note_KairosLetter.asset")); SetProp(a, "show", false); }
AddWait(op, 0.5f);
// 小舟とノアを一緒に岸へ（同じずらし・同じ時間）
float dy = shoreBoatPos.y - seaBoatPos.y;
{ var a = op.AddComponent<MoveObjectEventAction>(); SetProp(a, "targetKind", 0); SetProp(a, "target", seaBoat.transform); SetProp(a, "type", 1); SetProp(a, "offset", new UnityEngine.Vector2(0f, dy)); SetProp(a, "duration", rideTime); SetProp(a, "ease", 2); SetProp(a, "waitUntilDone", false); }
{ var a = op.AddComponent<MoveObjectEventAction>(); SetProp(a, "targetKind", 1); SetProp(a, "type", 1); SetProp(a, "offset", new UnityEngine.Vector2(0f, dy)); SetProp(a, "duration", rideTime); SetProp(a, "ease", 2); SetProp(a, "waitUntilDone", true); }
AddWait(op, 0.4f);
// 降りる
{ var a = op.AddComponent<MoveObjectEventAction>(); SetProp(a, "targetKind", 1); SetProp(a, "type", 1); SetProp(a, "offset", new UnityEngine.Vector2(0f, stepUp)); SetProp(a, "duration", 0.9f); SetProp(a, "ease", 0); SetProp(a, "waitUntilDone", true); }
AddFlags(op, new[]{ letterRead }); // 海の小舟 → 岸の小舟、通り道 → 歩けない形
AddWait(op, 0.4f);
// 「ついた」→ 振動（小）→「震えてる？」（2026-11-25 ユーザーの指示：揺れは「ついた」と「震えてる？」の間）
AddTalk(op, L("OP_02b_Landing"));
AddSe(op, Se("Se_kiiin"));
AddShake(op, shakeS, shakeSTime, false);
AddWait(op, 0.5f);
AddTalk(op, L("OP_02b_Vibe"));
AddFlags(op, new[]{ Flag("Flag_Land_VibeS"), opDone });
op.AddComponent<AutoSaveEventAction>();
{ var se = op.AddComponent<SceneStartEvent>(); var so = new UnityEditor.SerializedObject(se); so.FindProperty("sequence").objectReferenceValue = opSeq; so.FindProperty("startDark").boolValue = true;
  var fl = so.FindProperty("condition.flags"); fl.arraySize = 1; fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = opDone; fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 1;
  so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- E3：残骸（調べる・何度でも） ----
var wreckAny = Flag("Flag_Land_WreckAny");
var wrecks = new[]{
  new object[]{ "残骸1（西。調べる）", new UnityEngine.Vector3(-120.7f, 25.6f, 0f), "OP_02c_Wreck1", "Flag_Land_Wreck1", "Land_Wreck1" },
  new object[]{ "残骸2（東。調べる）", new UnityEngine.Vector3(-110.8f, 24.6f, 0f), "OP_02d_Wreck2", "Flag_Land_Wreck2", "Land_Wreck2" },
  new object[]{ "残骸3（奥。調べる）", new UnityEngine.Vector3(-109.6f, 29.5f, 0f), "OP_02e_Wreck3", "Flag_Land_Wreck3", "Land_Wreck3" },
};
foreach (var w in wrecks) { var g = Area((string)w[0], (UnityEngine.Vector3)w[1], new UnityEngine.Vector2(1.0f, 0f), true, 0, (string)w[4], false);
  AddTalk(g, L((string)w[2])); AddFlags(g, new[]{ Flag((string)w[3]), wreckAny }); }

// ---- E4：足跡（範囲・1回だけ。足跡は床の絵に描いてある） ----
{ var g = Area("足跡（範囲に入ると気づく）", new UnityEngine.Vector3(-115.1f, 23.8f, 0f), new UnityEngine.Vector2(14.2f, 1.0f), false, 1, "Land_Footprints", true);
  AddTalk(g, L("OP_02f_Footprints")); AddFlags(g, new[]{ Flag("Flag_Land_Footprints") }); }

// ---- E5：振動（中）→ 砂漠へ（範囲・1回だけ） ----
{ var g = Area("振動（中）→ 砂漠へ（奥）", new UnityEngine.Vector3(-115.1f, 36.4f, 0f), new UnityEngine.Vector2(14.2f, 1.0f), false, 1, "Land_VibeMid", true);
  var kick = Se("Se_rumble_short");
  AddShake(g, shakeM, shakeMTime, false);
  AddSe(g, kick); AddWait(g, rhythm); AddSe(g, kick); AddWait(g, rhythm); AddSe(g, kick); AddWait(g, rhythm);
  AddTalk(g, L("OP_02g_VibeMid"));
  AddFlags(g, new[]{ Flag("Flag_Land_VibeM") });
  { var a = g.AddComponent<SceneChangeEventAction>(); SetProp(a, "sceneName", "Stage1_1"); SetProp(a, "spawnPointId", "FromStage1_0"); } }

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
return Log.ToString();
