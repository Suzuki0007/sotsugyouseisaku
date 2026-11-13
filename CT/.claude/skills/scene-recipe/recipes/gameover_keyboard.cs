// SceneGameOver：Canvas に MenuKeyboardNav（リトライ → タイトルへ）
var prev = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (prev.isDirty) return "ABORT: active scene has unsaved changes: " + prev.path;
string prevPath = prev.path;
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SceneGameOver.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
UnityEngine.UI.Button retry = null, title = null;
foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsInactive.Include)) {
  sb.Append(b.name + " / ");
  if (b.name == "Button") retry = b; else if (b.name == "TitleButton") title = b;
}
if (retry == null || title == null) return "buttons not found: " + sb;
var canvas = retry.GetComponentInParent<UnityEngine.Canvas>();
var nav = canvas.GetComponent<MenuKeyboardNav>(); if (nav == null) nav = canvas.gameObject.AddComponent<MenuKeyboardNav>();
var so = new UnityEditor.SerializedObject(nav); var p = so.FindProperty("buttons"); p.arraySize = 2;
p.GetArrayElementAtIndex(0).objectReferenceValue = retry; p.GetArrayElementAtIndex(1).objectReferenceValue = title;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
// layout info
string info = "canvas=" + canvas.name + " retry pos " + retry.GetComponent<UnityEngine.RectTransform>().anchoredPosition + " title pos " + title.GetComponent<UnityEngine.RectTransform>().anchoredPosition;
if (!string.IsNullOrEmpty(prevPath) && prevPath != scene.path) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prevPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
return "OK " + info + " back to " + prevPath;
