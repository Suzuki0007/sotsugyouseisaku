// 夜のマップ（2026-11-19 ユーザーの指示「ステージ1_0 とステージ1_1 を夜に見えるように暗く」）
// ・シーンの全体の明かり（Global Light 2D）を、青みのある月明かりの色・強さ 0.45 にする（昼は白・1）
// ・マップ・物・ノア・シグルは明かりに反応する（Sprite-Lit-Default）。UI は暗くならない
// ・明かりに反応しない波（SpriteFrameBlend）は「シーンの明るさに合わせる」で同じ色になる
// 何度実行しても同じ。2つのシーンを保存する
var sb = new System.Text.StringBuilder();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! unsaved scene: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var night = new UnityEngine.Color(0.5f, 0.6f, 0.9f, 1f);
const float intensity = 0.45f;
foreach (var path in new[] { "Assets/Scenes/Stage1_0.unity", "Assets/Scenes/Stage1_1.unity" })
{
    var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    int n = 0;
    foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(UnityEngine.FindObjectsInactive.Include))
    {
        if (l.lightType != UnityEngine.Rendering.Universal.Light2D.LightType.Global || l.gameObject.scene != s) continue;
        l.color = night; l.intensity = intensity;
        UnityEditor.EditorUtility.SetDirty(l);
        n++;
        sb.Append(s.name + ": " + l.name + " → color " + night + " intensity " + intensity + "\n");
    }
    if (n == 0) sb.Append("!! " + s.name + ": no Global Light 2D\n");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
    sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
}
if (!string.IsNullOrEmpty(start)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
