// asset-intake：一時置き場（Assets/__Intake/__NAME__/）の素材を調べ、問題の候補と割り当て表の案を作る
// inspect.ps1 が __NAME__・__DRAFT__ を置き換えて実行する
var sb = new System.Text.StringBuilder();
string root = "Assets/__Intake/__NAME__";
string draftPath = "__DRAFT__";
UnityEditor.AssetDatabase.Refresh();

// ---- 素材チェックリストの行（ID → 素材名・入れる場所・形式）と確認キー ----
var rows = new System.Collections.Generic.Dictionary<string, string[]>();
foreach (var line in System.IO.File.ReadAllLines(".claude/skills/asset-audit/data/list.tsv", System.Text.Encoding.UTF8))
{
    var c = line.Split('\t'); if (c.Length > 8 && c[0] != "ID") rows[c[0]] = c;
}
var keys = new System.Collections.Generic.Dictionary<string, string>();
foreach (var line in System.IO.File.ReadAllLines(".claude/skills/asset-audit/data/keys.tsv", System.Text.Encoding.UTF8))
{
    var c = line.Split('\t'); if (c.Length > 1) keys[c[0]] = c[1];
}
System.Func<string, string> IdOf = folder =>
{
    var m = System.Text.RegularExpressions.Regex.Match(folder, @"^([A-Z]+-\d+)");
    return m.Success ? m.Groups[1].Value : null;
};

// ---- 1つずつ調べる ----
var draft = new System.Text.StringBuilder("ファイル\t入れる先\t設定\tメモ\n# 案（asset-intake の inspect が作った）。「?」の所を決めてから apply.ps1 で試す → 流す\n");
var byFolder = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.List<string>>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AudioClip t:Texture2D", new[] { root }))
{
    string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    string rel = p.Substring(root.Length + 1);
    string folder = rel.Contains("/") ? rel.Substring(0, rel.IndexOf('/')) : "(top)";
    if (!byFolder.TryGetValue(folder, out var list)) { list = new System.Collections.Generic.List<string>(); byFolder[folder] = list; }
    list.Add(p);
}
int warn = 0;
foreach (var kv in byFolder)
{
    string id = IdOf(kv.Key);
    string[] row = id != null && rows.ContainsKey(id) ? rows[id] : null;
    sb.Append("\n== " + kv.Key + (row != null ? "  [" + id + " " + row[4] + "]" : id != null ? "  [" + id + "：チェックリストにない]" : "  [ID なし]") + "\n");
    if (row != null) sb.Append("   入れる場所：" + row[7] + "\n   形式の目安：" + row[8] + "\n");
    if (id != null && keys.ContainsKey(id)) sb.Append("   確認キー：" + keys[id] + "\n");
    draft.Append("# ---- " + kv.Key + (row != null ? "：" + row[4] + "｜入れる場所：" + row[7] : "") + (id != null && keys.ContainsKey(id) ? "｜キー：" + keys[id] : "") + "\n");

    foreach (var p in kv.Value)
    {
        string file = System.IO.Path.GetFileName(p);
        var notes = new System.Collections.Generic.List<string>();
        string info = "";
        bool skip = file.Contains("_image_") || p.Contains("/_old/");
        if (skip) notes.Add("通しの試聴用・古い物（入れない）");
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(p);
        if (clip != null)
        {
            info = clip.length.ToString("0.00") + "s " + clip.channels + "ch " + clip.frequency + "Hz";
            bool isBgm = kv.Key.StartsWith("BGM");
            if (p.EndsWith(".mp3") && (isBgm || file.Contains("loop"))) notes.Add("mp3（ループのつなぎ目に隙間が出やすい。ogg がよい）");
            if (!isBgm && clip.loadType == UnityEngine.AudioClipLoadType.DecompressOnLoad && clip.length < 30)
            {
                var data = new float[clip.samples * clip.channels];
                if (clip.GetData(data, 0))
                {
                    double sum = 0; float peak = 0; int lead = 0; bool found = false;
                    for (int i = 0; i < data.Length; i++) { float a = System.Math.Abs(data[i]); sum += a * a; if (a > peak) peak = a; if (!found) { if (a > 0.01f) found = true; else lead++; } }
                    double rms = 20 * System.Math.Log10(System.Math.Max(1e-6, System.Math.Sqrt(sum / data.Length)));
                    double pk = 20 * System.Math.Log10(System.Math.Max(1e-6, peak));
                    float leadSec = (float)lead / clip.channels / clip.frequency;
                    info += " peak " + pk.ToString("0.0") + "dB rms " + rms.ToString("0.0") + "dB";
                    if (rms > -14) notes.Add("大きい（SeData の音量で下げる。自動の音量で 0.3 前後）");
                    if (rms < -28) notes.Add("かなり小さい（音量 1 でも小さい。素材を上げてもらう）");
                    if (pk > -0.3) notes.Add("割れているかも（最大がほぼ 0dB）");
                    if (leadSec > 0.08f) notes.Add("頭に無音 " + leadSec.ToString("0.00") + " 秒（鳴るのが遅れる）");
                }
            }
            if (isBgm && clip.length > 180 && file.Contains("loop")) notes.Add("ループの曲が " + (clip.length / 60).ToString("0.0") + " 分（長い）");
        }
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(p);
        if (tex != null)
        {
            var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(p);
            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            info = w + "x" + h + (imp.DoesSourceTextureHaveAlpha() ? " 透過あり" : " 透過なし");
            if (!imp.DoesSourceTextureHaveAlpha() && !kv.Key.StartsWith("MAP")) notes.Add("透過がない（キャラ・アイコンなら背景が四角く出る）");
            if (System.Math.Max(w, h) > 2048) notes.Add("2048 より大きい（取り込みの決まりで Max Size を上げる）");
            if (kv.Key.StartsWith("POR") && (w != 1920 || h != 1080)) notes.Add("立ち絵は 1920×1080 のキャンバスがそろえやすい（今のノアと同じ）");
            if (kv.Key.StartsWith("UI") && file.ToLower().Contains("icon") && w != h) notes.Add("アイコンは正方形がよい");
            if (System.Text.RegularExpressions.Regex.IsMatch(file, @"_\d{2}\.png$")) notes.Add("連番（アニメ・エフェクトのデータにまとめる）");
        }
        // 直した版（同じ名前がプロジェクトにある）
        bool revised = false;
        foreach (var eg in UnityEditor.AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(file)))
        {
            string ep = UnityEditor.AssetDatabase.GUIDToAssetPath(eg);
            if (System.IO.Path.GetFileName(ep) == file && !ep.StartsWith("Assets/__Intake/")) { notes.Add("直した版（同じ名前が " + ep + " にある → place.ps1 -Update で中身を上書き。表は要らない）"); revised = true; break; }
        }
        if (revised) { if (notes.Count > 0) warn++; sb.Append("   " + file + "  " + info + "  ⚠ " + string.Join("／", notes) + "\n"); draft.Append("# " + file + "\t-\t\t直した版（place.ps1 -Update で上書き）\n"); continue; }

        // 名前の決まり（Docs/素材の渡し方.md）から入れる先を推す
        string guess = "?";
        string stem = System.IO.Path.GetFileNameWithoutExtension(file);
        var exprs = new[] { "Normal", "Smile", "Angry", "Sad", "Surprise", "Troubled" };
        if (tex != null)
        {
            string chara = System.Text.RegularExpressions.Regex.IsMatch(stem, "(?i)noah|ノア") ? "Noah" : System.Text.RegularExpressions.Regex.IsMatch(stem, "(?i)sigle|sigul|siguru|シグル") ? "Sigle" : null;
            foreach (var e in exprs) if (chara != null && stem.EndsWith("_" + e, System.StringComparison.OrdinalIgnoreCase)) guess = "chara:" + chara + ":" + e;
            if (guess == "?" && stem.StartsWith("Item_"))
            {
                foreach (var ig in UnityEditor.AssetDatabase.FindAssets(stem + " t:ItemData"))
                    if (System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GUIDToAssetPath(ig)) == stem) guess = "item:" + stem;
            }
        }
        if (guess == "?" && id != null && keys.ContainsKey(id) && kv.Value.Count == 1 && !keys[id].Contains(";") && keys[id].StartsWith("slot:")) guess = keys[id];
        if (guess != "?") notes.Add("入れる先の案：" + guess);

        if (notes.Count > 0) warn++;
        sb.Append("   " + file + "  " + info + (notes.Count > 0 ? "  ⚠ " + string.Join("／", notes) : "") + "\n");
        if (!skip) draft.Append(file + "\t" + guess + "\t\t" + (guess != "?" ? "名前の決まりから推した（確かめる）" : "") + "\n");
        else draft.Append("# " + file + "\t-\t\t入れない\n");
    }
}
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(draftPath));
System.IO.File.WriteAllText(draftPath, draft.ToString(), new System.Text.UTF8Encoding(false));
sb.Insert(0, "素材の点検：" + root + "（気になる所 " + warn + " 個）\n割り当て表の案：" + draftPath + "\n");
return sb.ToString();
