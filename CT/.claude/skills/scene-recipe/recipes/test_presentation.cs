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

// 作り直しても GUID（.meta）が変わらないように、あればファイルの中身だけ Stage1_0 で上書きする（11-26）
if (System.IO.File.Exists(dst))
{
    System.IO.File.Copy("Assets/Scenes/Stage1_0.unity", dst, true);
    UnityEditor.AssetDatabase.ImportAsset(dst, UnityEditor.ImportAssetOptions.ForceUpdate);
}
else if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Stage1_0.unity", dst)) return "!! copy failed";
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

// 場所の色味（北西の角。夕方の色＝夜とはっきり違う色。11-26：前の「暗く青く」は夜の光とほぼ同じ値で、入っても変わらないように見えた）
var at = Child("場所の色味（夕方の色）", root.transform, V(-120.5f, 36.5f));
var box3 = at.AddComponent<UnityEngine.BoxCollider2D>(); box3.isTrigger = true; box3.size = new UnityEngine.Vector2(3.2f, 3.4f);
var tone = at.AddComponent<AreaTone>();
{ var so = new UnityEditor.SerializedObject(tone); so.FindProperty("color").colorValue = new UnityEngine.Color(1f, 0.62f, 0.4f, 1f); so.FindProperty("intensity").floatValue = 0.9f; so.FindProperty("priority").intValue = 0; so.FindProperty("blendSeconds").floatValue = 0.8f; so.ApplyModifiedPropertiesWithoutUndo(); }
Label(at.transform, "場所の色味\n（入ると夕方の色）", V(-120.5f, 36.5f), 2.6f);

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

// ===== 青と黄色の哀愁（11-26 依頼書 CT_mood_blue_yellow）：塗り替えの色調 P1〜P6 と、強い場面の光（最初は消えている。光のプリセット「青い夜」で点く） =====
var byRoot = Child("青と黄色の光", root.transform, V(0f, 0f));
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Window, V(-110.8f, 27.6f), byRoot.transform);   // 残骸2 の上から床へ（残骸の影が光の中に落ちる）
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Lamp, V(-117.2f, 24.2f), byRoot.transform);     // 出現位置の左
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Moon, V(-118.0f, 37.6f), byRoot.transform);     // 北の空
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.TowerTop, V(-112.8f, 38.0f), byRoot.transform); // 北の遠く
PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Watch, V(-121.0f, 25.2f), byRoot.transform);    // 西の手元
// 残骸2 に影の形（光の中に青い影を落とす）
var wreck2 = UnityEngine.GameObject.Find("sunahama_object_2_0");
if (wreck2 != null && wreck2.GetComponent<UnityEngine.Rendering.Universal.ShadowCaster2D>() == null) { MapLookMenu.AddCircle(wreck2, 0.8f, 8); sb.Append("shadow caster: sunahama_object_2_0\n"); }

var byLight = Load<LightPreset>(R + "Light/Light_青い夜（塗り替え用）.asset");
var byStation = Station("青と黄色\n（P1〜P6）", PresentationStation.Kind.Mood, V(-119.0f, 22.5f), new UnityEngine.Color(0.95f, 0.85f, 0.35f),
    All<MoodPreset>("Mood", "Mood_P1_ファミレス型", "Mood_P2_夏のうちに型", "Mood_P3_月夜の砂浜", "Mood_P4_回想ぬくもり", "Mood_P5_喪失", "Mood_P6_夜明け"));
Set(byStation, "withLight", byLight);
// 案ごとの光（11-26：光が主役。全体の光＝案の青、場面の光＝案の黄色）
{ var so = new UnityEditor.SerializedObject(byStation); var wl = so.FindProperty("withLights"); var ln = new[] { "Light_P1_ファミレス型", "Light_P2_夏のうちに型", "Light_P3_月夜の砂浜", "Light_P4_回想ぬくもり", "Light_P5_喪失", "Light_P6_夜明け" }; wl.arraySize = ln.Length; for (int i = 0; i < ln.Length; i++) wl.GetArrayElementAtIndex(i).objectReferenceValue = Load<LightPreset>(R + "Light/" + ln[i] + ".asset"); so.ApplyModifiedPropertiesWithoutUndo(); }
Station("塗り替えの\n境目を表示", PresentationStation.Kind.Bands, V(-108.8f, 24.6f), new UnityEngine.Color(0.3f, 0.9f, 0.9f));

// ===== 室内（11-27 ユーザーの依頼）：Stage1_2 の通路の絵を砂浜の北につなぐ。歩いてそのまま入れる。青と黄色の光・明るさの台座 =====
// Stage1_2 の通路の絵 Room_1_Rouka（中心 (-115.7, 13.9)・28.8×10.8・下の端 y 8.5・下の入口 x -109.2〜-105.7）を、
// 入口の真ん中が広場の真ん中（始まる場所の真上、x -115.2）、下の端が砂浜の上の端 (38.43) に来るようにずらす（11-27：前は x -113 で、広場からまっすぐ上へ歩くと入れなかった）
var roomOffset = new UnityEngine.Vector3(-7.75f, 29.95f, 0f);
const float cx = -115.2f;   // 入口の真ん中
var stage12 = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1_2.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEngine.GameObject srcRoom = null;
foreach (var r in stage12.GetRootGameObjects()) { var t = r.transform.Find("Room_1_Rouka"); if (t != null) { srcRoom = t.gameObject; break; } }
UnityEngine.GameObject room = null;
if (srcRoom == null) sb.Append("!! Stage1_2 に Room_1_Rouka がない\n");
else
{
    room = UnityEngine.Object.Instantiate(srcRoom);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room, s);
    room.name = "室内（Stage1_2 の通路の絵）";
    room.transform.SetParent(root.transform, true);
    room.transform.position = srcRoom.transform.position + roomOffset;
    var mbCopy = room.GetComponent<MapBounds>(); if (mbCopy != null) UnityEngine.Object.DestroyImmediate(mbCopy);
    // 壁：絵の下の端（ずらした後の y 38.43）より下にはみ出した分を切る（砂浜をふさがないように）
    float bottom = room.GetComponent<UnityEngine.SpriteRenderer>().bounds.min.y;
    foreach (var bc in room.GetComponents<UnityEngine.BoxCollider2D>())
    {
        var b = bc.bounds;
        if (b.size.x < 0.01f) continue;
        if (b.min.y < bottom)
        {
            float newMin = bottom, max = b.max.y;
            var center = new UnityEngine.Vector2(b.center.x, (newMin + max) * 0.5f);
            var local = (UnityEngine.Vector2)room.transform.InverseTransformPoint(center);
            var sc = room.transform.lossyScale;
            bc.offset = local;
            bc.size = new UnityEngine.Vector2(bc.size.x, (max - newMin) / sc.y);
        }
    }
    sb.Append("room copied: " + room.GetComponent<UnityEngine.SpriteRenderer>().bounds + "\n");
}
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(stage12, true);

// ===== 動作と波紋の広場（11-27 指示書 CT_presentation_ripple_subtitle_gesture）：砂浜の床の絵をもう 1 枚、東の隣に並べる =====
// 砂浜の床 soto_yuka は x -122.36〜-107.88（幅 14.48）。東の隣 x -107.88〜-93.40 に同じ絵を置き、砂浜の東の端（x -108.0）とつなぐ
UnityEngine.Bounds eastBounds = default; bool hasEast = false;
{
    var floor = UnityEngine.GameObject.Find("soto_yuka");
    if (floor == null) sb.Append("!! soto_yuka がない\n");
    else
    {
        var east = UnityEngine.Object.Instantiate(floor, floor.transform.parent);
        east.name = "動作と波紋の広場の床（砂浜の絵のコピー）";
        var fsr = floor.GetComponent<UnityEngine.SpriteRenderer>();
        east.transform.position = floor.transform.position + new UnityEngine.Vector3(fsr.bounds.size.x, 0f, 0f);
        foreach (var mbE in east.GetComponents<MapBounds>()) UnityEngine.Object.DestroyImmediate(mbE);
        eastBounds = east.GetComponent<UnityEngine.SpriteRenderer>().bounds; hasEast = true;
        sb.Append("east floor: " + eastBounds + "\n");
    }
}
if (hasEast)
{
    var walkEast = Child("歩ける範囲（動作と波紋の広場）", root.transform, V(0f, 0f));
    walkEast.AddComponent<WalkArea>();
    void WalkRectE(string name, float x0, float y0, float x1, float y1)
    {
        var go = Child(name, walkEast.transform, V((x0 + x1) * 0.5f, (y0 + y1) * 0.5f));
        var bc = go.AddComponent<UnityEngine.BoxCollider2D>(); bc.isTrigger = true; bc.size = new UnityEngine.Vector2(x1 - x0, y1 - y0);
    }
    WalkRectE("つなぎ（砂浜の東の端）", -108.4f, 21.0f, -107.4f, 37.6f);
    WalkRectE("広場", eastBounds.min.x + 0.15f, 20.3f, eastBounds.max.x - 0.2f, 38.3f);
}

if (room != null)
{
    var rb = room.GetComponent<UnityEngine.SpriteRenderer>().bounds;
    // 歩ける範囲：通路の入口（砂浜とつなぐ）と床
    var walkRoom = Child("歩ける範囲（室内）", root.transform, V(0f, 0f));
    var wa2 = walkRoom.AddComponent<WalkArea>();
    void WalkRect(string name, float x0, float y0, float x1, float y1)
    {
        var go = Child(name, walkRoom.transform, V((x0 + x1) * 0.5f, (y0 + y1) * 0.5f));
        var bc = go.AddComponent<UnityEngine.BoxCollider2D>(); bc.isTrigger = true; bc.size = new UnityEngine.Vector2(x1 - x0, y1 - y0);
    }
    WalkRect("入口（砂浜とつなぐ）", cx - 1.6f, 37.9f, cx + 1.6f, 40.4f);
    WalkRect("床", rb.min.x + 0.2f, 40.2f, rb.max.x - 0.2f, 45.7f);

    // マップの範囲：砂浜と室内の両方を覆う見えない絵に移す（プレイヤーが出られる範囲。PlayerSceneContext の MapBounds もつなぎ替える）
    var beachMb = UnityEngine.Object.FindAnyObjectByType<MapBounds>(UnityEngine.FindObjectsInactive.Include);
    var beachSr = beachMb != null ? beachMb.GetComponent<UnityEngine.SpriteRenderer>() : null;
    if (beachSr != null)
    {
        var bb = beachSr.bounds;
        if (hasEast) bb.Encapsulate(eastBounds);   // 動作と波紋の広場も含める（カメラは砂浜と広場を横に続けて動く）
        float x0 = UnityEngine.Mathf.Min(bb.min.x, rb.min.x), x1 = UnityEngine.Mathf.Max(bb.max.x, rb.max.x), y0 = UnityEngine.Mathf.Min(bb.min.y, rb.min.y), y1 = UnityEngine.Mathf.Max(bb.max.y, rb.max.y);
        var areaGo = Child("マップの範囲（砂浜と室内・見えない絵）", root.transform, V((x0 + x1) * 0.5f, (y0 + y1) * 0.5f));
        var asr = areaGo.AddComponent<UnityEngine.SpriteRenderer>(); asr.sprite = beachSr.sprite; asr.color = new UnityEngine.Color(1f, 1f, 1f, 0f);
        var ss = beachSr.sprite.bounds.size;
        areaGo.transform.localScale = new UnityEngine.Vector3((x1 - x0) / ss.x, (y1 - y0) / ss.y, 1f);
        var newMb = areaGo.AddComponent<MapBounds>();
        UnityEngine.Object.DestroyImmediate(beachMb);
        var ctx = UnityEngine.Object.FindAnyObjectByType<PlayerSceneContext>(UnityEngine.FindObjectsInactive.Include);
        if (ctx != null) { Set(ctx, "mapBounds", newMb); sb.Append("map bounds → 砂浜と室内\n"); } else sb.Append("!! PlayerSceneContext がない\n");

        // カメラの範囲：砂浜と室内それぞれ（カメラはそれぞれの絵の中で止まる）
        void CamArea(string name, UnityEngine.Bounds b2)
        {
            var go = Child(name, root.transform, V(b2.center.x, b2.center.y));
            var bc = go.AddComponent<UnityEngine.BoxCollider2D>(); bc.isTrigger = true; bc.size = new UnityEngine.Vector2(b2.size.x, b2.size.y);
            go.AddComponent<CameraArea>();
        }
        // 砂浜のカメラの範囲は上へ 3 マス広げる（砂浜の上の端から、通路の入口と室内の下が見えるように）。重なる所では室内のほうを優先
        var bbUp = bb; bbUp.SetMinMax(bb.min, new UnityEngine.Vector3(bb.max.x, bb.max.y + 3f, bb.max.z));
        CamArea("カメラの範囲（砂浜と動作と波紋の広場・上へ 3 マス広げる）", bbUp);
        CamArea("カメラの範囲（室内）", rb);
        foreach (var ca in root.GetComponentsInChildren<CameraArea>(true)) if (ca.name == "カメラの範囲（室内）") Set(ca, "priority", 1);
    }

    // 室内の光：壁のランタン 3 つにランプ、左に窓（Stage1_2 と同じ置き方・強さ）
    var roomLights = Child("室内の光（青と黄色）", root.transform, V(0f, 0f));
    foreach (var x in new[] { -117.96f, -110.1f, -102.06f })
        PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Lamp, new UnityEngine.Vector3(x, 16.9f, 0f) + roomOffset, roomLights.transform);
    PresentationMenu.BuildBlueYellowLight(PresentationMenu.BlueYellowLight.Window, new UnityEngine.Vector3(-123.5f, 15.0f, 0f) + roomOffset, roomLights.transform);
    foreach (var l in roomLights.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
    {
        bool window = l.transform.parent != null && l.transform.parent.name == "窓の光";
        if (l.name == "光") l.intensity = window ? 2.4f : 3f;
        else if (l.name == "縁") l.intensity = window ? 0.6f : 0.8f;
    }

    // 室内の台座（通路の入口を上がった所）：明るさ（4 段）・青と黄色・塗り替えの境目
    var roomDark = Station("室内の明るさ", PresentationStation.Kind.Darkness, V(cx - 4.0f, 41.4f), new UnityEngine.Color(0.45f, 0.45f, 0.65f));
    Set(roomDark, "darkness", sd);
    {
        var so = new UnityEditor.SerializedObject(roomDark); var lv = so.FindProperty("darknessLevels");
        var names = new[] { "明るい", "薄暗い", "暗い", "真っ暗" }; var ds = new[] { 0f, 0.6f, 0.85f, 0.97f }; var rs = new[] { 3f, 3f, 2f, 1f };
        lv.arraySize = 4;
        for (int i = 0; i < 4; i++) { var e = lv.GetArrayElementAtIndex(i); e.FindPropertyRelative("name").stringValue = names[i]; e.FindPropertyRelative("darkness").floatValue = ds[i]; e.FindPropertyRelative("radius").floatValue = rs[i]; }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    var roomBy = Station("青と黄色\n（P1〜P6）", PresentationStation.Kind.Mood, V(cx + 4.0f, 41.4f), new UnityEngine.Color(0.95f, 0.85f, 0.35f),
        All<MoodPreset>("Mood", "Mood_P1_ファミレス型", "Mood_P2_夏のうちに型", "Mood_P3_月夜の砂浜", "Mood_P4_回想ぬくもり", "Mood_P5_喪失", "Mood_P6_夜明け"));
    { var so = new UnityEditor.SerializedObject(roomBy); var wl = so.FindProperty("withLights"); var ln = new[] { "Light_P1_ファミレス型", "Light_P2_夏のうちに型", "Light_P3_月夜の砂浜", "Light_P4_回想ぬくもり", "Light_P5_喪失", "Light_P6_夜明け" }; wl.arraySize = ln.Length; for (int i = 0; i < ln.Length; i++) wl.GetArrayElementAtIndex(i).objectReferenceValue = Load<LightPreset>(R + "Light/" + ln[i] + ".asset"); so.ApplyModifiedPropertiesWithoutUndo(); }
    Station("塗り替えの\n境目を表示", PresentationStation.Kind.Bands, V(cx - 7.0f, 41.4f), new UnityEngine.Color(0.3f, 0.9f, 0.9f));
    Label(root.transform, "室内：入口の台座で明るさ・青と黄色を切り替える", V(cx, 44.2f), 2.8f);
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

// ===== 動作と波紋の広場の中身（11-27）=====
//   北の列 y35：手の動作 3 つ（回す＝時計の針・長押し＝伸びる手・繰り返し＝墓標）。それぞれの上に「演出の数値で動かす」見た目
//   真ん中 y30：波紋（色が戻る・光が灯る・消す）　南 y25：色の戻り具合（+0.25・0 に戻す）と、組み合わせの場面 2 つ
if (hasEast)
{
    var gRoot = Child("動作と波紋の見本", root.transform, V(0f, 0f));
    T P<T>(string folder, string name) where T : UnityEngine.Object => Load<T>(R + folder + "/" + name + ".asset");
    // 案内は列の間に（画面下は字幕が出るので、南に置くと重なる。11-27）
    Label(gRoot.transform, "回す・長押しは E を押したまま／繰り返しは E を何度も", V(-100.6f, 32.4f), 2.4f);
    { var t = gRoot.transform.GetChild(gRoot.transform.childCount - 1).GetComponent<TMPro.TextMeshPro>(); t.rectTransform.sizeDelta = new UnityEngine.Vector2(14f, 1f); }

    UnityEngine.GameObject Seq2(string name) { var go = Child(name, gRoot.transform, V(0f, 0f)); go.AddComponent<EventSequence>(); return go; }
    void EvStation(string name, UnityEngine.Vector3 pos, UnityEngine.GameObject seq, UnityEngine.Color color)
    {
        var go = Child("台座_" + name.Replace("\n", "_"), gRoot.transform, pos);
        var box = go.AddComponent<UnityEngine.BoxCollider2D>(); box.isTrigger = true; box.size = new UnityEngine.Vector2(1.6f, 1.6f);
        var ex = go.AddComponent<ExamineSequence>();
        var so = new UnityEditor.SerializedObject(ex); var p = so.FindProperty("sequences"); p.arraySize = 1; p.GetArrayElementAtIndex(0).objectReferenceValue = seq.GetComponent<EventSequence>();
        so.FindProperty("afterLast").enumValueIndex = 1; so.ApplyModifiedPropertiesWithoutUndo();
        var look = Child("見た目", go.transform, pos);
        var sr = look.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = knob; sr.color = color; sr.sortingOrder = 5;
        look.transform.localScale = new UnityEngine.Vector3(6f, 3.6f, 1f);
        Label(go.transform, name, pos + V(0f, 1.1f), 2.8f);
    }
    UnityEngine.GameObject Shape(string name, UnityEngine.Transform parent, UnityEngine.Vector3 localPos, UnityEngine.Vector3 scale, UnityEngine.Color c, int order)
    {
        var go = new UnityEngine.GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localScale = scale;
        var sr = go.AddComponent<UnityEngine.SpriteRenderer>(); sr.sprite = knob; sr.color = c; sr.sortingOrder = order; return go;
    }
    PresentationValueBinder Binder(UnityEngine.GameObject go, string value, int mode, float from, float to)
    {
        var b = go.AddComponent<PresentationValueBinder>();
        Set(b, "valueName", value); Set(b, "mode", mode); Set(b, "valueFrom", from); Set(b, "valueTo", to); return b;
    }
    // 時計（文字盤と針。針は軸の子＝軸を回す）
    UnityEngine.Transform Clock(string name, UnityEngine.Vector3 pos, string value)
    {
        var c = Child(name, gRoot.transform, pos);
        Shape("文字盤", c.transform, UnityEngine.Vector3.zero, new UnityEngine.Vector3(9f, 9f, 1f), new UnityEngine.Color(0.92f, 0.88f, 0.75f), 6);
        var axis = Child("針の軸", c.transform, pos);
        Shape("針", axis.transform, new UnityEngine.Vector3(0f, 0.32f, 0f), new UnityEngine.Vector3(0.9f, 4.4f, 1f), new UnityEngine.Color(0.2f, 0.18f, 0.25f), 7);
        Binder(axis, value, 0, 0f, 1f);   // 回す：0 → -360 度
        return c.transform;
    }
    UnityEngine.GameObject Gesture(string name, GesturePreset preset, string progress, string count)
    {
        var go = Seq2(name);
        var g = go.AddComponent<GestureEventAction>(); Set(g, "preset", preset); Set(g, "progressValue", progress); Set(g, "countValue", count);
        return go;
    }
    void AddStep(GestureEventAction g, float at, UnityEngine.GameObject seq)
    {
        var so = new UnityEditor.SerializedObject(g); var st = so.FindProperty("steps"); int i = st.arraySize; st.arraySize = i + 1;
        var e = st.GetArrayElementAtIndex(i); e.FindPropertyRelative("at").floatValue = at; e.FindPropertyRelative("sequence").objectReferenceValue = seq.GetComponent<EventSequence>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    UnityEngine.GameObject SubtitleSeq(string name, string text, float secs)
    {
        var go = Seq2(name); var sub = go.AddComponent<SubtitleEventAction>(); Set(sub, "style", 0); Set(sub, "text", text); Set(sub, "advance", 1); Set(sub, "seconds", secs); return go;
    }

    // --- 北の列：手の動作 ---
    var col = new UnityEngine.Color(0.95f, 0.75f, 0.55f);
    Clock("時計（回す）", V(-105.0f, 37.0f), "見本_回す");
    EvStation("回す\n（時計を巻く）", V(-105.0f, 34.6f), Gesture("手の動作：回す（最後のひと巻き）", P<GesturePreset>("Gesture", "Gesture_最後のひと巻き"), "見本_回す", ""), col);

    var armRoot = Child("伸びる手（長押し）", gRoot.transform, V(-102.6f, 37.0f));
    Binder(armRoot, "見本_長押し", 1, 0f, 1f);
    { var b = armRoot.GetComponent<PresentationValueBinder>(); var so = new UnityEditor.SerializedObject(b); so.FindProperty("scaleFrom").vector3Value = new UnityEngine.Vector3(0.08f, 1f, 1f); so.FindProperty("scaleTo").vector3Value = UnityEngine.Vector3.one; so.ApplyModifiedPropertiesWithoutUndo(); }
    Shape("手", armRoot.transform, new UnityEngine.Vector3(2.0f, 0f, 0f), new UnityEngine.Vector3(25f, 2.4f, 1f), new UnityEngine.Color(1f, 0.85f, 0.7f), 7);
    Shape("向こうの手（ノア）", gRoot.transform, V(-98.3f, 37.0f) - gRoot.transform.position, new UnityEngine.Vector3(3f, 3f, 1f), new UnityEngine.Color(0.7f, 0.85f, 1f), 7);
    EvStation("長押し\n（手を伸ばす）", V(-100.6f, 34.6f), Gesture("手の動作：長押し（手を伸ばす）", P<GesturePreset>("Gesture", "Gesture_手を伸ばす"), "見本_長押し", ""), col);

    var graves = Child("墓標（繰り返し）", gRoot.transform, V(-96.2f, 37.0f));
    for (int i = 0; i < 5; i++) Shape("墓標" + (i + 1), graves.transform, new UnityEngine.Vector3(-1.6f + i * 0.8f, 0f, 0f), new UnityEngine.Vector3(2.4f, 4.2f, 1f), new UnityEngine.Color(0.75f, 0.75f, 0.8f), 7);
    Binder(graves, "見本_繰り返し", 4, 0f, 5f);
    EvStation("繰り返し\n（墓を掘る）", V(-96.2f, 34.6f), Gesture("手の動作：繰り返し（墓を掘る）", P<GesturePreset>("Gesture", "Gesture_墓を掘る"), "", "見本_繰り返し"), col);

    // --- 真ん中の列：波紋 ---
    var rc = new UnityEngine.Color(0.6f, 0.95f, 0.85f);
    UnityEngine.GameObject RippleSeq(string name, string preset, string id, UnityEngine.Vector3 at)
    {
        var go = Seq2(name); var spot = Child("波紋の中心", go.transform, at);
        var a = go.AddComponent<RippleEventAction>(); Set(a, "preset", P<RipplePreset>("Ripple", preset)); Set(a, "center", 0); Set(a, "target", spot.transform); Set(a, "id", id);
        return go;
    }
    EvStation("波紋\n色が戻る", V(-105.0f, 30.0f), RippleSeq("波紋：色が戻る（残る）", "Ripple_色が戻る", "見本_色が戻る", V(-105.0f, 30.0f)), rc);
    EvStation("波紋\n光が灯る", V(-100.6f, 30.0f), RippleSeq("波紋：光が灯る（残る）", "Ripple_光が灯る", "見本_光が灯る", V(-100.6f, 30.0f)), rc);
    { var go = Seq2("波紋を消す（全部）"); var a = go.AddComponent<RippleClearEventAction>(); Set(a, "id", ""); Set(a, "seconds", 1.5f); EvStation("波紋を\n全部消す", V(-96.2f, 30.0f), go, new UnityEngine.Color(0.85f, 0.85f, 0.85f)); }
    // 回想中だけ残る波紋（RippleSpot：フラグ Flag_Flashback_Active の間）
    { var go = Child("残る波紋（回想中だけ）", gRoot.transform, V(-102.4f, 27.6f)); var rs = go.AddComponent<RippleSpot>(); Set(rs, "preset", P<RipplePreset>("Ripple", "Ripple_光が灯る"));
      var so = new UnityEditor.SerializedObject(rs); var l = so.FindProperty("flagConditions"); l.arraySize = 1; l.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = flag; so.ApplyModifiedPropertiesWithoutUndo(); }

    // --- 南の列：色の戻り具合と、組み合わせの場面 ---
    var sp = UnityEngine.Object.FindAnyObjectByType<ScenePresentation>(UnityEngine.FindObjectsInactive.Include);
    if (sp != null) { Set(sp, "restoredMood", P<MoodPreset>("Mood", "Mood_温かい日常")); Set(sp, "restoredLight", P<LightPreset>("Light", "Light_夜明け")); sb.Append("ScenePresentation: 色が戻ったとき＝温かい日常・夜明け\n"); }
    else sb.Append("!! ScenePresentation がない（色の戻り具合は効かない）\n");
    { var go = Seq2("色の戻り具合 +0.25"); var a = go.AddComponent<ColorRestoreEventAction>(); Set(a, "mode", 0); Set(a, "amount", 0.25f); Set(a, "seconds", 2f); EvStation("色の戻り\n+0.25", V(-105.0f, 25.0f), go, new UnityEngine.Color(0.95f, 0.95f, 0.6f)); }
    { var go = Seq2("色の戻り具合 0 に戻す"); var a = go.AddComponent<ColorRestoreEventAction>(); Set(a, "mode", 1); Set(a, "amount", 0f); Set(a, "seconds", 1f); EvStation("色の戻り\n0 に戻す", V(-102.2f, 25.0f), go, new UnityEngine.Color(0.85f, 0.85f, 0.85f)); }

    // 組み合わせ①：埋葬の記憶（回想に入る → 墓を 5 回掘る〔墓標が 1 つずつ立つ〕→ 字幕 → 回想から戻る）
    var graves2 = Child("墓標（埋葬の記憶）", gRoot.transform, V(-99.2f, 27.0f));
    for (int i = 0; i < 5; i++) Shape("墓標" + (i + 1), graves2.transform, new UnityEngine.Vector3(-1.2f + i * 0.6f, 0f, 0f), new UnityEngine.Vector3(1.8f, 3.2f, 1f), new UnityEngine.Color(0.8f, 0.78f, 0.72f), 7);
    Binder(graves2, "見本_埋葬", 4, 0f, 5f);
    {
        var go = Seq2("組み合わせ：埋葬の記憶");
        var a1b = go.AddComponent<PresentationValueEventAction>(); Set(a1b, "valueName", "見本_埋葬"); Set(a1b, "mode", 0); Set(a1b, "value", 0f);
        var a2b = go.AddComponent<FlashbackEnterEventAction>(); Set(a2b, "preset", fbBurial); Set(a2b, "seconds", 2f);
        var a3b = go.AddComponent<GestureEventAction>(); Set(a3b, "preset", P<GesturePreset>("Gesture", "Gesture_墓を掘る")); Set(a3b, "countValue", "見本_埋葬");
        var a4b = go.AddComponent<SubtitleEventAction>(); Set(a4b, "style", 0); Set(a4b, "text", "……おやすみなさい。"); Set(a4b, "advance", 1); Set(a4b, "seconds", 2.5f);
        var a5b = go.AddComponent<FlashbackExitEventAction>(); Set(a5b, "seconds", 2f);
        EvStation("場面\n埋葬の記憶", V(-99.2f, 25.0f), go, new UnityEngine.Color(0.85f, 0.7f, 0.45f));
    }
    // 組み合わせ②：最後のひと巻き（回す。半分・9 割で字幕、巻き終わると時計から光が灯る）
    var clock2 = Clock("時計（最後のひと巻き）", V(-95.0f, 27.2f), "見本_ひと巻き");
    {
        var go = Seq2("組み合わせ：最後のひと巻き");
        var g = go.AddComponent<GestureEventAction>(); Set(g, "preset", P<GesturePreset>("Gesture", "Gesture_最後のひと巻き")); Set(g, "progressValue", "見本_ひと巻き");
        AddStep(g, 0.5f, SubtitleSeq("ひと巻き：半分", "あと、少し……", 2f));
        AddStep(g, 0.9f, SubtitleSeq("ひと巻き：9 割", "……重い。", 2f));
        var r = go.AddComponent<RippleEventAction>(); Set(r, "preset", P<RipplePreset>("Ripple", "Ripple_光が灯る")); Set(r, "center", 0); Set(r, "target", clock2); Set(r, "id", "");
        var sub = go.AddComponent<SubtitleEventAction>(); Set(sub, "style", 0); Set(sub, "text", "（時計が、また動き出した）"); Set(sub, "advance", 1); Set(sub, "seconds", 2.5f);
        EvStation("場面\n最後のひと巻き", V(-95.8f, 25.0f), go, new UnityEngine.Color(0.85f, 0.7f, 0.45f));
    }

    // --- 一番南の列 y21.8（11-27 見た目 A）：画面のレンズ・映画の字幕・空 ---
    //   画面の縁と光漏れ（色調の見本）・太陽（レンズフレア）・映画の字幕・空（7 つ）・空の見え方（見上げる／見下ろす）
    //   空は、広場の東の端（x -98 より東）に立つとカメラが東へ 8 マス先まで動き、マップの外が見える
    var lc = new UnityEngine.Color(0.75f, 0.85f, 1f);
    {
        var st = Station("画面の縁\n光漏れ", PresentationStation.Kind.Mood, V(-105.0f, 21.8f), lc,
            All<MoodPreset>("Mood", "Mood_見本_画面の縁", "Mood_見本_光漏れ", "Mood_白い廃墟", "Mood_ダリの午後"));
        st.transform.SetParent(gRoot.transform, true);
    }
    {
        // 太陽は台座の列に立ったときの画面の左上の端あたり（画面の外の遠くだと出ない）
        var sunGo = Child("太陽（レンズフレア・台座で出す）", gRoot.transform, V(-105.5f, 27.0f));
        sunGo.AddComponent<LensSun>();
        sunGo.SetActive(false);
        var st = Station("太陽\n（レンズフレア）", PresentationStation.Kind.Sun, V(-102.6f, 21.8f), new UnityEngine.Color(1f, 0.92f, 0.6f));
        Set(st, "sun", sunGo);
        st.transform.SetParent(gRoot.transform, true);
    }
    {
        var go = Seq2("映画の字幕（見本）"); var sub = go.AddComponent<SubtitleEventAction>(); Set(sub, "style", 1);
        Set(sub, "text", "彼女は、遠くで波の音がした気がした。けれど、何も聞こえなかった。"); Set(sub, "advance", 1); Set(sub, "seconds", 7.0f);
        EvStation("映画の字幕", V(-100.2f, 21.8f), go, new UnityEngine.Color(0.95f, 0.95f, 0.95f));
    }
    {
        var st = Station("空", PresentationStation.Kind.Sky, V(-97.0f, 21.8f), new UnityEngine.Color(0.5f, 0.75f, 1f),
            All<SkyPreset>("Sky", "Sky_青空", "Sky_暗い空", "Sky_夜明け", "Sky_夕焼け", "Sky_夜", "Sky_ダリの空", "Sky_黒"));
        Set(st, "seconds", 2.0f);
        st.transform.SetParent(gRoot.transform, true);
        var sv = Station("空の見え方\n（見上げる／見下ろす）", PresentationStation.Kind.SkyView, V(-94.6f, 21.8f), new UnityEngine.Color(0.6f, 0.7f, 0.9f));
        sv.transform.SetParent(gRoot.transform, true);
        // 広場の東の端に立つと、カメラが東へ 8 マス先まで動く（マップの外＝空が見える）
        var camGo = Child("カメラの範囲（空を見る・広場の東の端から東へ 8 マス）", gRoot.transform, V((-98.0f + eastBounds.max.x + 8f) * 0.5f, eastBounds.center.y));
        var cbc = camGo.AddComponent<UnityEngine.BoxCollider2D>(); cbc.isTrigger = true; cbc.size = new UnityEngine.Vector2(eastBounds.max.x + 8f + 98.0f, eastBounds.size.y);
        var ca = camGo.AddComponent<CameraArea>(); Set(ca, "priority", 2);
    }
}

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
sb.Append("saved " + s.name + " = " + UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s) + "\n");
if (!string.IsNullOrEmpty(start) && start != dst) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
return sb.ToString();
