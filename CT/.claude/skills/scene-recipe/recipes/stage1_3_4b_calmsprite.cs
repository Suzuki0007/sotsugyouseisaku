// Stage_1_3 段階4：おとなしくなった音楽生物の仮の絵
string path = "Assets/Images/Enemy/Enemy_Timpani_Creature_calm_kari.png";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path) == null) {
  int n = 128; var tex = new UnityEngine.Texture2D(n, n, UnityEngine.TextureFormat.RGBA32, false);
  for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) {
    float dx = x - 63.5f, dy = y - 58f; float r = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy * 1.15f);
    UnityEngine.Color c = new UnityEngine.Color(0, 0, 0, 0);
    if (r < 54) c = new UnityEngine.Color(0.3f, 0.6f, 0.65f, 1f);
    if (r < 50) c = new UnityEngine.Color(0.55f, 0.92f, 0.85f, 1f);
    // closed happy eyes (^ ^)
    foreach (var ex in new[]{ 44f, 84f }) { float ax = UnityEngine.Mathf.Abs(x - ex); if (ax < 10 && UnityEngine.Mathf.Abs((y - 66f) - (10 - ax) * 0.7f) < 2.2f) c = new UnityEngine.Color(0.1f, 0.25f, 0.3f, 1f); }
    // smile
    { float mx = x - 64f; if (UnityEngine.Mathf.Abs(mx) < 13 && UnityEngine.Mathf.Abs((y - 40f) + (169f - mx * mx) * 0.04f) < 2.2f) c = new UnityEngine.Color(0.1f, 0.25f, 0.3f, 1f); }
    // cheeks
    foreach (var ex in new[]{ 34f, 94f }) { float cd = UnityEngine.Mathf.Sqrt((x - ex) * (x - ex) + (y - 52f) * (y - 52f)); if (cd < 6) c = new UnityEngine.Color(1f, 0.7f, 0.75f, 1f); }
    // antenna note
    if (x > 86 && x < 90 && y > 96 && y < 122) c = new UnityEngine.Color(0.1f, 0.25f, 0.3f, 1f);
    { float nd = UnityEngine.Mathf.Sqrt((x - 82f) * (x - 82f) + (y - 98f) * (y - 98f)); if (nd < 7) c = new UnityEngine.Color(0.1f, 0.25f, 0.3f, 1f); }
    tex.SetPixel(x, y, c);
  }
  tex.Apply(); System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
  UnityEditor.AssetDatabase.ImportAsset(path);
  var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
  imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.spritePixelsPerUnit = 64; imp.alphaIsTransparency = true; imp.SaveAndReimport();
}
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
// enemy data: no tint (the calm sprite carries the colour)
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Enemies/Timpani/EnemyData_Timpani_Creature.asset");
{ var so = new UnityEditor.SerializedObject(data); so.FindProperty("remainTint").colorValue = UnityEngine.Color.white; so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(data); UnityEditor.AssetDatabase.SaveAssets(); }
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Stage_1_3.unity") scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
foreach (var u in UnityEngine.Object.FindObjectsByType<TimpaniUnit>(UnityEngine.FindObjectsInactive.Include)) { var so = new UnityEditor.SerializedObject(u); so.FindProperty("calmSprite").objectReferenceValue = sprite; so.ApplyModifiedPropertiesWithoutUndo(); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "saved=" + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
