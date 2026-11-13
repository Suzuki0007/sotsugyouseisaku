// Stage1_2・Stage1_1・Stage_1_3：イベントの最後に部品「オートセーブ」を足す（何度実行しても1つだけ）
var prev = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (prev.isDirty) return "ABORT: active scene has unsaved changes: " + prev.path;
string prevPath = prev.path;
var sb = new System.Text.StringBuilder();
var targets = new[] {
  new[]{ "Assets/Scenes/Stage1_2.unity", "勝ったとき（追い詰められる" },
  new[]{ "Assets/Scenes/Stage1_1.unity", "OP_はじめ" },
  new[]{ "Assets/Scenes/Stage_1_3.unity", "全部調律できたとき（会話）" },
};
foreach (var t in targets) {
  var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(t[0], UnityEditor.SceneManagement.OpenSceneMode.Single);
  EventSequence seq = null;
  foreach (var s in UnityEngine.Object.FindObjectsByType<EventSequence>(UnityEngine.FindObjectsInactive.Include)) { if (s.name.StartsWith(t[1])) { seq = s; break; } }
  if (seq == null) { sb.Append(t[0] + ": NOT FOUND\n"); continue; }
  var a = seq.GetComponent<AutoSaveEventAction>();
  bool added = false;
  if (a == null) { a = seq.gameObject.AddComponent<AutoSaveEventAction>(); added = true; }
  var so = new UnityEditor.SerializedObject(seq); var list = so.FindProperty("actions");
  bool inList = false; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == a) inList = true;
  if (list.arraySize > 0 && !inList) { list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = a; so.ApplyModifiedPropertiesWithoutUndo(); }
  // order check: last action
  var comps = seq.GetComponents<EventAction>();
  string last = list.arraySize > 0 ? list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue.GetType().Name : comps[comps.Length - 1].GetType().Name;
  string before = list.arraySize > 1 ? list.GetArrayElementAtIndex(list.arraySize - 2).objectReferenceValue.GetType().Name : (comps.Length > 1 ? comps[comps.Length - 2].GetType().Name : "-");
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
  sb.Append(t[0] + ": " + seq.name + " added=" + added + " listCount=" + list.arraySize + " last=" + last + " before=" + before + "\n");
}
if (!string.IsNullOrEmpty(prevPath)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prevPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
