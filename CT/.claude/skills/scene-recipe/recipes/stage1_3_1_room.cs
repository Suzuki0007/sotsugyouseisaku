// Stage_1_3 段階1：ティンパニ部屋のルート・歩ける範囲・出現位置・カメラ。ルート「ティンパニ部屋」を消して作るので、続けて 2→3b→3c→4→4b→5→autosave_points
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Stage_1_3.unity") scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage_1_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, UnityEngine.GameObject> GO = (parent, n, pos) => { var g = new UnityEngine.GameObject(n); if (parent != null) g.transform.SetParent(parent, false); else UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene); g.transform.position = pos; return g; };

var old = UnityEngine.GameObject.Find("ティンパニ部屋"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
var root = GO(null, "ティンパニ部屋", new UnityEngine.Vector3(-115.2f, 20.8f, 0f));

// ---- walk area: 3 timpani + the centre piece (matches room_timpani.png) ----
var walk = GO(root.transform, "歩ける範囲（ティンパニと中央）", root.transform.position);
walk.AddComponent<WalkArea>();
var centers = new[]{ new UnityEngine.Vector3(-115.23f, 25.04f, 0f), new UnityEngine.Vector3(-120.29f, 17.35f, 0f), new UnityEngine.Vector3(-110.11f, 17.35f, 0f) };
var names = new[]{ "ティンパニ_上", "ティンパニ_左下", "ティンパニ_右下" };
for (int i = 0; i < 3; i++) { var g = GO(walk.transform, names[i], centers[i]); var c = g.AddComponent<UnityEngine.CircleCollider2D>(); c.isTrigger = true; c.radius = 4.3f; }
{ var g = GO(walk.transform, "中央（つなぎ）", UnityEngine.Vector3.zero); var p = g.AddComponent<UnityEngine.PolygonCollider2D>(); p.isTrigger = true;
  p.points = new[]{ new UnityEngine.Vector2(-120.35f, 21.79f), new UnityEngine.Vector2(-119.04f, 22.64f), new UnityEngine.Vector2(-111.42f, 22.64f), new UnityEngine.Vector2(-110.10f, 21.79f), new UnityEngine.Vector2(-114.54f, 16.60f), new UnityEngine.Vector2(-115.85f, 16.60f) }; }

// ---- camera stays inside the room picture ----
var roomSr = UnityEngine.GameObject.Find("Grid/soto_yuka").GetComponent<UnityEngine.SpriteRenderer>();
{ var g = GO(root.transform, "カメラの範囲（部屋の絵の中）", roomSr.bounds.center); var b = g.AddComponent<UnityEngine.BoxCollider2D>(); b.isTrigger = true; b.size = new UnityEngine.Vector2(roomSr.bounds.size.x, roomSr.bounds.size.y); g.AddComponent<CameraArea>(); }
UnityEngine.Camera.main.backgroundColor = UnityEngine.Color.black;

// ---- spawn at the centre ----
var centre = new UnityEngine.Vector3(-115.2f, 20.0f, 0f);
{ var g = GO(root.transform, "出現位置_Stage1_2 から（中央）", centre); var sp = g.AddComponent<PlayerSpawnPoint>(); var so = new UnityEditor.SerializedObject(sp); so.FindProperty("spawnPointId").stringValue = "FromStage1_2"; var sg = GO(g.transform, "シグルの位置", centre + new UnityEngine.Vector3(1.2f, 0f, 0f)); so.FindProperty("sigleSpawnPoint").objectReferenceValue = sg.transform; so.ApplyModifiedPropertiesWithoutUndo(); }
foreach (var n in new[]{ "Spawner/PlayerPartySpawner/NoahSpaenPoint", "Spawner/PlayerPartySpawner/SigleSpawnPoint" }) { var g = UnityEngine.GameObject.Find(n); if (g != null) g.transform.position = centre; }

// ---- Sigle has joined by now (flag) ----
{ var so = new UnityEditor.SerializedObject(UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>()); so.FindProperty("sigleJoinedFlag").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Sigle_Joined.asset"); so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- leftovers from the copied scene ----
{ var sp = UnityEngine.GameObject.Find("SavePoint"); if (sp != null) { sp.SetActive(false); sb.Append("hid SavePoint (outside the room)\n"); } }

// ---- UI: same rules as the other stages ----
var notoSdf = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
var notoTtf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/Fonts/NotoSansJP-Regular.ttf");
{ var cs = UnityEngine.GameObject.Find("Canvas").GetComponent<UnityEngine.UI.CanvasScaler>(); cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new UnityEngine.Vector2(1920, 1080); cs.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; cs.matchWidthOrHeight = 0.5f; }
{ var life = UnityEngine.GameObject.Find("Canvas/LifeUI"); var rt = (UnityEngine.RectTransform)life.transform; rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0, 1); rt.pivot = new UnityEngine.Vector2(0, 1); rt.sizeDelta = new UnityEngine.Vector2(300, 100); rt.anchoredPosition = new UnityEngine.Vector2(24, -16); life.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = UnityEngine.TextAnchor.UpperLeft; }
foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsInactive.Include)) if (t.font != notoSdf) { t.font = notoSdf; UnityEditor.EditorUtility.SetDirty(t); }
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsInactive.Include)) if (t.font != notoTtf) { t.font = notoTtf; UnityEditor.EditorUtility.SetDirty(t); }

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
sb.Append("saved=").Append(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)).Append("\n");

// ---- build settings & place name ----
var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
if (!list.Exists(x => x.path == "Assets/Scenes/Stage_1_3.unity")) { int idx = list.FindIndex(x => x.path == "Assets/Scenes/Stage1_2.unity"); list.Insert(idx + 1, new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Stage_1_3.unity", true)); UnityEditor.EditorBuildSettings.scenes = list.ToArray(); sb.Append("added to build settings\n"); }
var table = UnityEngine.Resources.Load<PlaceNameTable>(PlaceNameTable.ResourcePath);
if (table != null && PlaceNameTable.GetName("Stage_1_3") == "Stage_1_3") { table.AddForEditor("Stage_1_3", "ティンパニの間"); UnityEditor.EditorUtility.SetDirty(table); UnityEditor.AssetDatabase.SaveAssets(); sb.Append("place name added\n"); }
sb.Append("place=").Append(PlaceNameTable.GetName("Stage_1_3"));
return sb.ToString();
