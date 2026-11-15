/*********************************************************************/
// * \file   AssetMapApplier.cs
// * \brief  割り当て表（Docs/素材の割り当て/*.tsv）を読んで、素材をプレハブ・データ・シーンの欄にまとめて入れる
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 割り当て表を流す
/// </summary>
///
/// 表（UTF-8 のタブ区切り。1行目は見出し、# で始まる行は飛ばす）：ファイル ／ 入れる先 ／ 設定 ／ メモ
/// ・ファイル：Assets からのパスかファイル名（同じ名前が1つだけなら）。「a.wav+b.wav」でまとめて1つの SeData。「x.png:絵の名前」で画像の中の絵。「-」は入れない（取り込むだけ）
/// ・入れる先（; で複数。素材チェックリストの確認キーと同じ書き方）：
///   slot:部品.欄@場所#n   … その部品の欄に入れる。場所＝シーン名・プレハブ／データのパス・オブジェクトの階層の一部。#n は見つかった n 番目だけ。
///                          欄の後ろに [] で「リストに足す」、[2] で「3つ目」（0 から）
///   bgm:シーン            … そのシーンの SceneBgm の基本の曲（設定 vol= で音量）
///   bgm-rule:シーン/名前=フラグ … SceneBgm の条件付きの曲（フラグが ON の間。子に「条件_名前」を作る）
///   event-se:シーン/イベントの階層@番号 … その EventSequence の流れの「番号」番目（1 から）に効果音の部品を足す（同じ音があれば足さない）
///   chara:キャラ:表情     … Assets/Data/Characters/キャラ.asset の表情の絵（Normal・Smile・Angry・Sad・Surprise・Troubled）
///   item:アイテム         … アイテムのデータのアイコン
///   replace:古い素材のパス … 古い素材を使っている所を全部この素材に付け替える（仮素材の差し替え）
/// ・設定（; 区切り）：SeData を作るとき vol=auto|0〜1・loop=on・pitch=0.95-1.05・interval=秒・max=数・name=Se_名前、bgm の vol
/// ・SeData の欄に音のファイルを書くと、同じフォルダに Se_〇〇 を作って（あれば使って）入れる
/// ・まず「試す」（dryRun）で何がどう変わるかを見てから流す。同じ表を何度流しても同じ結果
public static class AssetMapApplier
{
    public const string Folder = "Docs/素材の割り当て";

    private class Row
    {
        public int line;
        public string files;
        public string target;
        public Dictionary<string, string> opts = new Dictionary<string, string>();
        public int hits;
        public List<string> notes = new List<string>();
    }

    //=====================================================================
    // 入口
    //=====================================================================

    /// <summary>
    /// 表を流す（dryRun なら何も変えずに予定を返す）
    /// </summary>
    public static string Run(string tsvPath, bool dryRun)
    {
        var sb = new StringBuilder();
        string dirty = AssetWalker.DirtySceneName();
        if(dirty != null) { return $"!! シーン「{dirty}」に保存していない変更があります（先に保存してください）"; }
        if(Application.isPlaying) { return "!! 再生中は流せません"; }

        List<Row> rows = Parse(tsvPath, sb);
        sb.Insert(0, (dryRun ? "【試す：何も変えていない】" : "【流した】") + " " + tsvPath + "（" + rows.Count + " 行）\n");

        // 1) 入れる物を用意する（SeData を作る・素材を探す）。シーンを開いても消えないように AssetKey で覚える
        var values = new Dictionary<Row, Dictionary<string, AssetKey>>();

        // 2) 入れる先ごとに分ける
        var slotOps = new List<(Row row, string comp, string field, string place, int nth)>();
        var sceneOps = new Dictionary<string, List<(Row row, string kind, string arg)>>();
        var charaOps = new List<(Row row, string arg)>();
        var replaceOps = new List<(Row row, string arg)>();

        foreach(Row r in rows)
        {
            foreach(string raw in r.target.Split(';'))
            {
                string t = raw.Trim();
                if(t.Length == 0 || t == "-") { continue; }
                int colon = t.IndexOf(':');
                string kind = colon > 0 ? t.Substring(0, colon) : t;
                string arg = colon > 0 ? t.Substring(colon + 1) : "";

                switch(kind)
                {
                    case "slot":
                        if(ParseSlot(arg, out string comp, out string field, out string place, out int nth)) { slotOps.Add((r, comp, field, place, nth)); }
                        else { r.notes.Add("!! 書き方が違う：" + t); }
                        break;
                    case "item":
                        slotOps.Add((r, "ItemData", "icon", "/" + arg + ".asset", 0));
                        break;
                    case "bgm":
                    case "bgm-rule":
                    case "event-se":
                    {
                        string scene = arg.Split('/')[0];
                        if(!sceneOps.TryGetValue(scene, out var list)) { list = new List<(Row, string, string)>(); sceneOps[scene] = list; }
                        list.Add((r, kind, arg.Substring(Math.Min(arg.Length, scene.Length + 1))));
                        break;
                    }
                    case "chara":
                        charaOps.Add((r, arg));
                        break;
                    case "replace":
                        replaceOps.Add((r, arg));
                        break;
                    default:
                        r.notes.Add("!! わからない入れる先：" + t);
                        break;
                }
            }
        }

        // 3) 部品の欄（プレハブ → データ → シーン を1回ずつ見て回る）
        if(slotOps.Count > 0)
        {
            var counts = new Dictionary<int, int>();
            AssetWalker.Walk(place =>
            {
                bool changed = false;
                for(int i = 0; i < slotOps.Count; i++)
                {
                    var op = slotOps[i];
                    if(!TypeMatches(place.target.GetType(), op.comp)) { continue; }
                    if(!string.IsNullOrEmpty(op.place) && place.Label.IndexOf(op.place, StringComparison.Ordinal) < 0) { continue; }
                    counts.TryGetValue(i, out int n); n++; counts[i] = n;
                    if(op.nth > 0 && n != op.nth) { continue; }
                    if(SetField(op.row, place, op.field, dryRun)) { changed = true; }
                }
                return changed;
            }, !dryRun);
            for(int i = 0; i < slotOps.Count; i++)
            {
                if(!counts.ContainsKey(i)) { slotOps[i].row.notes.Add($"!! 見つからない：{slotOps[i].comp}.{slotOps[i].field}@{slotOps[i].place}"); }
            }
        }

        // 4) シーンごとの操作
        if(sceneOps.Count > 0)
        {
            string start = SceneManager.GetActiveScene().path;
            foreach(var kv in sceneOps)
            {
                string path = ScenePath(kv.Key);
                if(path == null) { foreach(var op in kv.Value) { op.row.notes.Add("!! シーンがない：" + kv.Key); } continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach(var op in kv.Value)
                {
                    if(op.kind == "bgm")      { changed |= ApplyBgm(op.row, scene, dryRun); }
                    if(op.kind == "bgm-rule") { changed |= ApplyBgmRule(op.row, scene, op.arg, dryRun); }
                    if(op.kind == "event-se") { changed |= ApplyEventSe(op.row, scene, op.arg, dryRun); }
                }
                if(changed && !dryRun) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            if(!string.IsNullOrEmpty(start)) { EditorSceneManager.OpenScene(start, OpenSceneMode.Single); }
        }

        // 5) キャラの表情
        foreach(var op in charaOps) { ApplyChara(op.row, op.arg, dryRun); }

        // 6) 差し替え（最後。上で入れた物も付け替えの対象になる）
        foreach(var op in replaceOps) { ApplyReplace(op.row, op.arg, dryRun); }

        if(!dryRun) { AssetDatabase.SaveAssets(); }

        // 7) 報告
        int ok = 0, bad = 0;
        foreach(Row r in rows)
        {
            bool err = r.notes.Exists(n => n.StartsWith("!!"));
            if(err) { bad++; } else { ok++; }
            sb.AppendLine($"{(err ? "NG" : "OK")} {r.line}行目 {r.files} → {r.target}");
            foreach(string n in r.notes) { sb.AppendLine("    " + n); }
        }
        sb.AppendLine($"合計：OK {ok}・NG {bad}");
        return sb.ToString();
    }

    //=====================================================================
    // 表を読む
    //=====================================================================

    private static List<Row> Parse(string path, StringBuilder sb)
    {
        var rows = new List<Row>();
        string full = Path.GetFullPath(path);
        if(!File.Exists(full)) { sb.AppendLine("!! 表がない：" + path); return rows; }
        string[] lines = File.ReadAllLines(full, Encoding.UTF8);
        for(int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if(string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) { continue; }
            string[] c = line.Split('\t');
            if(c.Length < 2) { sb.AppendLine($"!! {i + 1}行目：列が足りない"); continue; }
            var r = new Row { line = i + 1, files = c[0].Trim(), target = c[1].Trim() };
            if(c.Length > 2)
            {
                foreach(string kv in c[2].Split(';'))
                {
                    int eq = kv.IndexOf('=');
                    if(eq > 0) { r.opts[kv.Substring(0, eq).Trim().ToLowerInvariant()] = kv.Substring(eq + 1).Trim(); }
                }
            }
            rows.Add(r);
        }
        return rows;
    }

    private static bool ParseSlot(string arg, out string comp, out string field, out string place, out int nth)
    {
        comp = field = place = null; nth = 0;
        string body = arg;
        int hash = body.LastIndexOf('#');
        if(hash > 0 && int.TryParse(body.Substring(hash + 1), out int n)) { nth = n; body = body.Substring(0, hash); }
        int at = body.IndexOf('@');
        if(at >= 0) { place = body.Substring(at + 1); body = body.Substring(0, at); }
        int dot = body.IndexOf('.');
        if(dot <= 0) { return false; }
        comp = body.Substring(0, dot);
        field = body.Substring(dot + 1);
        return true;
    }

    private static bool TypeMatches(Type t, string name)
    {
        for(Type c = t; c != null; c = c.BaseType) { if(c.Name == name) { return true; } }
        return false;
    }

    //=====================================================================
    // 入れる物
    //=====================================================================

    /// <summary> 表の「ファイル」の1つを探す（パス・ファイル名・「:絵の名前」） </summary>
    private static string FindFile(string spec, out string subName)
    {
        subName = null;
        // 「x.png:絵の名前」：最後の : の前に拡張子があれば、後ろは画像の中の絵の名前
        int colon = spec.LastIndexOf(':');
        if(colon > 0 && Path.HasExtension(spec.Substring(0, colon)))
        {
            subName = spec.Substring(colon + 1);
            spec = spec.Substring(0, colon);
        }
        if(spec.StartsWith("Assets/")) { return File.Exists(spec) ? spec : null; }

        string name = Path.GetFileNameWithoutExtension(spec);
        string found = null;
        foreach(string guid in AssetDatabase.FindAssets(name))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if(Path.GetFileName(p) != spec && Path.GetFileNameWithoutExtension(p) != spec) { continue; }
            if(found != null && found != p) { return "!!2"; }
            found = p;
        }
        return found;
    }

    /// <summary> その欄（型）に入れる物を用意する（SeData なら作る）。用意できなければ null </summary>
    private static UnityEngine.Object ValueFor(Row r, string propType, bool dryRun)
    {
        // 欄の型（Unity の型は「PPtr<Sprite>」、スクリプトの型は「PPtr<$SeData>」）
        propType = "PPtr<$" + propType.Replace("PPtr<", "").Replace("$", "").TrimEnd('>') + ">";
        var paths = new List<string>();
        string sub = null;
        foreach(string f in r.files.Split('+'))
        {
            string p = FindFile(f.Trim(), out string s);
            if(p == null)   { r.notes.Add("!! ファイルがない：" + f); return null; }
            if(p == "!!2")  { r.notes.Add("!! 同じ名前が2つ以上：" + f + "（Assets からのパスで書く）"); return null; }
            if(s != null) { sub = s; }
            paths.Add(p);
        }
        if(paths.Count == 0) { return null; }

        if(propType == "PPtr<$SeData>")
        {
            var clips = new List<AudioClip>();
            foreach(string p in paths)
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if(c == null) { var existing = AssetDatabase.LoadAssetAtPath<SeData>(p); if(existing != null) { return existing; } r.notes.Add("!! 音ではない：" + p); return null; }
                clips.Add(c);
            }
            return GetOrCreateSeData(r, paths[0], clips, dryRun);
        }
        if(propType == "PPtr<$Sprite>")
        {
            List<Sprite> sprites = AssetReplacer.SpritesOf(paths[0]);
            Sprite sp = sub != null ? sprites.Find(x => x.name == sub) : (sprites.Count > 0 ? sprites[0] : null);
            if(sp == null) { r.notes.Add("!! 絵（Sprite）がない：" + paths[0] + (sub != null ? ":" + sub : "") + "（取り込みの種類を Sprite に）"); }
            return sp;
        }
        if(propType == "PPtr<$AudioClip>") { return AssetDatabase.LoadAssetAtPath<AudioClip>(paths[0]); }
        if(propType == "PPtr<$Texture2D>" || propType == "PPtr<$Texture>") { return AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]); }
        return AssetDatabase.LoadMainAssetAtPath(paths[0]);
    }

    /// <summary> 音から SeData を作る（同じフォルダの Se_名前。あれば音と設定を合わせる） </summary>
    private static SeData GetOrCreateSeData(Row r, string firstPath, List<AudioClip> clips, bool dryRun)
    {
        string baseName = Path.GetFileNameWithoutExtension(firstPath);
        string shortName = baseName.StartsWith("SE-") && baseName.IndexOf('_') > 0 ? baseName.Substring(baseName.IndexOf('_') + 1) : baseName;
        string name = r.opts.TryGetValue("name", out string n) ? n : "Se_" + shortName;
        // name= に Assets からのパスを書くと、その SeData を使う（今ある SeData の音を差し替えるとき）
        string path = name.StartsWith("Assets/") ? name : Path.GetDirectoryName(firstPath).Replace('\\', '/') + "/" + name + ".asset";

        var d = AssetDatabase.LoadAssetAtPath<SeData>(path);
        if(d == null)
        {
            if(dryRun) { r.notes.Add("SeData を作る：" + path); return null; }
            d = ScriptableObject.CreateInstance<SeData>();
            AssetDatabase.CreateAsset(d, path);
            if(!r.opts.ContainsKey("vol")) { r.opts["vol"] = "auto"; }
            r.notes.Add("SeData を作った：" + path);
        }
        if(dryRun) { return d; }

        var so = new SerializedObject(d);
        var arr = so.FindProperty("clips");
        arr.arraySize = clips.Count;
        for(int i = 0; i < clips.Count; i++) { arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i]; }
        if(r.opts.TryGetValue("vol", out string vol)) { so.FindProperty("volume").floatValue = vol == "auto" ? AutoVolume(clips[0]) : ParseFloat(vol, 1.0f); }
        if(r.opts.TryGetValue("loop", out string loop)) { so.FindProperty("loop").boolValue = loop == "on"; }
        if(r.opts.TryGetValue("pitch", out string pitch))
        {
            string[] ab = pitch.Split('-');
            so.FindProperty("pitchMin").floatValue = ParseFloat(ab[0], 1.0f);
            so.FindProperty("pitchMax").floatValue = ParseFloat(ab.Length > 1 ? ab[1] : ab[0], 1.0f);
        }
        if(r.opts.TryGetValue("interval", out string interval)) { so.FindProperty("minInterval").floatValue = ParseFloat(interval, 0.03f); }
        if(r.opts.TryGetValue("max", out string max)) { so.FindProperty("maxConcurrent").intValue = (int)ParseFloat(max, 4); }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(d);
        return d;
    }

    /// <summary> 聞こえ方（RMS）を -20dB くらいにそろえる音量（大きい音だけ下げる） </summary>
    public static float AutoVolume(AudioClip c)
    {
        if(c == null || c.loadType != AudioClipLoadType.DecompressOnLoad) { return 1.0f; }
        var data = new float[c.samples * c.channels];
        if(!c.GetData(data, 0)) { return 1.0f; }
        double sum = 0; foreach(float v in data) { sum += v * v; }
        double rms = 20 * Math.Log10(Math.Max(1e-6, Math.Sqrt(sum / Math.Max(1, data.Length))));
        float vol = (float)Math.Pow(10, (-20.0 - rms) / 20.0);
        return Mathf.Round(Mathf.Clamp(vol, 0.25f, 1.0f) * 100.0f) / 100.0f;
    }

    private static float ParseFloat(string s, float fallback) => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f) ? f : fallback;

    //=====================================================================
    // 入れる
    //=====================================================================

    /// <summary> 部品の欄に入れる（「欄[]」で足す・「欄[2]」で n 番目） </summary>
    private static bool SetField(Row r, AssetWalker.Place place, string field, bool dryRun)
    {
        var so = new SerializedObject(place.target);
        bool append = field.EndsWith("[]");
        int index = -1;
        string name = field;
        if(append) { name = field.Substring(0, field.Length - 2); }
        else if(field.EndsWith("]") && field.Contains("["))
        {
            int b = field.IndexOf('[');
            int.TryParse(field.Substring(b + 1, field.Length - b - 2), out index);
            name = field.Substring(0, b);
        }

        SerializedProperty p = so.FindProperty(name);
        if(p == null) { r.notes.Add($"!! {place.target.GetType().Name} に欄「{name}」がない"); return false; }

        SerializedProperty target = p;
        if(p.isArray && p.propertyType == SerializedPropertyType.Generic)
        {
            if(!append && index < 0) { r.notes.Add($"!! 「{name}」はリスト（{name}[] で足す・{name}[0] で n 番目）"); return false; }
            if(append) { index = p.arraySize; }
            if(index >= p.arraySize) { p.arraySize = index + 1; }
            target = p.GetArrayElementAtIndex(index);
        }
        if(target.propertyType != SerializedPropertyType.ObjectReference) { r.notes.Add($"!! 「{field}」は素材を入れる欄ではない"); return false; }

        int notesBefore = r.notes.Count;
        UnityEngine.Object value = ValueFor(r, target.type, dryRun);
        bool failed = r.notes.Count > notesBefore && r.notes[r.notes.Count - 1].StartsWith("!!");
        if(value == null && (failed || !dryRun)) { return false; }

        // リストに足すとき、もう入っていれば足さない
        if(append && value != null)
        {
            for(int i = 0; i < index; i++) { if(p.GetArrayElementAtIndex(i).objectReferenceValue == value) { r.notes.Add($"（もう入っている）{place.Label} {name}"); return false; } }
        }

        string before = target.objectReferenceValue != null ? target.objectReferenceValue.name : "空";
        string after = value != null ? value.name : "（作る SeData）";
        if(target.objectReferenceValue == value && value != null) { r.notes.Add($"（同じ）{place.Label} {field} = {after}"); r.hits++; return false; }
        r.notes.Add($"{place.Label} {place.target.GetType().Name}.{field}：{before} → {after}");
        r.hits++;
        if(dryRun) { return false; }
        target.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static bool ApplyBgm(Row r, Scene scene, bool dryRun)
    {
        SceneBgm bgm = FindInScene<SceneBgm>(scene);
        if(bgm == null) { r.notes.Add("!! SceneBgm がない：" + scene.name); return false; }
        var so = new SerializedObject(bgm);
        var clip = ValueFor(r, "PPtr<$AudioClip>", dryRun) as AudioClip;
        if(clip == null) { return false; }
        var p = so.FindProperty("defaultClip");
        r.hits++;
        if(p.objectReferenceValue == clip) { r.notes.Add($"（同じ）{scene.name} 基本の曲 = {clip.name}"); if(dryRun || !r.opts.ContainsKey("vol")) { return false; } }
        else { r.notes.Add($"{scene.name} 基本の曲：{(p.objectReferenceValue != null ? p.objectReferenceValue.name : "空")} → {clip.name}"); }
        if(dryRun) { return false; }
        p.objectReferenceValue = clip;
        if(r.opts.TryGetValue("vol", out string vol)) { so.FindProperty("defaultVolume").floatValue = ParseFloat(vol, 1.0f); }
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static bool ApplyBgmRule(Row r, Scene scene, string arg, bool dryRun)
    {
        int eq = arg.IndexOf('=');
        if(eq <= 0) { r.notes.Add("!! bgm-rule:シーン/名前=フラグ の形で"); return false; }
        string label = arg.Substring(0, eq);
        string flagName = arg.Substring(eq + 1);
        GameFlag flag = null;
        foreach(string g in AssetDatabase.FindAssets(flagName + " t:GameFlag"))
        {
            var f = AssetDatabase.LoadAssetAtPath<GameFlag>(AssetDatabase.GUIDToAssetPath(g));
            if(f != null && f.name == flagName) { flag = f; }
        }
        if(flag == null) { r.notes.Add("!! フラグがない：" + flagName); return false; }
        SceneBgm bgm = FindInScene<SceneBgm>(scene);
        if(bgm == null) { r.notes.Add("!! SceneBgm がない：" + scene.name); return false; }
        var clip = ValueFor(r, "PPtr<$AudioClip>", dryRun) as AudioClip;
        if(clip == null) { return false; }
        r.notes.Add($"{scene.name} 条件付きの曲「{label}」＝{clip.name}（{flagName} が ON の間）");
        if(dryRun) { return false; }

        Transform holderT = bgm.transform.Find("条件_" + label);
        GameObject holder = holderT != null ? holderT.gameObject : new GameObject("条件_" + label);
        holder.transform.SetParent(bgm.transform, false);
        var cond = holder.GetComponent<BgmConditionFlag>();
        if(cond == null) { cond = holder.AddComponent<BgmConditionFlag>(); }
        var cso = new SerializedObject(cond); cso.FindProperty("flag").objectReferenceValue = flag; cso.ApplyModifiedPropertiesWithoutUndo();

        var so = new SerializedObject(bgm);
        var rules = so.FindProperty("rules");
        SerializedProperty rule = null;
        for(int i = 0; i < rules.arraySize; i++) { if(rules.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue == label) { rule = rules.GetArrayElementAtIndex(i); } }
        if(rule == null) { rules.arraySize++; rule = rules.GetArrayElementAtIndex(rules.arraySize - 1); rule.FindPropertyRelative("priority").intValue = 10; rule.FindPropertyRelative("fadeTime").floatValue = 0.8f; rule.FindPropertyRelative("latch").boolValue = false; }
        rule.FindPropertyRelative("label").stringValue = label;
        rule.FindPropertyRelative("condition").objectReferenceValue = cond;
        rule.FindPropertyRelative("clip").objectReferenceValue = clip;
        rule.FindPropertyRelative("volume").floatValue = r.opts.TryGetValue("vol", out string vol) ? ParseFloat(vol, 1.0f) : 1.0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static bool ApplyEventSe(Row r, Scene scene, string arg, bool dryRun)
    {
        int at = arg.LastIndexOf('@');
        if(at <= 0 || !int.TryParse(arg.Substring(at + 1), out int position)) { r.notes.Add("!! event-se:シーン/イベントの階層@番号 の形で"); return false; }
        string path = arg.Substring(0, at);
        GameObject go = FindPath(scene, path);
        EventSequence seq = go != null ? go.GetComponent<EventSequence>() : null;
        if(seq == null) { r.notes.Add("!! EventSequence がない：" + scene.name + "/" + path); return false; }

        var se = ValueFor(r, "PPtr<$SeData>", dryRun) as SeData;
        if(se == null && !dryRun) { return false; }
        foreach(EventAction a in seq.GetActions())
        {
            if(a is SeEventAction s && se != null && new SerializedObject(s).FindProperty("seOnEnter").objectReferenceValue == se)
            {
                r.notes.Add($"（もう入っている）{path} の効果音 {se.name}");
                r.hits++;
                return false;
            }
        }
        r.notes.Add($"{scene.name} {path} の流れの {position} 番目に効果音 {(se != null ? se.name : "（作る SeData）")} を足す");
        r.hits++;
        if(dryRun) { return false; }

        var add = go.AddComponent<SeEventAction>();
        var aso = new SerializedObject(add);
        aso.FindProperty("seOnEnter").objectReferenceValue = se;
        aso.FindProperty("playAtThisPosition").boolValue = false;
        aso.ApplyModifiedPropertiesWithoutUndo();

        var so = new SerializedObject(seq);
        var list = so.FindProperty("actions");
        if(list.arraySize == 0)
        {
            foreach(EventAction a in seq.GetComponents<EventAction>()) { if(a == add) { continue; } list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = a; }
        }
        int index = Mathf.Clamp(position - 1, 0, list.arraySize);
        if(index >= list.arraySize) { list.arraySize++; }
        else                        { list.InsertArrayElementAtIndex(index); }
        list.GetArrayElementAtIndex(index).objectReferenceValue = add;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static void ApplyChara(Row r, string arg, bool dryRun)
    {
        string[] c = arg.Split(':');
        if(c.Length < 2) { r.notes.Add("!! chara:キャラ:表情 の形で"); return; }
        var profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>("Assets/Data/Characters/" + c[0] + ".asset");
        if(profile == null) { r.notes.Add("!! キャラがない：Assets/Data/Characters/" + c[0] + ".asset"); return; }
        if(!Enum.TryParse(c[1], out Expression expr)) { r.notes.Add("!! 表情の名前が違う：" + c[1] + "（Normal・Smile・Angry・Sad・Surprise・Troubled …）"); return; }
        var sprite = ValueFor(r, "PPtr<$Sprite>", dryRun) as Sprite;
        if(sprite == null) { return; }

        var so = new SerializedObject(profile);
        var list = so.FindProperty("expressions");
        SerializedProperty slot = null;
        for(int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            if(e.FindPropertyRelative("expression").intValue == (int)expr) { slot = e; }
        }
        string before = slot != null && slot.FindPropertyRelative("sprite").objectReferenceValue != null ? slot.FindPropertyRelative("sprite").objectReferenceValue.name : "なし";
        r.notes.Add($"{c[0]} の表情 {expr}：{before} → {sprite.name}");
        r.hits++;
        if(dryRun) { return; }
        if(slot == null) { list.arraySize++; slot = list.GetArrayElementAtIndex(list.arraySize - 1); slot.FindPropertyRelative("expression").intValue = (int)expr; }
        slot.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    private static void ApplyReplace(Row r, string oldPath, bool dryRun)
    {
        UnityEngine.Object oldAsset = AssetDatabase.LoadMainAssetAtPath(oldPath);
        if(oldAsset == null) { r.notes.Add("!! 古い素材がない：" + oldPath); return; }
        string p = FindFile(r.files.Split('+')[0].Trim(), out _);
        if(p == null || p == "!!2") { r.notes.Add("!! 新しい素材が見つからない：" + r.files); return; }
        UnityEngine.Object newAsset = AssetDatabase.LoadMainAssetAtPath(p);
        var log = new StringBuilder();
        var map = AssetReplacer.BuildMap(oldAsset, newAsset, log);
        var hits = AssetReplacer.Replace(map, dryRun);
        if(log.Length > 0) { r.notes.Add(log.ToString().TrimEnd()); }
        r.notes.Add(AssetReplacer.Report(hits, dryRun).TrimEnd());
        r.hits += hits.Count;
        if(hits.Count == 0) { r.notes.Add("（使っている所がない）"); }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static string ScenePath(string name)
    {
        foreach(string guid in AssetDatabase.FindAssets(name + " t:Scene"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if(Path.GetFileNameWithoutExtension(p) == name) { return p; }
        }
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            T c = root.GetComponentInChildren<T>(true);
            if(c != null) { return c; }
        }
        return null;
    }

    private static GameObject FindPath(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            if(root.name != parts[0]) { continue; }
            Transform t = root.transform;
            for(int i = 1; i < parts.Length && t != null; i++) { t = t.Find(parts[i]); }
            if(t != null) { return t.gameObject; }
        }
        return null;
    }

    //=====================================================================
    // メニュー（Project で表を選んで右クリック）
    //=====================================================================

    [MenuItem("Tools/素材/割り当て表を試す（変えない）")]
    private static void MenuDry() { RunSelected(true); }

    [MenuItem("Tools/素材/割り当て表を流す")]
    private static void MenuRun()
    {
        if(!EditorUtility.DisplayDialog("割り当て表", "選んだ表のとおりに素材を入れて、プレハブ・データ・シーンを保存します。先に「試す」で中身を確かめてください。", "流す", "やめる")) { return; }
        RunSelected(false);
    }

    private static void RunSelected(bool dryRun)
    {
        string path = EditorUtility.OpenFilePanel("割り当て表（.tsv）", Path.GetFullPath(Folder), "tsv");
        if(string.IsNullOrEmpty(path)) { return; }
        string result = Run(path, dryRun);
        Debug.Log(result);
        EditorUtility.DisplayDialog("割り当て表", result.Length > 1500 ? result.Substring(0, 1500) + "\n…（続きは Console）" : result, "OK");
    }
}
