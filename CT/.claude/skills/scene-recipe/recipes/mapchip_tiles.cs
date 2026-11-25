// マップチップの追加タイル（アセットだけ。シーンは開かない）：壁の縁 Kabe.2 と波打ち際 Umi.2 の向き違い、砂・海・床のランダム反転（Rule Tile）、影（半透明の黒 2 段）を作り、パレット Mapchip の下の段に足す。何度流しても同じ
// @helpers
// 向き：Kabe.2 は「縁がマスの下」、Umi.2 は「海がマスの下（上が砂）」が元の向き。上は上下反転、左右は 90 度回す（+90＝下の物が右へ）。
const string Dir = "Assets/Images/Mapchip/";
const string Palette = "Assets/Images/Mapchip/Mapchip.prefab";

System.Func<string, UnityEngine.Sprite> Spr = n => {
  var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Dir + n + ".png");
  if (s == null) throw new System.Exception("no sprite " + n); return s; };

// ---- 向き違いのタイル（Tile の Transform で回す。画像は増やさない） ----
System.Func<string, string, UnityEngine.Matrix4x4, UnityEngine.Tilemaps.Tile> Variant = (src, name, m) => {
  var path = Dir + name + ".asset";
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(path);
  if (t == null) { t = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>(); UnityEditor.AssetDatabase.CreateAsset(t, path); }
  t.sprite = Spr(src); t.transform = m; t.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
  t.flags = UnityEngine.Tilemaps.TileFlags.LockColor | UnityEngine.Tilemaps.TileFlags.LockTransform;
  UnityEditor.EditorUtility.SetDirty(t); Log.Append("tile ").Append(name).Append("\n"); return t; };
var flipY = UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(1, -1, 1));
var rotR = UnityEngine.Matrix4x4.Rotate(UnityEngine.Quaternion.Euler(0, 0, 90));    // 下にある物が右へ
var rotL = UnityEngine.Matrix4x4.Rotate(UnityEngine.Quaternion.Euler(0, 0, -90));   // 下にある物が左へ
var kUp = Variant("Kabe.2", "Kabe.2_縁が上", flipY);
var kL  = Variant("Kabe.2", "Kabe.2_縁が左", rotL);
var kR  = Variant("Kabe.2", "Kabe.2_縁が右", rotR);
var uUp = Variant("Umi.2", "Umi.2_海が上", flipY);
var uL  = Variant("Umi.2", "Umi.2_海が左", rotL);
var uR  = Variant("Umi.2", "Umi.2_海が右", rotR);

// ---- ランダムに反転するタイル（Rule Tile：条件なしの規則 1 つ・出力ランダム・上下左右の反転。場所で決まるので毎回同じ見た目） ----
System.Func<string, string, RuleTile> RandomTile = (src, name) => {
  var path = Dir + name + ".asset";
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<RuleTile>(path);
  if (t == null) { t = UnityEngine.ScriptableObject.CreateInstance<RuleTile>(); UnityEditor.AssetDatabase.CreateAsset(t, path); }
  var s = Spr(src);
  t.m_DefaultSprite = s; t.m_DefaultColliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
  var r = new RuleTile.TilingRule();
  r.m_Neighbors = new System.Collections.Generic.List<int>();
  r.m_NeighborPositions = new System.Collections.Generic.List<UnityEngine.Vector3Int>();
  r.m_Sprites = new[]{ s };
  r.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random;
  r.m_RandomTransform = RuleTile.TilingRuleOutput.Transform.MirrorXY;
  r.m_PerlinScale = 0.5f;
  r.m_ColliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
  t.m_TilingRules = new System.Collections.Generic.List<RuleTile.TilingRule>{ r };
  UnityEditor.EditorUtility.SetDirty(t); Log.Append("rule tile ").Append(name).Append("\n"); return t; };
var sunaR = RandomTile("Suna.1", "Suna_ランダム反転");
var umiR  = RandomTile("Umi.1", "Umi_ランダム反転");
var yukaR = RandomTile("Yuka.1", "Yuka_ランダム反転");
// ---- 影（半透明の黒い四角）：どの絵でも黒をかければ同じ黒になるので Suna.1 を使う。壁の下・海の深い所に重ねる ----
System.Func<string, float, UnityEngine.Tilemaps.Tile> Shade = (name, a) => {
  var t = Variant("Suna.1", name, UnityEngine.Matrix4x4.identity);
  t.color = new UnityEngine.Color(0f, 0f, 0f, a); UnityEditor.EditorUtility.SetDirty(t); return t; };
var shadeL = Shade("影_薄い", 0.2f);
var shadeD = Shade("影_濃い", 0.36f);
UnityEditor.AssetDatabase.SaveAssets();

// ---- パレットの下の段に足す（今あるタイルは動かさない） ----
var root = UnityEditor.PrefabUtility.LoadPrefabContents(Palette);
var tm = root.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
System.Action<int, UnityEngine.Tilemaps.TileBase[]> Row = (y, tiles) => { for (int i = 0; i < tiles.Length; i++) tm.SetTile(new UnityEngine.Vector3Int(-1 + i, y, 0), tiles[i]); };
Row(-4, new UnityEngine.Tilemaps.TileBase[]{ kUp, kL, kR });
Row(-5, new UnityEngine.Tilemaps.TileBase[]{ uUp, uL, uR });
Row(-6, new UnityEngine.Tilemaps.TileBase[]{ sunaR, umiR, yukaR });
Row(-7, new UnityEngine.Tilemaps.TileBase[]{ shadeL, shadeD });
tm.CompressBounds();
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, Palette);
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
Log.Append("palette rows -4..-6 set\n");
return Log.ToString();
