// Test_Mapchip の東：マップチップで「時計塔のふもとの、砂に半分埋もれた遺跡と浜辺」を作る（x26〜66・y0〜28、40×28 マス）。mapchip_tiles → mapchip_test のあとに流す。ルート「浜辺と遺跡（Test_Mapchip_Beach）」と「Grid（浜辺と遺跡）」を消して作る（何度でも）
// @helpers
// 置き方のコツ（2026-11-25 に調べた物）をここで使っている：
//  ・層を分ける（海と砂 0 → 床 1 → 隅の影 2 → 床の上の砂 3・影 3（物の影 4）→ 壁の面 4 → 平らな小物 5 →〔キャラ 100・立っている物 100 は Y で前後〕→ 壁の縁 110・111）
//  ・同じ絵の繰り返しを崩す（砂・海・床はランダム反転の Rule Tile）、四角い形を避ける（段々の海岸線・入り江・崩れて砂が入り込んだ部屋・途切れた石畳）
//  ・奥行き（海は沖ほど暗く、壁の面の下に影）、見せ場は 1 つ（奥の部屋の大きな歯車）、小物は濃い所と薄い所を作る（歯車の散らばり）
// マス：1 マス＝64px＝1 ワールド単位。ここの座標 (lx, ly) は、この場所の左下を (0,0) にしたマス（ワールド x = 26 + lx、y = ly）。
const string ScenePath = "Assets/Scenes/Test_Mapchip.unity";
const string Chip = "Assets/Images/Mapchip/";
const int OX = 26, BW = 40, BH = 28;

var scene = OpenSceneSafe(ScenePath);
System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
foreach (var n in new[]{ "浜辺と遺跡（Test_Mapchip_Beach）", "Grid（浜辺と遺跡）" }) { var old = FindRoot(n); if (old != null) { UnityEngine.Object.DestroyImmediate(old); Log.Append("removed ").Append(n).Append("\n"); } }
if (FindRoot("マップチップの試し（Test_Mapchip）") == null) throw new System.Exception("先に mapchip_test を流す");

System.Func<string, UnityEngine.Tilemaps.TileBase> T = n => {
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(Chip + n + ".asset");
  if (t == null) throw new System.Exception("no tile " + n + "（先に mapchip_tiles を流す）"); return t; };
var sand = T("Suna_ランダム反転"); var sea = T("Umi_ランダム反転"); var stone = T("Yuka_ランダム反転");
var face = T("Kabe.1");
var rimD = T("Kabe.2"); var rimU = T("Kabe.2_縁が上"); var rimL = T("Kabe.2_縁が左"); var rimR = T("Kabe.2_縁が右");
var shoreD = T("Umi.2"); var shoreU = T("Umi.2_海が上"); var shoreL = T("Umi.2_海が左"); var shoreR = T("Umi.2_海が右");
var shadeL = T("影_薄い"); var shadeD = T("影_濃い");
UnityEngine.Material litMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
var gear1 = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Chip + "Haguruma.1.png");
var gear2 = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Chip + "Haguruma.2.png");
// 決まった乱数（何度流しても同じ形）
System.Func<int, int, float> Hash = (x, y) => { double v = System.Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453; return (float)(v - System.Math.Floor(v)); };

// ---- Grid と層 ----
var grid = new UnityEngine.GameObject("Grid（浜辺と遺跡）"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(grid, scene);
grid.AddComponent<UnityEngine.Grid>();
System.Func<string, int, UnityEngine.Tilemaps.Tilemap> Layer = (n, order) => {
  var g = NewGO(grid.transform, n, UnityEngine.Vector3.zero); var tm = g.AddComponent<UnityEngine.Tilemaps.Tilemap>();
  var r = g.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>(); r.sortingOrder = order; r.sharedMaterial = litMat; return tm; };
var lBase  = Layer("海と砂", 0);
var lFloor = Layer("床（石畳・部屋）", 1);
var lSand  = Layer("床の上の砂（入り込んだ砂）", 3);   // 隅の影（2）より上＝砂に埋もれた床には影が出ない
var lShade = Layer("影（海の深さ・壁の下）", 3);
var lFace  = Layer("壁の面", 4);
var lRimA  = Layer("壁の縁 A（キャラより手前）", 110);
var lRimB  = Layer("壁の縁 B（角の重ね）", 111);
System.Action<UnityEngine.Tilemaps.Tilemap, int, int, UnityEngine.Tilemaps.TileBase> Put = (tm, lx, ly, t) => tm.SetTile(new UnityEngine.Vector3Int(OX + lx, ly, 0), t);

// ---- 海岸線（段々・入り江）：その列の「波打ち際のマス」の高さ。これより下が海 ----
int[] coast = new int[BW];
for (int lx = 0; lx < BW; lx++) coast[lx] = lx <= 5 ? 6 : lx <= 11 ? 5 : lx <= 15 ? 3 : lx <= 21 ? 4 : lx <= 29 ? 5 : 6;
System.Func<int, int, bool> IsSea = (lx, ly) => lx >= 0 && lx < BW && ly < coast[lx];
for (int lx = 0; lx < BW; lx++) for (int ly = 0; ly < BH; ly++) {
  if (IsSea(lx, ly)) {
    Put(lBase, lx, ly, sea);
    int depth = coast[lx] - ly;                       // 波打ち際からの深さ：沖ほど暗く
    if (depth >= 4) Put(lShade, lx, ly, shadeD); else if (depth == 3) Put(lShade, lx, ly, shadeL);
    continue; }
  // 陸：まわりに海があれば波打ち際（海が下 → 左 → 右 の順。角は「下」で代用）
  if (IsSea(lx, ly - 1)) Put(lBase, lx, ly, shoreD);
  else if (IsSea(lx - 1, ly)) Put(lBase, lx, ly, shoreL);
  else if (IsSea(lx + 1, ly)) Put(lBase, lx, ly, shoreR);
  else Put(lBase, lx, ly, sand);
}

// ---- 遺跡の部屋：外側の四角（左の列 x0〜右の列 x1、床 y0〜y1、壁の面 y1+1〜y1+2、上の縁 y1+3）。縁は外側の辺に描く ----
// doorsB＝下の辺の開いた列、doorsL／doorsR＝左右の辺の開いた行
System.Action<int, int, int, int, int[], int[], int[]> BuildRoom = (x0, x1, y0, y1, doorsB, doorsL, doorsR) => {
  var dB = new System.Collections.Generic.HashSet<int>(doorsB); var dL = new System.Collections.Generic.HashSet<int>(doorsL); var dR = new System.Collections.Generic.HashSet<int>(doorsR);
  for (int x = x0; x <= x1; x++) {
    for (int y = y0; y <= y1; y++) Put(lFloor, x, y, stone);
    // 壁の面の下・壁ぎわの影は、あとで「隅の影（AutoShade）」が自動で塗る
    Put(lFace, x, y1 + 1, face); Put(lFace, x, y1 + 2, face);
    Put(lRimA, x, y1 + 3, rimD);                       // 上の縁（壁の上を上から見た所）
    if (x > x0 && x < x1 && !dB.Contains(x)) Put(lRimA, x, y0, rimD);
  }
  for (int y = y0; y <= y1 + 2; y++) {
    if (!dL.Contains(y)) Put(lRimA, x0, y, rimL);
    if (!dR.Contains(y)) Put(lRimA, x1, y, rimR);
  }
  // 下の角は縁を 2 枚重ねて L 字に
  if (!dB.Contains(x0)) Put(lRimB, x0, y0, rimD);
  if (!dB.Contains(x1)) Put(lRimB, x1, y0, rimD);
  // 手前（下）の壁の外側の面（11-27 段階 1）：見下ろし（3/4）では手前の壁は「上面（縁）＋外側の面」が見える。
  // 縁（y0 の下の端）の真下に、奥の壁と同じ高さ 2 段の面を置き、その下の砂に薄い影。扉の列は開ける
  for (int x = x0; x <= x1; x++) {
    if (dB.Contains(x)) continue;
    Put(lFace, x, y0 - 1, face); Put(lFace, x, y0 - 2, face); Put(lShade, x, y0 - 3, shadeL); }
};
// 部屋 A（西の小部屋）：下の扉 lx11〜12、右の辺の y16〜17 が廊下へ
BuildRoom(8, 16, 14, 20, new[]{ 11, 12 }, new int[0], new[]{ 16, 17 });
// 部屋 B（奥の広間・見せ場）：左の辺の y16〜17 が廊下から、下の辺 lx26〜28 は崩れて開いている
BuildRoom(20, 35, 15, 22, new[]{ 26, 27, 28 }, new[]{ 16, 17 }, new int[0]);
// 段階 2（11-27）：崩れた所の両脇（lx25・29）は壁が 1 段低い（上の段の面と縁を外し、1 段目の上に縁）。開いた所が「切った穴」ではなく崩れた跡に見える
foreach (int x in new[]{ 25, 29 }) {
  lFace.SetTile(new UnityEngine.Vector3Int(OX + x, 14, 0), null); lRimA.SetTile(new UnityEngine.Vector3Int(OX + x, 15, 0), null);
  Put(lRimA, x, 14, rimD); }
// 廊下 A↔B（lx17〜19・床 y16〜17）：部屋と同じ形の壁（11-27 段階 1。前は上下とも細い縁だけで、線に見えた）
//  奥＝面 2 段（y18〜19）＋上面の縁（y20 の下の端）、手前＝上面の縁（y16 の下の端）＋外側の面 2 段（y14〜15）＋その下の影
for (int x = 17; x <= 19; x++) {
  Put(lFloor, x, 16, stone); Put(lFloor, x, 17, stone);
  Put(lFace, x, 18, face); Put(lFace, x, 19, face); Put(lRimA, x, 20, rimD);
  Put(lRimA, x, 16, rimD); Put(lFace, x, 15, face); Put(lFace, x, 14, face); Put(lShade, x, 13, shadeL); }

// ---- 入り込んだ砂（部屋 B の崩れた所から・部屋 A の左下）：床の上に砂を重ねる（ふちはランダムに欠けさせる） ----
System.Action<float, float, float> SandBlob = (cx, cy, r) => {
  for (int x = (int)(cx - r - 1); x <= (int)(cx + r + 1); x++) for (int y = (int)(cy - r - 1); y <= (int)(cy + r + 1); y++) {
    float d = UnityEngine.Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
    if (d < r - Hash(x, y) * 1.1f && lFloor.HasTile(new UnityEngine.Vector3Int(OX + x, y, 0))) Put(lSand, x, y, sand); } };
SandBlob(27.5f, 15.2f, 3.2f);
// 段階 2（11-27）：部屋 A の左下の砂はやめた（崩れていない部屋の隅に四角い砂があり、遺跡の暗い色味で床の穴に見えた）。
// 床の上の砂は少し暗く・暖かくして、明るい穴ではなく「積もった砂」に見せる
lSand.color = new UnityEngine.Color(0.8f, 0.74f, 0.64f, 1f);

// ---- 石畳の道：西の入口（y8〜9）から東へ → 北へ曲がって部屋 A の扉へ。ところどころ砂に埋もれて途切れる ----
var path = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
for (int x = 0; x <= 12; x++) { path.Add(new UnityEngine.Vector2Int(x, 8)); path.Add(new UnityEngine.Vector2Int(x, 9)); }
for (int y = 10; y <= 13; y++) { path.Add(new UnityEngine.Vector2Int(11, y)); path.Add(new UnityEngine.Vector2Int(12, y)); }
// 段階 2（11-27）：前は乱数で道の中のマスも抜いていて、曲がり角の先が 1 マス幅・十字のような形になった。
// 道は扉まで 2 マス幅のまま通し、外側の辺だけを数マス欠けさせる（まっすぐな縁を崩す）。東の端は 1 枚だけはみ出して途切れる
var notches = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>{ new UnityEngine.Vector2Int(3, 9), new UnityEngine.Vector2Int(4, 9), new UnityEngine.Vector2Int(8, 8), new UnityEngine.Vector2Int(12, 11) };
foreach (var p in path) if (!notches.Contains(p)) Put(lFloor, p.x, p.y, stone);
// 道のまわりに残った敷石：(11-27) 部屋 B の手前の壁の面の下になる (24,13)(25,13)(31,12)、道から離れて浮いていた (14,11)(18,12)、
// 道の辺から飛び出して十字に見えた (5,10)(9,7) を外した。残すのは東の端の 1 枚だけ
Put(lFloor, 13, 9, stone);

// ---- 崩れた壁のかけら（何もない砂浜に目印を置く）：壁の面 1 段＋上面の縁。歩けない形はあとで ----
// 11-27 段階 1：面の段の左右にも縁を描いていた（上面ではない所に上面が出て、線で囲んだように見えた）のをやめた。
// lx30〜31 のかけらは部屋 B の手前の壁の面（y13〜14）とくっつくので y11 → y9 へ
var fragments = new System.Collections.Generic.List<int[]>{ new[]{ 2, 4, 22 }, new[]{ 30, 31, 9 }, new[]{ 37, 38, 18 }, new[]{ 15, 17, 25 }, new[]{ 4, 5, 15 } };
foreach (var f in fragments) {
  for (int x = f[0]; x <= f[1]; x++) { Put(lFace, x, f[2], face); Put(lRimA, x, f[2] + 1, rimD); Put(lShade, x, f[2] - 1, shadeL); }
}

// ---- 西の端（lx0）は崖の縁。y8〜9 だけ試しの場所からの通り道（x24〜25 は暗い中の細い通路） ----
for (int ly = 0; ly < BH; ly++) if (ly != 8 && ly != 9) Put(lRimA, 0, ly, rimL);
for (int x = -2; x <= -1; x++) { Put(lFloor, x, 8, stone); Put(lFloor, x, 9, stone); Put(lRimA, x, 10, rimD); Put(lRimA, x, 7, rimU); }

foreach (var tm in new[]{ lBase, lFloor, lSand, lShade, lFace, lRimA, lRimB }) tm.CompressBounds();

// ---- C 隅の影（AutoShade）：床のうち壁ぎわ・壁の面の下を薄く、角を濃く（自動で塗る） ----
var lAO = Layer("隅の影（自動・AutoShade）", 2);
{ var ao = lAO.gameObject.AddComponent<AutoShade>();
  ao.floors = new[]{ lFloor }; ao.walls = new[]{ lFace, lRimA, lRimB }; ao.lightShade = shadeL; ao.darkShade = shadeD; ao.shadeSides = true;
  Log.Append("auto shade cells=").Append(AutoShadeEditor.Rebuild(ao, false)).Append("\n"); }

// ---- A 壁の影：壁の面（崩れた壁のかけらも）から、明かりをさえぎる形（Shadow Caster 2D）を作る ----
Log.Append("shadow casters=").Append(MapLookMenu.BuildShadowCasters(lFace, false)).Append("\n");
Log.Append("layers bounds base=").Append(lBase.cellBounds).Append(" floor=").Append(lFloor.cellBounds).Append("\n");

// ---- ルート（部屋の型）・歩ける範囲・カメラの範囲・出現位置 ----
var room = RoomMenu.Build(RoomKind.Normal, null, "Test_Mapchip_Beach");
room.name = "浜辺と遺跡（Test_Mapchip_Beach）";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, scene);
var center = new UnityEngine.Vector3(OX + BW / 2f, BH / 2f, 0f);
room.transform.position += center - room.transform.position;
{ var r = room.GetComponent<Room>(); var so = new UnityEditor.SerializedObject(r); var p = so.FindProperty("displayName"); if (p != null) { p.stringValue = "浜辺と遺跡（マップチップの見本）"; so.ApplyModifiedPropertiesWithoutUndo(); } }
var walk = room.transform.Find("歩ける範囲");
UnityEngine.Object.DestroyImmediate(walk.Find("床（仮の四角。マスク画像から作ったら消す）").gameObject);
System.Action<string, float, float, float, float, bool> Box = (n, xMin, yMin, xMax, yMax, exclude) => {
  var g = NewGO(walk, n, new UnityEngine.Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f));
  var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(xMax - xMin, yMax - yMin);
  if (exclude) g.AddComponent<WalkAreaExclude>(); };
// 陸：海岸線の段ごとに 1 つ（波打ち際のマスは砂の側＝上半分だけ）。隣の段のほうが高い側は、その高さまで 0.45 内側に
int segStart = 0;
for (int lx = 1; lx <= BW; lx++) {
  if (lx < BW && coast[lx] == coast[segStart]) continue;
  int a = segStart, b = lx - 1, c = coast[a];
  int leftC = a > 0 ? coast[a - 1] : c, rightC = b < BW - 1 ? coast[b + 1] : c;
  int top = System.Math.Max(c, System.Math.Max(leftC, rightC));   // ここより上は左右も陸
  float edge = a == 0 ? OX + 0.45f : OX + a;                       // 西の崖の縁
  float xMin = a == 0 ? edge : OX + a + (leftC > c ? 0.45f : 0f), xMax = OX + b + 1 - (rightC > c ? 0.45f : 0f);
  if (top > c) Box("陸 lx" + a + "〜" + b + "（下・横に海）", xMin, c + 0.55f, xMax, top + 0.6f, false);
  Box("陸 lx" + a + "〜" + b, edge, top + 0.55f, OX + b + 1, BH - 0.3f, false);
  segStart = lx;
}
Box("通り道（試しの場所から）", 23.9f, 8.1f, OX + 1f, 9.9f, false);   // 試しの場所の通り道（x23〜24）と重ねてつなぐ
// 壁は細い帯だけ抜く（部屋の中は陸の四角に入っている）
// 歩ける範囲はキャラの「位置」（体の中心）で決まるが、絵は中心から下へ 0.61（シグル 0.64）まで出る。
// 上下の壁は、足が縁より下へはみ出ないように、縁から FEET（足の分）だけ離して止める（11-25 の不具合：壁の上側を歩くと足が壁の面にはみ出た）。
// 左右の壁は、体の半分の幅（0.29）が縁の太さ（0.33）に収まるので、縁の太さ＋少しのまま。
const float FEET = 0.7f;
const float FRONT_FEET = 0.61f;   // 手前の壁の面の前に立つとき：足（位置の 0.61 下）が面の下の端に来るまで

System.Action<int, int, int, int, int[], int[], int[]> RoomWalls = (x0, x1, y0, y1, doorsB, doorsL, doorsR) => {
  float X0 = OX + x0, X1 = OX + x1 + 1;
  Box("壁 面と上の縁", X0, y1 + 1, X1, y1 + 3 + FEET + 0.05f, true);   // 上の縁の下の端（y1+3）から足の分だけ上まで
  // 下の辺（扉の所は開ける）
  int run = -1;
  for (int x = x0; x <= x1 + 1; x++) {
    bool open = x > x1 || System.Array.IndexOf(doorsB, x) >= 0;
    if (!open && run < 0) run = x;
    // 部屋の中から：足が下の縁より外へ出ない／外から：足が手前の壁の面（y0-2〜y0）に乗らない（面の下の端から足の分 0.61 上まで歩ける）
    if (open && run >= 0) { Box("壁 手前（面と縁）", OX + run, y0 - 2 + FRONT_FEET, OX + x, y0 + FEET + 0.05f, true); run = -1; } }
  // 左右の辺（扉の行は開ける）
  foreach (var side in new[]{ 0, 1 }) {
    int[] doors = side == 0 ? doorsL : doorsR; float xa = side == 0 ? X0 : X1 - 0.45f; run = -1;
    for (int y = y0; y <= y1 + 1; y++) {
      bool open = y > y1 || System.Array.IndexOf(doors, y) >= 0;
      if (!open && run < 0) run = y;
      if (open && run >= 0) { Box(side == 0 ? "壁 左の縁" : "壁 右の縁", xa, run, xa + 0.45f, y, true); run = -1; } } }
};
RoomWalls(8, 16, 14, 20, new[]{ 11, 12 }, new int[0], new[]{ 16, 17 });
RoomWalls(20, 35, 15, 22, new[]{ 26, 27, 28 }, new[]{ 16, 17 }, new int[0]);
foreach (var f in fragments) Box("壁のかけら lx" + f[0], OX + f[0], f[2] - 0.1f, OX + f[1] + 1, f[2] + 1 + FEET + 0.05f, true);   // 上の縁（f2+1 の下の端）から足の分だけ上まで
Box("廊下 奥の壁（面と縁）", OX + 17, 18f, OX + 20, 20f + FEET + 0.05f, true);        // 面（y18〜20）と上面の縁（y20 の下の端）。外から：足が縁より下へ出ない
Box("廊下 手前の壁（面と縁）", OX + 17, 14f + FRONT_FEET, OX + 20, 16f + FEET + 0.05f, true);   // 中から：足が縁（y16〜16.33）より下へ出ない／外から：面（y14〜16）に乗らない

// ---- 歯車：見せ場（部屋 B の奥）は大きく立てて重ねる（キャラと Y で前後）。ほかは小さく平らに散らす ----
// 立っている物は「奥行きのある物」（DepthProp）にする：足元の当たり判定は四角でざっくり（11-25 ユーザーの指示：丸く細かい形は挙動がおかしくなるもと）、
// キャラとの前後は Sorting Group の位置（前後の境目）で決まる。
var props = NewGO(room.transform, "歯車（見せ場と散らばり）", UnityEngine.Vector3.zero).transform;
System.Func<string, UnityEngine.Sprite, float, float, float, float, bool, UnityEngine.Color, UnityEngine.GameObject> Gear = (n, spr, x, y, scale, rot, standing, col) => {
  // ドット絵は 90 度ごとの回転と反転だけにする（斜めに回すと画素がゆがむ）。rot の 90 の倍数に丸め、端数の向きで左右反転を決める
  float r90 = UnityEngine.Mathf.Round(rot / 90f) * 90f; bool flip = UnityEngine.Mathf.Repeat(rot, 90f) > 45f;
  var g = NewGO(props, n, new UnityEngine.Vector3(x, y, 0f)); g.transform.localScale = new UnityEngine.Vector3(scale, scale, 1f); g.transform.rotation = UnityEngine.Quaternion.Euler(0, 0, r90);
  var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = spr; sr.sharedMaterial = litMat; sr.color = col; sr.sortingOrder = standing ? 100 : 5; sr.flipX = flip;
  // B 影を付ける（DropShadow2D：同じ形を黒く半透明にして右下へずらす。物は影がないと浮いて見える）。キャラより下に描く
  var ds = g.AddComponent<DropShadow2D>();
  SetProp(ds, "strength", standing ? 0.32f : 0.22f);
  SetProp(ds, "offset", new UnityEngine.Vector2(0.08f * scale, -(standing ? 0.14f : 0.06f) * scale));
  SetProp(ds, "shadowOrder", 4);
  // A 立っている物は明かりをさえぎる（8 角形の Shadow Caster 2D。見た目の影なので、ざっくりでよい）
  if (standing) MapLookMenu.AddCircle(g, scale * 0.42f, 8);
  return g; };
var white = UnityEngine.Color.white; var dull = new UnityEngine.Color(0.82f, 0.78f, 0.72f, 1f); var dim = new UnityEngine.Color(0.7f, 0.66f, 0.6f, 1f);
// 見せ場：大きな歯車の残骸（3 枚を重ねる）
var gBig = Gear("見せ場 大歯車", gear1, OX + 30.5f, 20.2f, 4.2f, 12f, true, white);
var gMid = Gear("見せ場 中歯車", gear2, OX + 26.8f, 19.4f, 2.4f, -24f, true, dull);
var gSmall = Gear("見せ場 小歯車", gear1, OX + 33.4f, 17.4f, 1.3f, 40f, true, dull);
// 見せ場の「奥行きのある物」（DepthProp）：大と中は重なっているので 1 つにまとめる（当たり判定 1 つ・前後の境目 1 本）。小は別。
// 赤い四角（足元の当たり判定）＝見た目の幅 7 割 × 下半分、青い線（前後の境目）＝四角の上の端から 0.25 下（11-25：同じ高さだと前後が入れ替わった）
System.Action<DepthProp> LogProp = p => { UnityEngine.Rect r; float line; p.TryGetFootprint(out r); p.TryGetSortLine(out line); Log.Append("depth ").Append(p.name).Append(" footprint=").Append(r).Append(" line=").Append(line.ToString("0.##")).Append("\n"); };
LogProp(DepthPropEditor.Wrap(new[]{ gBig, gMid }, "見せ場 大歯車と中歯車（奥行き）", false));
LogProp(DepthPropEditor.Wrap(new[]{ gSmall }, "見せ場 小歯車（奥行き）", false));
// 段階 2（11-27）：部屋 A にも見せ場を 1 つ（奥の壁に立てかけた中くらいの歯車。たいまつの明かりが当たる）
var gA = Gear("部屋 A の見せ場 立てかけた歯車", gear2, OX + 10.4f, 20.1f, 1.7f, 0f, true, white);
LogProp(DepthPropEditor.Wrap(new[]{ gA }, "部屋 A の見せ場（奥行き）", false));

// ---- 段階 2（11-27）がれき：壁の色に近い小さなかけら（Kabe.1 を小さくした絵）を、壁・崩れた所の近くに「落ちた」ように散らす ----
// 崩れた辺ほど多く、1 種類を固めない・線や四角に並べない（崖の作り方の解説）。平らな小物（並び順 5）なので歩ける
var rubble = NewGO(props, "がれき（壁のかけら）", UnityEngine.Vector3.zero).transform;
int rubbleCount = 0;
System.Action<float, float, int, float> Rubble = (cx, cy, n, spread) => {
  for (int i = 0; i < n; i++) {
    float hx = Hash((int)(cx * 10) + i * 7, (int)(cy * 10) + i * 13), hy = Hash((int)(cy * 10) + i * 11, (int)(cx * 10) - i * 5);
    float x = OX + cx + (hx - 0.5f) * 2f * spread, y = cy + (hy - 0.5f) * 2f * spread * 0.6f;
    float s = 0.16f + Hash(i * 31, (int)(cx * 7)) * 0.2f;
    var g = NewGO(rubble, "がれき", new UnityEngine.Vector3(x, y, 0f));
    g.transform.localScale = new UnityEngine.Vector3(s, s * (0.7f + hy * 0.4f), 1f);
    g.transform.rotation = UnityEngine.Quaternion.Euler(0, 0, hx > 0.5f ? 90f : 0f);
    var sr = g.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Chip + "Kabe.1.png");
    sr.sharedMaterial = litMat; sr.sortingOrder = 5; float c = 0.72f + hx * 0.18f; sr.color = new UnityEngine.Color(c, c * 0.97f, c * 0.93f, 1f);
    var ds = g.AddComponent<DropShadow2D>(); SetProp(ds, "strength", 0.25f); SetProp(ds, "offset", new UnityEngine.Vector2(0.03f, -0.04f)); SetProp(ds, "shadowOrder", 4);
    rubbleCount++; } };
Rubble(25.6f, 12.3f, 5, 0.9f);    // 部屋 B の崩れた所の左（外へこぼれる。面の下の端 y13 より下）
Rubble(29.4f, 12.2f, 4, 0.8f);    // 崩れた所の右
Rubble(27.6f, 16.4f, 3, 1.1f);    // 崩れた所の内側（砂の上）
Rubble(22.3f, 22.3f, 3, 0.6f);    // 部屋 B の奥の壁ぎわ（左）
Rubble(34.2f, 18.2f, 2, 0.5f);    // 部屋 B の右の壁ぎわ
Rubble(9.2f, 14.6f, 2, 0.5f);     // 部屋 A の左下の隅
foreach (var f in fragments) Rubble(f[0] + (f[1] - f[0] + 1) * 0.5f + 0.4f, f[2] - 0.55f, 3, 0.8f);   // 壁のかけらの足もと
Log.Append("rubble=").Append(rubbleCount).Append("\n");

// 遺跡の入口の脇に倒れた歯車（立てる）
var gGate = Gear("入口の脇の歯車", gear2, OX + 6.2f, 12.6f, 1.6f, 8f, true, dull);
LogProp(DepthPropEditor.Wrap(new[]{ gGate }, "入口の脇の歯車（奥行き）", false));
// 部屋 A の隅にまとまって転がる歯車（濃い所）
Gear("部屋 A 転がる歯車 1", gear2, OX + 14.6f, 19.3f, 0.9f, 30f, false, dull);
Gear("部屋 A 転がる歯車 2", gear1, OX + 15.5f, 18.6f, 0.6f, -50f, false, dim);
Gear("部屋 A 転がる歯車 3", gear1, OX + 13.8f, 18.4f, 0.45f, 75f, false, dim);
// 砂浜にまばらに（薄い所）。波打ち際に打ち上げられた物も
Gear("砂浜の歯車 1", gear1, OX + 18.4f, 9.6f, 0.7f, 22f, false, dim);
Gear("砂浜の歯車 2", gear2, OX + 27.6f, 11.4f, 0.55f, -15f, false, dim);
Gear("砂浜の歯車 3", gear1, OX + 34.2f, 9.2f, 0.9f, 60f, false, dull);
Gear("波打ち際の歯車", gear2, OX + 14.2f, 4.0f, 0.5f, 100f, false, dim);
Gear("道ばたの歯車", gear1, OX + 9.8f, 10.9f, 0.4f, -35f, false, dim);

// ---- A 明かり（ゆらぐ）・E 場所の色味 ----
var look = NewGO(room.transform, "見た目（明かり・場所の色味）", UnityEngine.Vector3.zero).transform;
var torch = new UnityEngine.Color(1f, 0.62f, 0.3f); var seaGlow = new UnityEngine.Color(0.6f, 0.9f, 1f);
// 段階 4（11-27）：光だまりを部屋ごとに絞る。見せ場のそばだけ強く、ほかのたいまつは弱く小さく（どこを見ればよいかわかる）
foreach (var t in new[]{ new UnityEngine.Vector4(OX + 12.5f, 20.2f, 1.3f, 5f), new UnityEngine.Vector4(OX + 22.6f, 22.2f, 0.8f, 3.8f), new UnityEngine.Vector4(OX + 34.4f, 22.2f, 0.8f, 3.8f), new UnityEngine.Vector4(OX + 18.5f, 17f, 0.7f, 3.5f) }) {
  var l = MapLookMenu.CreateLight("たいまつの明かり", new UnityEngine.Vector2(t.x, t.y), torch, t.z, t.w, true); l.transform.SetParent(look, true); }
// 部屋 B の見せ場（大きな歯車）に上から差す光だまり（絵の明かり＝影を作らない・軽い）
{ var l = MapLightSpriteMaker.CreatePool("見せ場の光だまり（部屋 B）", new UnityEngine.Vector3(OX + 30.5f, 19.2f, 0f), new UnityEngine.Color(1f, 0.84f, 0.6f), 0.9f, 4f); l.transform.SetParent(look, true); }
// 部屋 A：奥の壁の高い窓から、床に窓枠の形の光が落ちる（型抜き）
{ var l = MapLightSpriteMaker.CreateWindow("窓の光（部屋 A）", new UnityEngine.Vector3(OX + 13.6f, 18.4f, 0f), new UnityEngine.Color(0.85f, 0.92f, 1f), 0.7f, 1.6f); l.transform.SetParent(look, true); }
foreach (var p in new[]{ new UnityEngine.Vector2(OX + 6f, 2.2f), new UnityEngine.Vector2(OX + 19f, 1.2f), new UnityEngine.Vector2(OX + 33f, 2.6f) }) {
  var l = MapLookMenu.CreateLight("水面のきらめき", p, seaGlow, 0.35f, 3.5f, true); l.transform.SetParent(look, true);
  var lr = l.GetComponent<UnityEngine.Rendering.Universal.Light2D>(); lr.shadowsEnabled = false;
  var fl = l.GetComponent<LightFlicker>(); SetProp(fl, "strength", 0.45f); SetProp(fl, "speed", 0.7f); SetProp(fl, "radiusStrength", 0.12f); }
System.Action<string, float, float, float, float, UnityEngine.Color, float, int> Tone = (n, xMin, yMin, xMax, yMax, col, inten, prio) => {
  var g = NewGO(look, n, new UnityEngine.Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f));
  var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(xMax - xMin, yMax - yMin);
  var t = g.AddComponent<AreaTone>(); SetProp(t, "color", col); SetProp(t, "intensity", inten); SetProp(t, "priority", prio); };
var dusk = new UnityEngine.Color(1f, 0.86f, 0.72f); var ruin = new UnityEngine.Color(0.55f, 0.62f, 0.88f);
Tone("場所の色味：夕方の浜辺", 23.8f, 0f, OX + BW, BH, dusk, 0.85f, 0);
Tone("場所の色味：遺跡の中（部屋 A）", OX + 8, 14f, OX + 17, 21f, ruin, 0.42f, 1);
Tone("場所の色味：遺跡の中（廊下）", OX + 17, 16f, OX + 20, 18f, ruin, 0.42f, 1);
Tone("場所の色味：遺跡の中（部屋 B）", OX + 20, 15f, OX + 36, 23f, ruin, 0.42f, 1);

// ---- 段階 3（11-27）地面の色むら（並び順 2・床の格子を目立たなくする）・流れる雲の影（並び順 120・遺跡は屋根がないので全体に） ----
{ var area = new UnityEngine.Rect(OX, 0f, BW, BH);
  var tint = MapOverlayMaker.CreateGroundTint(area, 0.45f); tint.transform.SetParent(look, true);
  var cloud = MapOverlayMaker.CreateCloudShadow(area, 0.25f); cloud.transform.SetParent(look, true); }

// カメラの範囲＝この場所と通り道（x24〜66）。出現位置＝道の上（試すときの入口）
var camArea = room.transform.Find("カメラの範囲");
const float camX0 = 23.8f;   // 通り道の端（23.9）まで入れる
camArea.position = new UnityEngine.Vector3((camX0 + OX + BW) / 2f, BH / 2f, 0f);
camArea.GetComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(OX + BW - camX0, BH);
var spawn = room.transform.Find("出現位置（Test_Mapchip_Beach_Entrance）");
spawn.position = new UnityEngine.Vector3(OX + 4.5f, 9f, 0f);
Log.Append("spawn=").Append(spawn.position).Append(" cam=").Append(camArea.position).Append("\n");

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
