// 夜の設定を光のプリセット「夜」に移す（2026-11-26、指示書 CT_presentation_mood_light_weather_still。ユーザーの許可あり）
// ・Stage1_0・Stage1_1 のルートに「シーンの演出の既定」（ScenePresentation、光＝Light_夜）を置く
// ・Global Light 2D の値（0.5, 0.6, 0.9）×0.45 はそのまま残す（窓口が書く値も同じなので見た目は変わらない）
// ・何度実行しても同じ（あれば作り直す）。2つのシーンを保存する
var sb = new System.Text.StringBuilder();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! unsaved scene: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
foreach (var path in new[] { "Assets/Scenes/Stage1_0.unity", "Assets/Scenes/Stage1_1.unity" })
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    var night = UnityEditor.AssetDatabase.LoadAssetAtPath<LightPreset>("Assets/Resources/Presentation/Light/Light_夜.asset");
    if (night == null) return "!! Light_夜 がない（Tools > 演出 > 最初のプリセットを作る）";
    foreach (var r in s.GetRootGameObjects()) if (r.name == "シーンの演出の既定") UnityEngine.Object.DestroyImmediate(r);
    var go = new UnityEngine.GameObject("シーンの演出の既定");
    var sp = go.AddComponent<ScenePresentation>();
    var so = new UnityEditor.SerializedObject(sp);
    so.FindProperty("lightPreset").objectReferenceValue = night;
    so.ApplyModifiedPropertiesWithoutUndo();
    foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(UnityEngine.FindObjectsInactive.Include))
        if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global && l.gameObject.scene == s)
            sb.Append(s.name + ": Global Light " + l.color + " x " + l.intensity + " / preset " + night.LightColor + " x " + night.Intensity + "\n");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
    sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
}
if (!string.IsNullOrEmpty(start)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
