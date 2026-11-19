// Stage1_3（書斎）：フラグ・アイテム（マレット・チューニングキー）・メモ・止まっているときの一言のアセットを作る（あれば値を合わせるだけ。GUID は変わらない）
// @helpers
System.Func<string, System.Type, UnityEngine.Object> Ensure = (path, type) => {
  var a = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
  if (a == null) {
    var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
    if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) { var parent = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/'); UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(dir)); }
    a = UnityEngine.ScriptableObject.CreateInstance(type); UnityEditor.AssetDatabase.CreateAsset(a, path); Log.Append("created ").Append(path).Append("\n");
  }
  return a; };

// ---- フラグ（すべてセーブする） ----
var flags = new[]{
  new[]{ "Flag_Shosai_DoorABroken", "書斎：シグルが扉A（追跡廊下の奥）を壊した。Stage1_2 の扉A から書斎へ入れる" },
  new[]{ "Flag_Shosai_DoorsLocked", "書斎：シグルが仕掛け扉を殴って、仕掛け扉と扉A がロックされた（入室の演出が済んだ）" },
  new[]{ "Flag_Shosai_Q1Clear", "書斎：第1問（3音）正解。東の本棚（チューニングキー）と西の譜面箱（暗号のメモ）の鍵が外れる" },
  new[]{ "Flag_Shosai_T2Tuned", "書斎：T2（29インチ）の調律が済んだ。第2問の前提。T2 を叩くとミが出る" },
  new[]{ "Flag_Shosai_DoorOpen", "書斎：第2問（5音）正解。北西の出口が開いた" },
  new[]{ "Flag_Shosai_SigleLeft", "書斎：紹介イベントのあと、シグルが先へ進んだ（NPC を消す）" },
  new[]{ "Flag_Sigle_NameKnown", "シグルの名前を聞いた（書斎の紹介イベント）。以降の会話は名前を「シグル」にする" },
  new[]{ "Flag_Shosai_ClueScore", "書斎の手がかり：机の楽譜を読んだ" },
  new[]{ "Flag_Shosai_ClueWall", "書斎の手がかり：本棚の側板の傷を見た" },
  new[]{ "Flag_Shosai_ClueFloor", "書斎の手がかり：絨毯の叩き跡を見た" },
  new[]{ "Flag_Shosai_GotKey", "書斎：東の本棚からチューニングキーを取った（第1問のあと）" },
  new[]{ "Flag_Shosai_ClueCipher", "書斎の手がかり：西の譜面箱から暗号のメモを取った（第2問の答え。第1問のあと）" },
  new[]{ "Flag_Shosai_TuneExplained", "書斎：T2 の調律の前の説明（よし直すぞ・遊び方）を見た" },
};
foreach (var f in flags) { var a = (GameFlag)Ensure("Assets/Data/Flags/" + f[0] + ".asset", typeof(GameFlag)); SetProp(a, "id", f[0]); SetProp(a, "memo", f[1]); SetProp(a, "saved", true); }

// ---- アイテム（Resources/Items） ----
{ var a = (ItemData)Ensure("Assets/Resources/Items/Item_Mallet.asset", typeof(ItemData)); SetProp(a, "id", "Item_Mallet"); SetProp(a, "displayName", "マレット"); SetProp(a, "description", "ティンパニを叩くばち。書斎のマレット置き場にあった"); SetProp(a, "isKey", true);
  SetProp(a, "icon", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Images/Map/Stage1_3/Karisozai/mallet_stand.png")); }
{ var a = (ItemData)Ensure("Assets/Resources/Items/Item_TuningKey.asset", typeof(ItemData)); SetProp(a, "id", "Item_TuningKey"); SetProp(a, "displayName", "チューニングキー"); SetProp(a, "description", "太鼓の張りを締める鍵。書斎の東の本棚の格子の奥にあった（仮：以降の調律ギミックでも使う想定）"); SetProp(a, "isKey", true); }

// ---- メモ（Resources/Notes/Shosai） ----
var notes = new[]{
  new[]{ "Shosai_TimpaniBasic", "ティンパニの基礎", "大きい太鼓ほど低い音。1台で1つの音を出す。\n（本当はペダルで音を変えられるらしい）", "10" },
  new[]{ "Shosai_Score", "机の楽譜", "最初の3音だけ読める。\nド（低）→ ソ → ド（高）\n4音目から先は破れている。", "11" },
  new[]{ "Shosai_Wall", "本棚の側板の傷", "大・中・小の丸が3つ、左から右へ。\n大きい太鼓ほど低い音。", "12" },
  new[]{ "Shosai_Floor", "絨毯の叩き跡", "奏者の跡が5つ。\n3番目だけ、緑の太鼓（ミ）の前に深く残っている。", "13" },
  new[]{ "Shosai_Cipher", "譜面箱のメモ（扉の歌）", "はじまりは、いちばん大きな太鼓。\nそこから\n　→2　←1　→1　→1\n（矢印の向きへ、数の分だけ隣の太鼓へ）", "14" },
  new[]{ "Shosai_Tuning", "ティンパニの調律", "張りが緩むと音が下がる。直してから叩く。", "16" },
  new[]{ "Shosai_Q2Answer", "書斎の扉の楽譜（まとめ）", "ド（低）→ ソ → ミ → ソ → ド（高）", "17" },
};
foreach (var n in notes) { var a = (NoteData)Ensure("Assets/Resources/Notes/Shosai/Note_" + n[0] + ".asset", typeof(NoteData)); SetProp(a, "id", n[0]); SetProp(a, "title", n[1]); SetProp(a, "body", n[2]); SetProp(a, "order", int.Parse(n[3])); SetProp(a, "memo", "書斎（Stage1_3）の仮の文。2026-11-19"); }

// ---- 止まっているときの一言（Resources/Barks/Shosai。25 秒。進み具合ごとに1つだけ条件が合う） ----
// 条件：{フラグ名, ON?} と {アイテム, 持っていない?}
var mallet = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Item_Mallet.asset");
var tkey = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Item_TuningKey.asset");
System.Action<string, string, object[][], object[][], string[]> IdleBark = (file, memo, fls, its, texts) => {
  var a = (BarkData)Ensure("Assets/Resources/Barks/Shosai/" + file + ".asset", typeof(BarkData));
  var so = new UnityEditor.SerializedObject(a);
  so.FindProperty("active").boolValue = true;
  so.FindProperty("trigger").enumValueIndex = (int)BarkTrigger.Idle;
  so.FindProperty("seconds").floatValue = 25f;
  var fl = so.FindProperty("condition.flags"); fl.arraySize = fls.Length;
  for (int i = 0; i < fls.Length; i++) { var e = fl.GetArrayElementAtIndex(i); e.FindPropertyRelative("flag").objectReferenceValue = Flag((string)fls[i][0]); e.FindPropertyRelative("state").enumValueIndex = (bool)fls[i][1] ? 0 : 1; }
  var it = so.FindProperty("condition.items"); it.arraySize = its == null ? 0 : its.Length;
  for (int i = 0; its != null && i < its.Length; i++) { var e = it.GetArrayElementAtIndex(i); e.FindPropertyRelative("item").objectReferenceValue = (ItemData)its[i][0]; e.FindPropertyRelative("count").intValue = 1; e.FindPropertyRelative("notHave").boolValue = (bool)its[i][1]; }
  so.FindProperty("priority").intValue = 40; so.FindProperty("chance").floatValue = 1f; so.FindProperty("repeat").enumValueIndex = 0; so.FindProperty("cooldown").floatValue = 45f;
  var v = so.FindProperty("variants"); v.arraySize = texts.Length;
  for (int i = 0; i < texts.Length; i++) { var e = v.GetArrayElementAtIndex(i); e.FindPropertyRelative("weight").floatValue = 1f; var ln = e.FindPropertyRelative("lines"); ln.arraySize = 1; var l0 = ln.GetArrayElementAtIndex(0); l0.FindPropertyRelative("speaker").enumValueIndex = 2; l0.FindPropertyRelative("text").stringValue = texts[i]; l0.FindPropertyRelative("seconds").floatValue = 2.5f; }
  so.FindProperty("memo").stringValue = "書斎（Stage1_3）で 25 秒止まっているとき（仮の文）。" + memo;
  so.ApplyModifiedPropertiesWithoutUndo(); };
IdleBark("Bark_Shosai_Idle", "ロックのあと・マレットを持つ前",
  new[]{ new object[]{ "Flag_Shosai_DoorsLocked", true }, new object[]{ "Flag_Shosai_Q1Clear", false } }, new[]{ new object[]{ mallet, true } },
  new[]{ "(まずは、太鼓を叩く物を探そう)", "(手がかりは、メモで見返せる、、Tab)" });
IdleBark("Bark_Shosai_IdleQ1", "マレットを持ってから第1問の前",
  new[]{ new object[]{ "Flag_Shosai_DoorsLocked", true }, new object[]{ "Flag_Shosai_Q1Clear", false } }, new[]{ new object[]{ mallet, false } },
  new[]{ "(机の楽譜の順に、叩いてみよう)", "(ランプは、合った音の数だけつくみたい、、)" });
IdleBark("Bark_Shosai_IdleTake", "第1問のあと・キーを取る前",
  new[]{ new object[]{ "Flag_Shosai_Q1Clear", true }, new object[]{ "Flag_Shosai_GotKey", false } }, null,
  new[]{ "(光ってる本棚と譜面箱、、見てみよう)" });
IdleBark("Bark_Shosai_IdleTune", "キーを取ってから調律の前",
  new[]{ new object[]{ "Flag_Shosai_GotKey", true }, new object[]{ "Flag_Shosai_T2Tuned", false } }, null,
  new[]{ "(キーで、ゆれてる緑の太鼓を直そう)" });
IdleBark("Bark_Shosai_IdleCipher", "調律のあと・暗号のメモを取る前",
  new[]{ new object[]{ "Flag_Shosai_T2Tuned", true }, new object[]{ "Flag_Shosai_ClueCipher", false }, new object[]{ "Flag_Shosai_DoorOpen", false } }, null,
  new[]{ "(譜面箱にも、何かあったはず)" });
IdleBark("Bark_Shosai_IdleQ2", "調律と暗号のメモのあと・第2問の前",
  new[]{ new object[]{ "Flag_Shosai_T2Tuned", true }, new object[]{ "Flag_Shosai_ClueCipher", true }, new object[]{ "Flag_Shosai_DoorOpen", false } }, null,
  new[]{ "(メモの矢印のとおりに、叩いてみよう)", "(はじまりは、一番大きい太鼓、、)" });

UnityEditor.AssetDatabase.SaveAssets();
return Log.ToString();
