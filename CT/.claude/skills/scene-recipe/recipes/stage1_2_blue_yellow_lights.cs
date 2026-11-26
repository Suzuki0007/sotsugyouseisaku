// Stage1_2 に「青と黄色の光」を置く（2026-11-26、依頼書 CT_mood_blue_yellow。色調 P1〜P6 を見比べるため。ユーザーの依頼）
// ・上の壁のランタンの絵 3 つにランプの光、左の壁ぎわに窓の光（床に落ちる台形）。どれも最初は消えている（光のプリセット「青い夜（塗り替え用）」のときだけ点く）
// ・今のゲームの見た目は変わらない。何度実行しても同じ（ルート「青と黄色の光（比べる用）」を作り直す）。シーンを保存する
var sb = new System.Text.StringBuilder();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! unsaved scene: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_2.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
foreach (var r in s.GetRootGameObjects()) if (r.name == "青と黄色の光（比べる用）") UnityEngine.Object.DestroyImmediate(r);
var root = new UnityEngine.GameObject("青と黄色の光（比べる用）");
// ランタンの絵の位置（配置図から：x -117.96 / -110.1 / -102.06、y 17.5）。光は少し下（床に届くように）
foreach (var x in new[] { -117.96f, -110.1f, -102.06f })
    PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Lamp, new UnityEngine.Vector3(x, 16.9f, 0f), root.transform);
// 窓の光：左の壁の高い所から床へ
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Window, new UnityEngine.Vector3(-123.5f, 15.0f, 0f), root.transform);
// 一層の壁・床の絵は砂浜より暗いので、少しだけ強くする（11-26：光が主役の作りに変えたので、塗り替えに合わせた 9／3.5 から下げた。光 3・縁 0.8・窓 2.4／0.6）
foreach (var l in root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
{
    bool window = l.transform.parent != null && l.transform.parent.name == "窓の光";
    if (l.name == "光") l.intensity = window ? 2.4f : 3f;
    else if (l.name == "縁") l.intensity = window ? 0.6f : 0.8f;
}
// シーンの演出の既定：色調・光・天候は空（今の見た目のまま）。全体の光の倍率だけ 1.8（絵が暗いので、光のプリセットを使ったときに青い夜に見えるように。11-26）
foreach (var r in s.GetRootGameObjects()) if (r.name == "シーンの演出の既定") UnityEngine.Object.DestroyImmediate(r);
var def = new UnityEngine.GameObject("シーンの演出の既定");
var sp = def.AddComponent<ScenePresentation>();
{ var so = new UnityEditor.SerializedObject(sp); so.FindProperty("globalLightScale").floatValue = 1.8f; so.ApplyModifiedPropertiesWithoutUndo(); }
int n = root.GetComponentsInChildren<SceneLight>(true).Length;
sb.Append("lights " + n + "\n");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
if (!string.IsNullOrEmpty(start)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
