// 上陸（Stage1_0〜Stage1_2 入口、2026-11-20）：フラグ・メモ「カイロスの手紙」・砂漠の怪物の EnemyData・塔のスチルの取り込み設定（あれば値を合わせるだけ。GUID は変わらない）
// @helpers
System.Func<string, System.Type, UnityEngine.Object> Ensure = (path, type) => {
  var a = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
  if (a == null) {
    var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
    if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) { var parent = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/'); UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(dir)); }
    a = UnityEngine.ScriptableObject.CreateInstance(type); UnityEditor.AssetDatabase.CreateAsset(a, path); Log.Append("created ").Append(path).Append("\n");
  }
  return a; };

// ---- フラグ（すべてセーブする。指示書の FLAG_LAND_* に対応） ----
var flags = new[]{
  new[]{ "Flag_Land_LetterRead", "上陸：小舟の上でカイロスの手紙を読んだ（Stage1_0 の始まり。上陸まで済んだ）。指示書 FLAG_LAND_LETTER_READ" },
  new[]{ "Flag_Land_VibeS", "上陸：上陸直後の振動（小）を感じた。指示書 FLAG_LAND_VIBE_S" },
  new[]{ "Flag_Land_Wreck1", "上陸：砂浜の残骸1（西）を調べた。指示書 FLAG_LAND_WRECK1" },
  new[]{ "Flag_Land_Wreck2", "上陸：砂浜の残骸2（東）を調べた。指示書 FLAG_LAND_WRECK2" },
  new[]{ "Flag_Land_Wreck3", "上陸：砂浜の残骸3（奥）を調べた。指示書 FLAG_LAND_WRECK3" },
  new[]{ "Flag_Land_WreckAny", "上陸：砂浜の残骸をどれか1つでも調べた（砂漠の OP_02j で「あの残骸と、同じ」を出すか）" },
  new[]{ "Flag_Land_Footprints", "上陸：砂浜の足跡に気づいた。指示書 FLAG_LAND_FOOTPRINTS" },
  new[]{ "Flag_Land_VibeM", "上陸：砂浜の奥で振動（中）を感じた（このあと砂漠へ）。指示書 FLAG_LAND_VIBE_M" },
  new[]{ "Flag_Land_TowerStill", "上陸：砂漠に入って塔の全景（スチル）を見た" },
  new[]{ "Flag_Land_DoorMark", "上陸：塔の扉の爪痕に気づいた（このあと怪物が出て追跡）。指示書 FLAG_LAND_DOOR_MARK" },
};
foreach (var f in flags) { var a = (GameFlag)Ensure("Assets/Data/Flags/" + f[0] + ".asset", typeof(GameFlag)); SetProp(a, "id", f[0]); SetProp(a, "memo", f[1]); SetProp(a, "saved", true); }

// ---- メモ「カイロスの手紙」 ----
{ var a = (NoteData)Ensure("Assets/Resources/Notes/Landing/Note_KairosLetter.asset", typeof(NoteData));
  SetProp(a, "id", "KairosLetter"); SetProp(a, "title", "カイロスの手紙"); SetProp(a, "body", "「塔で待っている」\n\n何年も前に消えた、あの街一番の調律師から。"); SetProp(a, "order", 1);
  SetProp(a, "memo", "上陸（Stage1_0）の小舟の上で手に入る。2026-11-20 仮の文"); }

// ---- 砂漠の怪物（Stage1_2 の追跡の敵のコピー。攻撃では倒れない） ----
{ var path = "Assets/Data/Enemies/EnemyData_Stage1_1_DesertChaser.asset";
  if (UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null) { UnityEditor.AssetDatabase.CopyAsset("Assets/Data/Enemies/EnemyData_Stage1_2_Chaser.asset", path); Log.Append("copied ").Append(path).Append("\n"); }
  var d = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(path);
  SetProp(d, "displayName", "砂漠の怪物（Stage1_1 の追跡）");
  SetProp(d, "moveSpeed", 2.6f);
  var so = new UnityEditor.SerializedObject(d); var dr = so.FindProperty("damageRules"); dr.arraySize = 1; var r = dr.GetArrayElementAtIndex(0);
  r.FindPropertyRelative("kind").intValue = (int)DamageKindFilter.Any; r.FindPropertyRelative("character").intValue = (int)CharacterFilter.Any;
  r.FindPropertyRelative("multiplier").floatValue = 0f; r.FindPropertyRelative("judge").intValue = (int)GaugeJudge.None;
  so.FindProperty("memo").stringValue = "上陸（2026-11-20）：砂漠で背後から出て追いかける。攻撃は効き方で 0 倍＝倒れない（仮）。速さ 2.6 はノアの歩く 3 より少し遅い（仮）。見た目は仮";
  so.ApplyModifiedPropertiesWithoutUndo(); }

// ---- 塔のスチル（仮画像）：Sprite・Single ----
{ var path = "Assets/Images/UI/Still/tower_still_placeholder_lowangle.png";
  var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
  if (ti != null && (ti.textureType != UnityEditor.TextureImporterType.Sprite || ti.spriteImportMode != UnityEditor.SpriteImportMode.Single)) { ti.textureType = UnityEditor.TextureImporterType.Sprite; ti.spriteImportMode = UnityEditor.SpriteImportMode.Single; ti.SaveAndReimport(); Log.Append("still: sprite single\n"); }
  Log.Append("still sprite=").Append(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path) != null).Append("\n"); }

UnityEditor.AssetDatabase.SaveAssets();
return Log.ToString();
