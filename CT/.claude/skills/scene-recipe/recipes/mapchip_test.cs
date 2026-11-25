// Test_Mapchip（マップチップの見え方を試すシーン）：なければ Stage1_4 をコピーして、T字通路の物を外し、Mapchip のタイルを並べる・部屋の型（歩ける範囲・カメラの範囲・出現位置）・Pixel Perfect Camera・試すキー。Build Settings には入れない。何度流しても同じ
// @helpers
// マス（タイル）の座標：1 マス＝64px＝1 ワールド単位（PPU 64）。マップは 24×18 マス、左下のマスが (0,0)。
// 並び（下から）：y0〜2 海（Umi.1/Umi.2 の市松）／y3〜15 床（左 x1〜11 砂 Suna.1・右 x12〜22 床 Yuka.1。床の中に砂の 3×3）／y16〜17 上の壁 Kabe.1／x0・x23 横の壁 Kabe.2。歯車 Haguruma.1/2 は飾りの層。
const string ScenePath = "Assets/Scenes/Test_Mapchip.unity";
const string Chip = "Assets/Images/Mapchip/";
const int W = 24, H = 18;

// ---- シーン：なければ Stage1_4 をコピー ----
bool created = false;
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ScenePath) == null) {
  var cur0 = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
  if (cur0.isDirty) throw new System.Exception("開いているシーンに保存していない変更があります: " + cur0.path);
  if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Stage1_4.unity", ScenePath)) throw new System.Exception("copy failed");
  created = true;
}
var scene = OpenSceneSafe(ScenePath);
Log.Append("scene=").Append(scene.path).Append(created ? " (copied from Stage1_4)" : "").Append("\n");
System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
// コピー元の物（T字通路・古い Grid・使っていない MapEntity）と、前に作った物を外す
foreach (var n in new[]{ "T字通路（Stage1_4）", "書斎（Stage1_3）", "Grid", "MapEntity", "マップチップの試し（Test_Mapchip）", "Grid（Mapchip）" }) {
  var old = FindRoot(n); if (old != null) { UnityEngine.Object.DestroyImmediate(old); Log.Append("removed ").Append(n).Append("\n"); } }

System.Func<string, UnityEngine.Tilemaps.TileBase> T = n => {
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(Chip + n + ".asset");
  if (t == null) throw new System.Exception("no tile " + n); return t; };
var suna = T("Suna.1"); var yuka = T("Yuka.1"); var umi1 = T("Umi.1"); var umi2 = T("Umi.2");
var kabe1 = T("Kabe.1"); var kabe2 = T("Kabe.2"); var gear1 = T("Haguruma.1"); var gear2 = T("Haguruma.2");
UnityEngine.Material litMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
if (litMat == null) throw new System.Exception("no Sprite-Lit-Default material");

// ---- Grid と Tilemap（床 0 → 壁 1 → 飾り 2）。当たり判定は付けない（歩ける範囲で決める） ----
var grid = new UnityEngine.GameObject("Grid（Mapchip）"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(grid, scene);
grid.AddComponent<UnityEngine.Grid>();   // セルの大きさ 1・すき間 0（変えない）
System.Func<string, int, UnityEngine.Tilemaps.Tilemap> Layer = (n, order) => {
  var g = NewGO(grid.transform, n, UnityEngine.Vector3.zero); var tm = g.AddComponent<UnityEngine.Tilemaps.Tilemap>();
  var r = g.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>(); r.sortingOrder = order; r.sharedMaterial = litMat; return tm; };
var floor = Layer("床（海・砂・床）", 0);
var wall = Layer("壁", 1);
var deco = Layer("飾り（歯車）", 2);
var rim = Layer("壁の縁（キャラより手前）", 110);
var rimL = T("Kabe.2_縁が左"); var rimR = T("Kabe.2_縁が右");   // mapchip_tiles で作る（先に流す）

for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) {
  var c = new UnityEngine.Vector3Int(x, y, 0);
  if (y <= 2) { floor.SetTile(c, (x + y) % 2 == 0 ? umi1 : umi2); continue; }
  if (y >= 16) { wall.SetTile(c, kabe1); continue; }
  // 左右の壁：Kabe.1 の上に、部屋の側の縁（Kabe.2 は 11-25 に白を透過して「縁」だけの絵になった）。右の壁の y8〜9 は東の浜辺と遺跡（mapchip_beach）への通り道
  if (x == W - 1 && (y == 8 || y == 9)) { floor.SetTile(c, yuka); continue; }
  if (x == 0)     { wall.SetTile(c, kabe1); rim.SetTile(c, rimR); continue; }
  if (x == W - 1) { wall.SetTile(c, kabe1); rim.SetTile(c, rimL); continue; }
  bool sandPatch = x >= 16 && x <= 18 && y >= 6 && y <= 8;   // 床の中の砂の 3×3（頼まれた形）
  floor.SetTile(c, (x <= 11 || sandPatch) ? suna : yuka);
}
// 歯車：上の壁の手前に2つずつ、床の真ん中にも1つずつ
foreach (var p in new[]{ new UnityEngine.Vector3Int(4, 14, 0), new UnityEngine.Vector3Int(5, 14, 0), new UnityEngine.Vector3Int(8, 10, 0) }) deco.SetTile(p, gear1);
foreach (var p in new[]{ new UnityEngine.Vector3Int(15, 14, 0), new UnityEngine.Vector3Int(16, 14, 0), new UnityEngine.Vector3Int(20, 10, 0) }) deco.SetTile(p, gear2);
foreach (var tm in new[]{ floor, wall, deco, rim }) tm.CompressBounds();
Log.Append("tiles floor=").Append(floor.GetUsedTilesCount()).Append(" wall=").Append(wall.GetUsedTilesCount()).Append(" deco=").Append(deco.GetUsedTilesCount()).Append("\n");

// ---- マップの範囲（MapBounds）：プレイヤーの初期化が必ず使う（ないとエラーで一時停止する）。MapBounds は SpriteRenderer の大きさで決まるので、
//      マップと同じ 24×18 の見えない絵（Suna.1 を引き伸ばして透明に）を置き、ステージと同じく「部屋の絵」として部屋の型に渡す ----
//      大きさはシーン全体（この試しの場所 24×18 ＋ 東の浜辺と遺跡〔mapchip_beach〕x26〜66・y0〜28）＝ 66×28
const float TotalW = 66f, TotalH = 28f;
var mapCenter = new UnityEngine.Vector3(W / 2f, H / 2f, 0f);
var tmpRoot = new UnityEngine.GameObject("tmp"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tmpRoot, scene);
var boundsGo = NewGO(tmpRoot.transform, "マップの範囲（見えない絵。シーン全体 66×28 マス）", new UnityEngine.Vector3(TotalW / 2f, TotalH / 2f, 0f));
var boundsSr = boundsGo.AddComponent<UnityEngine.SpriteRenderer>();
boundsSr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Chip + "Suna.1.png");
boundsSr.color = new UnityEngine.Color(1f, 1f, 1f, 0f); boundsSr.sortingOrder = -100; boundsSr.sharedMaterial = litMat;
boundsGo.transform.localScale = new UnityEngine.Vector3(TotalW / boundsSr.sprite.bounds.size.x, TotalH / boundsSr.sprite.bounds.size.y, 1f);
boundsGo.AddComponent<MapBounds>();
Log.Append("map bounds=").Append(boundsSr.bounds.min).Append("〜").Append(boundsSr.bounds.max).Append("\n");

// ---- 部屋の型（歩ける範囲・カメラの範囲・出現位置）。部屋の絵＝上の見えない絵 ----
var room = RoomMenu.Build(RoomKind.Normal, boundsSr, "Test_Mapchip");
room.name = "マップチップの試し（Test_Mapchip）";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
boundsGo.transform.SetParent(room.transform, true); boundsGo.transform.SetSiblingIndex(0);
UnityEngine.Object.DestroyImmediate(tmpRoot);
{ var r = room.GetComponent<Room>(); var so = new UnityEditor.SerializedObject(r); var p = so.FindProperty("displayName"); if (p != null) { p.stringValue = "マップチップの試し"; so.ApplyModifiedPropertiesWithoutUndo(); } }
// 歩ける範囲＝壁の内側の床（x1〜23・y3〜16）。海と壁には入れない
var walkFloor = room.transform.Find("歩ける範囲/床（仮の四角。マスク画像から作ったら消す）");
walkFloor.name = "床（壁の内側。x1〜22・y3〜15 のマス）";
walkFloor.position = new UnityEngine.Vector3(12f, 9.5f, 0f);
walkFloor.GetComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(22f, 13f);
// 東の浜辺と遺跡への通り道（右の壁 x23 の y8〜9 → x24 まで。この先は mapchip_beach の歩ける範囲〔x23.9〜〕とつながる。カメラの範囲 x0〜24 からはみ出さない）
{ var g = NewGO(walkFloor.parent, "通り道（東の浜辺と遺跡へ）", new UnityEngine.Vector3(23.5f, 9f, 0f)); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(1f, 1.8f); }
// カメラの範囲＝マップ全体（24×18。画面の 14.4×10.8 より広いので、歩くとカメラが動く）
var camArea = room.transform.Find("カメラの範囲");
camArea.position = mapCenter;
camArea.GetComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(W, H);
// 出現位置：床の下のほう（砂と床の境目の近く）
var spawn = room.transform.Find("出現位置（Test_Mapchip_Entrance）");
spawn.position = new UnityEngine.Vector3(12f, 5.5f, 0f);
foreach (var n in new[]{ "Spawner/PlayerPartySpawner/NoahSpaenPoint", "Spawner/PlayerPartySpawner/SigleSpawnPoint" }) { var g = UnityEngine.GameObject.Find(n); if (g != null) g.transform.position = spawn.position; }

// ---- シーンの参照：マップの範囲＝見えない絵、Tilemap＝床 ----
{ var ctx = UnityEngine.Object.FindAnyObjectByType<PlayerSceneContext>(); SetProp(ctx, "mapBounds", boundsGo.GetComponent<MapBounds>()); SetProp(ctx, "playerLimitTilemap", floor); }

// ---- カメラ：Pixel Perfect Camera（ステージと同じ見え方＝縦 10.8 マス・4:3）と試すキー ----
var cam = UnityEngine.Camera.main;
cam.backgroundColor = UnityEngine.Color.black;
cam.orthographic = true;
cam.orthographicSize = 5.4f;   // ステージと同じ（Pixel Perfect Camera を切ったとき）
var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
if (ppc == null) ppc = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
ppc.assetsPPU = 64;
ppc.refResolutionX = 920;   // 4:3。縦 690px ÷ 64 = 10.78 マス（ステージの 10.8 とほぼ同じ）
ppc.refResolutionY = 690;
ppc.cropFrame = UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.StretchFill;
ppc.gridSnapping = UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.PixelSnapping;
if (cam.GetComponent<MapchipViewTester>() == null) cam.gameObject.AddComponent<MapchipViewTester>();
Log.Append("ppc ppu=").Append(ppc.assetsPPU).Append(" ref=").Append(ppc.refResolutionX).Append("x").Append(ppc.refResolutionY).Append(" crop=").Append(ppc.cropFrame).Append(" snap=").Append(ppc.gridSnapping).Append("\n");

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
var inBuild = System.Array.Exists(UnityEditor.EditorBuildSettings.scenes, x => x.path == ScenePath);
Log.Append("in build settings=").Append(inBuild).Append("（入れない）\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
