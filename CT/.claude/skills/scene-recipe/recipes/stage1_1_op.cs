// Stage1_1：OP（OP_はじめ・OP_途中・OP_塔の入口）を作り直す。ルート「OP」を消して作る
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Stage1_1.unity") return "active scene is " + scene.path;

System.Func<string, DialogueData> D = n => {
    foreach (var dir in new[]{"Scenario_OP","Scenario_F1"}) {
        var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/" + dir + "/" + n + ".asset");
        if (d != null) return d;
    }
    throw new System.Exception("dialogue not found " + n);
};
var opDone = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_OP_Done.asset");
var sigleJoined = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Sigle_Joined.asset");

var old = UnityEngine.GameObject.Find("OP");
if (old != null) UnityEngine.Object.DestroyImmediate(old);
var root = new UnityEngine.GameObject("OP");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = new UnityEngine.Vector3(-115.3f, 20f, 0f);

// talk holder: child with a Dialogue component (no collider = never examined)
System.Func<UnityEngine.Transform, string, Dialogue> MakeTalk = (parent, n) => {
    var go = new UnityEngine.GameObject("会話_" + n);
    go.transform.SetParent(parent, false);
    var dlg = go.AddComponent<Dialogue>();
    var so = new UnityEditor.SerializedObject(dlg);
    so.FindProperty("dialogueData").objectReferenceValue = D(n);
    so.ApplyModifiedPropertiesWithoutUndo();
    return dlg;
};
System.Action<UnityEngine.GameObject, Dialogue> AddTalk = (go, dlg) => {
    var a = go.AddComponent<DialogueEventAction>();
    var so = new UnityEditor.SerializedObject(a);
    so.FindProperty("dialogue").objectReferenceValue = dlg;
    so.FindProperty("closeMode").enumValueIndex = 0; // Input
    so.FindProperty("waitUntilClosed").boolValue = true;
    so.ApplyModifiedPropertiesWithoutUndo();
};
System.Action<UnityEngine.GameObject, float> AddWait = (go, s) => {
    var a = go.AddComponent<WaitEventAction>();
    var so = new UnityEditor.SerializedObject(a);
    so.FindProperty("seconds").floatValue = s;
    so.ApplyModifiedPropertiesWithoutUndo();
};
System.Action<UnityEngine.GameObject, int, float> AddFade = (go, type, dur) => {
    var a = go.AddComponent<ScreenFadeEventAction>();
    var so = new UnityEditor.SerializedObject(a);
    so.FindProperty("type").enumValueIndex = type;
    so.FindProperty("duration").floatValue = dur;
    so.ApplyModifiedPropertiesWithoutUndo();
};
System.Action<UnityEngine.GameObject, bool> SetLock = (go, v) => {
    var so = new UnityEditor.SerializedObject(go.GetComponent<EventSequence>());
    so.FindProperty("lockPlayer").boolValue = v;
    so.ApplyModifiedPropertiesWithoutUndo();
};

// ---- 1. OP start (black -> monologue -> fade in -> Noah) ----
var start = new UnityEngine.GameObject("OP_はじめ（暗転からマップへ）");
start.transform.SetParent(root.transform, false);
var startEvent = start.AddComponent<SceneStartEvent>();
var seq = start.AddComponent<EventSequence>();
SetLock(start, true);
var talks = new UnityEngine.GameObject("会話");
talks.transform.SetParent(start.transform, false);
AddWait(start, 1.0f);
AddTalk(start, MakeTalk(talks.transform, "F1_00_Prologue"));
AddWait(start, 0.8f);
AddTalk(start, MakeTalk(talks.transform, "OP_01_Monologue"));
AddFade(start, 1, 1.5f);
AddWait(start, 0.3f);
AddTalk(start, MakeTalk(talks.transform, "OP_02_Desert"));
var setFlag = start.AddComponent<SetFlagEventAction>();
setFlag.AddFlagForEditor(opDone, true);
{
    var so = new UnityEditor.SerializedObject(startEvent);
    so.FindProperty("sequence").objectReferenceValue = seq;
    so.FindProperty("startDark").boolValue = true;
    var flags = so.FindProperty("condition.flags");
    flags.arraySize = 1;
    flags.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = opDone;
    flags.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 1; // OFF
    so.ApplyModifiedPropertiesWithoutUndo();
}

// ---- area talks (ItemInteraction, enter area, once, saved) ----
System.Action<string, string, string, UnityEngine.Vector2, UnityEngine.Vector2> MakeArea = (objName, talk, saveId, center, size) => {
    var go = new UnityEngine.GameObject(objName);
    go.transform.SetParent(root.transform, false);
    go.transform.position = new UnityEngine.Vector3(center.x, center.y, 0f);
    var box = go.AddComponent<UnityEngine.BoxCollider2D>();
    box.isTrigger = true; box.size = size;
    var inter = go.AddComponent<ItemInteraction>();
    var s = go.AddComponent<EventSequence>();
    SetLock(go, true);
    var t = new UnityEngine.GameObject("会話");
    t.transform.SetParent(go.transform, false);
    AddTalk(go, MakeTalk(t.transform, talk));
    var so = new UnityEditor.SerializedObject(inter);
    so.FindProperty("trigger").enumValueIndex = 1; // EnterArea
    so.FindProperty("saveId").stringValue = saveId;
    so.FindProperty("saveCompleted").boolValue = true;
    var rules = so.FindProperty("rules");
    rules.arraySize = 1;
    var r = rules.GetArrayElementAtIndex(0);
    r.FindPropertyRelative("label").stringValue = "1回だけ会話";
    r.FindPropertyRelative("sequence").objectReferenceValue = s;
    r.FindPropertyRelative("markCompleted").boolValue = true;
    r.FindPropertyRelative("consumeItems").boolValue = false;
    so.ApplyModifiedPropertiesWithoutUndo();
};
MakeArea("OP_途中（鐘の言い伝え）", "OP_03_Bell", "OP_Bell", new UnityEngine.Vector2(-115.2f, 21.5f), new UnityEngine.Vector2(14.4f, 1.0f));
MakeArea("OP_塔の入口（F1_01）", "F1_01_Arrive", "OP_TowerEntrance", new UnityEngine.Vector2(-115.5f, 25.6f), new UnityEngine.Vector2(4.4f, 1.0f));

// ---- Sigle joins later: Noah alone in this scene until the flag ----
var psm = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>();
{
    var so = new UnityEditor.SerializedObject(psm);
    so.FindProperty("sigleJoinedFlag").objectReferenceValue = sigleJoined;
    so.ApplyModifiedPropertiesWithoutUndo();
}

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "built; scene dirty=" + scene.isDirty;
