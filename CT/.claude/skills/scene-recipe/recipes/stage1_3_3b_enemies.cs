// Stage_1_3 段階3：各ティンパニの中心に音楽生物を置く・守りのゲージ
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Stage_1_3.unity") scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
var root = UnityEngine.GameObject.Find("ティンパニ部屋");
if (root == null) return "no root";
var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Enemies/Timpani/EnemyData_Timpani_Creature.asset");
var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("Enemies/Enemy_Base");
foreach (var n in new[]{ "ティンパニ_上", "ティンパニ_左下", "ティンパニ_右下" }) {
  var group = root.transform.Find(n);
  var old = group.Find("音楽生物"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
  var e = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
  e.name = "音楽生物";
  e.transform.SetParent(group, false);
  e.transform.position = group.position;
  var enemy = e.GetComponent<Enemy>();
  var so = new UnityEditor.SerializedObject(enemy); so.FindProperty("data").objectReferenceValue = data; so.FindProperty("saveDefeat").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
  sb.Append(n).Append(" enemy at ").Append(e.transform.position.ToString("F1")).Append("\n");
}
var oldG = root.transform.Find("守りのゲージ"); if (oldG != null) UnityEngine.Object.DestroyImmediate(oldG.gameObject);
var g = new UnityEngine.GameObject("守りのゲージ"); g.transform.SetParent(root.transform, false); g.AddComponent<GuardGauge>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
return sb.ToString();
