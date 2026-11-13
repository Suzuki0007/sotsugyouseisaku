// Stage1_2：正面の扉（合流後に調べると Stage_1_3 へ）を作り直す
UnityEditor.AssetDatabase.Refresh();
DialogueCsv.Import("Assets/Data/Dialogue/Scenario_F1.csv", false);
var sb = new System.Text.StringBuilder();
// place name: same style as the other stages
{ var table = UnityEngine.Resources.Load<PlaceNameTable>(PlaceNameTable.ResourcePath); var so = new UnityEditor.SerializedObject(table); var it = so.GetIterator(); bool en = true;
  while (it.NextVisible(en)) { en = true; if (it.propertyType == UnityEditor.SerializedPropertyType.String && it.name == "displayName" && it.stringValue == "ティンパニの間") { it.stringValue = "ステージ1-3"; } }
  so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(table); UnityEditor.AssetDatabase.SaveAssets(); sb.Append("place=").Append(PlaceNameTable.GetName("Stage_1_3")).Append("\n"); }

var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_2.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
System.Func<string, DialogueData> D = n => { var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/Scenario_F1/" + n + ".asset"); if (d == null) throw new System.Exception("no dialogue " + n); return d; };
var root = UnityEngine.GameObject.Find("Stage1_2_イベント");
if (root == null) return "no root";
var oldDoor = root.transform.Find("正面の扉（合流後に調べると Stage_1_3 へ）"); if (oldDoor != null) UnityEngine.Object.DestroyImmediate(oldDoor.gameObject);

var door = new UnityEngine.GameObject("正面の扉（合流後に調べると Stage_1_3 へ）");
door.transform.SetParent(root.transform, false);
door.transform.position = new UnityEngine.Vector3(-107.25f, 15.6f, 0f);
var box = door.AddComponent<UnityEngine.BoxCollider2D>(); box.isTrigger = true; box.size = new UnityEngine.Vector2(2.6f, 1.6f);
var inter = door.AddComponent<ItemInteraction>();
var seq = door.AddComponent<EventSequence>();
{ var so = new UnityEditor.SerializedObject(seq); so.FindProperty("lockPlayer").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
var holder = new UnityEngine.GameObject("会話"); holder.transform.SetParent(door.transform, false);
foreach (var n in new[]{ "F1_08_AutoDoor", "F1_08b_ForceDoor" }) {
  var h = new UnityEngine.GameObject("会話_" + n); h.transform.SetParent(holder.transform, false);
  var dlg = h.AddComponent<Dialogue>(); { var so = new UnityEditor.SerializedObject(dlg); so.FindProperty("dialogueData").objectReferenceValue = D(n); so.ApplyModifiedPropertiesWithoutUndo(); }
  var a = door.AddComponent<DialogueEventAction>(); { var so = new UnityEditor.SerializedObject(a); so.FindProperty("dialogue").objectReferenceValue = dlg; so.FindProperty("closeMode").enumValueIndex = 0; so.FindProperty("waitUntilClosed").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
}
door.AddComponent<SeEventAction>();
{ var a = door.AddComponent<ShakeEventAction>(); var so = new UnityEditor.SerializedObject(a); so.FindProperty("strength").floatValue = 0.25f; so.FindProperty("duration").floatValue = 0.5f; so.FindProperty("waitUntilDone").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
{ var a = door.AddComponent<SceneChangeEventAction>(); var so = new UnityEditor.SerializedObject(a); so.FindProperty("sceneName").stringValue = "Stage_1_3"; so.FindProperty("spawnPointId").stringValue = "FromStage1_2"; so.ApplyModifiedPropertiesWithoutUndo(); }
{
  var so = new UnityEditor.SerializedObject(inter);
  so.FindProperty("trigger").enumValueIndex = 0; so.FindProperty("saveId").stringValue = "Stage1_2_FrontDoor"; so.FindProperty("saveCompleted").boolValue = false;
  var rules = so.FindProperty("rules"); rules.arraySize = 1; var r = rules.GetArrayElementAtIndex(0);
  r.FindPropertyRelative("label").stringValue = "シグルが仲間になっていたら、こじ開けて Stage_1_3 へ";
  var fc = r.FindPropertyRelative("flagConditions"); fc.arraySize = 1; fc.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Sigle_Joined.asset"); fc.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 0;
  r.FindPropertyRelative("conditions").arraySize = 0;
  r.FindPropertyRelative("sequence").objectReferenceValue = seq; r.FindPropertyRelative("markCompleted").boolValue = false; r.FindPropertyRelative("consumeItems").boolValue = false;
  so.ApplyModifiedPropertiesWithoutUndo();
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("Stage1_2 saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene));
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
