/*********************************************************************/
// * \file   AssetImportRules.cs
// * \brief  素材の取り込み設定の自動化：置いたフォルダで、画像・音の取り込み設定を決める（Assets/Data/AssetTools/取り込み設定.tsv）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 素材の取り込み設定（フォルダごとの決まり）
/// </summary>
///
/// ・表は Assets/Data/AssetTools/取り込み設定.tsv（フォルダ・種類・設定・メモ）。一番長く一致したフォルダの行を使う
/// ・新しく入れたファイル（初めての取り込み）と、取り込みの一時置き場（Assets/__Intake/）から移したファイルにだけかける。今ある素材は変えない
/// ・今ある素材にかけ直すときは、Project で選んで右クリック > 素材 > 取り込み設定をかけ直す
/// ・設定の書き方：画像＝ppu・mesh(tight/full)・filter(bilinear/point)・compress(on/off/hq)・mip(on/off)・maxsize(auto/数)・mode(single/multiple)
///                ・sheet(名前に入っていたら連番として Multiple にする言葉を , で。例 _sheet,_animetion)
///                音＝load(streaming/decompress/compressed)・format(vorbis/pcm/adpcm)・quality(1〜100)・mono(on/off)・background(on/off)
public class AssetImportRules : AssetPostprocessor
{
    public const string RulesPath = "Assets/Data/AssetTools/取り込み設定.tsv";
    public const string IntakeFolder = "Assets/__Intake/";

    /// <summary> 1行 </summary>
    public class Rule
    {
        public string folder;
        public string kind;     // 画像 / 音
        public Dictionary<string, string> settings = new Dictionary<string, string>();
        public string memo;
    }

    private static List<Rule> cache;
    private static DateTime cacheTime;

    // 取り込みの一時置き場から移したファイル（移したあとの取り込みでかけ直す）
    private static readonly HashSet<string> forced = new HashSet<string>();

    //=====================================================================
    // 表を読む
    //=====================================================================

    /// <summary> 表を読む（ファイルが変わったら読み直す）。AssetDatabase を使わずに読む（取り込み中でも安全） </summary>
    public static List<Rule> Load()
    {
        string full = Path.GetFullPath(RulesPath);
        if(!File.Exists(full)) { return new List<Rule>(); }
        DateTime t = File.GetLastWriteTimeUtc(full);
        if(cache != null && t == cacheTime) { return cache; }

        var rules = new List<Rule>();
        string[] lines = File.ReadAllLines(full, System.Text.Encoding.UTF8);
        for(int i = 1; i < lines.Length; i++)
        {
            string[] c = lines[i].Split('\t');
            if(c.Length < 3 || string.IsNullOrWhiteSpace(c[0])) { continue; }
            var r = new Rule { folder = c[0].Trim(), kind = c[1].Trim(), memo = c.Length > 3 ? c[3] : "" };
            foreach(string kv in c[2].Split(';'))
            {
                int eq = kv.IndexOf('=');
                if(eq > 0) { r.settings[kv.Substring(0, eq).Trim().ToLowerInvariant()] = kv.Substring(eq + 1).Trim().ToLowerInvariant(); }
            }
            rules.Add(r);
        }
        cache = rules;
        cacheTime = t;
        return rules;
    }

    /// <summary> そのパス・種類に合う行（一番長く一致したフォルダ）。なければ null </summary>
    public static Rule Find(string assetPath, string kind)
    {
        Rule best = null;
        foreach(Rule r in Load())
        {
            if(r.kind != kind || !assetPath.StartsWith(r.folder, StringComparison.OrdinalIgnoreCase)) { continue; }
            if(best == null || r.folder.Length > best.folder.Length) { best = r; }
        }
        return best;
    }

    //=====================================================================
    // 取り込みのとき
    //=====================================================================

    private bool ShouldApply => assetImporter.importSettingsMissing || forced.Contains(assetPath);

    private void OnPreprocessTexture()
    {
        if(!ShouldApply) { return; }
        forced.Remove(assetPath);
        Rule r = Find(assetPath, "画像");
        if(r != null) { ApplyTexture((TextureImporter)assetImporter, r); }
    }

    private void OnPreprocessAudio()
    {
        if(!ShouldApply) { return; }
        forced.Remove(assetPath);
        Rule r = Find(assetPath, "音");
        if(r != null) { ApplyAudio((AudioImporter)assetImporter, r); }
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        // 一時置き場から移したファイルは、移した先のフォルダの決まりで取り込み直す
        for(int i = 0; i < moved.Length; i++)
        {
            if(!movedFrom[i].StartsWith(IntakeFolder) || moved[i].StartsWith(IntakeFolder)) { continue; }
            if(Find(moved[i], "画像") == null && Find(moved[i], "音") == null) { continue; }
            forced.Add(moved[i]);
            AssetDatabase.ImportAsset(moved[i], ImportAssetOptions.ForceUpdate);
        }
    }

    //=====================================================================
    // 設定をかける
    //=====================================================================

    /// <summary> 画像の設定をかける </summary>
    public static void ApplyTexture(TextureImporter imp, Rule r)
    {
        imp.textureType = TextureImporterType.Sprite;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);

        // 1枚の絵か、何コマも並んだ絵（連番）か。sheet= に書いた言葉が名前に入っていれば連番（Multiple。切るのは Sprite Editor で）
        bool isSheet = false;
        if(r.settings.TryGetValue("sheet", out string sheet))
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(imp.assetPath).ToLowerInvariant();
            foreach(string word in sheet.Split(',')) { if(word.Length > 0 && file.Contains(word.Trim())) { isSheet = true; } }
        }
        if(isSheet) { s.spriteMode = (int)SpriteImportMode.Multiple; }
        else if(r.settings.TryGetValue("mode", out string mode)) { s.spriteMode = mode == "multiple" ? (int)SpriteImportMode.Multiple : (int)SpriteImportMode.Single; }
        else if(s.spriteMode == (int)SpriteImportMode.None) { s.spriteMode = (int)SpriteImportMode.Single; }
        if(r.settings.TryGetValue("ppu", out string ppu) && float.TryParse(ppu, out float p)) { s.spritePixelsPerUnit = p; }
        if(r.settings.TryGetValue("mesh", out string mesh)) { s.spriteMeshType = mesh == "full" ? SpriteMeshType.FullRect : SpriteMeshType.Tight; }
        if(r.settings.TryGetValue("filter", out string filter)) { s.filterMode = filter == "point" ? FilterMode.Point : FilterMode.Bilinear; }
        if(r.settings.TryGetValue("mip", out string mip)) { s.mipmapEnabled = mip == "on"; }
        s.alphaIsTransparency = true;
        imp.SetTextureSettings(s);

        if(r.settings.TryGetValue("compress", out string comp))
        {
            imp.textureCompression = comp == "off" ? TextureImporterCompression.Uncompressed
                                   : comp == "hq" ? TextureImporterCompression.CompressedHQ
                                   : TextureImporterCompression.Compressed;
        }
        if(r.settings.TryGetValue("maxsize", out string max))
        {
            int size = 2048;
            if(max == "auto")
            {
                // 絵が縮まないように、絵の大きさ以上の 2 のべき乗にする（最大 8192）
                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                size = 32;
                while(size < Mathf.Max(w, h) && size < 8192) { size *= 2; }
                size = Mathf.Max(size, 2048);
            }
            else if(!int.TryParse(max, out size)) { size = 2048; }
            imp.maxTextureSize = size;
        }
    }

    /// <summary> 音の設定をかける </summary>
    public static void ApplyAudio(AudioImporter imp, Rule r)
    {
        AudioImporterSampleSettings s = imp.defaultSampleSettings;
        if(r.settings.TryGetValue("load", out string load))
        {
            s.loadType = load == "streaming" ? AudioClipLoadType.Streaming
                       : load == "compressed" ? AudioClipLoadType.CompressedInMemory
                       : AudioClipLoadType.DecompressOnLoad;
        }
        if(r.settings.TryGetValue("format", out string format))
        {
            s.compressionFormat = format == "pcm" ? AudioCompressionFormat.PCM
                                : format == "adpcm" ? AudioCompressionFormat.ADPCM
                                : AudioCompressionFormat.Vorbis;
        }
        if(r.settings.TryGetValue("quality", out string q) && float.TryParse(q, out float quality)) { s.quality = Mathf.Clamp01(quality / 100.0f); }
        imp.defaultSampleSettings = s;
        if(r.settings.TryGetValue("background", out string bg)) { imp.loadInBackground = bg == "on"; }
        if(r.settings.TryGetValue("mono", out string mono)) { imp.forceToMono = mono == "on"; }
    }

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Assets/素材/取り込み設定をかけ直す（フォルダの決まりで）")]
    private static void Reapply()
    {
        int n = 0;
        foreach(string guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var paths = new List<string>();
            if(AssetDatabase.IsValidFolder(path))
            {
                foreach(string g in AssetDatabase.FindAssets("t:Texture2D t:AudioClip", new[] { path })) { paths.Add(AssetDatabase.GUIDToAssetPath(g)); }
            }
            else { paths.Add(path); }

            foreach(string p in paths) { if(ReapplyOne(p)) { n++; } }
        }
        Debug.Log($"取り込み設定をかけ直しました：{n} 個（{RulesPath} の決まり）");
    }

    /// <summary> 1つにかけ直す（決まりがあれば true） </summary>
    public static bool ReapplyOne(string path)
    {
        AssetImporter imp = AssetImporter.GetAtPath(path);
        if(imp is TextureImporter ti) { Rule r = Find(path, "画像"); if(r == null) { return false; } ApplyTexture(ti, r); ti.SaveAndReimport(); return true; }
        if(imp is AudioImporter ai)   { Rule r = Find(path, "音");   if(r == null) { return false; } ApplyAudio(ai, r); ai.SaveAndReimport(); return true; }
        return false;
    }
}
