/*********************************************************************/
// * \file   BarkCsv.cs
// * \brief  相棒の一言と CSV の変換（取り込み・書き出し・ひな形）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 相棒の一言の CSV
/// </summary>
///
/// CSV の形（1行目は見出し）：
///   一言ID, きっかけ, 優先度, 確率, 回数, 次に出せるまで, 秒数, 候補, 話す, 文
///   ・一言ID …………… データのファイル名。空なら上の行と同じ一言の続き
///   ・きっかけ〜秒数 … 一言IDのある行だけ読む。空ならそのまま（新しく作るときは初期値）
///   ・候補 …………………  同じ番号の行が1つの候補（2行以上で掛け合い）。空なら上の行と同じ候補
///   ・話す ………………… 相棒 / 操作中 / ノア / シグル / きっかけ / きっかけの相棒
///   ・「#」で始まる行は無視する
///
/// 取り込み先：Resources/Barks/<CSVの名前>/。同じ名前のデータがあれば、候補と書いた項目だけ上書きする
/// （条件・倒した敵・SE は CSV にないので、Inspector で設定したものが残る）
public static class BarkCsv
{
    private const string Header = "一言ID,きっかけ,優先度,確率,回数,次に出せるまで,秒数,候補,話す,文";

    private static readonly (string name, BarkTrigger trigger)[] triggerNames =
    {
        ("イベント", BarkTrigger.Manual),
        ("使えないキャラ", BarkTrigger.WrongCharacter),
        ("使えない", BarkTrigger.CannotUse),
        ("相棒の出番", BarkTrigger.PartnerGimmickNear),
        ("来られない", BarkTrigger.CannotFollow),
        ("ダメージ", BarkTrigger.Damaged),
        ("体力が少ない", BarkTrigger.LowHealth),
        ("敵を倒した", BarkTrigger.EnemyDefeated),
        ("待たせた", BarkTrigger.Wait),
        ("長く待たせた", BarkTrigger.LongWait),
        ("呼び戻した", BarkTrigger.CalledBack),
        ("離れ離れ", BarkTrigger.Separated),
        ("合流", BarkTrigger.Reunited),
        ("切り替え", BarkTrigger.Switched),
        ("止まっている", BarkTrigger.Idle),
        ("シーンに着いた", BarkTrigger.SceneEnter),
        ("謎を解いた", BarkTrigger.PuzzleSolved),
        ("間違えた", BarkTrigger.PuzzleWrong),
    };

    private static readonly (string name, BarkSpeaker speaker)[] speakerNames =
    {
        ("相棒", BarkSpeaker.Partner),
        ("操作中", BarkSpeaker.Current),
        ("ノア", BarkSpeaker.Noah),
        ("シグル", BarkSpeaker.Sigle),
        ("きっかけ", BarkSpeaker.Subject),
        ("きっかけの相棒", BarkSpeaker.SubjectPartner),
    };

    private static readonly (string name, BarkRepeat repeat)[] repeatNames =
    {
        ("何度でも", BarkRepeat.Always),
        ("シーンで1回", BarkRepeat.OncePerScene),
        ("1回だけ", BarkRepeat.OnceEver),
    };

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Tools/仕掛け/相棒の一言/CSVから取り込む", false, 330)]
    public static void ImportFromPanel()
    {
        string full = EditorUtility.OpenFilePanel("一言のCSVを選ぶ", Application.dataPath, "csv");
        if(string.IsNullOrEmpty(full)) { return; }
        Import(full, true);
    }

    [MenuItem("Tools/仕掛け/相棒の一言/CSVのひな形を作る", false, 331)]
    public static void CreateTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject("ひな形の保存先", "Barks", "csv", "一言のCSVのひな形を保存する場所を選んでください");
        if(string.IsNullOrEmpty(path)) { return; }
        WriteTemplate(path);
        AssetDatabase.ImportAsset(path);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
    }

    [MenuItem("Tools/仕掛け/相棒の一言/すべてCSVに書き出す", false, 332)]
    public static void ExportAllFromPanel()
    {
        string path = EditorUtility.SaveFilePanelInProject("書き出し先", "Barks", "csv", "CSV の保存先を選んでください");
        if(string.IsNullOrEmpty(path)) { return; }
        Export(LoadAll(), path);
        AssetDatabase.ImportAsset(path);
    }

    /// <summary> ひな形を書く </summary>
    public static void WriteTemplate(string assetPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine("# 「#」で始まる行はメモ（取り込まれません）。きっかけ：" + JoinNames(triggerNames));
        sb.AppendLine("# 話す：" + JoinNames(speakerNames) + "　回数：" + JoinNames(repeatNames));
        sb.AppendLine("Bark_待たせた,待たせた,0,1,何度でも,20,,1,相棒,ここで待ってる");
        sb.AppendLine(",,,,,,,2,相棒,早く戻ってきて");
        sb.AppendLine("Bark_呼び戻した,呼び戻した,0,1,何度でも,20,,1,相棒,今行く");
        sb.AppendLine(",,,,,,,,操作中,急いで");
        sb.AppendLine("Bark_長く待たせた,長く待たせた,0,1,何度でも,60,25,1,相棒,…まだかな");
        DialogueCsv.WriteUtf8Bom(assetPath, sb.ToString());
    }

    //=====================================================================
    // 取り込み
    //=====================================================================

    /// <summary>
    /// CSV を取り込む
    /// </summary>
    /// <param name="fullPath"> CSV のパス（Assets の外でもよい） </param>
    /// <returns> 作った・上書きしたデータの数 </returns>
    public static int Import(string fullPath, bool showDialog)
    {
        List<List<string>> rows = DialogueCsv.Parse(DialogueCsv.ReadText(fullPath));
        string folder = BarkMenu.DataFolder + "/" + System.IO.Path.GetFileNameWithoutExtension(fullPath);
        System.IO.Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        var errors = new List<string>();
        var done = new List<BarkData>();
        BarkData current = null;
        BarkVariant variant = null;
        string variantKey = null;

        for(int i = 1; i < rows.Count; i++)
        {
            List<string> r = rows[i];
            string id = Cell(r, 0);
            if(id.StartsWith("#")) { continue; }
            if(r.TrueForAll(c => string.IsNullOrWhiteSpace(c))) { continue; }
            int line = i + 1;

            if(!string.IsNullOrEmpty(id))
            {
                if(current != null) { Finish(current); }
                current = LoadOrCreate(folder, id);
                current.variants.Clear();
                variant = null;
                variantKey = null;
                done.Add(current);

                string t = Cell(r, 1);
                if(t != "") { if(TryName(triggerNames, t, out BarkTrigger trig)) { current.trigger = trig; } else { errors.Add(line + " 行目：きっかけ「" + t + "」がわからない"); } }
                if(Cell(r, 2) != "") { if(int.TryParse(Cell(r, 2), out int p)) { current.priority = p; } else { errors.Add(line + " 行目：優先度が数でない"); } }
                if(Cell(r, 3) != "") { if(TryFloat(Cell(r, 3), out float c)) { current.chance = Mathf.Clamp01(c); } else { errors.Add(line + " 行目：確率が数でない"); } }
                string rep = Cell(r, 4);
                if(rep != "") { if(TryName(repeatNames, rep, out BarkRepeat rp)) { current.repeat = rp; } else { errors.Add(line + " 行目：回数「" + rep + "」がわからない"); } }
                if(Cell(r, 5) != "") { if(TryFloat(Cell(r, 5), out float cd)) { current.cooldown = Mathf.Max(0.0f, cd); } else { errors.Add(line + " 行目：次に出せるまでが数でない"); } }
                if(Cell(r, 6) != "") { if(TryFloat(Cell(r, 6), out float sec)) { current.seconds = Mathf.Max(0.0f, sec); } else { errors.Add(line + " 行目：秒数が数でない"); } }
            }
            else if(current == null)
            {
                errors.Add(line + " 行目：一言IDがない（最初の行には一言IDを書く）");
                continue;
            }

            // 候補（空なら上の行と同じ。最初の行なら新しい候補）
            string key = Cell(r, 7);
            if(variant == null || (key != "" && key != variantKey))
            {
                variant = new BarkVariant();
                current.variants.Add(variant);
                variantKey = key;
            }

            string text = Cell(r, 9);
            if(text == "") { continue; }

            BarkSpeaker speaker = BarkSpeaker.Partner;
            string sp = Cell(r, 8);
            if(sp != "" && !TryName(speakerNames, sp, out speaker)) { errors.Add(line + " 行目：話す「" + sp + "」がわからない（相棒にした）"); speaker = BarkSpeaker.Partner; }
            variant.lines.Add(new BarkLine { speaker = speaker, text = text });
        }
        if(current != null) { Finish(current); }

        AssetDatabase.SaveAssets();
        if(Application.isPlaying) { Bark.Reload(); }

        string message = done.Count + " 個の一言を取り込みました（" + folder + "）";
        if(errors.Count > 0) { message += "\n\n気をつけること：\n" + string.Join("\n", errors); }
        if(showDialog) { EditorUtility.DisplayDialog("一言のCSV", message, "OK"); }
        else           { Debug.Log("[一言のCSV] " + message); }
        return done.Count;
    }

    private static void Finish(BarkData d)
    {
        d.variants.RemoveAll(v => v == null || v.lines.Count == 0);
        EditorUtility.SetDirty(d);
    }

    private static BarkData LoadOrCreate(string folder, string id)
    {
        string path = folder + "/" + id + ".asset";
        var d = AssetDatabase.LoadAssetAtPath<BarkData>(path);
        if(d != null) { return d; }

        d = ScriptableObject.CreateInstance<BarkData>();
        AssetDatabase.CreateAsset(d, path);
        return d;
    }

    //=====================================================================
    // 書き出し
    //=====================================================================

    /// <summary> 一言を CSV に書き出す </summary>
    public static void Export(IEnumerable<BarkData> list, string assetPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach(BarkData d in list)
        {
            if(d == null) { continue; }
            bool first = true;
            int n = 0;
            foreach(BarkVariant v in d.variants)
            {
                if(v == null) { continue; }
                n++;
                bool firstInVariant = true;
                foreach(BarkLine l in v.lines)
                {
                    if(l == null) { continue; }
                    var cells = new string[10];
                    if(first)
                    {
                        cells[0] = d.name;
                        cells[1] = NameOf(triggerNames, d.trigger);
                        cells[2] = d.priority.ToString();
                        cells[3] = d.chance.ToString(CultureInfo.InvariantCulture);
                        cells[4] = NameOf(repeatNames, d.repeat);
                        cells[5] = d.cooldown.ToString(CultureInfo.InvariantCulture);
                        cells[6] = d.seconds.ToString(CultureInfo.InvariantCulture);
                    }
                    cells[7] = firstInVariant ? n.ToString() : "";
                    cells[8] = NameOf(speakerNames, l.speaker);
                    cells[9] = l.text;
                    for(int i = 0; i < cells.Length; i++) { cells[i] = DialogueCsv.Escape(cells[i] ?? ""); }
                    sb.AppendLine(string.Join(",", cells));
                    first = false;
                    firstInVariant = false;
                }
            }
        }
        DialogueCsv.WriteUtf8Bom(assetPath, sb.ToString());
    }

    //=====================================================================
    // 共通
    //=====================================================================

    /// <summary> Resources/Barks/ の一言を全部（名前の順） </summary>
    public static List<BarkData> LoadAll()
    {
        var list = new List<BarkData>();
        foreach(string guid in AssetDatabase.FindAssets("t:BarkData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<BarkData>(AssetDatabase.GUIDToAssetPath(guid));
            if(d != null) { list.Add(d); }
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    /// <summary> きっかけの短い名前 </summary>
    public static string TriggerName(BarkTrigger t) => NameOf(triggerNames, t);

    private static string Cell(List<string> r, int i) => i < r.Count && r[i] != null ? r[i].Trim() : "";

    private static bool TryFloat(string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private static bool TryName<T>((string name, T value)[] table, string s, out T value) where T : struct, Enum
    {
        foreach(var e in table) { if(e.name == s) { value = e.value; return true; } }
        return Enum.TryParse(s, true, out value) && Enum.IsDefined(typeof(T), value);
    }

    private static string NameOf<T>((string name, T value)[] table, T v) where T : struct, Enum
    {
        foreach(var e in table) { if(EqualityComparer<T>.Default.Equals(e.value, v)) { return e.name; } }
        return v.ToString();
    }

    private static string JoinNames<T>((string name, T value)[] table)
    {
        var names = new List<string>();
        foreach(var e in table) { names.Add(e.name); }
        return string.Join("・", names);
    }
}
