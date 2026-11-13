// Stage_1_3 C3：戦闘中の一言（TimpaniBarks）を、合図をきっかけにした一言データ（Resources/Barks/Timpani/）に移す
// ・Flag_Timpani_InBattle（セーブしない）を作り、ティンパニ戦の戦闘エリアの「戦闘中フラグ」に入れる（一言の条件「戦闘中だけ」に使う）
// ・TimpaniBarks の各できごと → 標準の合図。候補の行 → 候補（1行ずつ）。確率・次に出せるまでをそのまま。大事なできごとは優先度 60・一言どうしの間を待たない
// ・「戦闘中の一言」オブジェクトを消す（TimpaniBarks のスクリプトはこのあとで削除する。一度だけ実行するレシピ）
// 前提：Stage_1_3 を開いている。標準の合図が Assets/Data/Signals/標準/ にある
var sb = new System.Text.StringBuilder();
var root = UnityEngine.GameObject.Find("ティンパニ部屋");
if (root == null) return "no ティンパニ部屋";
var barkGo = root.transform.Find("戦闘中の一言");
if (barkGo == null) return "no 戦闘中の一言 (already migrated?)";
var tbComp = barkGo.GetComponent("TimpaniBarks");
var so = new UnityEditor.SerializedObject(tbComp);

// 戦闘中フラグ
string flagPath = "Assets/Data/Flags/Flag_Timpani_InBattle.asset";
var flag = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(flagPath);
if (flag == null)
{
    flag = UnityEngine.ScriptableObject.CreateInstance<GameFlag>();
    UnityEditor.AssetDatabase.CreateAsset(flag, flagPath);
    var fso = new UnityEditor.SerializedObject(flag);
    fso.FindProperty("id").stringValue = "Timpani_InBattle";
    fso.FindProperty("memo").stringValue = "ティンパニ戦の戦闘エリアが動いている間 ON（戦闘エリアの戦闘中フラグ）。戦闘中の一言の条件に使う。セーブしない";
    fso.FindProperty("saved").boolValue = false;
    fso.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("created flag\n");
}
var battle = so.FindProperty("battle").objectReferenceValue as BattleArea;
if (battle != null)
{
    var bso = new UnityEditor.SerializedObject(battle);
    bso.FindProperty("battleFlag").objectReferenceValue = flag;
    bso.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("battle flag set on " + battle.name + "\n");
}

// できごと → 標準の合図・名前
string[] ids = { StandardSignals.PegHit, StandardSignals.PegMiss, StandardSignals.PegTightened, StandardSignals.Calmed,
                 StandardSignals.GuardEmpty, StandardSignals.GuardReady, StandardSignals.ItemGuard, StandardSignals.ItemPicked,
                 StandardSignals.Damaged, StandardSignals.LowHealth, StandardSignals.Stalled, StandardSignals.BattleStarted };
string[] names = { "ペグで押せた", "ペグを外した", "ペグが締まった", "調律できた", "守りが切れた", "守りが戻った",
                   "守りのアイテム", "アイテムを拾った", "ダメージ", "体力が少ない", "ヒント", "戦闘が始まった" };
bool[] important = { false, false, false, true, false, false, false, false, false, true, true, true };

var signals = new System.Collections.Generic.Dictionary<string, GameSignal>();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:GameSignal"))
{
    var s = UnityEditor.AssetDatabase.LoadAssetAtPath<GameSignal>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (s != null) signals[s.Id] = s;
}

string folder = "Assets/Resources/Barks/Timpani";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Barks", "Timpani");

var list = so.FindProperty("entries");
for (int i = 0; i < list.arraySize; i++)
{
    var e = list.GetArrayElementAtIndex(i);
    int when = e.FindPropertyRelative("when").enumValueIndex;
    if (when < 0 || when >= ids.Length) { sb.Append("skip when " + when + "\n"); continue; }
    GameSignal sig;
    if (!signals.TryGetValue(ids[when], out sig)) { sb.Append("no signal " + ids[when] + "\n"); continue; }

    string path = folder + "/Bark_Timpani_" + names[when] + ".asset";
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<BarkData>(path);
    if (d == null) { d = UnityEngine.ScriptableObject.CreateInstance<BarkData>(); UnityEditor.AssetDatabase.CreateAsset(d, path); }
    d.active = true;
    d.trigger = BarkTrigger.Signal;
    d.signal = sig;
    d.priority = important[when] ? 60 : 50;
    d.ignoreGap = important[when];
    d.chance = e.FindPropertyRelative("chance").floatValue;
    d.cooldown = e.FindPropertyRelative("cooldown").floatValue;
    d.delay = 0.0f;
    d.repeat = BarkRepeat.Always;
    d.condition = new GameCondition();
    d.condition.flags.Add(new FlagCondition { flag = flag, state = FlagCondition.State.On });
    d.memo = "ティンパニ戦（Stage_1_3）の戦闘中だけ。もとは TimpaniBarks の「" + names[when] + "」";
    d.variants.Clear();
    var lines = e.FindPropertyRelative("lines");
    for (int j = 0; j < lines.arraySize; j++)
    {
        var l = lines.GetArrayElementAtIndex(j);
        var v = new BarkVariant();
        v.lines.Add(new BarkLine
        {
            speaker = (BarkSpeaker)l.FindPropertyRelative("speaker").enumValueIndex,
            text = l.FindPropertyRelative("text").stringValue,
            seconds = l.FindPropertyRelative("seconds").floatValue,
            gap = l.FindPropertyRelative("gap").floatValue,
            se = l.FindPropertyRelative("se").objectReferenceValue as SeData,
        });
        d.variants.Add(v);
    }
    UnityEditor.EditorUtility.SetDirty(d);
    sb.Append("bark " + d.name + " variants=" + d.variants.Count + "\n");
}
UnityEditor.AssetDatabase.SaveAssets();

// 一言のオブジェクトを消す
var comps = barkGo.GetComponents<UnityEngine.Component>();
sb.Append("components on 戦闘中の一言: " + comps.Length + "\n");
UnityEngine.Object.DestroyImmediate(barkGo.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
sb.Append("scene saved\n");
return sb.ToString();
