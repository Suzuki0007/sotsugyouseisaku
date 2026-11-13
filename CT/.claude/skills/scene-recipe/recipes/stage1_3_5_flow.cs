// Stage_1_3 段階5：戦闘前後の会話・戦闘エリア・左の出口・戦闘中の一言
UnityEditor.AssetDatabase.Refresh();
DialogueCsv.Import("Assets/Data/Dialogue/Scenario_F1.csv", false);
var sb = new System.Text.StringBuilder();
System.Func<string, GameFlag> F = n => UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/" + n + ".asset");
// intro-seen flag (saved: survives game over so the retry is short)
{ string p = "Assets/Data/Flags/Flag_Timpani_IntroSeen.asset"; var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(p); if (f == null) { f = UnityEngine.ScriptableObject.CreateInstance<GameFlag>(); UnityEditor.AssetDatabase.CreateAsset(f, p); }
  var so = new UnityEditor.SerializedObject(f); so.FindProperty("id").stringValue = "Timpani_IntroSeen"; so.FindProperty("memo").stringValue = "ティンパニ戦の前の会話（調律のしかた）を見た。ゲームオーバーからやり直したときは短い会話にする。セーブする"; so.FindProperty("saved").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.AssetDatabase.SaveAssets(); }

var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var cleared = F("Flag_Timpani_Cleared"); var introSeen = F("Flag_Timpani_IntroSeen");
System.Func<string, DialogueData> D = n => { var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/Scenario_F1/" + n + ".asset"); if (d == null) throw new System.Exception("no dialogue " + n); return d; };
System.Action<UnityEngine.Object, string, object> P = (o, prop, val) => {
  var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop); if (p == null) throw new System.Exception("no prop " + prop + " on " + o.GetType().Name);
  if (val is float) p.floatValue = (float)val; else if (val is int) { if (p.propertyType == UnityEditor.SerializedPropertyType.Enum) p.enumValueIndex = (int)val; else p.intValue = (int)val; }
  else if (val is bool) p.boolValue = (bool)val; else if (val is string) p.stringValue = (string)val; else if (val is UnityEngine.Vector2) p.vector2Value = (UnityEngine.Vector2)val; else p.objectReferenceValue = (UnityEngine.Object)val;
  so.ApplyModifiedPropertiesWithoutUndo(); };
System.Action<UnityEngine.Object, string, UnityEngine.Object[]> L = (o, prop, arr) => { var so = new UnityEditor.SerializedObject(o); var p = so.FindProperty(prop); p.arraySize = arr.Length; for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i]; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, UnityEngine.GameObject> GO = (parent, n, pos) => { var g = new UnityEngine.GameObject(n); g.transform.SetParent(parent, false); g.transform.position = pos; return g; };
System.Action<UnityEngine.GameObject, string> Talk = (go, n) => { var holder = go.transform.Find("会話"); if (holder == null) holder = GO(go.transform, "会話", go.transform.position).transform; var h = GO(holder, "会話_" + n, go.transform.position); var dlg = h.AddComponent<Dialogue>(); P(dlg, "dialogueData", D(n)); var a = go.AddComponent<DialogueEventAction>(); P(a, "dialogue", dlg); P(a, "closeMode", 0); P(a, "waitUntilClosed", true); };
System.Action<UnityEngine.GameObject, float> Wait = (go, s) => { var a = go.AddComponent<WaitEventAction>(); P(a, "seconds", s); };
System.Func<UnityEngine.GameObject, UnityEngine.GameObject> Seq = go => { var s = go.AddComponent<EventSequence>(); P(s, "lockPlayer", true); return go; };
System.Action<UnityEngine.Component, GameFlag[], int[]> Cond = (c, flags, states) => { var so = new UnityEditor.SerializedObject(c); var fl = so.FindProperty("condition.flags"); fl.arraySize = flags.Length; for (int i = 0; i < flags.Length; i++) { fl.GetArrayElementAtIndex(i).FindPropertyRelative("flag").objectReferenceValue = flags[i]; fl.GetArrayElementAtIndex(i).FindPropertyRelative("state").enumValueIndex = states[i]; } so.ApplyModifiedPropertiesWithoutUndo(); };

var root = UnityEngine.GameObject.Find("ティンパニ部屋");
foreach (var n in new[]{ "ティンパニ戦（戦闘エリア）", "着いたとき（戦闘前の会話）", "やり直したとき（短い会話）", "全部調律できたとき（会話）", "左の出口（会話）", "戦闘中の一言" }) { var old = root.transform.Find(n); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
var walk = root.transform.Find("歩ける範囲（ティンパニと中央）");
{ var old = walk.Find("左の出口への通路（クリアで開く）"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }

var creatures = new System.Collections.Generic.List<UnityEngine.Object>();
foreach (var n in new[]{ "ティンパニ_上", "ティンパニ_左下", "ティンパニ_右下" }) creatures.Add(root.transform.Find(n + "/音楽生物").GetComponent<Enemy>());
var topCreature = root.transform.Find("ティンパニ_上/音楽生物");

// ---- battle area ----
var battleGo = GO(root.transform, "ティンパニ戦（戦闘エリア）", root.transform.position);
{ var b = battleGo.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(0.1f, 0.1f); }
var battle = battleGo.AddComponent<BattleArea>();
battleGo.GetComponent<UnityEngine.BoxCollider2D>().isTrigger = true;
P(battle, "start", 1); L(battle, "enemies", creatures.ToArray()); P(battle, "holdUntilStart", true);
P(battle, "win", 4); P(battle, "winFlag", cleared); P(battle, "lose", 0);
P(battle, "objective", "ペグを全部締めて、ティンパニを調律しろ（ペグ：E　守り：Q）"); P(battle, "startMessage", ""); P(battle, "winMessage", ""); P(battle, "loseMessage", "");
P(battle, "endEnemies", 0); P(battle, "once", true); P(battle, "saveId", "Stage_1_3_Timpani"); P(battle, "showHud", true);

// ---- intro (first time) ----
var intro = Seq(GO(root.transform, "着いたとき（戦闘前の会話）", new UnityEngine.Vector3(-115.2f, 20f, 0f)));
{ var se = intro.AddComponent<SceneStartEvent>(); P(se, "sequence", intro.GetComponent<EventSequence>()); P(se, "delay", 0.5f); Cond(se, new[]{ cleared, introSeen }, new[]{ 1, 1 }); }
Wait(intro, 0.4f);
Talk(intro, "F1_09_BigRoom");
intro.AddComponent<SeEventAction>();
{ var a = intro.AddComponent<ShakeEventAction>(); P(a, "strength", 0.25f); P(a, "duration", 0.7f); P(a, "waitUntilDone", true); }
{ var a = intro.AddComponent<CameraLookEventAction>(); P(a, "target", topCreature); P(a, "moveTime", 0.7f); P(a, "holdTime", 0.8f); P(a, "returnAfter", true); }
Talk(intro, "F1_10_Timpani");
Talk(intro, "F1_10b_HowTo");
{ var a = intro.AddComponent<SetFlagEventAction>(); a.AddFlagForEditor(introSeen, true); }
{ var a = intro.AddComponent<BattleControlEventAction>(); P(a, "battle", battle); P(a, "command", 0); }

// ---- retry (after game over) ----
var retry = Seq(GO(root.transform, "やり直したとき（短い会話）", new UnityEngine.Vector3(-115.2f, 20f, 0f)));
{ var se = retry.AddComponent<SceneStartEvent>(); P(se, "sequence", retry.GetComponent<EventSequence>()); P(se, "delay", 0.5f); Cond(se, new[]{ cleared, introSeen }, new[]{ 1, 0 }); }
Wait(retry, 0.3f);
Talk(retry, "F1_10c_Retry");
{ var a = retry.AddComponent<BattleControlEventAction>(); P(a, "battle", battle); P(a, "command", 0); }

// ---- after clear ----
var passage = GO(walk, "左の出口への通路（クリアで開く）", new UnityEngine.Vector3(-124.4f, 20.55f, 0f));
{ var b = passage.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(7.2f, 2.6f); }
passage.SetActive(false);
var done = Seq(GO(root.transform, "全部調律できたとき（会話）", new UnityEngine.Vector3(-115.2f, 20f, 0f)));
Wait(done, 1.2f);
Talk(done, "F1_11_TimpaniCalm");
{ var room = root.GetComponent<TimpaniRoom>(); P(room, "onAllCalmed", done.GetComponent<EventSequence>()); L(room, "showWhenCleared", new UnityEngine.Object[]{ passage }); }

// ---- left exit: one line (next scene not built yet) ----
var exit = GO(root.transform, "左の出口（会話）", new UnityEngine.Vector3(-126.6f, 20.55f, 0f));
{ var b = exit.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(1.6f, 2.6f); }
var exitInter = exit.AddComponent<ItemInteraction>();
Seq(exit);
Talk(exit, "F1_12_Continue");
{ var so = new UnityEditor.SerializedObject(exitInter); so.FindProperty("trigger").enumValueIndex = 1; so.FindProperty("saveId").stringValue = "Stage_1_3_LeftExit"; so.FindProperty("saveCompleted").boolValue = true;
  var rules = so.FindProperty("rules"); rules.arraySize = 1; var r = rules.GetArrayElementAtIndex(0); r.FindPropertyRelative("label").stringValue = "1回だけ会話"; r.FindPropertyRelative("sequence").objectReferenceValue = exit.GetComponent<EventSequence>(); r.FindPropertyRelative("markCompleted").boolValue = true; r.FindPropertyRelative("consumeItems").boolValue = false;
  r.FindPropertyRelative("conditions").arraySize = 0; r.FindPropertyRelative("flagConditions").arraySize = 0; so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- battle barks ----
var barkGo = GO(root.transform, "戦闘中の一言", root.transform.position);
var tb = barkGo.AddComponent<TimpaniBarks>();
{
  var so = new UnityEditor.SerializedObject(tb);
  so.FindProperty("battle").objectReferenceValue = battle;
  var entries = so.FindProperty("entries");
  var data = new object[][] {
    new object[]{ 11, 1f, 999f, new[]{ "3|来るぞ。ノア、ペグに走れ！" } },
    new object[]{ 0, 0.35f, 6f, new[]{ "2|いい感じ！", "2|あと少し…！", "3|その調子だ" } },
    new object[]{ 1, 0.6f, 5f, new[]{ "3|焦るな。輪が重なる瞬間だ", "2|(落ち着いて、音を聴くみたいに…)" } },
    new object[]{ 2, 1f, 4f, new[]{ "2|ひとつ締まった！", "3|攻撃が止んだ。今のうちだ！" } },
    new object[]{ 3, 1f, 3f, new[]{ "2|一台、静かになった！", "3|残りが荒れだした。気をつけろ" } },
    new object[]{ 4, 1f, 6f, new[]{ "3|くっ、守りが切れた！少しもたせろ！", "3|防ぎきれない…しばらく自分でよけろ！" } },
    new object[]{ 5, 0.7f, 8f, new[]{ "3|守りは戻った。いつでも呼べ" } },
    new object[]{ 6, 0.6f, 10f, new[]{ "2|あ、何か光るものが落ちてきた！", "3|あの青い石…拾えば守りが早く戻る" } },
    new object[]{ 7, 0.5f, 8f, new[]{ "3|力が戻ってくる…助かる", "2|よし、拾った！" } },
    new object[]{ 8, 0.5f, 6f, new[]{ "3|下がれ、ノア！", "2|っ…！" } },
    new object[]{ 9, 1f, 15f, new[]{ "3|無理をするな！ハートを拾え", "2|(まずい、このままじゃ…)" } },
    new object[]{ 10, 1f, 25f, new[]{ "2|(ペグに近づいて E…輪が重なった瞬間にもう一度！)", "3|ノア、ペグだ。赤いペグを締めろ" } },
  };
  entries.arraySize = data.Length;
  for (int i = 0; i < data.Length; i++) {
    var e = entries.GetArrayElementAtIndex(i);
    e.FindPropertyRelative("when").enumValueIndex = (int)data[i][0]; e.FindPropertyRelative("chance").floatValue = (float)data[i][1]; e.FindPropertyRelative("cooldown").floatValue = (float)data[i][2];
    var lines = e.FindPropertyRelative("lines"); var arr = (string[])data[i][3]; lines.arraySize = arr.Length;
    for (int k = 0; k < arr.Length; k++) { var parts = arr[k].Split('|'); var l = lines.GetArrayElementAtIndex(k); l.FindPropertyRelative("speaker").enumValueIndex = int.Parse(parts[0]); l.FindPropertyRelative("text").stringValue = parts[1]; l.FindPropertyRelative("seconds").floatValue = 0f; l.FindPropertyRelative("gap").floatValue = 0.2f; }
  }
  so.ApplyModifiedPropertiesWithoutUndo();
}

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
{ var so = new UnityEditor.SerializedObject(battle); sb.Append("winFlag=").Append(so.FindProperty("winFlag").objectReferenceValue).Append(" enemies=").Append(so.FindProperty("enemies").arraySize); }
{ var so = new UnityEditor.SerializedObject(root.GetComponent<TimpaniRoom>()); sb.Append(" onAllCalmed=").Append(so.FindProperty("onAllCalmed").objectReferenceValue).Append(" cleared=").Append(so.FindProperty("clearedFlag").objectReferenceValue); }
return sb.ToString();
