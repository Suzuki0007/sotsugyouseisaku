// 外のマップの足音（2026-11-14）：Stage1_1（砂漠）に「足音の設定（シーン全体）」を置き、砂の足音（Se_Walk_sabaku の録音・歩いている間ループ）にする
// 何度実行しても同じ（名前で見つけて作り直さない）。Stage1_1 を保存する
var sb = new System.Text.StringBuilder();
var startPath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! a scene has unsaved changes: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;

// 注意：アセットはシーンを開いてから読む（先に読むと、シーンを開いたときの「使っていないアセットの片付け」で消えて、空の参照が保存される）
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_1.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var walk = UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>("Assets/SE/Se_Walk_sabaku.asset");
var walkFollow = UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>("Assets/SE/Se_Walk_sabaku_follow.asset");
if (walk == null || walkFollow == null) return "!! no Se_Walk_sabaku";
const string Name = "足音の設定（外・砂）";
UnityEngine.GameObject go = null;
foreach (var r in s.GetRootGameObjects()) if (r.name == Name) go = r;
if (go == null) { go = new UnityEngine.GameObject(Name); sb.Append("+ " + Name + "\n"); }
var fs = go.GetComponent<SceneFootstep>(); if (fs == null) fs = go.AddComponent<SceneFootstep>();
var so = new UnityEditor.SerializedObject(fs);
so.FindProperty("playMode").enumValueIndex = (int)FootstepSe.PlayMode.LoopWhileWalking;
so.FindProperty("noahSe").objectReferenceValue = walk;
so.FindProperty("noahFollowSe").objectReferenceValue = walkFollow;
so.FindProperty("sigleSe").objectReferenceValue = walk;
so.FindProperty("sigleFollowSe").objectReferenceValue = walkFollow;
so.FindProperty("memo").stringValue = "外（砂漠）の足音。素材が歩き続ける録音なので「歩いている間ループ」。1歩ずつの砂の音ができたら「一歩ずつ」にして差し替える";
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
if (!string.IsNullOrEmpty(startPath) && startPath != s.path) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(startPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
