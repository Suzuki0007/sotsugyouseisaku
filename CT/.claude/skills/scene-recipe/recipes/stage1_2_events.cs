// Stage1_2：一層の序盤（Stage1_2_イベント 一式）を作り直す。正面の扉が消えるので、続けて stage1_2_door → autosave_points
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_2.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
System.Func<string, DialogueData> D = n => { var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/Scenario_F1/" + n + ".asset"); if (d == null) throw new System.Exception("no dialogue " + n); return d; };
System.Func<string, GameFlag> F = n => UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/" + n + ".asset");
var sigleJoined = F("Flag_Sigle_Joined"); var introDone = F("Flag_1_2_Intro_Done");
var lanternItem = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/Items/Item_Lantern.asset");
var chaserData = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Enemies/EnemyData_Stage1_2_Chaser.asset");

System.Action<UnityEngine.Object, string, object> P = (o, prop, val) => {
  var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop); if (p == null) throw new System.Exception("no prop " + prop + " on " + o.GetType().Name);
  if (val is float) p.floatValue = (float)val; else if (val is int) { if (p.propertyType == UnityEditor.SerializedPropertyType.Enum) p.enumValueIndex = (int)val; else p.intValue = (int)val; }
  else if (val is bool) p.boolValue = (bool)val; else if (val is string) p.stringValue = (string)val; else if (val is UnityEngine.Vector2) p.vector2Value = (UnityEngine.Vector2)val; else p.objectReferenceValue = (UnityEngine.Object)val;
  so.ApplyModifiedPropertiesWithoutUndo(); };
System.Action<UnityEngine.Object, string, UnityEngine.Object[]> L = (o, prop, arr) => {
  var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop); p.arraySize = arr.Length; for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i]; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, UnityEngine.GameObject> GO = (parent, n, pos) => { var g = new UnityEngine.GameObject(n); if (parent != null) g.transform.SetParent(parent, false); else UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene); g.transform.position = pos; return g; };

// helper builders for event parts on a sequence object
System.Func<UnityEngine.GameObject, UnityEngine.Transform> TalkHolder = seqGo => { var t = seqGo.transform.Find("会話"); if (t == null) { t = GO(seqGo.transform, "会話", seqGo.transform.position).transform; } return t; };
System.Action<UnityEngine.GameObject, string> Talk = (go, n) => { var h = GO(TalkHolder(go), "会話_" + n, go.transform.position); var dlg = h.AddComponent<Dialogue>(); P(dlg, "dialogueData", D(n)); var a = go.AddComponent<DialogueEventAction>(); P(a, "dialogue", dlg); P(a, "closeMode", 0); P(a, "waitUntilClosed", true); };
System.Action<UnityEngine.GameObject, float> Wait = (go, s) => { var a = go.AddComponent<WaitEventAction>(); P(a, "seconds", s); };
System.Action<UnityEngine.GameObject, float, float, bool> Shake = (go, st, du, w) => { var a = go.AddComponent<ShakeEventAction>(); P(a, "strength", st); P(a, "duration", du); P(a, "waitUntilDone", w); };
System.Action<UnityEngine.GameObject> SeSlot = go => { go.AddComponent<SeEventAction>(); };
System.Action<UnityEngine.GameObject, int, int> Face = (go, who, dir) => { var a = go.AddComponent<FaceDirectionEventAction>(); P(a, "who", who); P(a, "direction", dir); };
System.Action<UnityEngine.GameObject, UnityEngine.Transform> Look = (go, t) => { var a = go.AddComponent<CameraLookEventAction>(); P(a, "target", t); P(a, "moveTime", 0.8f); P(a, "holdTime", 1.2f); P(a, "returnAfter", true); };
System.Func<UnityEngine.GameObject, bool, UnityEngine.GameObject> Seq = (go, lockP) => { var s = go.AddComponent<EventSequence>(); P(s, "lockPlayer", lockP); return go; };

// ---- remove previous build ----
var old = UnityEngine.GameObject.Find("Stage1_2_イベント"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var root = GO(null, "Stage1_2_イベント", new UnityEngine.Vector3(-115.7f, 13f, 0f));

// ---- hide samples / old things ----
foreach (var path in new[]{"ItemInteractionSample","MapEntity/Itm_Key","MapEntity/OB_TRbox","Spawner/EnemySpawner","Event/DialogueEventZone"}) { var g = UnityEngine.GameObject.Find(path); if (g != null) { g.SetActive(false); sb.Append("hid ").Append(path).Append("\n"); } }
var lamps = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (var lsrc in UnityEngine.Object.FindObjectsByType<LightSource>(UnityEngine.FindObjectsInactive.Include)) { var g = lsrc.gameObject; if (g.transform.parent == null && (g.name == "Light" || g.name == "Light (1)" || g.name == "Light (2)")) { g.SetActive(false); lamps.Add(g); } }
sb.Append("lamps=").Append(lamps.Count).Append("\n");
UnityEngine.Camera.main.backgroundColor = UnityEngine.Color.black;

// ---- darkness: very dark at first ----
var sd = UnityEngine.Object.FindAnyObjectByType<StageDarkness>();
P(sd, "darkness", 0.97f); P(sd, "radius", 1.0f); P(sd, "innerRadius", 0.0f);

// ---- Sigle joins in this scene ----
P(UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>(), "sigleJoinedFlag", sigleJoined);

// ---- markers ----
var markLeft = GO(root.transform, "目印_左を映す（左端は映さない）", new UnityEngine.Vector3(-116.5f, 13.0f, 0f));
var sigleDest = GO(root.transform, "目印_シグルが待つ位置（画面の外）", new UnityEngine.Vector3(-119.8f, 12.8f, 0f));
var restartMark =GO(root.transform, "目印_やり直す位置（敵が出たところ）", new UnityEngine.Vector3(-107.25f, 14.4f, 0f));
var roomSr0 = UnityEngine.GameObject.Find("Grid/Room_1_Rouka").GetComponent<UnityEngine.SpriteRenderer>();
var roomMat = roomSr0.sharedMaterial;
var uiSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/UISprite.psd");
// rubble that blocks the entrance (placeholder squares)
var rubble = GO(root.transform, "ガレキ（入口をふさぐ・仮の絵）", new UnityEngine.Vector3(-107.45f, 9.9f, 0f));
var rubbleWall = GO(rubble.transform, "当たり判定（落ちてから有効）", rubble.transform.position);
{ var rb = rubbleWall.AddComponent<UnityEngine.BoxCollider2D>(); rb.size = new UnityEngine.Vector2(3.6f, 1.2f); rubbleWall.SetActive(false);
  var rnd = new System.Random(7);
  for (int i = 0; i < 7; i++) { var piece = GO(rubble.transform, "かけら" + i, rubble.transform.position + new UnityEngine.Vector3(-1.5f + i * 0.5f, (float)(rnd.NextDouble() * 0.6 - 0.3), 0f));
    var sr = piece.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = uiSprite; sr.sharedMaterial = roomMat; sr.sortingLayerID = roomSr0.sortingLayerID; sr.sortingOrder = roomSr0.sortingOrder + 2; float g = 0.6f + (float)rnd.NextDouble() * 0.2f; sr.color = new UnityEngine.Color(g, g * 0.95f, g * 0.85f);
    piece.transform.localScale = new UnityEngine.Vector3(2.0f + (float)rnd.NextDouble() * 1.2f, 1.5f + (float)rnd.NextDouble() * 1.0f, 1f); piece.transform.rotation = UnityEngine.Quaternion.Euler(0, 0, (float)(rnd.NextDouble() * 60 - 30)); } }
rubble.SetActive(false);
// Sigle's shot (placeholder: player bullet sprite)
var shot = GO(root.transform, "シグルの攻撃（仮の絵）", new UnityEngine.Vector3(-120.0f, 13.3f, 0f));
{ var bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefab/Bullet/PlayerBullet.prefab"); var bsr = bulletPrefab.GetComponentInChildren<UnityEngine.SpriteRenderer>();
  var sr = shot.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = bsr.sprite; sr.color = bsr.color; sr.sortingLayerID = bsr.sortingLayerID; sr.sortingOrder = bsr.sortingOrder + 1; shot.transform.localScale = bulletPrefab.transform.localScale * 1.5f; }
shot.SetActive(false);

// ---- lane walls (dark: only straight to the lantern) ----
var walls = GO(root.transform, "暗闇の見えない壁（ランタンまでまっすぐ）", new UnityEngine.Vector3(-107.25f, 12.2f, 0f));
foreach (var x in new[]{-108.65f, -105.85f}) { var b = walls.AddComponent<UnityEngine.BoxCollider2D>(); b.offset = new UnityEngine.Vector2(x - walls.transform.position.x, 0f); b.size = new UnityEngine.Vector2(0.4f, 7.6f); }

// ---- lantern ----
var lantern = GO(root.transform, "ランタン（調べると拾う）", new UnityEngine.Vector3(-107.25f, 15.2f, 0f));
var lcol = lantern.AddComponent<UnityEngine.CircleCollider2D>(); lcol.isTrigger = true; lcol.radius = 1.0f;
var lvis = GO(lantern.transform, "見た目（仮）", lantern.transform.position);
var lsr = lvis.AddComponent<UnityEngine.SpriteRenderer>();
lsr.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/Knob.psd");
lsr.color = new UnityEngine.Color(1f, 0.75f, 0.3f); lvis.transform.localScale = new UnityEngine.Vector3(0.5f, 0.5f, 1f);
var roomSr = UnityEngine.GameObject.Find("Grid/Room_1_Rouka").GetComponent<UnityEngine.SpriteRenderer>();
lsr.sortingLayerID = roomSr.sortingLayerID; lsr.sortingOrder = roomSr.sortingOrder + 1;
var ls = lvis.AddComponent<LightSource>(); P(ls, "radius", 2.6f); P(ls, "innerRadius", 0.6f); P(ls, "intensity", 1.2f);
{ var so = new UnityEditor.SerializedObject(ls); so.FindProperty("color").colorValue = new UnityEngine.Color(1f, 0.8f, 0.5f); so.FindProperty("flickerAmount").floatValue = 0.08f; so.FindProperty("selfIlluminate").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- enemy (hidden until it falls) ----
var enemyPrefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("Enemies/Enemy_Base");
var enemyGo = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
enemyGo.name = "塔の化け物（降ってくる・追いかける）";
enemyGo.transform.SetParent(root.transform, true);
enemyGo.transform.position = new UnityEngine.Vector3(-104.4f, 13.4f, 0f);
var enemy = enemyGo.GetComponent<Enemy>(); P(enemy, "data", chaserData); P(enemy, "saveDefeat", false);
enemyGo.SetActive(false);

// ---- chase (battle area) ----
var goal = GO(root.transform, "追跡のゴール（左の行き止まり）", new UnityEngine.Vector3(-128.8f, 13.05f, 0f));
var gcol = goal.AddComponent<UnityEngine.BoxCollider2D>(); gcol.isTrigger = true; gcol.size = new UnityEngine.Vector2(2.6f, 5.7f);
var battleGo = GO(root.transform, "追跡（戦闘エリア）", new UnityEngine.Vector3(-115.7f, 13f, 0f));
var bcol = battleGo.AddComponent<UnityEngine.BoxCollider2D>(); bcol.isTrigger = true; bcol.size = new UnityEngine.Vector2(0.1f, 0.1f);
var battle = battleGo.AddComponent<BattleArea>();
bcol = battleGo.GetComponent<UnityEngine.BoxCollider2D>(); bcol.isTrigger = true;
P(battle, "start", 1); L(battle, "enemies", new UnityEngine.Object[]{ enemy }); P(battle, "holdUntilStart", true);
P(battle, "win", 3); P(battle, "goal", gcol); P(battle, "lose", 1); P(battle, "catchDistance", 0.9f); P(battle, "loseResult", 0); P(battle, "restartGrace", 2.0f); P(battle, "restartPoint", restartMark.transform);
P(battle, "objective", "左の奥へ逃げろ"); P(battle, "startMessage", ""); P(battle, "loseMessage", ""); P(battle, "winMessage", "");
P(battle, "endEnemies", 1); P(battle, "once", true); P(battle, "saveId", "Stage1_2_Chase");

// ---- rescue (on win) ----
var rescue = Seq(GO(battleGo.transform, "勝ったとき（追い詰められる → シグルに助けられる）", goal.transform.position), true);
Wait(rescue, 0.3f); // let the run-in motion settle, then face right
Face(rescue, 0, 3);
Talk(rescue, "F1_04_Cornered");
// Sigle joins but waits off-screen (not shown at the moment of the attack)
{ var a = rescue.AddComponent<SetFlagEventAction>(); a.AddFlagForEditor(sigleJoined, true); }
{ var a = rescue.AddComponent<PartyModeEventAction>(); P(a, "mode", 1); }
{ var a = rescue.AddComponent<PartyWarpEventAction>(); P(a, "destination", sigleDest.transform); P(a, "fade", false); }
// Sigle's shot flies in from off-screen and hits the enemy
{ var a = rescue.AddComponent<SetActiveEventAction>(); L(a, "show", new UnityEngine.Object[]{ shot }); }
{ var a = rescue.AddComponent<MoveObjectEventAction>(); P(a, "target", shot.transform); P(a, "type", 2); P(a, "destination", enemyGo.transform); P(a, "offset", new UnityEngine.Vector2(0f, 0.5f)); P(a, "duration", 0.18f); P(a, "ease", 0); P(a, "waitUntilDone", true); }
{ var a = rescue.AddComponent<SetActiveEventAction>(); L(a, "hide", new UnityEngine.Object[]{ shot }); }
{ var a = rescue.AddComponent<EffectEventAction>(); P(a, "effectOnEnter", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Effect/Sample/Fx_Sample_Hit.asset")); P(a, "spawnAt", 2); P(a, "customPoint", enemyGo.transform); P(a, "offset", new UnityEngine.Vector2(0f, 0.5f)); }
SeSlot(rescue);
// white flash
{ var a = rescue.AddComponent<ScreenFadeEventAction>(); P(a, "type", 0); P(a, "duration", 0.05f); P(a, "waitUntilDone", true); var so = new UnityEditor.SerializedObject(a); so.FindProperty("color").colorValue = UnityEngine.Color.white; so.ApplyModifiedPropertiesWithoutUndo(); }
Shake(rescue, 0.35f, 0.5f, false);
{ var a = rescue.AddComponent<EnemyCommandEventAction>(); L(a, "enemies", new UnityEngine.Object[]{ enemy }); P(a, "command", 5); }
{ var a = rescue.AddComponent<ScreenFadeEventAction>(); P(a, "type", 1); P(a, "duration", 0.45f); P(a, "waitUntilDone", true); var so = new UnityEditor.SerializedObject(a); so.FindProperty("color").colorValue = UnityEngine.Color.white; so.ApplyModifiedPropertiesWithoutUndo(); }
Wait(rescue, 0.8f);
// Sigle walks in from off-screen and stops facing Noah (to Noah's right, a little apart)
{ var a = rescue.AddComponent<MoveObjectEventAction>(); P(a, "targetKind", 2); P(a, "type", 2); P(a, "destinationKind", 1); P(a, "offset", new UnityEngine.Vector2(2.6f, 0f)); P(a, "duration", 2.2f); P(a, "ease", 2); P(a, "waitUntilDone", true); }
Face(rescue, 0, 3);
Face(rescue, 1, 2);
Talk(rescue, "F1_05_Rescue");
Talk(rescue, "F1_06_MeetSigle");
Talk(rescue, "F1_07_SigleIntro");
// after the talk: together (Sigle follows from here)
{ var a = rescue.AddComponent<PartyModeEventAction>(); P(a, "mode", 0); }
P(battle, "onWin", rescue.GetComponent<EventSequence>());

// ---- lantern event ----
var inter = lantern.AddComponent<ItemInteraction>();
var lseq = Seq(lantern, true);
{ var a = lantern.AddComponent<SetActiveEventAction>(); var arr = new System.Collections.Generic.List<UnityEngine.Object>(); foreach (var g in lamps) if (g.name != "Light") arr.Add(g); L(a, "show", arr.ToArray()); L(a, "hide", new UnityEngine.Object[]{ lvis, walls }); }
{ var a = lantern.AddComponent<ItemGiveEventAction>(); P(a, "item", lanternItem); P(a, "count", 1); }
{ var a = lantern.AddComponent<DarknessEventAction>(); P(a, "darkness", 0.88f); P(a, "radius", 3.0f); P(a, "duration", 1.2f); P(a, "waitUntilDone", false); }
Talk(lantern, "F1_02c_Lantern");
SeSlot(lantern);
Shake(lantern, 0.2f, 0.8f, true);
Talk(lantern, "F1_02d_Again");
Face(lantern, 0, 3);
Wait(lantern, 0.5f);
{ var a = lantern.AddComponent<SetActiveEventAction>(); L(a, "show", new UnityEngine.Object[]{ enemyGo, rubble }); }
{ var a = lantern.AddComponent<EnemyCommandEventAction>(); L(a, "enemies", new UnityEngine.Object[]{ enemy }); P(a, "command", 0); P(a, "tintWhenStopped", false); }
{ var a = lantern.AddComponent<MoveObjectEventAction>(); P(a, "target", rubble.transform); P(a, "type", 0); P(a, "offset", new UnityEngine.Vector2(0f, 7f)); P(a, "duration", 0.3f); P(a, "ease", 1); P(a, "waitUntilDone", false); }
{ var a = lantern.AddComponent<MoveObjectEventAction>(); P(a, "target", enemyGo.transform); P(a, "type", 0); P(a, "offset", new UnityEngine.Vector2(0f, 8f)); P(a, "duration", 0.35f); P(a, "ease", 1); P(a, "waitUntilDone", true); }
{ var a = lantern.AddComponent<SetActiveEventAction>(); L(a, "show", new UnityEngine.Object[]{ rubbleWall }); }
Face(lantern, 0, 3);
SeSlot(lantern);
Shake(lantern, 0.35f, 0.6f, false);
Wait(lantern, 0.6f);
Talk(lantern, "F1_03_EnemyAppear");
Look(lantern, markLeft.transform);
Talk(lantern, "F1_03b_RunLeft");
{ var a = lantern.AddComponent<BattleControlEventAction>(); P(a, "battle", battle); P(a, "command", 0); }
{ var a = lantern.AddComponent<BarkEventAction>(); var so = new UnityEditor.SerializedObject(a); var ln = so.FindProperty("lines"); ln.arraySize = 1; var e0 = ln.GetArrayElementAtIndex(0); e0.FindPropertyRelative("speaker").enumValueIndex = 2; e0.FindPropertyRelative("text").stringValue = "逃げなきゃ！"; e0.FindPropertyRelative("seconds").floatValue = 1.6f; so.FindProperty("waitUntilDone").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
{
  var so = new UnityEditor.SerializedObject(inter);
  so.FindProperty("trigger").enumValueIndex = 0; so.FindProperty("saveId").stringValue = "Stage1_2_Lantern"; so.FindProperty("saveCompleted").boolValue = true;
  var rules = so.FindProperty("rules"); rules.arraySize = 1; var r = rules.GetArrayElementAtIndex(0);
  r.FindPropertyRelative("label").stringValue = "ランタンを拾う（1回だけ）"; r.FindPropertyRelative("sequence").objectReferenceValue = lantern.GetComponent<EventSequence>(); r.FindPropertyRelative("markCompleted").boolValue = true; r.FindPropertyRelative("consumeItems").boolValue = false;
  so.ApplyModifiedPropertiesWithoutUndo();
}

// ---- intro (scene start) ----
var intro = Seq(GO(root.transform, "着いたとき（頭痛 → 暗い → ランタンを取ろう）", new UnityEngine.Vector3(-107.4f, 10.9f, 0f)), true);
var startEvent = intro.AddComponent<SceneStartEvent>();
{ var so = new UnityEditor.SerializedObject(startEvent); so.FindProperty("sequence").objectReferenceValue = intro.GetComponent<EventSequence>(); so.FindProperty("delay").floatValue = 0.3f; var fl = so.FindProperty("condition.flags"); fl.arraySize = 1; fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = introDone; fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
Wait(intro, 0.8f);
SeSlot(intro);
Shake(intro, 0.15f, 1.0f, true);
Talk(intro, "F1_02_Headache");
Look(intro, lantern.transform);
Talk(intro, "F1_02b_Dark");
{ var a = intro.AddComponent<SetFlagEventAction>(); a.AddFlagForEditor(introDone, true); }

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
