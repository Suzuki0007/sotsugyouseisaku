// Stage_1_3 段階2：ペグ（各ティンパニに3つ）を置く
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Stage_1_3.unity") scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
var root = UnityEngine.GameObject.Find("ティンパニ部屋");
if (root == null) return "no root";
var roomSr = UnityEngine.GameObject.Find("Grid/soto_yuka").GetComponent<UnityEngine.SpriteRenderer>();
var knob = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/Knob.psd");

// ---- peg prefab (planners can drag it into a scene) ----
string dir = "Assets/Prefab/Gimmicks/Timpani";
if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) UnityEditor.AssetDatabase.CreateFolder("Assets/Prefab/Gimmicks", "Timpani");
string prefabPath = dir + "/Peg.prefab";
{
  var peg = new UnityEngine.GameObject("ペグ");
  var col = peg.AddComponent<UnityEngine.CircleCollider2D>(); col.isTrigger = true; col.radius = 1.2f;
  var vis = new UnityEngine.GameObject("見た目（仮）"); vis.transform.SetParent(peg.transform, false); vis.transform.localScale = new UnityEngine.Vector3(0.7f, 0.7f, 1f);
  var sr = vis.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = knob; sr.color = new UnityEngine.Color(0.95f, 0.3f, 0.2f); sr.sortingLayerID = roomSr.sortingLayerID; sr.sortingOrder = roomSr.sortingOrder + 2;
  var gs = peg.AddComponent<GimmickState>();
  var so = new UnityEditor.SerializedObject(gs); var it = so.GetIterator(); bool en = true;
  while (it.NextVisible(en)) { en = false; if (it.propertyType == UnityEditor.SerializedPropertyType.Boolean && it.name.ToLower().Contains("save")) { it.boolValue = false; sb.Append("GimmickState.").Append(it.name).Append("=false\n"); } }
  so.ApplyModifiedPropertiesWithoutUndo();
  var tp = peg.AddComponent<TimpaniPeg>();
  { var s2 = new UnityEditor.SerializedObject(tp); s2.FindProperty("body").objectReferenceValue = sr; s2.ApplyModifiedPropertiesWithoutUndo(); }
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(peg, prefabPath);
  UnityEngine.Object.DestroyImmediate(peg);
}
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);

// ---- 3 timpani groups, 3 pegs each ----
var centers = new[]{ new UnityEngine.Vector3(-115.23f, 25.04f, 0f), new UnityEngine.Vector3(-120.29f, 17.35f, 0f), new UnityEngine.Vector3(-110.11f, 17.35f, 0f) };
var names = new[]{ "ティンパニ_上", "ティンパニ_左下", "ティンパニ_右下" };
var angles = new[]{ 90f, 210f, 330f };
for (int i = 0; i < 3; i++) {
  var old = root.transform.Find(names[i]); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
  var group = new UnityEngine.GameObject(names[i]); group.transform.SetParent(root.transform, false); group.transform.position = centers[i];
  for (int k = 0; k < 3; k++) {
    var peg = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    peg.name = "ペグ" + (k + 1);
    peg.transform.SetParent(group.transform, true);
    float a = angles[k] * UnityEngine.Mathf.Deg2Rad;
    peg.transform.position = centers[i] + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), UnityEngine.Mathf.Sin(a), 0f) * 3.5f;
    var gs = peg.GetComponent<GimmickState>(); var so = new UnityEditor.SerializedObject(gs); so.FindProperty("displayName").stringValue = names[i] + " ペグ" + (k + 1); so.ApplyModifiedPropertiesWithoutUndo();
  }
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
