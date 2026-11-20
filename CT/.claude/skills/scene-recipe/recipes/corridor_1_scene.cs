// Stage1_4（T字通路）段階1：シーンを作り（なければ Stage1_3 をコピー）、仮素材を指示書の配置表どおりに置く・歩ける範囲・カメラ・出現位置。ルート「T字通路（Stage1_4）」を消して作るので、続けて corridor_2_contents
// @helpers
// 座標は 2160×1440 の絵のピクセル（左上が 0,0）。100px = 1 マス、絵の中心がワールドの (0,0)。指示書の配置は「左上」なので、ここでは中心に直して置く。
// 本番の絵に差し替えるときは、Karisozai の同じ名前のファイルを上書きする（大きさが変わるなら下の表を直して流し直す）。
const string ScenePath = "Assets/Scenes/Stage1_4.unity";
const string Dir = "Assets/Images/Map/Stage1_4/Karisozai/";

// ---- シーン：なければ Stage1_3（書斎）をコピーして、書斎の物を外す ----
bool created = false;
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ScenePath) == null) {
  var cur0 = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
  if (cur0.isDirty) throw new System.Exception("開いているシーンに保存していない変更があります: " + cur0.path);
  if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Stage1_3.unity", ScenePath)) throw new System.Exception("copy failed");
  created = true;
}
var scene = OpenSceneSafe(ScenePath);
Log.Append("scene=").Append(scene.path).Append(created ? " (copied from Stage1_3)" : "").Append("\n");
System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
foreach (var n in new[]{ "書斎（Stage1_3）", "T字通路（Stage1_4）" }) { var old = FindRoot(n); if (old != null) { UnityEngine.Object.DestroyImmediate(old); Log.Append("removed ").Append(n).Append("\n"); } }
UnityEngine.Material litMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
if (litMat == null) throw new System.Exception("no Sprite-Lit-Default material");

// 左上ピクセル＋大きさ → 中心のワールド座標
System.Func<float, float, UnityEngine.Vector3> P = (x, y) => new UnityEngine.Vector3((x - 1080f) / 100f, (720f - y) / 100f, 0f);
System.Func<float, float, float, float, UnityEngine.Vector3> TL = (x, y, w, h) => P(x + w / 2f, y + h / 2f);
System.Func<string, UnityEngine.Sprite> Spr = f => {
  var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Dir + f + ".png");
  if (s == null) throw new System.Exception("no sprite " + f + "（Texture Type を Sprite にする）");
  if (System.Math.Abs(s.pixelsPerUnit - 100f) > 0.01f) Log.Append("WARN ppu ").Append(f).Append("=").Append(s.pixelsPerUnit).Append("\n");
  return s; };
System.Func<UnityEngine.Transform, string, string, UnityEngine.Vector3, int, UnityEngine.SpriteRenderer> Pic = (parent, n, file, pos, order) => {
  var g = NewGO(parent, n, pos); var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = Spr(file); sr.sortingOrder = order; sr.sharedMaterial = litMat; return sr; };

// ---- 部屋の絵（背景 1 枚）。マップの範囲（MapBounds）もこの絵＝カメラは絵の端で止まる ----
var tmpRoot = new UnityEngine.GameObject("tmp"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tmpRoot, scene);
var bg = Pic(tmpRoot.transform, "背景（仮素材 corridor_background）", "corridor_background_2160x1440", TL(0, 0, 2160, 1440), 0);
bg.gameObject.AddComponent<MapBounds>();
var room = RoomMenu.Build(RoomKind.Normal, bg, "Stage1_4");
room.name = "T字通路（Stage1_4）";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
bg.transform.SetParent(room.transform, true); bg.transform.SetSiblingIndex(0);
UnityEngine.Object.DestroyImmediate(tmpRoot);
{ var ctx = UnityEngine.Object.FindAnyObjectByType<PlayerSceneContext>(); SetProp(ctx, "mapBounds", bg.GetComponent<MapBounds>()); }
{ var r = room.GetComponent<Room>(); var so = new UnityEditor.SerializedObject(r); var p = so.FindProperty("displayName"); if (p != null) { p.stringValue = "T字通路"; so.ApplyModifiedPropertiesWithoutUndo(); } }

// ---- 物（描画順：背景 0 → 扉 3 → 管の震え 4 → 音符の光 5 → ランプ 6・7 → 人物） ----
var objs = NewGO(room.transform, "物", UnityEngine.Vector3.zero).transform;
{ var t = NewGO(objs, "扉（開かない。開く処理はまだ作らない）", TL(990, 0, 180, 180)).transform;
  Pic(t, "絵_閉", "door_closed_180x180", TL(990, 0, 180, 180), 3);
  Pic(t, "音符の光（E3 から）", "door_note_glow_180x180", TL(990, 0, 180, 180), 5).gameObject.SetActive(false); }
Pic(objs, "西の管の震え（西の部屋の成果）", "pipe_vibration_west_2160x1440", TL(0, 0, 2160, 1440), 4).gameObject.SetActive(false);
Pic(objs, "東の管の震え（東の部屋の成果）", "pipe_vibration_east_2160x1440", TL(0, 0, 2160, 1440), 4).gameObject.SetActive(false);
foreach (var side in new[]{ new object[]{ "西のランプ", 988f }, new object[]{ "東のランプ", 1108f } }) {
  var x = (float)side[1]; var t = NewGO(objs, (string)side[0], TL(x, 40, 64, 64)).transform;
  Pic(t, "絵_消", "lamp_off_64x64", TL(x, 40, 64, 64), 6);
  Pic(t, "絵_点", "lamp_on_64x64", TL(x, 40, 64, 64), 7).gameObject.SetActive(false); }
// シグル（NPC）：絵はプレイヤーのシグル（Anim_Sample_Sigle の上・下の1コマ目）。E3 の前は北向き（扉を見ている）、E3 から南向き。すり抜けられる（当たり判定は話しかける範囲だけ）
{ var t = NewGO(objs, "シグル（NPC）", P(1245, 315)).transform;
  var spawner = UnityEngine.Object.FindAnyObjectByType<PlayerPartySpawner>(); var sp = (UnityEngine.GameObject)new UnityEditor.SerializedObject(spawner).FindProperty("siglePrefab").objectReferenceValue; var psr = sp.GetComponentInChildren<UnityEngine.SpriteRenderer>();
  var anim = (SpriteAnimData)new UnityEditor.SerializedObject(sp.GetComponent<SpriteAnimator>()).FindProperty("data").objectReferenceValue;
  var aso = new UnityEditor.SerializedObject(anim);
  System.Func<string, UnityEngine.Sprite> First = prop => { var p = aso.FindProperty(prop); return p != null && p.arraySize > 0 ? (UnityEngine.Sprite)p.GetArrayElementAtIndex(0).objectReferenceValue : null; };
  var up = First("up"); var down = First("down"); if (down == null) down = psr.sprite; if (up == null) { up = down; Substitute("anim:Sigle:up", "シグルの後ろ向き（北向き）の絵", "正面の絵", "Stage1_4 シグル（NPC）の絵_北向き"); }
  foreach (var v in new[]{ new object[]{ "絵_北向き（E3 の前）", up, true }, new object[]{ "絵_南向き（E3 から）", down, false } }) {
    var g = NewGO(t, (string)v[0], P(1245, 315)); var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = (UnityEngine.Sprite)v[1]; sr.sharedMaterial = psr.sharedMaterial; sr.sortingLayerID = psr.sortingLayerID; sr.sortingOrder = 98; g.transform.localScale = psr.transform.lossyScale; g.SetActive((bool)v[2]); } }

// ---- 歩ける範囲：横の通路＋縦の通路（指示書：横 x180〜1980・y180〜540、縦 x810〜1350・y540〜1440） ----
var walk = room.transform.Find("歩ける範囲");
{ var f = walk.Find("床（仮の四角。マスク画像から作ったら消す）"); f.name = "床_横の通路（仮の四角）"; f.position = TL(180, 180, 1800, 360); f.GetComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(18.0f, 3.6f); }
{ var g = NewGO(walk, "床_縦の通路（仮の四角）", TL(810, 520, 540, 920)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(5.4f, 9.2f); }

// ---- 出現位置：入口（書斎から）・西の部屋から・東の部屋から（部屋はまだない＝受け口だけ） ----
{ var sp = room.transform.Find("出現位置（Stage1_4_Entrance）"); sp.name = "出現位置_Stage1_3 から（南の入口）"; sp.position = P(1080, 1305); SetProp(sp.GetComponent<PlayerSpawnPoint>(), "spawnPointId", "FromStage1_3"); }
foreach (var v in new[]{ new object[]{ "出現位置_西の部屋から（受け口）", "FromWestRoom", 360f }, new object[]{ "出現位置_東の部屋から（受け口）", "FromEastRoom", 1800f } }) {
  var g = NewGO(room.transform, (string)v[0], P((float)v[2], 360)); SetProp(g.AddComponent<PlayerSpawnPoint>(), "spawnPointId", (string)v[1]); }
foreach (var n in new[]{ "Spawner/PlayerPartySpawner/NoahSpaenPoint", "Spawner/PlayerPartySpawner/SigleSpawnPoint" }) { var g = UnityEngine.GameObject.Find(n); if (g != null) g.transform.position = P(1080, 1305); }

UnityEngine.Camera.main.backgroundColor = UnityEngine.Color.black;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");

// ---- Build Settings（Stage1_3 の次）・場所の名前 ----
var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!list.Exists(x => x.path == ScenePath)) { int idx = list.FindIndex(x => x.path == "Assets/Scenes/Stage1_3.unity"); list.Insert(idx + 1, new UnityEditor.EditorBuildSettingsScene(ScenePath, true)); UnityEditor.EditorBuildSettings.scenes = list.ToArray(); Log.Append("added to build settings\n"); }
var table = UnityEngine.Resources.Load<PlaceNameTable>(PlaceNameTable.ResourcePath);
if (table != null && PlaceNameTable.GetName("Stage1_4") == "Stage1_4") { table.AddForEditor("Stage1_4", "T字通路"); UnityEditor.EditorUtility.SetDirty(table); UnityEditor.AssetDatabase.SaveAssets(); Log.Append("place name added\n"); }
Log.Append("place=").Append(PlaceNameTable.GetName("Stage1_4")).Append("\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
