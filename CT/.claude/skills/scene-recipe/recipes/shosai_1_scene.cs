// Stage1_3（書斎）段階1：シーンを作り（なければ Stage_1_Boss をコピー）、仮素材を README の座標表どおりに置く・歩ける範囲・カメラ・出現位置。ルート「書斎（Stage1_3）」を消して作るので、続けて shosai_2 以降
// @helpers
// 座標は 1440×1080 の絵の中心ピクセル（左上が 0,0）。100px = 1 マス、絵の中心がワールドの (0,0)。
// 本番の絵に差し替えるときは、Karisozai の同じ名前のファイルを上書きする（または下の表のファイル名・座標を直して流し直す）。
const string ScenePath = "Assets/Scenes/Stage1_3.unity";
const string Dir = "Assets/Images/Map/Stage1_3/Karisozai/";

// ---- シーン：なければ Stage_1_Boss をコピーして、ティンパニ戦の物を外す ----
bool created = false;
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ScenePath) == null) {
  var cur0 = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
  if (cur0.isDirty) throw new System.Exception("開いているシーンに保存していない変更があります: " + cur0.path);
  if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Stage_1_Boss.unity", ScenePath)) throw new System.Exception("copy failed");
  created = true;
}
var scene = OpenSceneSafe(ScenePath);
Log.Append("scene=").Append(scene.path).Append(created ? " (copied from Stage_1_Boss)" : "").Append("\n");

System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
UnityEngine.Material litMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

if (created) {
  { var y = UnityEngine.GameObject.Find("Grid/soto_yuka"); if (y != null && litMat == null) litMat = y.GetComponent<UnityEngine.SpriteRenderer>().sharedMaterial; }
  foreach (var n in new[]{ "ティンパニ部屋", "room_timpani_kabe_0", "SavePoint", "Event", "PlayerSpawnPoint" }) { var g = FindRoot(n); if (g != null) { UnityEngine.Object.DestroyImmediate(g); Log.Append("removed ").Append(n).Append("\n"); } }
  var grid = FindRoot("Grid");
  foreach (var n in new[]{ "soto_yuka", "room_timpani_yuka_0" }) { var t = grid.transform.Find(n); if (t != null) { UnityEngine.Object.DestroyImmediate(t.gameObject); Log.Append("removed Grid/").Append(n).Append("\n"); } }
  // BGM：ティンパニ戦の切り替えを外し、塔の探索の BGM だけにする
  var bgm = FindRoot("BGMset");
  if (bgm != null) {
    var so = new UnityEditor.SerializedObject(bgm.GetComponent<SceneBgm>()); so.FindProperty("rules").arraySize = 0; so.ApplyModifiedPropertiesWithoutUndo();
    for (int i = bgm.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(bgm.transform.GetChild(i).gameObject);
    Log.Append("BGM rules cleared\n");
  }
}
// コピー元の残り（使わない宝箱。Id が空でシーンチェックのエラーになる）
{ var me = FindRoot("MapEntity"); var t = me != null ? me.transform.Find("OB_TRbox") : null; if (t != null) { UnityEngine.Object.DestroyImmediate(t.gameObject); Log.Append("removed MapEntity/OB_TRbox\n"); } }
if (litMat == null) throw new System.Exception("no Sprite-Lit-Default material");

// ---- 作り直し：ルートを消す ----
{ var old = FindRoot("書斎（Stage1_3）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }

System.Func<float, float, UnityEngine.Vector3> P = (x, y) => new UnityEngine.Vector3((x - 720f) / 100f, (540f - y) / 100f, 0f);
System.Func<string, UnityEngine.Sprite> Spr = f => {
  var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Dir + f + ".png");
  if (s == null) throw new System.Exception("no sprite " + f + "（Texture Type を Sprite にする）");
  if (System.Math.Abs(s.pixelsPerUnit - 100f) > 0.01f) Log.Append("WARN ppu ").Append(f).Append("=").Append(s.pixelsPerUnit).Append("\n");
  return s; };
// 絵を置く（親の下に、ピクセル座標で）
System.Func<UnityEngine.Transform, string, string, float, float, int, UnityEngine.SpriteRenderer> Pic = (parent, n, file, x, y, order) => {
  var g = NewGO(parent, n, P(x, y)); var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = Spr(file); sr.sortingOrder = order; sr.sharedMaterial = litMat; return sr; };

// ---- 部屋の絵（背景 1 枚）。マップの範囲（MapBounds）もこの絵 ----
var tmpRoot = new UnityEngine.GameObject("tmp"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tmpRoot, scene);
var bg = Pic(tmpRoot.transform, "背景（仮素材 bg_shosai_placeholder）", "bg_shosai_placeholder", 720, 540, 0);
bg.gameObject.AddComponent<MapBounds>();

var room = RoomMenu.Build(RoomKind.Normal, bg, "Stage1_3");
room.name = "書斎（Stage1_3）";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
bg.transform.SetParent(room.transform, true); bg.transform.SetSiblingIndex(0);
UnityEngine.Object.DestroyImmediate(tmpRoot);
{ var ctx = UnityEngine.Object.FindAnyObjectByType<PlayerSceneContext>(); SetProp(ctx, "mapBounds", bg.GetComponent<MapBounds>()); }
{ var r = room.GetComponent<Room>(); var so = new UnityEditor.SerializedObject(r); var p = so.FindProperty("displayName"); if (p != null) { p.stringValue = "書斎"; so.ApplyModifiedPropertiesWithoutUndo(); } }

// ---- 物（親だけ作り、絵は子「絵」。段階2 で親に調べる・仕掛けの部品を足す） ----
var objs = NewGO(room.transform, "物", UnityEngine.Vector3.zero).transform;
System.Func<string, float, float, UnityEngine.Transform> Obj = (n, x, y) => NewGO(objs, n, P(x, y)).transform;

Pic(objs, "絨毯の叩き跡（仮素材）", "floor_marks", 720, 360, 1);
Pic(objs, "本棚の側板の傷（仮素材）", "wall_scratches", 120, 405, 2);

{ var t = Obj("扉A（入口）", 1395, 630); Pic(t, "絵_閉", "door_A_closed", 1395, 630, 3).gameObject.SetActive(false); Pic(t, "絵_開", "door_A_open", 1395, 630, 3); }
{ var t = Obj("北西の出口（音符文様の仕掛け扉）", 270, 45); Pic(t, "絵_閉", "door_exit_note_closed", 270, 45, 3); Pic(t, "絵_開", "door_exit_note_open", 270, 45, 3).gameObject.SetActive(false); }
{ var t = Obj("ランプ", 721, 110);
  for (int i = 0; i < 8; i++) { float x = 560f + i * 46f; var l = NewGO(t, "ランプ" + (i + 1), P(x, 110)).transform; Pic(l, "絵_消", "lamp_off", x, 110, 3); Pic(l, "絵_点", "lamp_on", x, 110, 4).gameObject.SetActive(false); } }

var tx = new[]{ 480f, 710f, 940f, 1170f };
var tfile = new[]{ "timpani_T1_32_placeholder", "timpani_T2_29_placeholder", "timpani_T3_26_placeholder", "timpani_T4_23_placeholder" };
var tname = new[]{ "T1（32インチ・低いド）", "T2（29インチ・ミ）", "T3（26インチ・ソ）", "T4（23インチ・高いド）" };
for (int i = 0; i < 4; i++) { var t = Obj(tname[i], tx[i], 225); Pic(t, "絵", tfile[i], tx[i], 225, 3); }

{ var t = Obj("机", 720, 620); Pic(t, "絵", "desk", 720, 620, 3); }
{ var t = Obj("机の楽譜（第1問の手がかり）", 720, 620); Pic(t, "絵", "score_on_desk", 720, 620, 4); }
{ var t = Obj("マレット置き場", 560, 620); Pic(t, "絵_あり", "mallet_stand", 560, 620, 3); Pic(t, "絵_なし", "mallet_stand_empty", 560, 620, 3).gameObject.SetActive(false); }
// 東の本棚（チューニングキー）：右下の壁ぎわ。11-20 ユーザーの指示「ティンパニの側によりすぎ」で (1300,300) から移した（README の座標とは違う）
var shelfX = 1300f; var shelfY = 860f;
// 西の譜面箱（暗号のメモ）：左下の壁ぎわ。11-20 に足した（仮素材なし。四角と丸で仮の絵）
var boxX = 240f; var boxY = 830f;
{ var t = Obj("東の本棚", shelfX, shelfY); Pic(t, "絵", "bookshelf_east", shelfX, shelfY, 3); }
// 第1問の前後で、取れる所の見た目を変える（2026-11-20 ユーザーの指示「クリアしたときに、取れるようになる所の変化を分かりやすく」。仮の絵＝Unity 付属の四角・丸に色を付けた物）
{ var uiSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/UISprite.psd");
  var knob = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/Knob.psd");
  System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, float, float, float, float, UnityEngine.Color, int, UnityEngine.GameObject> Shape = (parent, n, sp, x, y, w, h, col, order) => {
    var g = NewGO(parent, n, P(x, y)); var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = sp; sr.color = col; sr.sortingOrder = order; sr.sharedMaterial = litMat;
    var size = sp.bounds.size; g.transform.localScale = new UnityEngine.Vector3(w / 100f / size.x, h / 100f / size.y, 1f); return g; };
  var barCol = new UnityEngine.Color(0.22f, 0.2f, 0.22f, 1f);
  var glowCol = new UnityEngine.Color(1f, 0.85f, 0.3f, 0.55f);
  var gold = new UnityEngine.Color(0.95f, 0.75f, 0.2f, 1f);
  // 本棚の格子：縦の棒 4 本＋横の棒 2 本（第1問で消える）
  var lattice = NewGO(objs, "本棚の格子（仮の絵・第1問で開く）", P(shelfX, shelfY)).transform;
  foreach (var dx in new[]{ -35f, -12f, 12f, 35f }) Shape(lattice, "縦の棒", uiSprite, shelfX + dx, shelfY, 6, 176, barCol, 6);
  foreach (var dy in new[]{ -60f, 60f }) Shape(lattice, "横の棒", uiSprite, shelfX, shelfY + dy, 86, 6, barCol, 6);
  // 本棚の取れる目印：黄色い光＋チューニングキー（第1問のあと、取るまで）
  var shine = NewGO(objs, "本棚の取れる目印（仮の絵・第1問のあと取るまで）", P(shelfX, shelfY)).transform;
  Shape(shine, "光", knob, shelfX, shelfY, 150, 150, glowCol, 4);
  var key = NewGO(shine, "チューニングキー（仮の絵）", P(shelfX, shelfY + 35)).transform;
  Shape(key, "持つ所", knob, shelfX - 12, shelfY + 35, 22, 22, gold, 5);
  Shape(key, "軸", uiSprite, shelfX + 8, shelfY + 35, 30, 6, gold, 5);
  shine.gameObject.SetActive(false);
  // 譜面箱：箱（いつも見える）
  var box = Obj("西の譜面箱", boxX, boxY);
  Shape(box, "箱（仮の絵）", uiSprite, boxX, boxY, 110, 70, new UnityEngine.Color(0.42f, 0.28f, 0.16f, 1f), 3);
  Shape(box, "ふた（仮の絵）", uiSprite, boxX, boxY - 30, 116, 14, new UnityEngine.Color(0.3f, 0.19f, 0.1f, 1f), 4);
  // 譜面箱の鍵：帯 2 本＋音符の形の錠（第1問で消える）
  var lockGo = NewGO(objs, "譜面箱の鍵（仮の絵・第1問で開く）", P(boxX, boxY)).transform;
  foreach (var dx in new[]{ -30f, 30f }) Shape(lockGo, "帯", uiSprite, boxX + dx, boxY, 8, 74, barCol, 6);
  Shape(lockGo, "錠", knob, boxX, boxY - 2, 26, 26, new UnityEngine.Color(0.55f, 0.55f, 0.6f, 1f), 7);
  Shape(lockGo, "錠の棒（音符の旗）", uiSprite, boxX + 11, boxY - 20, 4, 26, new UnityEngine.Color(0.55f, 0.55f, 0.6f, 1f), 7);
  // 譜面箱の取れる目印：光＋暗号のメモ（第1問のあと、取るまで。紙は楽譜断片の仮素材を使い回す）
  var shine2 = NewGO(objs, "譜面箱の取れる目印（仮の絵・第1問のあと取るまで）", P(boxX, boxY)).transform;
  Shape(shine2, "光", knob, boxX, boxY, 150, 150, glowCol, 4);
  Pic(shine2, "暗号のメモ（仮素材 score_fragment）", "score_fragment", boxX, boxY - 10, 5);
  shine2.gameObject.SetActive(false); }
// シグル（NPC）：絵はプレイヤーのシグル（Player_Sigle）と同じ（ユーザーの指示 2026-11-20。仮素材 sigle_npc_placeholder は使わない）
{ var t = Obj("シグル（NPC）", 270, 400);
  var spawner = UnityEngine.Object.FindAnyObjectByType<PlayerPartySpawner>(); var sp = (UnityEngine.GameObject)new UnityEditor.SerializedObject(spawner).FindProperty("siglePrefab").objectReferenceValue; var psr = sp.GetComponentInChildren<UnityEngine.SpriteRenderer>();
  var g = NewGO(t, "絵", P(270, 400)); var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = psr.sprite; sr.sharedMaterial = psr.sharedMaterial; sr.sortingLayerID = psr.sortingLayerID; sr.sortingOrder = 98; g.transform.localScale = psr.transform.lossyScale; }

// ---- 歩ける範囲：床（壁の内側）から、ティンパニ・家具を抜く ----
var walk = room.transform.Find("歩ける範囲");
{ var f = walk.Find("床（仮の四角。マスク画像から作ったら消す）"); f.name = "床（壁の内側。仮の四角）"; f.position = P(750, 570); f.GetComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(12.0f, 8.4f); }
System.Action<string, float, float, float> NoCircle = (n, x, y, r) => { var g = NewGO(walk, n, P(x, y)); var c = g.AddComponent<UnityEngine.CircleCollider2D>(); c.isTrigger = true; c.radius = r; g.AddComponent<WalkAreaExclude>(); };
System.Action<string, float, float, float, float> NoBox = (n, x, y, w, h) => { var g = NewGO(walk, n, P(x, y)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(w / 100f, h / 100f); g.AddComponent<WalkAreaExclude>(); };
var tr = new[]{ 100f, 90f, 80f, 70f };   // 直径の目安 200/180/160/140px の半分
for (int i = 0; i < 4; i++) NoCircle("歩けない_" + tname[i], tx[i], 225, tr[i] / 100f);
NoBox("歩けない_机", 720, 620, 180, 120);
NoBox("歩けない_マレット置き場", 560, 630, 60, 50);
NoBox("歩けない_東の本棚（壁まで）", shelfX + 5, shelfY, 110, 180);
NoBox("歩けない_西の譜面箱（壁まで）", boxX - 20, boxY, 150, 80);
// 扉の通路（イベントで開ける。今は閉じている）
{ var g = NewGO(walk, "通路_扉A（閉じている）", P(1395, 630)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(1.0f, 1.4f); g.SetActive(false); }
{ var g = NewGO(walk, "通路_北西の出口（開くと表示）", P(270, 90)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(1.4f, 1.4f); g.SetActive(false); }

// ---- 出現位置：扉A の内側（Stage1_2 から） ----
{ var sp = room.transform.Find("出現位置（Stage1_3_Entrance）"); sp.name = "出現位置_Stage1_2 から（扉Aの内側）"; sp.position = P(1280, 630); SetProp(sp.GetComponent<PlayerSpawnPoint>(), "spawnPointId", "FromStage1_2"); }
foreach (var n in new[]{ "Spawner/PlayerPartySpawner/NoahSpaenPoint", "Spawner/PlayerPartySpawner/SigleSpawnPoint" }) { var g = UnityEngine.GameObject.Find(n); if (g != null) g.transform.position = P(1280, 630); }
// T字通路（Stage1_4）から戻ってきたとき：北西の出口の内側（11-20）
{ var g = NewGO(room.transform, "出現位置_Stage1_4 から（北西の出口の内側）", P(270, 180)); SetProp(g.AddComponent<PlayerSpawnPoint>(), "spawnPointId", "FromStage1_4"); }

// ---- 合流前：シグルは操作キャラとしては出さない（合流フラグ）----
{ var so = new UnityEditor.SerializedObject(UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>()); Log.Append("sigleJoinedFlag=").Append(so.FindProperty("sigleJoinedFlag").objectReferenceValue).Append("\n"); }
UnityEngine.Camera.main.backgroundColor = UnityEngine.Color.black;

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");

// ---- Build Settings（Stage1_2 の次）・場所の名前 ----
var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!list.Exists(x => x.path == ScenePath)) { int idx = list.FindIndex(x => x.path == "Assets/Scenes/Stage1_2.unity"); list.Insert(idx + 1, new UnityEditor.EditorBuildSettingsScene(ScenePath, true)); UnityEditor.EditorBuildSettings.scenes = list.ToArray(); Log.Append("added to build settings\n"); }
var table = UnityEngine.Resources.Load<PlaceNameTable>(PlaceNameTable.ResourcePath);
if (table != null && PlaceNameTable.GetName("Stage1_3") == "Stage1_3") { table.AddForEditor("Stage1_3", "書斎"); UnityEditor.EditorUtility.SetDirty(table); UnityEditor.AssetDatabase.SaveAssets(); Log.Append("place name added\n"); }
Log.Append("place=").Append(PlaceNameTable.GetName("Stage1_3")).Append("\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
