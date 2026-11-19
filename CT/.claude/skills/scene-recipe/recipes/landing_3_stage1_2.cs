// 上陸 Stage1_2（塔の入口）：着いたとき（頭痛 → 暗い → ランタン）の頭に「扉が閉まる（音・絵）→ OP_02k」を差し込む。入口の扉の仮の絵を置く（ルート「上陸（Stage1_2 入口）」を消して作る。差し込んだ部品は消してから入れ直す。stage1_2_events で作り直したら、そのあとにもう一度流す）
// @helpers
var scene = OpenSceneSafe("Assets/Scenes/Stage1_2.unity");
System.Func<string, UnityEngine.GameObject> Find = path => { var parts = path.Split('/'); foreach (var r in scene.GetRootGameObjects()) { if (r.name != parts[0]) continue; var t = r.transform; for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]); if (t != null) return t.gameObject; } return null; };
System.Func<string, SeData> Se = n => { foreach (var g in UnityEditor.AssetDatabase.FindAssets(n + " t:SeData")) { var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g); if (System.IO.Path.GetFileNameWithoutExtension(p) == n) return UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(p); } throw new System.Exception("no se " + n); };
var talkK = Dlg("Scenario_Landing", "OP_02k_TowerDoor");
var introDone = Flag("Flag_1_2_Intro_Done");

var intro = Find("Stage1_2_イベント/着いたとき（頭痛 → 暗い → ランタンを取ろう）");
if (intro == null) throw new System.Exception("no intro (stage1_2_events を先に)");

// ---- 前に差し込んだ部品を消す（SceneStartEvent のすぐ後から、OP_02k の会話まで） ----
{ var comps = intro.GetComponents<EventAction>(); int end = -1;
  for (int i = 0; i < comps.Length; i++) { var d = comps[i] as DialogueEventAction; if (d != null && new UnityEditor.SerializedObject(d).FindProperty("dialogueData").objectReferenceValue == talkK) { end = i; break; } }
  for (int i = 0; i <= end; i++) UnityEngine.Object.DestroyImmediate(comps[i]);
  if (end >= 0) Log.Append("removed old inserted parts: ").Append(end + 1).Append("\n"); }
{ var old = Find("上陸（Stage1_2 入口）"); if (old != null) UnityEngine.Object.DestroyImmediate(old); }
var root = NewGO(null, "上陸（Stage1_2 入口）", new UnityEngine.Vector3(-107.57f, 8.7f, 0f)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

// ---- 入口の扉（仮の絵＝書斎の扉A の仮素材。開いた絵 ⇔ 閉じた絵） ----
var roomSr = Find("Grid/Room_1_Rouka").GetComponent<UnityEngine.SpriteRenderer>();
System.Func<string, string, UnityEngine.GameObject> DoorPic = (n, file) => { var g = NewGO(root.transform, n, root.transform.position); var sr = g.AddComponent<UnityEngine.SpriteRenderer>();
  sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Images/Map/Stage1_3/Karisozai/" + file + ".png"); sr.sharedMaterial = roomSr.sharedMaterial; sr.sortingLayerID = roomSr.sortingLayerID; sr.sortingOrder = roomSr.sortingOrder + 1; return g; };
var doorOpen = DoorPic("入口の扉_開（仮の絵）", "door_A_open");
var doorClosed = DoorPic("入口の扉_閉（仮の絵）", "door_A_closed");
doorClosed.SetActive(false);
// ロードしたとき（着いたときの演出が済んでいる）は閉じた絵
{ var fs = root.AddComponent<FlagSwitch>(); var so = new UnityEditor.SerializedObject(fs);
  var fl = so.FindProperty("flagConditions"); fl.arraySize = 1; fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = introDone; fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 0;
  var on = so.FindProperty("activeWhenMet"); on.arraySize = 1; on.GetArrayElementAtIndex(0).objectReferenceValue = doorClosed;
  var off = so.FindProperty("activeWhenNotMet"); off.arraySize = 1; off.GetArrayElementAtIndex(0).objectReferenceValue = doorOpen;
  so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- 差し込む部品（扉を閉める → 安心 → 束の間：OP_02k）。このあとは今の流れ（キーーーン → 頭痛 F1_02 → 暗い → ランタン → 敵が降る → 追跡） ----
var added = new System.Collections.Generic.List<UnityEngine.Component>();
{ var a = intro.AddComponent<WaitEventAction>(); SetProp(a, "seconds", 0.3f); added.Add(a); }
{ var a = intro.AddComponent<SeEventAction>(); SetProp(a, "seOnEnter", Se("Se_Door_Close")); SetProp(a, "playAtThisPosition", false); added.Add(a); }
{ var a = intro.AddComponent<SetActiveEventAction>(); SetList(a, "show", new UnityEngine.Object[]{ doorClosed }); SetList(a, "hide", new UnityEngine.Object[]{ doorOpen }); added.Add(a); }
{ var a = intro.AddComponent<ShakeEventAction>(); SetProp(a, "strength", 0.08f); SetProp(a, "duration", 0.3f); SetProp(a, "waitUntilDone", true); added.Add(a); }
{ var a = intro.AddComponent<WaitEventAction>(); SetProp(a, "seconds", 0.4f); added.Add(a); }
{ var a = intro.AddComponent<DialogueEventAction>(); SetProp(a, "dialogueData", talkK); SetProp(a, "closeMode", 0); SetProp(a, "waitUntilClosed", true); added.Add(a); }
// SceneStartEvent のすぐ後へ並べ替える（EventSequence は部品を上から順に実行する）
{ var all = new System.Collections.Generic.List<UnityEngine.Component>(intro.GetComponents<UnityEngine.Component>());
  int anchor = all.IndexOf(intro.GetComponent<SceneStartEvent>());
  for (int i = 0; i < added.Count; i++) {
    int target = anchor + 1 + i;
    while (true) { var now = new System.Collections.Generic.List<UnityEngine.Component>(intro.GetComponents<UnityEngine.Component>()); if (now.IndexOf(added[i]) <= target) break; UnityEditorInternal.ComponentUtility.MoveComponentUp(added[i]); } } }
foreach (var c in intro.GetComponents<UnityEngine.Component>()) Log.Append("  ").Append(c.GetType().Name).Append("\n");

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
return Log.ToString();
