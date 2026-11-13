// asset-audit: survey of empty asset slots, placeholder sprites and data (run by audit.ps1 through Invoke-UnityEval).
// Opens every enabled Build Settings scene (Single) and reopens the original one. Refuses while playing or with unsaved changes.
// Output: %TEMP%\ct_asset_survey.txt (SLOT / SPRITE / UIIMG / AUDIOSRC / COMP / SEDATA / EFFECT / ANIM / ITEM / CHARA lines, tab separated)
if (UnityEditor.EditorApplication.isPlaying) return "ABORT playing";
var cur = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (cur.isDirty) return "ABORT dirty " + cur.path;
string curPath = cur.path;
var sb = new System.Text.StringBuilder();
string outPath = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"), "ct_asset_survey.txt");

System.Func<UnityEngine.Object, string> PathOf = o => {
  if (o is UnityEngine.Component c) { string p = c.gameObject.name; for (var t = c.transform.parent; t != null; t = t.parent) p = t.name + "/" + p; return p; }
  return UnityEditor.AssetDatabase.GetAssetPath(o);
};
var watch = new System.Collections.Generic.Dictionary<string, string> {
  { "PPtr<$SeData>", "SE" }, { "PPtr<$AudioClip>", "音" }, { "PPtr<$Sprite>", "絵" }, { "PPtr<$EffectData>", "エフェクト" },
  { "PPtr<$SpriteAnimData>", "アニメ" }, { "PPtr<$Texture2D>", "画像" }, { "PPtr<$TMP_FontAsset>", "フォント" } };

System.Func<System.Type, string, string> LabelOf = (type, field) => {
  for (var t = type; t != null && t != typeof(UnityEngine.MonoBehaviour) && t != typeof(UnityEngine.ScriptableObject); t = t.BaseType) {
    var f = t.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
    if (f == null) continue;
    foreach (var a in f.GetCustomAttributes(true)) { var lf = a.GetType().GetField("label"); if (lf != null) return (string)lf.GetValue(a) + "(" + field + ")"; }
    return field;
  }
  return field;
};

// empty slots on an object
System.Action<UnityEngine.Object, string> Scan = (o, where) => {
  if (o == null) return;
  var so = new UnityEditor.SerializedObject(o);
  var p = so.GetIterator(); bool enter = true;
  while (p.NextVisible(enter)) {
    enter = true;
    if (p.propertyType != UnityEditor.SerializedPropertyType.ObjectReference) continue;
    string kind; if (!watch.TryGetValue(p.type, out kind)) continue;
    var v = p.objectReferenceValue;
    string val = v == null ? "EMPTY" : UnityEditor.AssetDatabase.GetAssetPath(v) + (v.name.Length > 0 ? "#" + v.name : "");
    // ignore array elements' duplicates: keep path
    sb.Append("SLOT\t" + where + "\t" + o.GetType().Name + "\t" + PathOf(o) + "\t" + kind + "\t" + p.propertyPath + "\t" + (v == null ? "EMPTY" : val) + "\n");
  }
};

// 1) data assets
foreach (var folder in new[]{ "Assets/Data", "Assets/Resources" }) {
  foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[]{ folder })) {
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var o = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
    if (o is SeData se) sb.Append("SEDATA\t" + path + "\t" + (se.HasClip ? "OK" : "NOCLIP") + "\n");
    if (o is EffectData ef) sb.Append("EFFECT\t" + path + "\t" + (ef.IsValid ? "OK" : "INVALID") + "\t" + ef.DisplayMode + "\t" + (ef.HasFrames ? ef.Frames.Length + "f " + (ef.Frames[0] != null ? UnityEditor.AssetDatabase.GetAssetPath(ef.Frames[0]) : "") : "") + "\n");
    if (o is SpriteAnimData an) { sb.Append("ANIM\t" + path); foreach (var c in an.Clips) { UnityEngine.Sprite f0 = c.down.Length > 0 ? c.down[0] : (c.right.Length > 0 ? c.right[0] : null); sb.Append("\t" + c.name + ":" + (c.HasAnyFrames ? "OK" : "EMPTY") + (f0 != null ? "=" + UnityEditor.AssetDatabase.GetAssetPath(f0) : "")); } sb.Append("\n"); }
    if (o is ItemData it) sb.Append("ITEM\t" + path + "\t" + it.DisplayName + "\t" + (it.Icon != null ? UnityEditor.AssetDatabase.GetAssetPath(it.Icon) : "NOICON") + "\n");
    if (o is CharacterProfile cp) { sb.Append("CHARA\t" + path + "\t" + cp.DisplayName); var so = new UnityEditor.SerializedObject(cp); var ex = so.FindProperty("expressions"); for (int i = 0; i < ex.arraySize; i++) { var e = ex.GetArrayElementAtIndex(i); sb.Append("\t" + e.FindPropertyRelative("expression").enumDisplayNames[e.FindPropertyRelative("expression").enumValueIndex] + ":" + (e.FindPropertyRelative("sprite").objectReferenceValue != null ? "OK" : "EMPTY")); } sb.Append("\n"); }
    if (o is EnemyData) Scan(o, "DATA");
    if (o.GetType().Name == "UISoundSettings" || o.GetType().Name == "BarkSettings" || o.GetType().Name == "PartyHintSettings" || o.GetType().Name == "PuzzleUISettings") Scan(o, "DATA");
    if (o is AttackData) Scan(o, "DATA");
  }
}
// prefabs under Resources and Prefab
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[]{ "Assets/Resources", "Assets/Prefab" })) {
  var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
  var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
  if (path.Contains("/Players/")) { var seen = new System.Collections.Generic.HashSet<string>(); foreach (var mb in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (mb != null && seen.Add(mb.GetType().Name)) sb.Append("COMP\tPREFAB " + System.IO.Path.GetFileName(path) + "\t" + mb.GetType().Name + "\n"); }
  foreach (var mb in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) { if (mb == null) continue; if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("UnityEngine")) continue; if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("TMPro")) continue; Scan(mb, "PREFAB " + path); }
  foreach (var sr in go.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true)) sb.Append("SPRITE\tPREFAB " + path + "\t" + PathOf(sr) + "\t" + (sr.sprite != null ? UnityEditor.AssetDatabase.GetAssetPath(sr.sprite) + "#" + sr.sprite.name : "NONE") + "\n");
}
// 2) build scenes
foreach (var s in UnityEditor.EditorBuildSettings.scenes) {
  if (!s.enabled) continue;
  var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(s.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
  string sn = scene.name;
  var seenTypes = new System.Collections.Generic.HashSet<string>();
  foreach (var mb in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include)) {
    if (mb != null && seenTypes.Add(mb.GetType().Name)) sb.Append("COMP\tSCENE " + sn + "\t" + mb.GetType().Name + "\n");
    if (mb == null) continue; var ns = mb.GetType().Namespace;
    if (ns != null && (ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro") || ns.StartsWith("Unity"))) continue;
    Scan(mb, "SCENE " + sn);
  }
  foreach (var sr in UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>(UnityEngine.FindObjectsInactive.Include))
    sb.Append("SPRITE\tSCENE " + sn + "\t" + PathOf(sr) + "\t" + (sr.sprite != null ? UnityEditor.AssetDatabase.GetAssetPath(sr.sprite) + "#" + sr.sprite.name : "NONE") + "\n");
  foreach (var img in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(UnityEngine.FindObjectsInactive.Include))
    sb.Append("UIIMG\tSCENE " + sn + "\t" + PathOf(img) + "\t" + (img.sprite != null ? UnityEditor.AssetDatabase.GetAssetPath(img.sprite) + "#" + img.sprite.name : "NONE") + "\n");
  foreach (var src in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsInactive.Include))
    sb.Append("AUDIOSRC\tSCENE " + sn + "\t" + PathOf(src) + "\t" + (src.clip != null ? UnityEditor.AssetDatabase.GetAssetPath(src.clip) : "NONE") + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(curPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
System.IO.File.WriteAllText(outPath, sb.ToString(), new System.Text.UTF8Encoding(false));
return "wrote " + outPath + " lines=" + sb.ToString().Split('\n').Length;
