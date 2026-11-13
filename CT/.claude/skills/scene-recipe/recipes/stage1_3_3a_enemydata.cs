// Stage_1_3 段階3：音楽生物の EnemyData・行動・攻撃のアセットを作る/上書きする（注意：後で調整した値が戻る。下の README を読む）
var sb = new System.Text.StringBuilder();
System.Func<string, string, UnityEngine.Object> CopyAsset = (src, dst) => {
  if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst) == null) UnityEditor.AssetDatabase.CopyAsset(src, dst);
  return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst);
};
string dir = "Assets/Data/Enemies/Timpani";
if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) UnityEditor.AssetDatabase.CreateFolder("Assets/Data/Enemies", "Timpani");

// ---- placeholder sprite: purple blob with eyes ----
string imgDir = "Assets/Images/Enemy";
string spritePath = imgDir + "/Enemy_Timpani_Creature_kari.png";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath) == null) {
  int n = 128; var tex = new UnityEngine.Texture2D(n, n, UnityEngine.TextureFormat.RGBA32, false);
  var clear = new UnityEngine.Color(0, 0, 0, 0);
  for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) {
    float dx = x - 63.5f, dy = y - 58f; float r = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy * 1.15f);
    UnityEngine.Color c = clear;
    if (r < 54) c = new UnityEngine.Color(0.45f, 0.25f, 0.65f, 1f);
    if (r < 50) c = new UnityEngine.Color(0.62f, 0.38f, 0.85f, 1f);
    // eyes
    foreach (var ex in new[]{ 44f, 84f }) { float ed = UnityEngine.Mathf.Sqrt((x - ex) * (x - ex) + (y - 70f) * (y - 70f)); if (ed < 11) c = UnityEngine.Color.white; if (ed < 5) c = new UnityEngine.Color(0.1f, 0.05f, 0.15f, 1f); }
    // mouth (a note-like slit)
    if (y > 34 && y < 40 && x > 50 && x < 78) c = new UnityEngine.Color(0.2f, 0.08f, 0.3f, 1f);
    // little antennae notes
    if (x > 86 && x < 90 && y > 96 && y < 122) c = new UnityEngine.Color(0.2f, 0.08f, 0.3f, 1f);
    { float nd = UnityEngine.Mathf.Sqrt((x - 82f) * (x - 82f) + (y - 98f) * (y - 98f)); if (nd < 7) c = new UnityEngine.Color(0.2f, 0.08f, 0.3f, 1f); }
    tex.SetPixel(x, y, c);
  }
  tex.Apply();
  System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(spritePath), tex.EncodeToPNG());
  UnityEditor.AssetDatabase.ImportAsset(spritePath);
  var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(spritePath);
  imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.spritePixelsPerUnit = 64; imp.alphaIsTransparency = true; imp.SaveAndReimport();
}
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath);
sb.Append("sprite=").Append(sprite != null).Append("\n");

// ---- attacks ----
System.Action<UnityEngine.Object, System.Action<UnityEditor.SerializedObject>> Edit = (o, f) => { var so = new UnityEditor.SerializedObject(o); f(so); so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(o); };
var ring = CopyAsset("Assets/Data/Enemies/Sample/Attacks/Attack_Sample_全方位弾.asset", dir + "/Attack_Timpani_Ring.asset");
Edit(ring, so => { so.FindProperty("telegraphTime").floatValue = 1.0f; so.FindProperty("showTelegraph").boolValue = true; so.FindProperty("telegraphColor").colorValue = new UnityEngine.Color(1f, 0.85f, 0.2f, 0.45f); so.FindProperty("telegraphBlink").boolValue = true;
  so.FindProperty("shape.type").enumValueIndex = 3; so.FindProperty("shape.radius").floatValue = 2.2f; so.FindProperty("shape.innerRadius").floatValue = 1.6f;
  so.FindProperty("bulletCount").intValue = 12; so.FindProperty("bulletVolleys").intValue = 1; so.FindProperty("bulletSpeed").floatValue = 3.0f; so.FindProperty("bulletDeletable").boolValue = true; so.FindProperty("damage").intValue = 1;
  so.FindProperty("memo").stringValue = "ティンパニの音楽生物：予兆（黄色い輪が点滅）→ まわり 12 方向に弾の輪。弾消し（シグルの守り）で消せる"; });
var ring2 = CopyAsset(dir + "/Attack_Timpani_Ring.asset", dir + "/Attack_Timpani_Ring_Hard.asset");
Edit(ring2, so => { so.FindProperty("telegraphTime").floatValue = 0.8f; so.FindProperty("bulletCount").intValue = 16; so.FindProperty("bulletVolleys").intValue = 2; so.FindProperty("bulletVolleyInterval").floatValue = 0.6f; so.FindProperty("bulletSpeed").floatValue = 3.3f;
  so.FindProperty("memo").stringValue = "激しい段階：16 方向に 2 回。弾消しで消せる"; });
var spiral = CopyAsset("Assets/Data/Enemies/Sample/Attacks/Attack_Sample_うず巻き弾.asset", dir + "/Attack_Timpani_Spiral.asset");
Edit(spiral, so => { so.FindProperty("telegraphTime").floatValue = 0.9f; so.FindProperty("showTelegraph").boolValue = true; so.FindProperty("telegraphColor").colorValue = new UnityEngine.Color(1f, 0.5f, 0.9f, 0.45f); so.FindProperty("telegraphBlink").boolValue = true;
  so.FindProperty("shape.type").enumValueIndex = 3; so.FindProperty("shape.radius").floatValue = 2.2f; so.FindProperty("shape.innerRadius").floatValue = 1.6f;
  so.FindProperty("bulletSpeed").floatValue = 3.0f; so.FindProperty("bulletDeletable").boolValue = true; so.FindProperty("damage").intValue = 1;
  so.FindProperty("memo").stringValue = "もっと激しい段階で増える攻撃：うず巻きの弾（予兆はピンクの輪）。弾消しで消せる"; });
var rock = CopyAsset("Assets/Data/Enemies/Sample/Attacks/Attack_Sample_落石.asset", dir + "/Attack_Timpani_Rock.asset");
Edit(rock, so => { so.FindProperty("telegraphTime").floatValue = 1.3f; so.FindProperty("shape.radius").floatValue = 1.0f; so.FindProperty("activeShake").floatValue = 0.15f; so.FindProperty("damage").intValue = 1;
  so.FindProperty("memo").stringValue = "振動で岩を降らせる：プレイヤーの足元に影（予兆）→ 落ちる。発動で画面が揺れる。弾消しでは消えない（よける）"; });
sb.Append("attacks ok\n");

// ---- flags for escalation (set in stage 4) ----
foreach (var pair in new[]{ new[]{"Flag_Timpani_Calmed1","Timpani_Calmed1","ティンパニの音楽生物が1体おとなしくなった（残りが激しくなる）。セーブしない"}, new[]{"Flag_Timpani_Calmed2","Timpani_Calmed2","ティンパニの音楽生物が2体おとなしくなった（残りがもっと激しくなる）。セーブしない"} }) {
  string fp = "Assets/Data/Flags/" + pair[0] + ".asset"; var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(fp);
  if (f == null) { f = UnityEngine.ScriptableObject.CreateInstance<GameFlag>(); UnityEditor.AssetDatabase.CreateAsset(f, fp); }
  Edit(f, so => { so.FindProperty("id").stringValue = pair[1]; so.FindProperty("memo").stringValue = pair[2]; so.FindProperty("saved").boolValue = false; });
}
var calm1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Timpani_Calmed1.asset");
var calm2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/Flag_Timpani_Calmed2.asset");

// ---- pattern: 3 phases ----
var pattern = CopyAsset("Assets/Data/Enemies/Sample/Patterns/Pattern_Sample_砲台（体力で変わる）.asset", dir + "/Pattern_Timpani_Creature.asset");
var attackType = System.Type.GetType("AttackAction, Assembly-CSharp"); var waitType = System.Type.GetType("WaitAction, Assembly-CSharp");
{
  var so = new UnityEditor.SerializedObject(pattern);
  var phases = so.FindProperty("phases"); phases.arraySize = 3;
  System.Action<UnityEditor.SerializedProperty, UnityEngine.Object, int, float> SetAttack = (el, atk, rep, interval) => { el.managedReferenceValue = System.Activator.CreateInstance(attackType); el.FindPropertyRelative("attack").objectReferenceValue = atk; el.FindPropertyRelative("repeat").intValue = rep; el.FindPropertyRelative("interval").floatValue = interval; el.FindPropertyRelative("weight").floatValue = 1; };
  System.Action<UnityEditor.SerializedProperty, float, float> SetWait = (el, a, b) => { el.managedReferenceValue = System.Activator.CreateInstance(waitType); el.FindPropertyRelative("minSeconds").floatValue = a; el.FindPropertyRelative("maxSeconds").floatValue = b; el.FindPropertyRelative("weight").floatValue = 1; };
  System.Action<UnityEditor.SerializedProperty, string, int, GameFlag, string, float> SetPhase = (ph, nm, exit, flag, anim, pause) => {
    ph.FindPropertyRelative("name").stringValue = nm; ph.FindPropertyRelative("order").enumValueIndex = 0; ph.FindPropertyRelative("exit").enumValueIndex = exit; ph.FindPropertyRelative("interrupt").boolValue = true;
    var fl = ph.FindPropertyRelative("condition.flags"); fl.arraySize = flag != null ? 1 : 0; if (flag != null) { fl.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = flag; fl.GetArrayElementAtIndex(0).FindPropertyRelative("state").enumValueIndex = 0; }
    ph.FindPropertyRelative("enterAnimation").stringValue = anim; ph.FindPropertyRelative("enterPause").floatValue = pause; };
  // phase 0
  var p0 = phases.GetArrayElementAtIndex(0); SetPhase(p0, "通常", 3, calm1, "", 0f);
  var a0 = p0.FindPropertyRelative("actions"); a0.arraySize = 5;
  SetWait(a0.GetArrayElementAtIndex(0), 0.5f, 2.5f); SetAttack(a0.GetArrayElementAtIndex(1), ring, 1, 0f); SetWait(a0.GetArrayElementAtIndex(2), 2.0f, 3.0f); SetAttack(a0.GetArrayElementAtIndex(3), rock, 2, 0.7f); SetWait(a0.GetArrayElementAtIndex(4), 2.0f, 3.0f);
  // phase 1
  var p1 = phases.GetArrayElementAtIndex(1); SetPhase(p1, "激しい（1体おとなしくなった）", 3, calm2, "Angry", 0.6f);
  var a1 = p1.FindPropertyRelative("actions"); a1.arraySize = 5;
  SetWait(a1.GetArrayElementAtIndex(0), 0.3f, 1.5f); SetAttack(a1.GetArrayElementAtIndex(1), ring2, 1, 0f); SetWait(a1.GetArrayElementAtIndex(2), 1.6f, 2.4f); SetAttack(a1.GetArrayElementAtIndex(3), rock, 3, 0.6f); SetWait(a1.GetArrayElementAtIndex(4), 1.6f, 2.4f);
  // phase 2
  var p2 = phases.GetArrayElementAtIndex(2); SetPhase(p2, "もっと激しい（2体おとなしくなった）", 0, null, "Angry", 0.6f);
  var a2 = p2.FindPropertyRelative("actions"); a2.arraySize = 6;
  SetAttack(a2.GetArrayElementAtIndex(0), spiral, 1, 0f); SetWait(a2.GetArrayElementAtIndex(1), 1.4f, 2.0f); SetAttack(a2.GetArrayElementAtIndex(2), ring2, 1, 0f); SetWait(a2.GetArrayElementAtIndex(3), 1.4f, 2.0f); SetAttack(a2.GetArrayElementAtIndex(4), rock, 3, 0.6f); SetWait(a2.GetArrayElementAtIndex(5), 1.4f, 2.0f);
  so.FindProperty("memo").stringValue = "ティンパニの音楽生物（動かない）。通常：予兆 → 弾の輪 → 落石×2。1体おとなしくなると（Flag_Timpani_Calmed1）激しい段階：弾の輪が 16 方向×2・落石×3。2体で（Flag_Timpani_Calmed2）うず巻きが増える。待ち時間で激しさを調整する";
  so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(pattern);
}
sb.Append("pattern ok\n");

// ---- enemy data ----
var data = CopyAsset("Assets/Data/Enemies/Sample/EnemyData_Sample_砲台.asset", dir + "/EnemyData_Timpani_Creature.asset");
Edit(data, so => {
  so.FindProperty("displayName").stringValue = "音楽生物（ティンパニ）"; so.FindProperty("maxHealth").intValue = 99; so.FindProperty("moveSpeed").floatValue = 0f;
  so.FindProperty("pattern").objectReferenceValue = pattern;
  var dr = so.FindProperty("damageRules"); dr.arraySize = 1; var r0 = dr.GetArrayElementAtIndex(0); r0.FindPropertyRelative("kind").intValue = -1; r0.FindPropertyRelative("character").enumValueIndex = 0; r0.FindPropertyRelative("multiplier").floatValue = 0f; r0.FindPropertyRelative("judge").enumValueIndex = 0;
  so.FindProperty("contactDamage").intValue = 1; so.FindProperty("sprite").objectReferenceValue = sprite;
  so.FindProperty("defeatStyle").enumValueIndex = 2; so.FindProperty("remainTint").colorValue = new UnityEngine.Color(0.75f, 0.95f, 1f, 1f);
  so.FindProperty("deathEffect").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Data/Effect/Sample/Fx_Sample_Spawn.asset");
  so.FindProperty("memo").stringValue = "ティンパニの中央にいる音楽生物。攻撃では倒れない（すべての攻撃 ×0）。ペグを全部締めると「おとなしくなる」（段階4で連動）。絵は仮";
});
UnityEditor.AssetDatabase.SaveAssets();
sb.Append("data ok kind=").Append(new UnityEditor.SerializedObject(data).FindProperty("damageRules").GetArrayElementAtIndex(0).FindPropertyRelative("kind").intValue);
return sb.ToString();
