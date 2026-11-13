// Stage_1_3 段階4：すぐ効くアイテムのプレハブ（回復・守り）・各ティンパニの TimpaniUnit・部屋の TimpaniRoom とクリアのフラグ
var sb = new System.Text.StringBuilder();
// ---- placeholder item sprites ----
System.Func<string, System.Func<int, int, UnityEngine.Color>, UnityEngine.Sprite> MakeSprite = (path, paint) => {
  if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path) == null) {
    int n = 64; var tex = new UnityEngine.Texture2D(n, n, UnityEngine.TextureFormat.RGBA32, false);
    for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) tex.SetPixel(x, y, paint(x, y));
    tex.Apply(); System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
    UnityEditor.AssetDatabase.ImportAsset(path);
    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.spritePixelsPerUnit = 80; imp.alphaIsTransparency = true; imp.SaveAndReimport();
  }
  return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
};
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Images/Item")) UnityEditor.AssetDatabase.CreateFolder("Assets/Images", "Item");
// heart (heal)
var heart = MakeSprite("Assets/Images/Item/Item_Heal_kari.png", (x, y) => {
  float u = (x - 31.5f) / 26f, v = (y - 30f) / 26f;
  float h = u * u + v * v - 1f; bool inside = h * h * h - u * u * v * v * v < 0f;
  float u2 = (x - 31.5f) / 30f, v2 = (y - 30f) / 30f; float h2 = u2 * u2 + v2 * v2 - 1f; bool outline = h2 * h2 * h2 - u2 * u2 * v2 * v2 * v2 < 0f;
  if (inside) return (x < 26 && y > 36) ? new UnityEngine.Color(1f, 0.75f, 0.8f, 1f) : new UnityEngine.Color(0.95f, 0.25f, 0.45f, 1f);
  if (outline) return UnityEngine.Color.white;
  return new UnityEngine.Color(0, 0, 0, 0); });
// blue diamond (guard)
var gem = MakeSprite("Assets/Images/Item/Item_Guard_kari.png", (x, y) => {
  float d = UnityEngine.Mathf.Abs(x - 31.5f) + UnityEngine.Mathf.Abs(y - 31.5f);
  if (d < 22) return (x < 30 && y > 32) ? new UnityEngine.Color(0.75f, 0.95f, 1f, 1f) : new UnityEngine.Color(0.25f, 0.65f, 1f, 1f);
  if (d < 27) return UnityEngine.Color.white;
  return new UnityEngine.Color(0, 0, 0, 0); });

// ---- item prefabs ----
string dir = "Assets/Prefab/Gimmicks/Timpani";
System.Func<string, string, UnityEngine.Sprite, int, UnityEngine.GameObject> MakeItem = (file, title, sprite, kind) => {
  string path = dir + "/" + file + ".prefab";
  var go = new UnityEngine.GameObject(title);
  var col = go.AddComponent<UnityEngine.CircleCollider2D>(); col.isTrigger = true; col.radius = 0.45f;
  var vis = new UnityEngine.GameObject("見た目（仮）"); vis.transform.SetParent(go.transform, false);
  var sr = vis.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 150;
  var ip = go.AddComponent<InstantPickup>();
  var so = new UnityEditor.SerializedObject(ip); so.FindProperty("kind").enumValueIndex = kind; so.ApplyModifiedPropertiesWithoutUndo();
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go);
  return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
};
var healPrefab = MakeItem("Item_Heal", "回復アイテム（ハート1つ）", heart, 0);
var guardPrefab = MakeItem("Item_GuardBoost", "守りのアイテム（リチャージが速くなる）", gem, 1);
sb.Append("items ok\n");

// ---- rock drops the guard item ----
var rock = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Enemies/Timpani/Attack_Timpani_Rock.asset");
{ var so = new UnityEditor.SerializedObject(rock); so.FindProperty("spawnOnActive").objectReferenceValue = guardPrefab; so.FindProperty("spawnChance").floatValue = 0.35f;
  so.FindProperty("memo").stringValue = "振動で岩を降らせる：プレイヤーの足元に影（予兆）→ 落ちる。発動で画面が揺れる。弾消しでは消えない（よける）。35% で守りのアイテム（リチャージが速くなる）も落ちてくる"; so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(rock); }

// ---- clear flag (saved) ----
string cfp = "Assets/Data/Flags/Flag_Timpani_Cleared.asset";
var cleared = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(cfp);
if (cleared == null) { cleared = UnityEngine.ScriptableObject.CreateInstance<GameFlag>(); UnityEditor.AssetDatabase.CreateAsset(cleared, cfp); }
{ var so = new UnityEditor.SerializedObject(cleared); so.FindProperty("id").stringValue = "Timpani_Cleared"; so.FindProperty("memo").stringValue = "ティンパニの部屋（Stage_1_3）を全部調律した（クリア）。セーブする"; so.FindProperty("saved").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
UnityEditor.AssetDatabase.SaveAssets();

// ---- scene ----
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var root = UnityEngine.GameObject.Find("ティンパニ部屋");
foreach (var n in new[]{ "ティンパニ_上", "ティンパニ_左下", "ティンパニ_右下" }) {
  var g = root.transform.Find(n).gameObject;
  var u = g.GetComponent<TimpaniUnit>(); if (u == null) u = g.AddComponent<TimpaniUnit>();
  var so = new UnityEditor.SerializedObject(u);
  so.FindProperty("creature").objectReferenceValue = g.transform.Find("音楽生物").GetComponent<Enemy>();
  var pegs = so.FindProperty("pegs"); var list = g.GetComponentsInChildren<TimpaniPeg>(true); pegs.arraySize = list.Length; for (int i = 0; i < list.Length; i++) pegs.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
  so.FindProperty("healItem").objectReferenceValue = healPrefab;
  so.FindProperty("calmEffect").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Effect/Sample/Fx_Sample_Spawn.asset");
  so.FindProperty("clearEffect").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Effect/Sample/Fx_Sample_AttackRing.asset");
  so.ApplyModifiedPropertiesWithoutUndo();
  sb.Append(n).Append(" pegs=").Append(list.Length).Append("\n");
}
var room = root.GetComponent<TimpaniRoom>(); if (room == null) room = root.AddComponent<TimpaniRoom>();
{ var so = new UnityEditor.SerializedObject(room);
  var units = so.FindProperty("units"); var ul = root.GetComponentsInChildren<TimpaniUnit>(true); units.arraySize = ul.Length; for (int i = 0; i < ul.Length; i++) units.GetArrayElementAtIndex(i).objectReferenceValue = ul[i];
  var ef = so.FindProperty("escalationFlags"); ef.arraySize = 2; ef.GetArrayElementAtIndex(0).objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Timpani_Calmed1.asset"); ef.GetArrayElementAtIndex(1).objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Timpani_Calmed2.asset");
  so.FindProperty("clearedFlag").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(cfp);
  so.ApplyModifiedPropertiesWithoutUndo(); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
