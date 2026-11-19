// Stage1_3（書斎）段階2・3：太鼓4台・答え合わせ2問・T2 の調律・手がかり・扉・ランプ・ヒント・シグル（NPC）・着いたとき／紹介のイベント。shosai_1_scene のすぐあとに流す（1 が物を作り直すので、2 だけを2回流さない）
// @helpers
const string ScenePath = "Assets/Scenes/Stage1_3.unity";
var scene = OpenSceneSafe(ScenePath);
UnityEditor.AssetDatabase.Refresh();
System.Func<string, UnityEngine.GameObject> FindRoot = n => { foreach (var g in scene.GetRootGameObjects()) if (g.name == n) return g; return null; };
var room = FindRoot("書斎（Stage1_3）"); if (room == null) throw new System.Exception("先に shosai_1_scene");
if (room.transform.Find("仕掛けとイベント") != null) throw new System.Exception("もう組んである。shosai_1_scene から流し直す");
var objs = room.transform.Find("物");

System.Func<float, float, UnityEngine.Vector3> P = (x, y) => new UnityEngine.Vector3((x - 720f) / 100f, (540f - y) / 100f, 0f);
System.Func<string, UnityEngine.Object> A = path => { var o = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path); if (o == null) throw new System.Exception("no asset " + path); return o; };
System.Func<string, ItemData> Item = n => (ItemData)A("Assets/Resources/Items/" + n + ".asset");
System.Func<string, NoteData> Note = n => (NoteData)A("Assets/Resources/Notes/Shosai/Note_" + n + ".asset");
System.Func<string, DialogueData> T = id => Dlg("Scenario_F1", id);
var seHit = A("Assets/SE/SE-18_TimpaniAttacks/Se_ring_hit.asset");
var seOk = A("Assets/SE/SE-22_Puzzle/Se_codelock_correct.asset");
var seNg = A("Assets/SE/SE-22_Puzzle/Se_codelock_wrong.asset");
var seDoorOpen = A("Assets/SE/SE-22_Puzzle/Se_door_open.asset");
var seDoorClose = A("Assets/SE/Se_Door_Close.asset");
var sePunch = A("Assets/SE/SE-06_Door/Se_door_force_open.asset");
var sePage = A("Assets/SE/SE-24_PageTurn/Se_page_turn.asset");
var fxRing = A("Assets/Data/Effect/Sample/Fx_Sample_AttackRing.asset");
var fxWall = A("Assets/Data/Effect/Sample/Fx_Sample_Wall.asset");
var mallet = Item("Item_Mallet"); var tkey = Item("Item_TuningKey");

var root = NewGO(room.transform, "仕掛けとイベント", room.transform.position).transform;
var evRoot = NewGO(root, "イベント（流れ）", root.position).transform;
var gimRoot = NewGO(root, "答え合わせと状態", root.position).transform;
var reRoot = NewGO(root, "見た目の切り替え・ヒント", root.position).transform;
var markRoot = NewGO(root, "目印", root.position).transform;

// ---------------- 小道具 ----------------
System.Func<string, bool, UnityEngine.GameObject> Seq = (n, lockP) => { var g = NewGO(evRoot, n, evRoot.position); MakeSeq(g, lockP); return g; };
System.Action<UnityEngine.GameObject, string> Talk = (g, id) => AddTalk(g, T(id));
System.Action<UnityEngine.GameObject, int, string, float> Bark = (g, who, text, sec) => { var a = g.AddComponent<BarkEventAction>(); var so = new UnityEditor.SerializedObject(a); var ln = so.FindProperty("lines"); ln.arraySize = 1; var e = ln.GetArrayElementAtIndex(0); e.FindPropertyRelative("speaker").enumValueIndex = who; e.FindPropertyRelative("text").stringValue = text; e.FindPropertyRelative("seconds").floatValue = sec; so.FindProperty("waitUntilDone").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Action<UnityEngine.GameObject, UnityEngine.Object> Se = (g, se) => { var a = g.AddComponent<SeEventAction>(); SetProp(a, "seOnEnter", se); };
System.Action<UnityEngine.GameObject, UnityEngine.Object, UnityEngine.Transform> Fx = (g, fx, at) => { var a = g.AddComponent<EffectEventAction>(); SetProp(a, "effectOnEnter", fx); SetProp(a, "spawnAt", 2); SetProp(a, "customPoint", at); };
System.Action<UnityEngine.GameObject, float, float, bool> Shake = (g, st, du, w) => { var a = g.AddComponent<ShakeEventAction>(); SetProp(a, "strength", st); SetProp(a, "duration", du); SetProp(a, "waitUntilDone", w); };
System.Action<UnityEngine.GameObject, string, bool> SetF = (g, f, v) => { var a = g.AddComponent<SetFlagEventAction>(); a.AddFlagForEditor(Flag(f), v); };
System.Action<UnityEngine.GameObject, NoteData> AddNote = (g, n) => { var a = g.AddComponent<NoteEventAction>(); SetProp(a, "note", n); SetProp(a, "show", false); };
System.Action<UnityEngine.GameObject, ItemData> Give = (g, it) => { var a = g.AddComponent<ItemGiveEventAction>(); SetProp(a, "item", it); SetProp(a, "count", 1); };
System.Action<UnityEngine.GameObject, GimmickState, int> Gim = (g, st, cmd) => { var a = g.AddComponent<GimmickEventAction>(); SetProp(a, "target", st); SetProp(a, "command", cmd); };
System.Action<UnityEngine.GameObject, UnityEngine.Transform, UnityEngine.Transform, float> MoveTo = (g, who, dest, sec) => { var a = g.AddComponent<MoveObjectEventAction>(); SetProp(a, "targetKind", 0); SetProp(a, "target", who); SetProp(a, "type", 2); SetProp(a, "destinationKind", 0); SetProp(a, "destination", dest); SetProp(a, "offset", UnityEngine.Vector2.zero); SetProp(a, "duration", sec); SetProp(a, "ease", 2); SetProp(a, "waitUntilDone", true); };
System.Action<UnityEngine.GameObject, int> FaceNoah = (g, dir) => { var a = g.AddComponent<FaceDirectionEventAction>(); SetProp(a, "who", 0); SetProp(a, "direction", dir); };
System.Action<UnityEngine.GameObject, UnityEngine.Transform, float> Look = (g, t, hold) => { var a = g.AddComponent<CameraLookEventAction>(); SetProp(a, "target", t); SetProp(a, "moveTime", 0.7f); SetProp(a, "holdTime", hold); SetProp(a, "returnAfter", true); };
System.Action<UnityEngine.GameObject> AutoSv = g => g.AddComponent<AutoSaveEventAction>();

// 条件（GameCondition）を書く：flags = (名前, ON か)、items = (アイテム, 持っていない か)、gimmicks = (状態, 比べ方, 値)
System.Action<UnityEditor.SerializedObject, string, object[][], object[][], object[][]> Cond = (so, path, fl, it, gm) => {
  var f = so.FindProperty(path + ".flags"); f.arraySize = fl == null ? 0 : fl.Length;
  for (int i = 0; fl != null && i < fl.Length; i++) { var e = f.GetArrayElementAtIndex(i); e.FindPropertyRelative("flag").objectReferenceValue = Flag((string)fl[i][0]); e.FindPropertyRelative("state").enumValueIndex = (bool)fl[i][1] ? 0 : 1; }
  var t = so.FindProperty(path + ".items"); t.arraySize = it == null ? 0 : it.Length;
  for (int i = 0; it != null && i < it.Length; i++) { var e = t.GetArrayElementAtIndex(i); e.FindPropertyRelative("item").objectReferenceValue = (ItemData)it[i][0]; e.FindPropertyRelative("count").intValue = 1; e.FindPropertyRelative("notHave").boolValue = (bool)it[i][1]; }
  var g = so.FindProperty(path + ".gimmicks"); g.arraySize = gm == null ? 0 : gm.Length;
  for (int i = 0; gm != null && i < gm.Length; i++) { var e = g.GetArrayElementAtIndex(i); e.FindPropertyRelative("target").objectReferenceValue = (GimmickState)gm[i][0]; e.FindPropertyRelative("compare").enumValueIndex = (int)gm[i][1]; e.FindPropertyRelative("value").intValue = (int)gm[i][2]; } };

// 見た目の切り替え（GimmickReaction）
System.Func<string, object[][], object[][], object[][], UnityEngine.GameObject[], UnityEngine.GameObject[], bool, UnityEngine.GameObject, GimmickReaction> React = (n, fl, it, gm, show, hide, latch, onMet) => {
  var g = NewGO(reRoot, n, reRoot.position); var r = g.AddComponent<GimmickReaction>(); var so = new UnityEditor.SerializedObject(r);
  Cond(so, "condition", fl, it, gm);
  so.FindProperty("latch").boolValue = latch;
  var s = so.FindProperty("showWhenMet"); s.arraySize = show == null ? 0 : show.Length; for (int i = 0; show != null && i < show.Length; i++) s.GetArrayElementAtIndex(i).objectReferenceValue = show[i];
  var h = so.FindProperty("hideWhenMet"); h.arraySize = hide == null ? 0 : hide.Length; for (int i = 0; hide != null && i < hide.Length; i++) h.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
  if (onMet != null) so.FindProperty("onMet").objectReferenceValue = onMet.GetComponent<EventSequence>();
  so.FindProperty("saveId").stringValue = latch ? "Shosai_" + n : "";
  so.ApplyModifiedPropertiesWithoutUndo(); return r; };

// 調べる範囲（Is Trigger の四角。ピクセルの中心・大きさ）
System.Action<UnityEngine.GameObject, float, float, float, float> Range = (g, x, y, w, h) => { var b = g.GetComponent<UnityEngine.BoxCollider2D>(); if (b == null) b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; var c = P(x, y) - g.transform.position; b.offset = new UnityEngine.Vector2(c.x, c.y); b.size = new UnityEngine.Vector2(w / 100f, h / 100f); };
// 調べると決まった流れを実行する（ItemInteraction）。rules = (ラベル, 流れの GO, flags, items)
System.Action<UnityEngine.GameObject, string, int, object[][], UnityEngine.GameObject, string> Inter = (g, saveId, trigger, rules, otherwise, prompt) => {
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
    var its = (object[][])rules[i][3]; var ic = r.FindPropertyRelative("conditions"); ic.arraySize = its == null ? 0 : its.Length;
    for (int k = 0; its != null && k < its.Length; k++) { var e = ic.GetArrayElementAtIndex(k); e.FindPropertyRelative("item").objectReferenceValue = (ItemData)its[k][0]; e.FindPropertyRelative("count").intValue = 1; e.FindPropertyRelative("notHave").boolValue = (bool)its[k][1]; }
  }
  if (otherwise != null) so.FindProperty("otherwise").objectReferenceValue = otherwise.GetComponent<EventSequence>();
  so.ApplyModifiedPropertiesWithoutUndo(); };
// 調べるたびに変わる（ExamineSequence）
System.Action<UnityEngine.GameObject, string, UnityEngine.GameObject[]> Examine = (g, saveId, seqs) => {
  if (g.GetComponent<UnityEngine.Collider2D>() == null) { var b0 = g.AddComponent<UnityEngine.BoxCollider2D>(); b0.isTrigger = true; } var e = g.AddComponent<ExamineSequence>(); var so = new UnityEditor.SerializedObject(e); var l = so.FindProperty("sequences"); l.arraySize = seqs.Length;
  for (int i = 0; i < seqs.Length; i++) l.GetArrayElementAtIndex(i).objectReferenceValue = seqs[i].GetComponent<EventSequence>();
  so.FindProperty("afterLast").enumValueIndex = 0; so.FindProperty("saveId").stringValue = saveId; so.ApplyModifiedPropertiesWithoutUndo(); };
System.Func<string, int, int, bool, GimmickState> State = (n, count, init, save) => {
  var g = NewGO(gimRoot, n, gimRoot.position); var s = g.AddComponent<GimmickState>(); SetProp(s, "displayName", n); SetProp(s, "stateCount", count); SetProp(s, "initialValue", init); SetProp(s, "wrap", false); SetProp(s, "save", save); SetProp(s, "saveId", save ? "Shosai_" + n : ""); return s; };
System.Func<string, UnityEngine.Transform> Obj = n => { var t = objs.Find(n); if (t == null) throw new System.Exception("no obj " + n); return t; };

// ---------------- 目印 ----------------
var mDoor = NewGO(markRoot, "目印_シグルが扉を殴る位置", P(270, 140)).transform;
var mSigle = NewGO(markRoot, "目印_シグルが待つ位置", P(270, 400)).transform;
var mDrums = NewGO(markRoot, "目印_ティンパニ列（カメラ）", P(825, 300)).transform;
var mExitOut = NewGO(markRoot, "目印_シグルが出ていく先（出口の先）", P(270, 20)).transform;
var sigle = Obj("シグル（NPC）");

// ---------------- 太鼓（T1・T3・T4、T2 は調律の前後で3つに分ける） ----------------
var tname = new[]{ "T1（32インチ・低いド）", "T2（29インチ・ミ）", "T3（26インチ・ソ）", "T4（23インチ・高いド）" };
var tx = new[]{ 480f, 710f, 940f, 1170f }; var tr = new[]{ 100f, 90f, 80f, 70f };
var tnote = new[]{ "♪ ド（低）", "♪ ミ", "♪ ソ", "♪ ド（高）" };
var drumState = new GimmickState[4];
var bareHands = Seq("太鼓：マレットがないとき", false); Bark(bareHands, 2, "(素手じゃ、こもってる、、)", 1.8f);
System.Action<UnityEngine.GameObject, int> MakeDrum = (g, i) => {
  var s = g.AddComponent<GimmickState>(); SetProp(s, "displayName", "叩いた " + tname[i]); SetProp(s, "stateCount", 2); SetProp(s, "save", false); SetProp(s, "resetAfter", 0.15f);
  var hit = Seq("太鼓：叩いたとき " + tname[i], false); Se(hit, seHit); Fx(hit, fxRing, g.transform.parent == objs ? g.transform : g.transform.parent); Bark(hit, 2, tnote[i], 0.9f);
  SetProp(s, "onChanged", hit.GetComponent<EventSequence>());
  var sw = g.AddComponent<GimmickSwitch>(); var so = new UnityEditor.SerializedObject(sw);
  so.FindProperty("action").enumValueIndex = 1; so.FindProperty("user").enumValueIndex = 1;
  Cond(so, "condition", null, new[]{ new object[]{ mallet, false } }, null);
  so.FindProperty("conditionNotMet").objectReferenceValue = bareHands.GetComponent<EventSequence>();
  so.ApplyModifiedPropertiesWithoutUndo();
  Range(g, tx[i], 225 + tr[i] + 35, 140, 60);
  drumState[i] = s; };
foreach (int i in new[]{ 0, 2, 3 }) MakeDrum(Obj(tname[i]).gameObject, i);

var t2 = Obj(tname[1]);
var t2Hit = NewGO(t2, "T2_叩く（調律のあと）", t2.position); MakeDrum(t2Hit, 1);
var t2Bad = NewGO(t2, "T2_濁った音（キーがない・調律の前）", t2.position);
{ var first = Seq("T2：濁った音（1回目）", true); Se(first, seHit); Fx(first, fxWall, t2); Talk(first, "F1_TP_T2Bad");
  var again = Seq("T2：濁った音（2回目から）", false); Se(again, seHit); Fx(again, fxWall, t2); Bark(again, 2, "(この音、ゆれてる、、)", 1.6f);
  Examine(t2Bad, "Shosai_T2Bad", new[]{ first, again }); Range(t2Bad, 710, 225 + 90 + 35, 140, 60); }
var tuned = Seq("T2：調律できたとき", true); SetF(tuned, "Flag_Shosai_T2Tuned", true); Talk(tuned, "F1_TP_Tuned"); AddNote(tuned, Note("Shosai_Tuning"));
var pegPrefab = (UnityEngine.GameObject)A("Assets/Prefab/Gimmicks/Timpani/Peg.prefab");
var peg = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pegPrefab, scene);
peg.name = "T2_調律（キーがある・調律の前。輪＋E を3回）"; peg.transform.SetParent(t2, true); peg.transform.position = P(710, 350);
{ peg.GetComponent<UnityEngine.CircleCollider2D>().radius = 0.45f; peg.transform.Find("見た目（仮）").localScale = new UnityEngine.Vector3(1.4f, 1.4f, 1f);
  var pg = peg.GetComponent<TimpaniPeg>(); SetProp(pg, "requiredSuccess", 3); SetProp(pg, "shrinkSeconds", 1.8f); SetProp(pg, "startRadius", 1.0f); SetProp(pg, "targetRadius", 0.35f); SetProp(pg, "tolerance", 0.12f); SetProp(pg, "onTightened", tuned.GetComponent<EventSequence>()); }

// ---------------- 答え合わせ ----------------
var q1Prog = State("第1問の進み具合（ランプ1〜3）", 4, 0, false);
var q2Prog = State("第2問の進み具合（ランプ4〜8）", 6, 0, false);
var q1Fail = State("第1問の失敗回数", 3, 0, true);
var q2Fail = State("第2問の失敗回数", 6, 0, true);
var wrong1 = Seq("第1問：間違えたとき", false); Gim(wrong1, q1Fail, 3); Bark(wrong1, 2, "(順番が違うみたいだ)", 1.6f);
var wrong2 = Seq("第2問：間違えたとき", false); Gim(wrong2, q2Fail, 3); Bark(wrong2, 2, "(順番が違うみたいだ)", 1.6f);
// 第1問：格子が消える瞬間を見せる（エフェクト・揺れ）→ 光とキーが出る → 「本棚の格子が開いた」
var shelfT = Obj("東の本棚");
var solved1 = Seq("第1問：正解したとき（本棚の格子が開く）", true); AddWait(solved1, 0.4f); FaceNoah(solved1, 3);
Se(solved1, seDoorOpen); Fx(solved1, A("Assets/Data/Effect/Sample/Fx_Sample_Vanish.asset"), shelfT); Shake(solved1, 0.12f, 0.3f, false);
SetF(solved1, "Flag_Shosai_Q1Clear", true); AddWait(solved1, 0.3f); Fx(solved1, A("Assets/Data/Effect/Sample/Fx_Sample_Spawn.asset"), shelfT); AddWait(solved1, 0.8f);
Talk(solved1, "F1_TP_Q1Clear"); AutoSv(solved1);
var solved2 = Seq("第2問：正解したとき（出口が開く → 紹介 → シグルが先へ）", true);
AddWait(solved2, 0.4f); Se(solved2, seDoorOpen); Shake(solved2, 0.25f, 0.6f, true); SetF(solved2, "Flag_Shosai_DoorOpen", true); AddWait(solved2, 0.8f);
Talk(solved2, "F1_TP_Q2Clear"); Talk(solved2, "F1_TP_Intro"); SetF(solved2, "Flag_Sigle_NameKnown", true);
MoveTo(solved2, sigle, mSigle, 0.01f); MoveTo(solved2, sigle, mExitOut, 1.6f); SetF(solved2, "Flag_Shosai_SigleLeft", true); AutoSv(solved2);

System.Func<string, GimmickState[], GimmickState, UnityEngine.GameObject, UnityEngine.GameObject, object[][], GimmickState> Puzzle = (n, inputs, prog, ok, ng, fl) => {
  var g = NewGO(gimRoot, n, gimRoot.position); var s = g.AddComponent<GimmickState>(); SetProp(s, "displayName", n); SetProp(s, "save", true); SetProp(s, "saveId", "Shosai_" + n);
  var pc = g.AddComponent<PuzzleCheck>(); var so = new UnityEditor.SerializedObject(pc);
  var l = so.FindProperty("inputs"); l.arraySize = inputs.Length; for (int i = 0; i < inputs.Length; i++) l.GetArrayElementAtIndex(i).objectReferenceValue = inputs[i];
  so.FindProperty("mode").enumValueIndex = 1; so.FindProperty("timing").enumValueIndex = 0;
  Cond(so, "condition", fl, null, null);
  so.FindProperty("hitInputs").boolValue = true; so.FindProperty("progressState").objectReferenceValue = prog;
  so.FindProperty("lockInputs").boolValue = false; so.FindProperty("resetOnWrong").boolValue = true;
  so.FindProperty("onSolved").objectReferenceValue = ok.GetComponent<EventSequence>(); so.FindProperty("solvedSe").objectReferenceValue = seOk;
  so.FindProperty("onWrong").objectReferenceValue = ng.GetComponent<EventSequence>(); so.FindProperty("wrongSe").objectReferenceValue = seNg;
  so.ApplyModifiedPropertiesWithoutUndo(); return s; };
var q1 = Puzzle("第1問（3音：T1→T3→T4）", new[]{ drumState[0], drumState[2], drumState[3] }, q1Prog, solved1, wrong1, null);
var q2 = Puzzle("第2問（5音：T1→T3→T2→T3→T4）", new[]{ drumState[0], drumState[2], drumState[1], drumState[2], drumState[3] }, q2Prog, solved2, wrong2,
  new[]{ new object[]{ "Flag_Shosai_Q1Clear", true }, new object[]{ "Flag_Shosai_T2Tuned", true } });

// ---------------- 見た目の切り替え ----------------
var GE = 4; // この値以上
React("T2：調律のあとは「叩く」", new[]{ new object[]{ "Flag_Shosai_T2Tuned", true } }, null, null, new[]{ t2Hit }, null, false, null);
React("T2：キーがあって調律の前は「調律」", new[]{ new object[]{ "Flag_Shosai_T2Tuned", false } }, new[]{ new object[]{ tkey, false } }, null, new[]{ peg }, null, false, null);
React("T2：キーがなく調律の前は「濁った音」", new[]{ new object[]{ "Flag_Shosai_T2Tuned", false } }, new[]{ new object[]{ tkey, true } }, null, new[]{ t2Bad }, null, false, null);
var lamps = Obj("ランプ");
for (int i = 0; i < 8; i++) {
  var l = lamps.Find("ランプ" + (i + 1)); var on = l.Find("絵_点").gameObject; var off = l.Find("絵_消").gameObject;
  var gm = i < 3 ? new[]{ new object[]{ q1Prog, GE, i + 1 } } : new[]{ new object[]{ q2Prog, GE, i - 2 } };
  React("ランプ" + (i + 1) + (i < 3 ? "（第1問 " + (i + 1) + " 音目）" : "（第2問 " + (i - 2) + " 音目）"), null, null, gm, new[]{ on }, new[]{ off }, false, null);
}
var doorA = Obj("扉A（入口）"); var exitDoor = Obj("北西の出口（音符文様の仕掛け扉）");
var walk = room.transform.Find("歩ける範囲");
var exitPass = walk.Find("通路_北西の出口（開くと表示）").gameObject;
var doorAClose = Seq("扉A：閉まったとき", false); Se(doorAClose, seDoorClose);
React("扉A：ロックされたら閉める", new[]{ new object[]{ "Flag_Shosai_DoorsLocked", true } }, null, null, new[]{ doorA.Find("絵_閉").gameObject }, new[]{ doorA.Find("絵_開").gameObject }, true, doorAClose);
var exitArea = NewGO(exitDoor, "出口の先（この先の廊下はまだない：仮）", P(270, 60));
React("出口：第2問の正解で開く", new[]{ new object[]{ "Flag_Shosai_DoorOpen", true } }, null, null, new[]{ exitDoor.Find("絵_開").gameObject, exitPass, exitArea }, new[]{ exitDoor.Find("絵_閉").gameObject }, true, null);
React("シグル：先へ進んだら消す", new[]{ new object[]{ "Flag_Shosai_SigleLeft", true } }, null, null, null, new[]{ sigle.gameObject }, true, null);
var mStand = Obj("マレット置き場");
React("マレット置き場：取ったら空にする", null, new[]{ new object[]{ mallet, false } }, null, new[]{ mStand.Find("絵_なし").gameObject }, new[]{ mStand.Find("絵_あり").gameObject }, false, null);
React("楽譜断片：取ったら消す", new[]{ new object[]{ "Flag_Shosai_ClueFragment", true } }, null, null, null, new[]{ Obj("楽譜断片（第2問の手がかり）").gameObject }, true, null);
// 本棚の見た目：第1問のあとは格子を消す／取るまでは光とキーの絵を出す
React("本棚の格子：第1問で開く", new[]{ new object[]{ "Flag_Shosai_Q1Clear", true } }, null, null, null, new[]{ Obj("本棚の格子（仮の絵・第1問で開く）").gameObject }, true, null);
React("本棚の光とキー：第1問のあと取るまで", new[]{ new object[]{ "Flag_Shosai_Q1Clear", true }, new object[]{ "Flag_Shosai_ClueFragment", false } }, null, null, new[]{ Obj("取れる目印（仮の絵・第1問のあと取るまで）").gameObject }, null, false, null);

// ヒント（失敗の回数・手がかりがそろった）
{ var h = Seq("ヒント：第1問で2回失敗", false); Bark(h, 2, "(大きい方から、かな、、)", 2.2f);
  React("ヒント：第1問で2回失敗", null, null, new[]{ new object[]{ q1Fail, GE, 2 } }, null, null, true, h); }
{ var h = Seq("ヒント：第2問で3回失敗", false); Bark(h, 2, "(最初は ♪ド ♪ソ ♪ミ、、だったはず)", 2.6f);
  React("ヒント：第2問で3回失敗", null, null, new[]{ new object[]{ q2Fail, GE, 3 } }, null, null, true, h); }
{ var h = Seq("ヒント：第2問で5回失敗（メモの楽譜が読める）", false); AddNote(h, Note("Shosai_Q2Answer")); Bark(h, 2, "(メモの楽譜が、読み取れた、、Tab で見よう)", 2.6f);
  React("ヒント：第2問で5回失敗", null, null, new[]{ new object[]{ q2Fail, GE, 5 } }, null, null, true, h); }
{ var h = Seq("手がかりが3つそろった（メモにまとめる）", false); AddNote(h, Note("Shosai_Q2Answer")); Bark(h, 2, "(ド、ソ、ミ、ソ、高いド、、メモにまとめよう)", 2.6f);
  React("手がかりが3つそろった", new[]{ new object[]{ "Flag_Shosai_ClueFragment", true }, new object[]{ "Flag_Shosai_ClueFloor", true }, new object[]{ "Flag_Shosai_ClueShelfMemo", true } }, null, null, null, null, true, h); }

// ---------------- 調べる物 ----------------
{ var g = mStand.gameObject; var get = Seq("マレット置き場：取る", true); Give(get, mallet); Talk(get, "F1_TP_Mallet"); AddNote(get, Note("Shosai_TimpaniBasic"));
  Inter(g, "Shosai_Mallet", 0, new[]{ new object[]{ "マレットを取る（まだ持っていないとき）", get, null, new[]{ new object[]{ mallet, true } } } }, null, "取る");
  Range(g, 560, 690, 100, 40); }
{ var g = Obj("机の楽譜（第1問の手がかり）").gameObject; var s = Seq("机の楽譜：読む", true); Se(s, sePage); Talk(s, "F1_TP_Score"); AddNote(s, Note("Shosai_Score")); SetF(s, "Flag_Shosai_ClueScore", true);
  Inter(g, "Shosai_Score", 0, new[]{ new object[]{ "読む", s, null, null } }, null, "読む"); Range(g, 720, 700, 160, 40); }
{ var g = NewGO(objs, "本棚の側板の傷を調べる", P(170, 405)); var s = Seq("側板の傷：見る", true); Talk(s, "F1_TP_Wall"); AddNote(s, Note("Shosai_Wall")); SetF(s, "Flag_Shosai_ClueWall", true);
  Inter(g, "Shosai_Wall", 0, new[]{ new object[]{ "見る", s, null, null } }, null, null); Range(g, 170, 405, 50, 140); }
{ var g = NewGO(objs, "絨毯の叩き跡を調べる", P(720, 475)); var s = Seq("叩き跡：見る", true); Talk(s, "F1_TP_Floor"); AddNote(s, Note("Shosai_Floor")); SetF(s, "Flag_Shosai_ClueFloor", true);
  Inter(g, "Shosai_Floor", 0, new[]{ new object[]{ "見る", s, null, null } }, null, null); Range(g, 720, 475, 400, 40); }
{ var g = Obj("東の本棚").gameObject;
  // 1回調べるだけで、チューニングキー・楽譜断片・棚のメモをまとめて手に入れる（2026-11-20 ユーザーの指示「二回調べないとメモが入手できないのは分かりにくい」）
  var take = Seq("東の本棚：キー・楽譜断片・棚のメモをまとめて取る", true); Se(take, sePage); Give(take, tkey); Talk(take, "F1_TP_Fragment");
  AddNote(take, Note("Shosai_Fragment")); AddNote(take, Note("Shosai_ShelfMemo")); SetF(take, "Flag_Shosai_ClueFragment", true); SetF(take, "Flag_Shosai_ClueShelfMemo", true);
  var done = Seq("東の本棚：もう何もない", false); Bark(done, 2, "(もう、何もなさそう)", 1.6f);
  var locked = Seq("東の本棚：格子で取れない（第1問の前）", true); Talk(locked, "F1_TP_ShelfLocked");
  Inter(g, "Shosai_Shelf", 0, new[]{
    new object[]{ "第1問のあと：キー・断片・メモをまとめて取る", take, new[]{ new object[]{ "Flag_Shosai_Q1Clear", true }, new object[]{ "Flag_Shosai_ClueFragment", false } }, null },
    new object[]{ "取ったあと", done, new[]{ new object[]{ "Flag_Shosai_ClueFragment", true } }, null } }, locked, null);
  Range(g, 1300, 420, 100, 50); }
{ var g = NewGO(objs, "扉A を調べる", P(1340, 630)); var s = Seq("扉A：閉まっている", true); Talk(s, "F1_TP_DoorA");
  Inter(g, "Shosai_DoorA", 0, new[]{ new object[]{ "閉まっている（ロックのあと）", s, new[]{ new object[]{ "Flag_Shosai_DoorsLocked", true } }, null } }, null, null); Range(g, 1340, 630, 20, 160); }
{ var g = NewGO(exitDoor, "出口とランプを調べる（開くまで）", P(270, 175)); var s = Seq("出口：ランプの説明", true); Talk(s, "F1_TP_ExitDoor");
  Inter(g, "Shosai_ExitDoor", 0, new[]{ new object[]{ "開くまで", s, new[]{ new object[]{ "Flag_Shosai_DoorOpen", false } }, null } }, null, null); Range(g, 270, 175, 160, 50); }
{ var s = Seq("出口の先：まだない（仮）", false); Bark(s, 2, "(シグルを、追いかけなきゃ、、)", 2.0f);
  Inter(exitArea, "Shosai_ExitArea", 1, new[]{ new object[]{ "入ったら一言（仮：この先の廊下ができたら「シーンを移る」を足す）", s, null, null } }, null, null); Range(exitArea, 270, 60, 180, 60);
  exitArea.SetActive(false); }
{ var g = sigle.gameObject; var t1 = Seq("シグル：1回目", true); Talk(t1, "F1_TP_SigleTalk1"); var t2s = Seq("シグル：2回目から", true); Talk(t2s, "F1_TP_SigleTalk2");
  var c = g.AddComponent<UnityEngine.CircleCollider2D>(); c.isTrigger = true; c.radius = 0.55f; Examine(g, "Shosai_SigleTalk", new[]{ t1, t2s }); }

// ---------------- 着いたとき（シグルが仕掛け扉を殴る → 扉がロック） ----------------
var arrive = room.transform.Find("着いたとき（使うなら表示する）").gameObject; arrive.name = "着いたとき（シグルが扉を殴る → ロック）"; arrive.SetActive(true);
foreach (var c in arrive.GetComponents<EventAction>()) UnityEngine.Object.DestroyImmediate(c);
{ var st = arrive.GetComponent<SceneStartEvent>(); var so = new UnityEditor.SerializedObject(st); Cond(so, "condition", new[]{ new object[]{ "Flag_Shosai_DoorsLocked", false } }, null, null); so.FindProperty("delay").floatValue = 0.2f; so.ApplyModifiedPropertiesWithoutUndo(); }
MoveTo(arrive, sigle, mDoor, 0.01f); FaceNoah(arrive, 2); AddWait(arrive, 0.5f);
Se(arrive, sePunch); Shake(arrive, 0.15f, 0.25f, true); AddWait(arrive, 0.35f); Se(arrive, sePunch); Shake(arrive, 0.15f, 0.25f, true);
Talk(arrive, "F1_TP_Enter");
Se(arrive, sePunch); Shake(arrive, 0.4f, 0.6f, true); SetF(arrive, "Flag_Shosai_DoorsLocked", true); AddWait(arrive, 0.6f);
Talk(arrive, "F1_TP_Enter2");
Look(arrive, mDrums, 1.4f);
MoveTo(arrive, sigle, mSigle, 1.0f);
AutoSv(arrive);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
Log.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");
foreach (var c in RoomChecks.Check(room.GetComponent<Room>())) if (c.level != RoomChecks.Level.Ok) Log.Append("check ").Append(c.level).Append(": ").Append(c.text).Append("\n");
return Log.ToString();
