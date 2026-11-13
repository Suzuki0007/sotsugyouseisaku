// Stage_1_3 段階3：丸い弾のプレハブと、Stage_1_3 の弾のプールの差し替え
var sb = new System.Text.StringBuilder();
// ---- note-like bullet sprite ----
string spritePath = "Assets/Images/Enemy/Bullet_Timpani_Note_kari.png";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath) == null) {
  int n = 64; var tex = new UnityEngine.Texture2D(n, n, UnityEngine.TextureFormat.RGBA32, false);
  for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) {
    float dx = x - 31.5f, dy = y - 31.5f; float r = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy);
    UnityEngine.Color c = new UnityEngine.Color(0, 0, 0, 0);
    if (r < 30) c = new UnityEngine.Color(0.95f, 0.75f, 0.2f, 1f);
    if (r < 26) c = new UnityEngine.Color(0.55f, 0.2f, 0.75f, 1f);
    float hd = UnityEngine.Mathf.Sqrt((x - 24f) * (x - 24f) + (y - 40f) * (y - 40f)); if (hd < 6) c = new UnityEngine.Color(1f, 1f, 1f, 0.9f);
    tex.SetPixel(x, y, c);
  }
  tex.Apply();
  System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(spritePath), tex.EncodeToPNG());
  UnityEditor.AssetDatabase.ImportAsset(spritePath);
  var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(spritePath);
  imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.spritePixelsPerUnit = 128; imp.alphaIsTransparency = true; imp.SaveAndReimport();
}
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath);

// ---- prefab variant of the enemy bullet ----
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var pool = UnityEngine.Object.FindAnyObjectByType<EnemyBulletPool>();
var pso = new UnityEditor.SerializedObject(pool);
var basePrefab = pso.FindProperty("bulletPrefab").objectReferenceValue as UnityEngine.Component;
sb.Append("base bullet=").Append(basePrefab != null ? UnityEditor.AssetDatabase.GetAssetPath(basePrefab) : "null").Append("\n");
string variantPath = "Assets/Prefab/Bullet/EnemyBullet_TimpaniNote.prefab";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(variantPath) == null) {
  var inst = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(basePrefab.gameObject);
  foreach (var sr in inst.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true)) { sr.sprite = sprite; sr.color = UnityEngine.Color.white; sb.Append("sr ").Append(sr.name).Append(" scale=").Append(sr.transform.localScale).Append("\n"); }
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(inst, variantPath);
  UnityEngine.Object.DestroyImmediate(inst);
}
var variant = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(variantPath);
pso.FindProperty("bulletPrefab").objectReferenceValue = variant.GetComponent(basePrefab.GetType());
pso.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
