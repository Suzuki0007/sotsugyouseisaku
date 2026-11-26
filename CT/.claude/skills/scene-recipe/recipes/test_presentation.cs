// 演出の試し用シーン Test_Presentation（2026-11-26、指示書 CT_presentation_mood_light_weather_still）。Build Settings に入れない
// ・Stage1_0（砂浜・夜）をコピーし、OP・上陸のイベント・演出の画像を切る（マップ・プレイヤー・会話の仕組みはそのまま）
// ・見本の広場（11-26 改）：直接再生でここから始まる。台座を E キーで調べると色調・光・天候・回想・暗さ・1枚絵を試せる（全部戻す・右上に今の状態）。場面の光・天候の範囲・場所の色味・回想中だけ出す物
// ・何度実行しても同じ（コピーし直して作る）。シーンを保存する。仮の1枚絵 Images/UI/Still/still_test_placeholder.png も作る
var sb = new System.Text.StringBuilder();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "!! unsaved scene: " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
const string dst = "Assets/Scenes/Test_Presentation.unity";

// 仮の1枚絵（横長 1600×900。空の色の段・丸・地平線）
const string stillPath = "Assets/Images/UI/Still/still_test_placeholder.png";
if (!System.IO.File.Exists(stillPath))
{
    int w = 1600, h = 900;
    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false);
    var px = new UnityEngine.Color32[w * h];
    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
    {
        float fy = (float)y / h, fx = (float)x / w;
        var c = UnityEngine.Color.Lerp(new UnityEngine.Color(0.95f, 0.7f, 0.55f), new UnityEngine.Color(0.35f, 0.45f, 0.75f), fy);
        if (fy < 0.3f) c = UnityEngine.Color.Lerp(new UnityEngine.Color(0.25f, 0.22f, 0.3f), new UnityEngine.Color(0.4f, 0.35f, 0.4f), fy / 0.3f);
        float dx = fx - 0.7f, dy = fy - 0.55f; if (dx * dx * 3.2f + dy * dy < 0.012f) c = new UnityEngine.Color(1f, 0.95f, 0.8f);
        if (((int)(fx * 16) + (int)(fy * 9)) % 2 == 0 && fy < 0.3f) c *= 0.9f;
        c.a = 1f; px[y * w + x] = c;
    }
    tex.SetPixels32(px); tex.Apply();
    System.IO.Directory.CreateDirectory("Assets/Images/UI/Still");
    System.IO.File.WriteAllBytes(stillPath, tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
    UnityEditor.AssetDatabase.ImportAsset(stillPath);
    sb.Append("made " + stillPath + "\n");
}

UnityEditor.AssetDatabase.DeleteAsset(dst);
if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Stage1_0.unity", dst)) return "!! copy failed";
var s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(dst, UnityEditor.SceneManagement.OpenSceneMode.Single);

// アセットはシーンを開いてから読む（scene-recipe の落とし穴）
T Load<T>(string p) where T : UnityEngine.Object { var a = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) sb.Append("!! missing " + p + "\n"); return a; }
const string R = "Assets/Resources/Presentation/";
var lightNight = Load<LightPreset>(R + "Light/Light_夜.asset");
var fbBurial = Load<FlashbackPreset>(R + "Flashback/Flashback_埋葬.asset");
var lightDawn = Load<LightPreset>(R + "Light/Light_夜明け.asset");
var rainWeak = Load<WeatherPreset>(R + "Weather/Weather_雨・弱.asset");
var moodDawn = Load<MoodPreset>(R + "Mood/Mood_夜明け.asset");
var still = Load<UnityEngine.Sprite>(stillPath);
var talk = Load<DialogueData>("Assets/Data/Dialogue/Scenario_Landing/OP_02h_TowerStill.asset");
var flag = Load<GameFlag>("Assets/Data/Flags/Flag_Flashback_Active.asset");

void Set(UnityEngine.Object o, string prop, object v)
{
    var so = new UnityEditor.SerializedObject(o);
    var p = so.FindProperty(prop);
    if (p == null) { sb.Append("!! no prop " + o.GetType().Name + "." + prop + "\n"); return; }
    switch (v)
    {
        case float f: p.floatValue = f; break;
        case int n: if (p.propertyType == UnityEditor.SerializedPropertyType.Enum) p.enumValueIndex = n; else p.intValue = n; break;
        case bool b: p.boolValue = b; break;
        case string str: p.stringValue = str; break;
        case UnityEngine.Object obj: p.objectReferenceValue = obj; break;
        case null: p.objectReferenceValue = null; break;
    }
    so.ApplyModifiedPropertiesWithoutUndo();
}

// 切る物（上陸の中の歩ける範囲は残す＝ルートへ出す）
foreach (var r in s.GetRootGameObjects())
    if (r.name == "上陸（Stage1_0）")
        foreach (var wa0 in r.GetComponentsInChildren<WalkArea>(true)) { wa0.transform.SetParent(null, true); sb.Append("keep " + wa0.name + "\n"); }
// 上端の出口（Stage1_1 へ）を切る（試している途中で砂漠へ移らないように。11-26 ユーザーの指示）
foreach (var c in UnityEngine.Object.FindObjectsByType<SceneChange>(UnityEngine.FindObjectsInactive.Include))
    if (c.gameObject.scene == s && c.transform.parent != null && c.transform.parent.name == "MapEntity" && c.gameObject.activeSelf) { c.gameObject.SetActive(false); sb.Append("off exit " + c.name + "\n"); }
foreach (var r in s.GetRootGameObjects())
{
    if (r.name == "OP" || r.name == "上陸（Stage1_0）" || r.name == "演出の画像（手紙・スチル）" || r.name == "Event") { r.SetActive(false); sb.Append("off " + r.name + "\n"); }
}


// ===== 見本の広場（2026-11-26 改：歩いて台座を E キーで調べるだけで全部を試せる） =====
// 砂浜の歩ける範囲は x -122.2〜-108.0・y 20.3〜38.3（残骸 3 つは歩けない）
//   北の列 y33.5：色調・光・天候・回想　　真ん中の列 y29.5：1枚絵 3 つ・暗さ　　南 y22：出現位置・案内の文・全部戻す
//   北西の角：場所の色味の範囲（遺跡の代わり）　北東の角：天候の範囲（室内の代わり＝止む）
var root = new UnityEngine.GameObject("演出の見本");
var V = new System.Func<float, float, UnityEngine.Vector3>((x, y) => new UnityEngine.Vector3(x, y, 0f));
var knob = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/Knob.psd");
var fontAsset = TMPro.TMP_Settings.defaultFontAsset;

UnityEngine.GameObject Child(string name, UnityEngine.Transform parent, UnityEngine.Vector3 pos)
{
    var go = new UnityEngine.GameObject(name); go.transform.SetParent(parent, false); go.transform.position = pos; return go;
}
void Label(UnityEngine.Transform parent, string text, UnityEngine.Vector3 pos, float size)
{
    var go = Child("名前", parent, pos);
    var t = go.AddComponent<TMPro.TextMeshPro>();
    if (fontAsset != null) t.font = fontAsset;
    t.text = text; t.fontSize = size; t.alignment = TMPro.TextAlignmentOptions.Center; t.color = new UnityEngine.Color(1f, 1f, 1f, 0.95f);
    t.rectTransform.sizeDelta = new UnityEngine.Vector2(6f, 1.2f);
    t.sortingOrder = 300;
}

// 直接再生の位置（PlayerPartySpawner の noahSpawnPoint・sigleSpawnPoint を広場の南へ）
var spawnRoot = Child("出現位置_見本の広場（直接再生）", root.transform, V(-115.2f, 22.0f));
var noahPt = Child("ノア", spawnRoot.transform, V(-115.2f, 22.0f));
var siglePt = Child("シグル", spawnRoot.transform, V(-116.0f, 22.0f));
var spawner = UnityEngine.Object.FindAnyObjectByType<PlayerPartySpawner>(UnityEngine.FindObjectsInactive.Include);
if (spawner != null) { Set(spawner, "noahSpawnPoint", noahPt.transform); Set(spawner, "sigleSpawnPoint", siglePt.transform); sb.Append("spawn → 見本の広場\n"); }
else sb.Append("!! PlayerPartySpawner がない\n");
Label(root.transform, "演出の見本：台座に近づいて E で調べる\n（右上に今の状態。デバッグウィンドウの「演出」でも試せる）", V(-115.2f, 24.6f), 3f);

// 今の状態の表示
Child("演出の今の状態を表示", root.transform, V(0f, 0f)).AddComponent<PresentationStatusView>();

// 台座
PresentationStation Station(string name, PresentationStation.Kind kind, UnityEngine.Vector3 pos, UnityEngine.Color color, params UnityEngine.ScriptableObject[] list)
{
    var go = Child("台座_" + name.Replace("\n", "_"), root.transform, pos);
    var box = go.AddComponent<UnityEngine.BoxCollider2D>(); box.isTrigger = true; box.size = new UnityEngine.Vector2(1.6f, 1.6f);
    var st = go.AddComponent<PresentationStation>();
    Set(st, "kind", (int)kind);
    var so = new UnityEditor.SerializedObject(st); var p = so.FindProperty("presets"); p.arraySize = list.Length;
    for (int i = 0; i < list.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
    so.ApplyModifiedPropertiesWithoutUndo();
    var look = Child("見た目", go.transform, pos);
    var sr = look.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = knob; sr.color = color; sr.sortingOrder = 5;
    look.transform.localScale = new UnityEngine.Vector3(6f, 3.6f, 1f);
    Label(go.transform, name, pos + V(0f, 1.1f), 2.8f);
    return st;
}
T[] All<T>(string folder, params string[] names) where T : UnityEngine.ScriptableObject
{
    var arr = new T[names.Length];
    for (int i = 0; i < names.Length; i++) arr[i] = Load<T>(R + folder + "/" + names[i] + ".asset");
    return arr;
}
Station("色調", PresentationStation.Kind.Mood, V(-120.5f, 33.5f), new UnityEngine.Color(1f, 0.6f, 0.75f),
    All<MoodPreset>("Mood", "Mood_夜・一人", "Mood_温かい日常", "Mood_喪失", "Mood_回想", "Mood_夜明け"));
Station("光", PresentationStation.Kind.Light, V(-117.5f, 33.5f), new UnityEngine.Color(1f, 0.9f, 0.45f),
    All<LightPreset>("Light", "Light_昼", "Light_夜", "Light_月明かり", "Light_夕日", "Light_夜明け", "Light_塔の中の暗がり"));
Station("天候", PresentationStation.Kind.Weather, V(-114.5f, 33.5f), new UnityEngine.Color(0.55f, 0.75f, 1f),
    All<WeatherPreset>("Weather", "Weather_雨・弱", "Weather_雨・強", "Weather_雪", "Weather_砂ぼこり", "Weather_霧", "Weather_光の粒", "Weather_花びら"));
Station("回想", PresentationStation.Kind.Flashback, V(-111.5f, 33.5f), new UnityEngine.Color(0.85f, 0.7f, 0.45f), fbBurial);

// 暗さ（暗いステージは最初は非表示・暗さ 0 で置く。台座が表示して暗くする）
var darkGo = Child("暗いステージ（暗さの台座で表示）", root.transform, V(0f, 0f));
var sd = darkGo.AddComponent<StageDarkness>();
Set(sd, "darkness", 0f); Set(sd, "radius", 3.5f); Set(sd, "innerRadius", 0.8f);
{ var so = new UnityEditor.SerializedObject(sd); so.FindProperty("ambientColor").colorValue = new UnityEngine.Color(0.75f, 0.8f, 1f, 1f); so.ApplyModifiedPropertiesWithoutUndo(); }
darkGo.SetActive(false);
var darkStation = Station("暗さ", PresentationStation.Kind.Darkness, V(-113.0f, 29.5f), new UnityEngine.Color(0.45f, 0.45f, 0.6f));
Set(darkStation, "darkness", sd);
var resetStation = Station("全部戻す", PresentationStation.Kind.ResetAll, V(-111.5f, 22.5f), new UnityEngine.Color(0.85f, 0.85f, 0.85f));
Set(resetStation, "darkness", sd);

// 場面の光
UnityEngine.GameObject MakeLight(string name, string group, UnityEngine.Vector3 pos, UnityEngine.Color c, float intensity, float radius)
{
    var go = Child(name, root.transform, pos);
    var l = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
    l.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
    l.color = c; l.intensity = intensity; l.pointLightOuterRadius = radius; l.pointLightInnerRadius = radius * 0.12f; l.falloffIntensity = 0.6f;
    var so = new UnityEditor.SerializedObject(l); var sl = so.FindProperty("m_ApplyToSortingLayers");
    var layers = UnityEngine.SortingLayer.layers; sl.arraySize = layers.Length; for (int i = 0; i < layers.Length; i++) sl.GetArrayElementAtIndex(i).intValue = layers[i].id; so.ApplyModifiedPropertiesWithoutUndo();
    var sc = go.AddComponent<SceneLight>(); Set(sc, "group", group);
    return go;
}
MakeLight("場面の光_月", "月", V(-116f, 36.5f), new UnityEngine.Color(0.7f, 0.8f, 1f), 0.6f, 9f);
MakeLight("場面の光_ランタン", "ランタン", V(-113f, 31.2f), new UnityEngine.Color(1f, 0.75f, 0.45f), 1.2f, 4f).AddComponent<LightFlicker>();
// 夕日のグループは、ふだんは消えている（光の台座の「夕日」で点く）。Light 2D を切って置く
MakeLight("場面の光_夕日", "夕日", V(-115f, 38f), new UnityEngine.Color(1f, 0.6f, 0.35f), 0.8f, 12f).GetComponent<UnityEngine.Rendering.Universal.Light2D>().enabled = false;

// 天候の範囲（北東の角。入ると止む＝室内の代わり）
var wa = Child("天候の範囲（室内の代わり・止む）", root.transform, V(-109.5f, 36.6f));
var box2 = wa.AddComponent<UnityEngine.BoxCollider2D>(); box2.isTrigger = true; box2.size = new UnityEngine.Vector2(3f, 3f);
wa.AddComponent<WeatherArea>();
Label(wa.transform, "天候の範囲\n（入ると止む）", V(-109.5f, 36.6f), 2.6f);

// 場所の色味（北西の角。遺跡の代わり＝暗く青く）
var at = Child("場所の色味（遺跡の代わり・暗く青く）", root.transform, V(-120.5f, 36.8f));
var box3 = at.AddComponent<UnityEngine.BoxCollider2D>(); box3.isTrigger = true; box3.size = new UnityEngine.Vector2(3f, 2.6f);
var tone = at.AddComponent<AreaTone>();
{ var so = new UnityEditor.SerializedObject(tone); so.FindProperty("color").colorValue = new UnityEngine.Color(0.55f, 0.65f, 0.95f, 1f); so.FindProperty("intensity").floatValue = 0.35f; so.FindProperty("priority").intValue = 0; so.FindProperty("blendSeconds").floatValue = 0.8f; so.ApplyModifiedPropertiesWithoutUndo(); }
Label(at.transform, "場所の色味\n（入ると暗く青く）", V(-120.5f, 36.8f), 2.6f);

// 回想中だけ出す物（回想の台座の上。残骸の絵を借りる）
var ghost = Child("回想中だけの物（仮）", root.transform, V(-113.5f, 37.0f));
var src = UnityEngine.GameObject.Find("sunahama_object_3");
var gsr = ghost.AddComponent<UnityEngine.SpriteRenderer>();
if (src != null && src.TryGetComponent(out UnityEngine.SpriteRenderer srcSr)) { gsr.sprite = srcSr.sprite; gsr.sharedMaterial = srcSr.sharedMaterial; gsr.sortingLayerID = srcSr.sortingLayerID; gsr.sortingOrder = srcSr.sortingOrder; }
ghost.transform.localScale = new UnityEngine.Vector3(0.4f, 0.4f, 1f);
gsr.color = new UnityEngine.Color(1f, 0.95f, 0.85f, 0.9f);
ghost.SetActive(false);
var ff = Child("フラグでなめらかに出す（回想中）", root.transform, V(0f, 0f));
var fade = ff.AddComponent<FlagFade>();
{
    var so = new UnityEditor.SerializedObject(fade);
    var list = so.FindProperty("flagConditions"); list.arraySize = 1; list.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = flag;
    var met = so.FindProperty("activeWhenMet"); met.arraySize = 1; met.GetArrayElementAtIndex(0).objectReferenceValue = ghost;
    so.ApplyModifiedPropertiesWithoutUndo();
}

// 見本のイベント（1枚絵の台座が使う。デバッグウィンドウの「イベント（その場で試す）」・event.ps1 -Run でも動く）
var events = Child("見本のイベント", root.transform, V(0f, 0f));
UnityEngine.GameObject Seq(string name)
{
    var go = Child(name, events.transform, V(0f, 0f));
    go.AddComponent<EventSequence>();
    return go;
}
var e1 = Seq("回想に入る（埋葬・雨・夜明け）");
var a1 = e1.AddComponent<FlashbackEnterEventAction>(); Set(a1, "preset", fbBurial); Set(a1, "seconds", 3f);
var a2 = e1.AddComponent<LightChangeEventAction>(); Set(a2, "preset", lightDawn); Set(a2, "seconds", 2f);
var a3 = e1.AddComponent<WeatherEventAction>(); Set(a3, "preset", rainWeak); Set(a3, "seconds", 4f);
var e2 = Seq("回想から戻る");
var b1 = e2.AddComponent<FlashbackExitEventAction>(); Set(b1, "seconds", 3f);

var e3 = Seq("1枚絵（ゆっくり寄る・キーで閉じる）");
var c1 = e3.AddComponent<MoodChangeEventAction>(); Set(c1, "preset", moodDawn); Set(c1, "seconds", 2f); Set(c1, "waitUntilDone", true);
var c2 = e3.AddComponent<StillShowEventAction>(); Set(c2, "sprite", still); Set(c2, "fadeSeconds", 2f); Set(c2, "motion", 1); Set(c2, "motionSeconds", 10f); Set(c2, "closeMode", 0);
var c3 = e3.AddComponent<MoodResetEventAction>(); Set(c3, "seconds", 2f);
var e4 = Seq("1枚絵（横に流す・時間で閉じる）");
var d1 = e4.AddComponent<StillShowEventAction>(); Set(d1, "sprite", still); Set(d1, "fadeSeconds", 1f); Set(d1, "motion", 3); Set(d1, "motionSeconds", 6f); Set(d1, "closeMode", 1); Set(d1, "showSeconds", 6f);
var e5 = Seq("1枚絵（部品で閉じる・会話を重ねる）");
var f1 = e5.AddComponent<StillShowEventAction>(); Set(f1, "sprite", still); Set(f1, "fadeSeconds", 1f); Set(f1, "motion", 2); Set(f1, "scaleFrom", 1.2f); Set(f1, "scaleTo", 1f); Set(f1, "closeMode", 2);
var f2 = e5.AddComponent<DialogueEventAction>(); Set(f2, "dialogueData", talk);
var f3 = e5.AddComponent<StillCloseEventAction>(); Set(f3, "seconds", 1f);
// 回想の中の会話（色調「回想」の「会話画面にもかける」で、立ち絵・文字・粒子も回想の見た目に。11-26）
var e6 = Seq("回想の中の会話（会話画面にもかける）");
var g1 = e6.AddComponent<FlashbackEnterEventAction>(); Set(g1, "preset", fbBurial); Set(g1, "seconds", 2f);
var g2 = e6.AddComponent<DialogueEventAction>(); Set(g2, "dialogueData", talk);
var g3 = e6.AddComponent<FlashbackExitEventAction>(); Set(g3, "seconds", 2f);

// 1枚絵の台座（調べるたびに同じイベント＝ExamineSequence の「最初に戻る」）
void StillStation(string name, UnityEngine.Vector3 pos, UnityEngine.GameObject seq)
{
    var go = Child("台座_" + name.Replace("\n", "_"), root.transform, pos);
    var box = go.AddComponent<UnityEngine.BoxCollider2D>(); box.isTrigger = true; box.size = new UnityEngine.Vector2(1.6f, 1.6f);
    var ex = go.AddComponent<ExamineSequence>();
    var so = new UnityEditor.SerializedObject(ex); var p = so.FindProperty("sequences"); p.arraySize = 1; p.GetArrayElementAtIndex(0).objectReferenceValue = seq.GetComponent<EventSequence>();
    so.FindProperty("afterLast").enumValueIndex = 1; so.ApplyModifiedPropertiesWithoutUndo();
    var look = Child("見た目", go.transform, pos);
    var sr = look.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = knob; sr.color = new UnityEngine.Color(0.75f, 1f, 0.75f); sr.sortingOrder = 5;
    look.transform.localScale = new UnityEngine.Vector3(6f, 3.6f, 1f);
    Label(go.transform, name, pos + V(0f, 1.1f), 2.8f);
}
StillStation("1枚絵\n寄る・キー", V(-121.0f, 29.5f), e3);
StillStation("1枚絵\n流す・時間", V(-118.5f, 29.5f), e4);
StillStation("1枚絵\n会話を重ねる", V(-116.0f, 29.5f), e5);
StillStation("回想の中の会話", V(-109.0f, 28.8f), e6);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
if (!string.IsNullOrEmpty(start) && start != dst) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
