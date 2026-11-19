// Stage1_4（T字通路）：フラグのアセットを作る（あれば値を合わせるだけ。GUID は変わらない）。指示書 CT_T_corridor_claudecode_instruction_2026-11-20
// 指示書のフラグ名（FLAG_TC_...）は、このプロジェクトの決まり（Flag_<場所>_<名前>）に合わせた
// @helpers
System.Func<string, System.Type, UnityEngine.Object> Ensure = (path, type) => {
  var a = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
  if (a == null) { a = UnityEngine.ScriptableObject.CreateInstance(type); UnityEditor.AssetDatabase.CreateAsset(a, path); Log.Append("created ").Append(path).Append("\n"); }
  return a; };

var flags = new[]{
  new[]{ "Flag_TC_EnterSeen", "T字通路：入ったときの会話（E1）を見た。指示書になし（E1 を1回だけにするため）" },
  new[]{ "Flag_TC_Rumble", "T字通路：縦の通路の途中で、床の震えと遠くの音符の光（E2）を見た。指示書になし（1回だけにするため）" },
  new[]{ "Flag_TC_DoorEventDone", "T字通路：扉の前のイベント（E3）が終わった（指示書 FLAG_TC_DOOR_EVENT_DONE）。E3 を1回だけ・シグルの向き・調べる／話しかけるの有効化・左右へ行ける" },
  new[]{ "Flag_TC_NoteGlowed", "T字通路：E3 でノアが扉に触れて音符のマークが光った（指示書 FLAG_TC_NOTE_GLOWED）。光を表示したまま" },
  new[]{ "Flag_TC_WestPipeOn", "T字通路：西の部屋の成果で西の管に伝わった（指示書 FLAG_WEST_PIPE_ON。仮。西の部屋の仕様で立てる。今はどこからも立てない）。西のランプが点く・西の管が震える" },
  new[]{ "Flag_TC_EastPipeOn", "T字通路：東の部屋の成果で東の管に伝わった（指示書 FLAG_EAST_PIPE_ON。仮。東の部屋の仕様で立てる。今はどこからも立てない）。東のランプが点く・東の管が震える" },
};
foreach (var f in flags) { var a = (GameFlag)Ensure("Assets/Data/Flags/" + f[0] + ".asset", typeof(GameFlag)); SetProp(a, "id", f[0]); SetProp(a, "memo", f[1]); SetProp(a, "saved", true); }

UnityEditor.AssetDatabase.SaveAssets();
return Log.ToString();
