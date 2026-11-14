// 音の素材（2026-11-14 BGM_SE_Assets）：SE の SeData を作る（何度実行しても同じ。あれば作り直さず値を合わせる）
// ・Assets/SE/SE-xx_*/ の wav ごとに Se_<名前>.asset（"SE-xx_" と拡張子を外した名前）
// ・足音は4つをまとめて1つ（ノア・シグル、ついてくるときは音量 0.5）、ピッチを少し揺らす
// ・音量は聞こえ方（RMS）を -20dB くらいにそろえる（大きい音は下げる。小さい音は 1 のまま＝素材側で上げてほしい）
// ・UI の音は、今の Assets/SE/UI/Se_UI_*.asset（UISounds が使っている）の音だけ新しい素材に差し替える
var sb = new System.Text.StringBuilder();
System.Func<string, UnityEngine.AudioClip> Clip = p => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(p);

System.Func<UnityEngine.AudioClip, float> AutoVolume = c =>
{
    if (c == null) return 1f;
    var data = new float[c.samples * c.channels];
    if (!c.GetData(data, 0)) return 1f;
    double sum = 0; foreach (var v in data) sum += v * v;
    double rms = 20 * System.Math.Log10(System.Math.Max(1e-6, System.Math.Sqrt(sum / System.Math.Max(1, data.Length))));
    float vol = (float)System.Math.Pow(10, (-20.0 - rms) / 20.0);
    return UnityEngine.Mathf.Round(UnityEngine.Mathf.Clamp(vol, 0.25f, 1f) * 100f) / 100f;
};

System.Action<string, UnityEngine.AudioClip[], float, float, float, bool, float, int> Make = (path, clips, vol, pmin, pmax, loop, minInterval, maxConc) =>
{
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(path);
    bool created = d == null;
    if (created) { d = UnityEngine.ScriptableObject.CreateInstance<SeData>(); UnityEditor.AssetDatabase.CreateAsset(d, path); }
    var so = new UnityEditor.SerializedObject(d);
    var arr = so.FindProperty("clips"); arr.arraySize = clips.Length;
    for (int i = 0; i < clips.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
    so.FindProperty("volume").floatValue = vol;
    so.FindProperty("pitchMin").floatValue = pmin;
    so.FindProperty("pitchMax").floatValue = pmax;
    so.FindProperty("loop").boolValue = loop;
    so.FindProperty("minInterval").floatValue = minInterval;
    so.FindProperty("maxConcurrent").intValue = maxConc;
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append((created ? "new " : "set ") + System.IO.Path.GetFileName(path) + " vol=" + vol + (loop ? " loop" : "") + "\n");
};

// ---- 足音 ----
foreach (var who in new[] { "noah", "sigul" })
{
    var clips = new UnityEngine.AudioClip[4];
    for (int i = 0; i < 4; i++) clips[i] = Clip($"Assets/SE/SE-01_Footsteps/SE-01_footstep_{who}_0{i + 1}.wav");
    string name = who == "noah" ? "Noah" : "Sigle";
    float v = 0.7f;
    Make($"Assets/SE/SE-01_Footsteps/Se_Footstep_{name}.asset", clips, v, 0.95f, 1.05f, false, 0.05f, 2);
    Make($"Assets/SE/SE-01_Footsteps/Se_Footstep_{name}_Follow.asset", clips, v * 0.5f, 0.95f, 1.05f, false, 0.05f, 2);
}

// ---- そのほかの SE（1ファイル 1 SeData） ----
var loops = new System.Collections.Generic.HashSet<string> { "SE-10_guard_loop", "SE-20_monster_steps_loop" };
foreach (var dir in System.IO.Directory.GetDirectories("Assets/SE", "SE-*"))
{
    string folder = dir.Replace('\\', '/');
    if (folder.EndsWith("SE-01_Footsteps") || folder.EndsWith("SE-15_UI")) continue;
    foreach (var f in System.IO.Directory.GetFiles(folder, "*.wav"))
    {
        string p = f.Replace('\\', '/');
        string baseName = System.IO.Path.GetFileNameWithoutExtension(p);              // SE-16_peg_start
        string shortName = baseName.Substring(baseName.IndexOf('_') + 1);                // peg_start
        var c = Clip(p);
        bool loop = loops.Contains(baseName);
        float vol = AutoVolume(c);
        float minInt = 0.03f; int maxConc = 4;
        if (baseName.StartsWith("SE-25_")) { vol = 0.45f; minInt = 0.025f; maxConc = 1; }   // 文字送り：控えめ・重ねない
        if (baseName.StartsWith("SE-18_")) { maxConc = 3; }                                  // 攻撃：重なりすぎない
        if (loop) { maxConc = 1; }
        Make($"{folder}/Se_{shortName}.asset", new[] { c }, vol, 1f, 1f, loop, minInt, maxConc);
    }
}

// ---- UI（今の SeData の音を差し替える） ----
var ui = new System.Collections.Generic.Dictionary<string, string> {
    { "Se_UI_Select", "select" }, { "Se_UI_Decide", "decide" }, { "Se_UI_Cancel", "cancel" },
    { "Se_UI_MenuOpen", "menu_open" }, { "Se_UI_MenuClose", "menu_close" }, { "Se_UI_Save", "save" }, { "Se_UI_Cannot", "cannot" } };
foreach (var kv in ui)
{
    string path = $"Assets/SE/UI/{kv.Key}.asset";
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<SeData>(path);
    if (d == null) { sb.Append("missing " + path + "\n"); continue; }
    var c = Clip($"Assets/SE/SE-15_UI/SE-15_ui_{kv.Value}.wav");
    var so = new UnityEditor.SerializedObject(d);
    var arr = so.FindProperty("clips"); arr.arraySize = 1; arr.GetArrayElementAtIndex(0).objectReferenceValue = c;
    so.FindProperty("volume").floatValue = AutoVolume(c);
    so.ApplyModifiedPropertiesWithoutUndo();
    sb.Append("ui " + kv.Key + " <- " + (c != null ? c.name : "NULL") + " vol=" + so.FindProperty("volume").floatValue + "\n");
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
