// Stage1_4（T字通路）段階2：入ったとき（E1）・床の震え（E2）・扉の前のイベント（E3）・扉／管／シグルの調べる・ランプと管の見た目・左右と南の出口。corridor_1_scene のすぐあとに流す（1 が物を作り直すので、2 だけを2回流さない）
// 左右の部屋の中身と、扉を開ける処理は作らない（指示書の範囲外）。左右の出口は一言だけの受け口（部屋ができたら部品「シーンを移る」を足す）
// @helpers
const string ScenePath = "Assets/Scenes/Stage1_4.unity";
var scene = OpenSceneSafe(ScenePath);
UnityEditor.AssetDatabase.Refresh();
System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
var room = FindRoot("T字通路（Stage1_4）"); if (room == null) throw new System.Exception("先に corridor_1_scene");
var objs = room.transform.Find("物");
// もう組んであるなら、中身だけ作り直す（11-20。配置を変えたときは corridor_1_scene から）
{ var built = room.transform.Find("仕掛けとイベント"); if (built != null) { UnityEngine.Object.DestroyImmediate(built.gameObject); StripContents(objs); } }

System.Func<float, float, UnityEngine.Vector3> P = (x, y) => new UnityEngine.Vector3((x - 1080f) / 100f, (720f - y) / 100f, 0f);
System.Func<string, UnityEngine.Object> A = path => { var o = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path); if (o == null) throw new System.Exception("no asset " + path); return o; };
System.Func<string, DialogueData> T = id => Dlg("Scenario_F1", id);
var seTouch = A("Assets/SE/SE-22_Puzzle/Se_codelock_correct.asset");
var fxSpawn = A("Assets/Data/Effect/Sample/Fx_Sample_Spawn.asset");

var root = NewGO(room.transform, "仕掛けとイベント", room.transform.position).transform;
var evRoot = NewGO(root, "イベント（流れ）", root.position).transform;
var reRoot = NewGO(root, "見た目の切り替え", root.position).transform;
var markRoot = NewGO(root, "目印", root.position).transform;

// ---------------- 小道具（書斎のレシピと同じ形） ----------------
System.Func<string, bool, UnityEngine.GameObject> Seq = (n, lockP) => { var g = NewGO(evRoot, n, evRoot.position); MakeSeq(g, lockP); return g; };
System.Action<UnityEngine.GameObject, string> Talk = (g, id) => AddTalk(g, T(id));
System.Action<UnityEngine.GameObject, int, string, float> Bark = (g, who, text, sec) => { var a = g.AddComponent<BarkEventAction>(); var so = new UnityEditor.SerializedObject(a); var ln = so.FindProperty("lines"); ln.arraySize = 1; var e = ln.GetArrayElementAtIndex(0); e.FindPropertyRelative("speaker").enumValueIndex = who; e.FindPropertyRelative("text").stringValue = text; e.FindPropertyRelative("seconds").floatValue = sec; so.FindProperty("waitUntilDone").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Action<UnityEngine.GameObject, UnityEngine.Object> Se = (g, se) => { var a = g.AddComponent<SeEventAction>(); SetProp(a, "seOnEnter", se); };
System.Action<UnityEngine.GameObject, UnityEngine.Object, UnityEngine.Transform> Fx = (g, fx, at) => { var a = g.AddComponent<EffectEventAction>(); SetProp(a, "effectOnEnter", fx); SetProp(a, "spawnAt", 2); SetProp(a, "customPoint", at); };
System.Action<UnityEngine.GameObject, float, float, bool> Shake = (g, st, du, w) => { var a = g.AddComponent<ShakeEventAction>(); SetProp(a, "strength", st); SetProp(a, "duration", du); SetProp(a, "waitUntilDone", w); };
System.Action<UnityEngine.GameObject, string, bool> SetF = (g, f, v) => { var a = g.AddComponent<SetFlagEventAction>(); a.AddFlagForEditor(Flag(f), v); };
System.Action<UnityEngine.GameObject, UnityEngine.GameObject[], UnityEngine.GameObject[]> Show = (g, show, hide) => { var a = g.AddComponent<SetActiveEventAction>(); SetList(a, "show", show ?? new UnityEngine.GameObject[0]); SetList(a, "hide", hide ?? new UnityEngine.GameObject[0]); };
System.Action<UnityEngine.GameObject, UnityEngine.Transform, float> NoahTo = (g, dest, sec) => { var a = g.AddComponent<MoveObjectEventAction>(); SetProp(a, "targetKind", 1); SetProp(a, "type", 2); SetProp(a, "destinationKind", 0); SetProp(a, "destination", dest); SetProp(a, "offset", UnityEngine.Vector2.zero); SetProp(a, "duration", sec); SetProp(a, "ease", 0); SetProp(a, "waitUntilDone", true); };
System.Action<UnityEngine.GameObject, int> FaceNoah = (g, dir) => { var a = g.AddComponent<FaceDirectionEventAction>(); SetProp(a, "who", 0); SetProp(a, "direction", dir); };
System.Action<UnityEngine.GameObject> AutoSv = g => g.AddComponent<AutoSaveEventAction>();

System.Action<UnityEditor.SerializedObject, string, object[][]> CondF = (so, path, fl) => {
  var f = so.FindProperty(path + ".flags"); f.arraySize = fl == null ? 0 : fl.Length;
  for (int i = 0; fl != null && i < fl.Length; i++) { var e = f.GetArrayElementAtIndex(i); e.FindPropertyRelative("flag").objectReferenceValue = Flag((string)fl[i][0]); e.FindPropertyRelative("state").enumValueIndex = (bool)fl[i][1] ? 0 : 1; } };
// 見た目の切り替え（GimmickReaction）：フラグの条件を満たす間 show を表示・hide を非表示。シーンに入るたびに計算し直す（latch しない）
System.Action<string, object[][], UnityEngine.GameObject[], UnityEngine.GameObject[]> React = (n, fl, show, hide) => {
  var g = NewGO(reRoot, n, reRoot.position); var r = g.AddComponent<GimmickReaction>(); var so = new UnityEditor.SerializedObject(r);
  CondF(so, "condition", fl); so.FindProperty("latch").boolValue = false;
  var s = so.FindProperty("showWhenMet"); s.arraySize = show == null ? 0 : show.Length; for (int i = 0; show != null && i < show.Length; i++) s.GetArrayElementAtIndex(i).objectReferenceValue = show[i];
  var h = so.FindProperty("hideWhenMet"); h.arraySize = hide == null ? 0 : hide.Length; for (int i = 0; hide != null && i < hide.Length; i++) h.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
  so.FindProperty("saveId").stringValue = ""; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Action<UnityEngine.GameObject, float, float, float, float> Range = (g, x, y, w, h) => { var b = g.GetComponent<UnityEngine.BoxCollider2D>(); if (b == null) b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; var c = P(x, y) - g.transform.position; b.offset = new UnityEngine.Vector2(c.x, c.y); b.size = new UnityEngine.Vector2(w / 100f, h / 100f); };
// 調べる／入ると、条件に合った流れを実行（ItemInteraction）。rules = (ラベル, 流れの GO, flags)。trigger 0＝調べたとき、1＝エリアに入ったとき
System.Action<UnityEngine.GameObject, string, int, object[][], string> Inter = (g, saveId, trigger, rules, prompt) => {
  if (g.GetComponent<UnityEngine.Collider2D>() == null) { var b0 = g.AddComponent<UnityEngine.BoxCollider2D>(); b0.isTrigger = true; } var it = g.AddComponent<ItemInteraction>(); var so = new UnityEditor.SerializedObject(it);
  so.FindProperty("trigger").enumValueIndex = trigger; so.FindProperty("saveId").stringValue = saveId; so.FindProperty("saveCompleted").boolValue = false;
  if (!string.IsNullOrEmpty(prompt)) so.FindProperty("promptLabel").stringValue = prompt;
  var rs = so.FindProperty("rules"); rs.arraySize = rules.Length;
  for (int i = 0; i < rules.Length; i++) {
    var r = rs.GetArrayElementAtIndex(i); r.FindPropertyRelative("label").stringValue = (string)rules[i][0];
    r.FindPropertyRelative("sequence").objectReferenceValue = ((UnityEngine.GameObject)rules[i][1]).GetComponent<EventSequence>();
    r.FindPropertyRelative("markCompleted").boolValue = false; r.FindPropertyRelative("consumeItems").boolValue = false;
    var fl = (object[][])rules[i][2]; var fc = r.FindPropertyRelative("flagConditions"); fc.arraySize = fl == null ? 0 : fl.Length;
    for (int k = 0; fl != null && k < fl.Length; k++) { var e = fc.GetArrayElementAtIndex(k); e.FindPropertyRelative("flag").objectReferenceValue = Flag((string)fl[k][0]); e.FindPropertyRelative("state").enumValueIndex = (bool)fl[k][1] ? 0 : 1; }
    r.FindPropertyRelative("conditions").arraySize = 0;
  }
  so.ApplyModifiedPropertiesWithoutUndo(); };
System.Func<string, UnityEngine.Transform> Obj = n => { var t = objs.Find(n); if (t == null) throw new System.Exception("no obj " + n); return t; };

// ランプの数の条件（西・東のフラグ）
const string W = "Flag_TC_WestPipeOn"; const string E = "Flag_TC_EastPipeOn"; const string Done = "Flag_TC_DoorEventDone";
var lamp0 = new[]{ new object[]{ Done, true }, new object[]{ W, false }, new object[]{ E, false } };
var lamp1W = new[]{ new object[]{ Done, true }, new object[]{ W, true }, new object[]{ E, false } };
var lamp1E = new[]{ new object[]{ Done, true }, new object[]{ W, false }, new object[]{ E, true } };

// ---------------- 物 ----------------
var door = Obj("扉（開かない。開く処理はまだ作らない）"); var glow = door.Find("音符の光（E3 から）").gameObject;
var lampW = Obj("西のランプ"); var lampE = Obj("東のランプ");
var lampWOn = lampW.Find("絵_点").gameObject; var lampWOff = lampW.Find("絵_消").gameObject;
var lampEOn = lampE.Find("絵_点").gameObject; var lampEOff = lampE.Find("絵_消").gameObject;
var sigle = Obj("シグル（NPC）"); var sigleN = sigle.Find("絵_北向き（E3 の前）").gameObject; var sigleS = sigle.Find("絵_南向き（E3 から）").gameObject;
var mDoorFront = NewGO(markRoot, "目印_E3 でノアが歩く先（扉の前。仮）", P(1080, 330)).transform;

// ---------------- 見た目の切り替え（フラグから。シーンに入るたびに計算し直す） ----------------
React("音符の光：E3 で触れたら表示したまま", new[]{ new object[]{ "Flag_TC_NoteGlowed", true } }, new[]{ glow }, null);
React("西のランプ：西の管に伝わったら点く", new[]{ new object[]{ W, true } }, new[]{ lampWOn }, new[]{ lampWOff });
React("東のランプ：東の管に伝わったら点く", new[]{ new object[]{ E, true } }, new[]{ lampEOn }, new[]{ lampEOff });
React("西の管の震え", new[]{ new object[]{ W, true } }, new[]{ Obj("西の管の震え（西の部屋の成果）").gameObject }, null);
React("東の管の震え", new[]{ new object[]{ E, true } }, new[]{ Obj("東の管の震え（東の部屋の成果）").gameObject }, null);
React("シグル：E3 から南向き", new[]{ new object[]{ Done, true } }, new[]{ sigleS }, new[]{ sigleN });

// ---------------- E1 入ったとき（書斎から。1回だけ） ----------------
var arrive = (room.transform.Find("着いたとき（使うなら表示する）") ?? room.transform.Find("E1 入ったとき（1回だけ）")).gameObject; arrive.name = "E1 入ったとき（1回だけ）"; arrive.SetActive(true);
foreach (var c in arrive.GetComponents<EventAction>()) UnityEngine.Object.DestroyImmediate(c);
{ var st = arrive.GetComponent<SceneStartEvent>(); var so = new UnityEditor.SerializedObject(st); CondF(so, "condition", new[]{ new object[]{ "Flag_TC_EnterSeen", false } }); so.FindProperty("delay").floatValue = 0.4f; so.ApplyModifiedPropertiesWithoutUndo(); }
FaceNoah(arrive, 0); AddWait(arrive, 0.3f); Talk(arrive, "F1_TC_Enter"); SetF(arrive, "Flag_TC_EnterSeen", true); AutoSv(arrive);

// ---------------- E2 縦の通路の途中：床の震え＋遠くの音符の光（会話なし。1回だけ。位置は仮） ----------------
{ var g = NewGO(objs, "E2 床の震えの範囲（仮 y=700）", P(1080, 700));
  var s = Seq("E2 床の震えと遠くの光", false); SetF(s, "Flag_TC_Rumble", true); Shake(s, 0.06f, 0.5f, false);
  Show(s, new[]{ glow }, null); AddWait(s, 0.35f);
  var back = s.AddComponent<SetActiveEventAction>(); // 光を戻す（E3 の前なので消す）
  SetList(back, "show", new UnityEngine.GameObject[0]); SetList(back, "hide", new[]{ glow });
  Inter(g, "TC_Rumble", 1, new[]{ new object[]{ "E3 の前・まだ見ていない", s, new[]{ new object[]{ "Flag_TC_Rumble", false }, new object[]{ Done, false } } } }, null);
  Range(g, 1080, 700, 540, 30); }

// ---------------- E3 扉の前のイベント（開始ラインを越えたら。1回だけ） ----------------
var e3 = Seq("E3 扉の前（シグルでも開かない → ノアが触れると光る → ランプが明滅 → それでも開かない）", true);
NoahTo(e3, mDoorFront, 1.8f); FaceNoah(e3, 0);
Show(e3, new[]{ sigleS }, new[]{ sigleN });                 // シグルが振り向く
AddWait(e3, 0.3f);
Talk(e3, "F1_TC_DoorFirst");
// ノアが扉に触れる（会話を飛ばしても、ここから下は必ず実行される）
FaceNoah(e3, 0); AddWait(e3, 0.3f);
Se(e3, seTouch); SetF(e3, "Flag_TC_NoteGlowed", true); Fx(e3, fxSpawn, door); Shake(e3, 0.06f, 0.4f, false);
AddWait(e3, 0.6f);
// ランプ2つを2回明滅 → 消灯に戻す（ランプのフラグはまだ OFF なので、最後は消えた絵）
for (int i = 0; i < 2; i++) { Show(e3, new[]{ lampWOn, lampEOn }, new[]{ lampWOff, lampEOff }); AddWait(e3, 0.28f); Show(e3, new[]{ lampWOff, lampEOff }, new[]{ lampWOn, lampEOn }); AddWait(e3, 0.22f); }
AddWait(e3, 0.3f);
Talk(e3, "F1_TC_DoorFirst2");
SetF(e3, Done, true); AutoSv(e3);
{ var g = NewGO(objs, "E3 開始ライン（y=650）", P(1080, 650));
  Inter(g, "TC_DoorEvent", 1, new[]{ new object[]{ "E3 をまだ見ていない", e3, new[]{ new object[]{ Done, false } } } }, null);
  Range(g, 1080, 650, 540, 30); }

// ---------------- 調べる／話しかける（E3 のあと。ランプの数で切り替え。2つとも点いたときは大なぞの仕様で決める＝今は何も出さない） ----------------
{ var g = NewGO(objs, "扉を調べる（前に立って E）", P(1080, 270));
  var s0 = Seq("扉：ランプ0つ", true); Talk(s0, "F1_TC_DoorCheck0");
  var s1 = Seq("扉：ランプ1つ", true); Talk(s1, "F1_TC_DoorCheck1");
  Inter(g, "TC_Door", 0, new[]{ new object[]{ "ランプ0つ", s0, lamp0 }, new object[]{ "ランプ1つ（西）", s1, lamp1W }, new object[]{ "ランプ1つ（東）", s1, lamp1E } }, "調べる");
  Range(g, 1080, 270, 180, 180); }   // 指示書は x945〜1215 だが、シグル（1245）の話しかける範囲と重なるので扉の幅（990〜1170）にした
{ var g = sigle.gameObject;
  var s0 = Seq("シグル：ランプ0つ", true); Talk(s0, "F1_TC_SigleTalk0");
  var s1 = Seq("シグル：ランプ1つ", true); Talk(s1, "F1_TC_SigleTalk1");
  var c = g.AddComponent<UnityEngine.CircleCollider2D>(); c.isTrigger = true; c.radius = 0.5f;
  Inter(g, "TC_Sigle", 0, new[]{ new object[]{ "ランプ0つ", s0, lamp0 }, new object[]{ "ランプ1つ（西）", s1, lamp1W }, new object[]{ "ランプ1つ（東）", s1, lamp1E } }, "話す"); }
foreach (var v in new[]{ new object[]{ "西", 600f, W, "F1_TC_PipeWest0", "F1_TC_PipeWest1" }, new object[]{ "東", 1560f, E, "F1_TC_PipeEast0", "F1_TC_PipeEast1" } }) {
  string side = (string)v[0]; float x = (float)v[1]; string fl = (string)v[2];
  var g = NewGO(objs, side + "の管を調べる（壁の前に立って E）", P(x, 240));
  var s0 = Seq(side + "の管：伝わる前", true); Talk(s0, (string)v[3]);
  var s1 = Seq(side + "の管：伝わったあと", true); Talk(s1, (string)v[4]);
  Inter(g, "TC_Pipe" + side, 0, new[]{ new object[]{ "伝わる前", s0, new[]{ new object[]{ fl, false } } }, new object[]{ "伝わったあと", s1, new[]{ new object[]{ fl, true } } } }, "調べる");
  Range(g, x, 240, 360, 120); }   // 横の通路の北のふち（y180〜300）。ノアの当たり判定は足元なので、壁ぎわに立てば入る

// ---------------- 出口 ----------------
// 南：書斎へ戻る（書斎の北の出口の前 FromStage1_4）。範囲は一番下の帯（y1415〜1440）だけ：
// ノアの当たり判定は足元（中心が 0.61 下・半径 0.40）なので、出現位置 (1080,1305) では y1366 まで届く。前は y1400 からにしていて 0.06 マス重なり、直接再生すると着いた瞬間に書斎へ戻っていた（11-20。書斎から移ってきたときは暗転中で反応しないので気づかなかった）
{ var g = NewGO(objs, "南の出口（書斎へ戻る）", P(1080, 1428)); var s = Seq("南の出口：書斎へ", true);
  var a = s.AddComponent<SceneChangeEventAction>(); SetProp(a, "sceneName", "Stage1_3"); SetProp(a, "spawnPointId", "FromStage1_4");
  Inter(g, "TC_ExitSouth", 1, new[]{ new object[]{ "いつでも", s, null } }, null); Range(g, 1080, 1428, 180, 25); }
// 西・東：部屋はまだない（受け口だけ）。E3 のあとだけ。部屋ができたら、この流れの一言を消して部品「シーンを移る」（行き先の部屋の入口 ID）を足す。戻りの出現位置は FromWestRoom／FromEastRoom
foreach (var v in new[]{ new object[]{ "西", 240f }, new object[]{ "東", 1920f } }) {
  string side = (string)v[0]; float x = (float)v[1];
  var g = NewGO(objs, side + "の出口（" + side + "の部屋はまだない：受け口）", P(x, 360));
  var s = Seq(side + "の出口：まだ部屋がない（仮）", false); Bark(s, 2, "(この先の部屋は、まだ、、：仮)", 1.8f);
  Inter(g, "TC_Exit" + side, 1, new[]{ new object[]{ "E3 のあと（仮：部屋ができたらシーンを移る）", s, new[]{ new object[]{ Done, true } } } }, null);
  Range(g, x, 360, 120, 180); }

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) if (c.level != RoomChecks.Level.Ok) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
