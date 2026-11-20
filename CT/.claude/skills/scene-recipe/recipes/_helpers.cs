// ---- 共通の小道具（レシピの先頭に「// @helpers」と書くと、run.ps1 がこのファイルを前に付ける） ----
// SetProp(obj, "field", value)        … SerializedObject 経由で値を入れる（float / int / enum / bool / string / Vector2 / Object）
// SetList(obj, "field", objects[])    … Object の配列・リストを入れる
// NewGO(parent, "名前", 位置)          … 子オブジェクトを作る（parent が null ならシーンの一番上）
// Flag("Flag_xxx")                    … Assets/Data/Flags/ の GameFlag
// Dlg("Scenario_F1", "F1_07_SigleIntro") … Assets/Data/Dialogue/<フォルダ>/<ID>.asset の DialogueData
// MakeSeq(go, lockPlayer)             … EventSequence を付ける（Actions は空＝同じオブジェクトの部品を上から順に）
// StripContents(物の Transform)        … 中身のレシピをもう一度流す前の片付け（絵だけ残して、足した部品・物を外す）
// AddTalk(go, dialogueData)           … DialogueEventAction を足して会話データを直接入れる（閉じるまで待つ）
// AddWait(go, 秒)                     … WaitEventAction を足す
// OpenSceneSafe("Assets/Scenes/x.unity") … 開いているシーンに保存していない変更があれば止める
// RoomMenu.Build(RoomKind.Battle, 絵の SpriteRenderer, "部屋ID") … 部屋の型を作る（歩ける範囲・カメラ・出現位置・戦闘エリアなど。Map/Editor/RoomMenu）
// RoomChecks.Check(room)              … 部屋の点検（OK も含む一覧）
// WalkAreaMask.Build(walkArea)        … マスク画像から歩ける範囲を作る（WalkArea にマスク画像・合わせる絵を入れてから）
// Log                                 … 最後に return Log.ToString(); で結果を返す

var Log = new System.Text.StringBuilder();
// Substitute("確認キー", "ない素材", "代わりに使った物", "場所") … ない素材を別の物で代用したら記録する（asset-audit の data/substitutes.tsv。audit.ps1 -NewSlots でチェックリストにない代用が出る）
System.Action<string, string, string, string> Substitute = (key, missing, instead, where) => {
  var file = ".claude/skills/asset-audit/data/substitutes.tsv";
  var lines = System.IO.File.Exists(file) ? new System.Collections.Generic.List<string>(System.IO.File.ReadAllLines(file, System.Text.Encoding.UTF8)) : new System.Collections.Generic.List<string>{ "確認キー\tない素材\t代わりに使った物\t場所\t記録日" };
  foreach (var l in lines) { var c = l.Split('\t'); if (c.Length >= 4 && c[0] == key && c[3] == where) return; }
  lines.Add(key + "\t" + missing + "\t" + instead + "\t" + where + "\t" + System.DateTime.Now.ToString("yyyy-MM-dd"));
  System.IO.File.WriteAllLines(file, lines.ToArray(), new System.Text.UTF8Encoding(false));
  Log.Append("SUBSTITUTE " + missing + " -> " + instead + " (" + where + ")\n");
};

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

// 会話：DialogueEventAction に会話データを直接入れる（11-13 から。子の「会話」オブジェクトは要らない）
System.Action<UnityEngine.GameObject, DialogueData> AddTalk = (go, data) => {
  var a = go.AddComponent<DialogueEventAction>(); SetProp(a, "dialogueData", data); SetProp(a, "closeMode", 0); SetProp(a, "waitUntilClosed", true); };

System.Action<UnityEngine.GameObject, float> AddWait = (go, s) => { var a = go.AddComponent<WaitEventAction>(); SetProp(a, "seconds", s); };

// 中身のレシピ（shosai_2・corridor_2）をもう一度だけ流すための片付け（11-20）：
// 「物」の下は配置のレシピ（_1）が絵（SpriteRenderer）だけで作るので、中身のレシピが足した物を外す：
//   ・プレハブのインスタンス（ペグなど）と、絵を1つも持たない物（調べる範囲・出口など）を消す
//   ・残った物から Transform と SpriteRenderer 以外の部品（調べる・仕掛け・当たり判定など）を外す
// 配置を変えたときは今までどおり _1 から流す（_1 は「物」ごと作り直す）
System.Action<UnityEngine.Transform> StripContents = objs => {
  var kill = new System.Collections.Generic.List<UnityEngine.GameObject>();
  foreach (var t in objs.GetComponentsInChildren<UnityEngine.Transform>(true)) {
    if (t == objs) continue;
    if (UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || t.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true).Length == 0) kill.Add(t.gameObject);
  }
  foreach (var g in kill) if (g != null) UnityEngine.Object.DestroyImmediate(g);
  // ほかの部品の RequireComponent で必要とされている部品は、あとの回で外す（先に外すとエラーになる）
  System.Func<UnityEngine.Component, bool> Required = c => {
    foreach (var other in c.GetComponents<UnityEngine.Component>()) {
      if (other == null || other == c) continue;
      foreach (UnityEngine.RequireComponent rc in other.GetType().GetCustomAttributes(typeof(UnityEngine.RequireComponent), true))
        foreach (var t in new[]{ rc.m_Type0, rc.m_Type1, rc.m_Type2 }) if (t != null && t.IsInstanceOfType(c)) return true;
    }
    return false; };
  for (int pass = 0; pass < 6; pass++) {
    int removed = 0, left = 0;
    foreach (var c in objs.GetComponentsInChildren<UnityEngine.Component>(true)) {
      if (c == null || c is UnityEngine.Transform || c is UnityEngine.SpriteRenderer) continue;
      if (Required(c)) { left++; continue; }
      UnityEngine.Object.DestroyImmediate(c); removed++;
    }
    if (left == 0 || removed == 0) break;
  }
  Log.Append("contents stripped (rebuild contents only)\n"); };

// ---- ここからレシピ本体 ----
