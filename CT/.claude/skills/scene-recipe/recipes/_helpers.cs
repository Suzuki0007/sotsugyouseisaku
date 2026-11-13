// ---- 共通の小道具（レシピの先頭に「// @helpers」と書くと、run.ps1 がこのファイルを前に付ける） ----
// SetProp(obj, "field", value)        … SerializedObject 経由で値を入れる（float / int / enum / bool / string / Vector2 / Object）
// SetList(obj, "field", objects[])    … Object の配列・リストを入れる
// NewGO(parent, "名前", 位置)          … 子オブジェクトを作る（parent が null ならシーンの一番上）
// Flag("Flag_xxx")                    … Assets/Data/Flags/ の GameFlag
// Dlg("Scenario_F1", "F1_07_SigleIntro") … Assets/Data/Dialogue/<フォルダ>/<ID>.asset の DialogueData
// MakeSeq(go, lockPlayer)             … EventSequence を付ける（Actions は空＝同じオブジェクトの部品を上から順に）
// AddTalk(go, dialogueData)           … 子の「会話」に Dialogue を作り、DialogueEventAction を足す（閉じるまで待つ）
// AddWait(go, 秒)                     … WaitEventAction を足す
// OpenSceneSafe("Assets/Scenes/x.unity") … 開いているシーンに保存していない変更があれば止める
// Log                                 … 最後に return Log.ToString(); で結果を返す

var Log = new System.Text.StringBuilder();

System.Action<UnityEngine.Object, string, object> SetProp = (o, prop, val) => {
  var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop);
  if (p == null) throw new System.Exception("no prop " + prop + " on " + o.GetType().Name);
  if (val is float) p.floatValue = (float)val;
  else if (val is int) { if (p.propertyType == UnityEditor.SerializedPropertyType.Enum) p.enumValueIndex = (int)val; else p.intValue = (int)val; }
  else if (val is bool) p.boolValue = (bool)val;
  else if (val is string) p.stringValue = (string)val;
  else if (val is UnityEngine.Vector2) p.vector2Value = (UnityEngine.Vector2)val;
  else if (val is UnityEngine.Color) p.colorValue = (UnityEngine.Color)val;
  else p.objectReferenceValue = (UnityEngine.Object)val;
  so.ApplyModifiedPropertiesWithoutUndo(); };

System.Action<UnityEngine.Object, string, UnityEngine.Object[]> SetList = (o, prop, arr) => {
  var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop); p.arraySize = arr.Length;
  for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
  so.ApplyModifiedPropertiesWithoutUndo(); };

System.Func<string, UnityEngine.SceneManagement.Scene> OpenSceneSafe = path => {
  var cur = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
  if (cur.path == path) return cur;
  if (cur.isDirty) throw new System.Exception("開いているシーンに保存していない変更があります: " + cur.path);
  return UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single); };

System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, UnityEngine.GameObject> NewGO = (parent, n, pos) => {
  var g = new UnityEngine.GameObject(n); if (parent != null) g.transform.SetParent(parent, false); g.transform.position = pos; return g; };

System.Func<string, GameFlag> Flag = n => {
  var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/" + n + ".asset");
  if (f == null) throw new System.Exception("no flag " + n); return f; };

System.Func<string, string, DialogueData> Dlg = (folder, id) => {
  var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/" + folder + "/" + id + ".asset");
  if (d == null) throw new System.Exception("no dialogue " + folder + "/" + id); return d; };

System.Func<UnityEngine.GameObject, bool, EventSequence> MakeSeq = (go, lockPlayer) => {
  var s = go.AddComponent<EventSequence>(); SetProp(s, "lockPlayer", lockPlayer); return s; };

System.Action<UnityEngine.GameObject, DialogueData> AddTalk = (go, data) => {
  var holder = go.transform.Find("会話"); if (holder == null) holder = NewGO(go.transform, "会話", go.transform.position).transform;
  var h = NewGO(holder, "会話_" + data.name, go.transform.position); var dlg = h.AddComponent<Dialogue>(); SetProp(dlg, "dialogueData", data);
  var a = go.AddComponent<DialogueEventAction>(); SetProp(a, "dialogue", dlg); SetProp(a, "closeMode", 0); SetProp(a, "waitUntilClosed", true); };

System.Action<UnityEngine.GameObject, float> AddWait = (go, s) => { var a = go.AddComponent<WaitEventAction>(); SetProp(a, "seconds", s); };

// ---- ここからレシピ本体 ----
