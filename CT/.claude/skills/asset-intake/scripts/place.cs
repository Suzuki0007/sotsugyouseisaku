// asset-intake：一時置き場（Assets/__Intake/__NAME__/）から、本当の置き場所へ移す（移すと取り込みの決まりがかかる）
// ・フォルダ名の頭（BGM・SE・POR・FX…）で置き場所を決める（data/destinations.tsv）。フォルダはそのまま
// ・「_image_」（通しの試聴用）・「_old」のフォルダ・__SKIP__ に書いた名前は移さない（一時置き場ごと消える）
var sb = new System.Text.StringBuilder();
string root = "Assets/__Intake/__NAME__";
var skip = new System.Collections.Generic.HashSet<string>("__SKIP__".Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
var dest = new System.Collections.Generic.List<string[]>();
foreach (var line in System.IO.File.ReadAllLines(".claude/skills/asset-intake/data/destinations.tsv")) { var c = line.Split('\t'); if (c.Length > 2 && c[0] != "prefix") dest.Add(c); }

System.Func<string, string, string> DestOf = (folder, kind) =>
{
    foreach (var d in dest) if (!d[0].StartsWith("*") && (folder == d[0] || folder.StartsWith(d[0] + "-") || folder.StartsWith(d[0] + "_"))) return d[1];
    foreach (var d in dest) if (d[0] == "*" + kind) return d[1];
    return null;
};
System.Action<string> EnsureFolder = null;
EnsureFolder = f =>
{
    if (UnityEditor.AssetDatabase.IsValidFolder(f)) return;
    string parent = System.IO.Path.GetDirectoryName(f).Replace('\\', '/');
    EnsureFolder(parent);
    UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(f));
};

int moved = 0, skipped = 0;
foreach (var g in UnityEditor.AssetDatabase.FindAssets("", new[] { root }))
{
    string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    if (UnityEditor.AssetDatabase.IsValidFolder(p)) continue;
    string rel = p.Substring(root.Length + 1);
    string file = System.IO.Path.GetFileName(p);
    if (file.Contains("_image_") || rel.Contains("/_old/") || skip.Contains(file)) { skipped++; sb.Append("  skip " + rel + "\n"); continue; }
    var t = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(p);
    string kind = t == typeof(UnityEngine.AudioClip) ? "audio" : t == typeof(UnityEngine.Texture2D) ? "image" : null;
    if (kind == null) { sb.Append("  ?? not audio/image: " + rel + "\n"); continue; }
    string folder = rel.Contains("/") ? rel.Substring(0, rel.IndexOf('/')) : "";
    string baseDest = DestOf(folder, kind);
    string to = baseDest + "/" + rel;

    // 直した版（同じファイル名がプロジェクトにある）：中身だけ上書きする（GUID・取り込みの設定・参照はそのまま）
    string existing = System.IO.File.Exists(to) ? to : null;
    if (existing == null)
    {
        foreach (var eg in UnityEditor.AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(file)))
        {
            string ep = UnityEditor.AssetDatabase.GUIDToAssetPath(eg);
            if (System.IO.Path.GetFileName(ep) == file && !ep.StartsWith("Assets/__Intake/")) { existing = ep; break; }
        }
    }
    if (existing != null)
    {
        if (!__UPDATE__) { sb.Append("  !! same name exists (not moved; use -Update to overwrite): " + existing + "\n"); continue; }
        System.IO.File.Copy(p, existing, true);
        UnityEditor.AssetDatabase.ImportAsset(existing, UnityEditor.ImportAssetOptions.ForceUpdate);
        moved++;
        sb.Append("  updated " + existing + "\n");
        continue;
    }
    EnsureFolder(System.IO.Path.GetDirectoryName(to).Replace('\\', '/'));
    string err = UnityEditor.AssetDatabase.MoveAsset(p, to);
    if (string.IsNullOrEmpty(err)) { moved++; sb.Append("  " + to + "\n"); } else sb.Append("  !! " + err + "\n");
}
UnityEditor.AssetDatabase.DeleteAsset(root);
if (UnityEditor.AssetDatabase.IsValidFolder("Assets/__Intake") && UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/__Intake" }).Length == 0) UnityEditor.AssetDatabase.DeleteAsset("Assets/__Intake");
sb.Insert(0, "moved " + moved + ", skipped " + skipped + " (staging folder removed)\n");
return sb.ToString();
