/*********************************************************************/
// * \file   DialogueCsv.cs
// * \brief  会話データと CSV の変換（取り込み・書き出し）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 会話データ（DialogueData）を CSV から作る・CSV に書き出すエディタ拡張
/// </summary>
///
/// CSV の形（1行目は見出し）：
///   会話ID, キャラ, 表情, 名前, 本文
///   ・会話ID ……… DialogueData のファイル名。空なら上の行と同じ会話の続き
///   ・キャラ ……… CharacterProfile のファイル名か表示名。空なら立ち絵なし（看板・ナレーション）
///   ・表情 ………… Normal / 通常 などの名前。空なら Normal
///   ・名前 ………… 名前欄の上書き（例：？？？）。空ならキャラの名前
///   ・本文 ………… 表示する文。セルの中で改行してもよい
///   ・「#」で始まる行は無視する（メモ用）
///
/// 取り込み先：CSV と同じフォルダの「CSV名」フォルダ。同じ名前の会話データがあれば上書きする
/// （上書きなので、シーンからの参照は切れない）
public static class DialogueCsv
{
    private const string MenuRoot = "Tools/会話/";

    // 表情の日本語名
    private static readonly Dictionary<string, Expression> expressionNames = new Dictionary<string, Expression>
    {
        { "通常", Expression.Normal },
        { "笑顔", Expression.Smile },
        { "怒り", Expression.Angry },
        { "悲しみ", Expression.Sad },
        { "驚き", Expression.Surprise },
        { "困り", Expression.Troubled },
    };

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Assets/会話CSVを取り込む", false, 2000)]
    private static void ImportSelected()
    {
        foreach(UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if(path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) { Import(path); }
        }
    }

    [MenuItem("Assets/会話CSVを取り込む", true)]
    private static bool ImportSelectedValidate()
    {
        foreach(UnityEngine.Object obj in Selection.objects)
        {
            if(AssetDatabase.GetAssetPath(obj).EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }

    [MenuItem(MenuRoot + "CSVから会話データを作成", false, 0)]
    private static void ImportFromPanel()
    {
        string full = EditorUtility.OpenFilePanel("会話CSVを選ぶ", Application.dataPath, "csv");
        if(string.IsNullOrEmpty(full)) { return; }

        string assetPath = ToAssetPath(full);
        if(assetPath == null)
        {
            EditorUtility.DisplayDialog("会話CSV", "CSV は Assets フォルダの中に置いてから選んでください。", "OK");
            return;
        }

        Import(assetPath);
    }

    [MenuItem(MenuRoot + "CSVのひな形を作成", false, 1)]
    private static void CreateTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject("ひな形の保存先", "Dialogues", "csv", "会話CSVのひな形を保存する場所を選んでください");
        if(string.IsNullOrEmpty(path)) { return; }

        var sb = new StringBuilder();
        sb.AppendLine("会話ID,キャラ,表情,名前,本文");
        sb.AppendLine("# 「#」で始まる行はメモ（取り込まれません）");
        sb.AppendLine("Elder_First,Elder,通常,,よく来たな、旅の者よ。");
        sb.AppendLine(",Elder,笑顔,,北の森へ行ってみるといい。");
        sb.AppendLine(",Noah,驚き,,森……ですか？");
        sb.AppendLine("Sign_Village,,,,ここは はじまりの村");
        sb.AppendLine("Mystery,,,？？？,\"誰かの声が聞こえる……\n（セルの中で改行もできます）\"");

        WriteUtf8Bom(path, sb.ToString());
        AssetDatabase.ImportAsset(path);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
    }

    [MenuItem(MenuRoot + "選んだ会話データをCSVに書き出す", false, 2)]
    private static void ExportSelected()
    {
        var list = new List<DialogueData>();
        foreach(UnityEngine.Object obj in Selection.objects)
        {
            if(obj is DialogueData data) { list.Add(data); }
        }

        if(list.Count == 0)
        {
            EditorUtility.DisplayDialog("会話CSV", "Project ウィンドウで会話データ（DialogueData）を選んでから実行してください。", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("書き出し先", "Dialogues", "csv", "CSV の保存先を選んでください");
        if(string.IsNullOrEmpty(path)) { return; }

        var sb = new StringBuilder();
        sb.AppendLine("会話ID,キャラ,表情,名前,本文");
        foreach(DialogueData data in list)
        {
            bool first = true;
            foreach(DialogueData.Line line in data.Lines)
            {
                sb.Append(Escape(first ? data.name : "")).Append(',')
                  .Append(Escape(line.character != null ? line.character.name : "")).Append(',')
                  .Append(Escape(line.expression.ToString())).Append(',')
                  .Append(Escape(line.speakerName ?? "")).Append(',')
                  .Append(Escape(line.text ?? "")).Append('\n');
                first = false;
            }
        }

        WriteUtf8Bom(path, sb.ToString());
        AssetDatabase.ImportAsset(path);
        Debug.Log($"[会話CSV] {list.Count} 件を {path} に書き出しました");
    }

    //=====================================================================
    // 取り込み
    //=====================================================================

    /// <param name="csvPath"> 取り込む CSV（Assets/ から始まるパス） </param>
    /// <param name="showDialog"> 結果をダイアログで出すか（スクリプトから呼ぶときは false） </param>
    public static void Import(string csvPath, bool showDialog = true)
    {
        string text;
        try
        {
            text = ReadText(Path.GetFullPath(csvPath));
        }
        catch(Exception e)
        {
            Debug.LogError("[会話CSV] CSV を読めませんでした。「CSV UTF-8」形式で保存し直してください。" + e.Message);
            if(showDialog) { EditorUtility.DisplayDialog("会話CSV", "CSV を読めませんでした。「CSV UTF-8」形式で保存し直してください。\n" + e.Message, "OK"); }
            return;
        }

        List<List<string>> rows = Parse(text);
        if(rows.Count == 0) { return; }

        // 見出し行から列の位置を決める（見出しがなければ 会話ID,キャラ,表情,名前,本文 の順とみなす）
        int colId = 0, colChara = 1, colExpr = 2, colName = 3, colText = 4;
        int start = 0;
        if(IsHeader(rows[0]))
        {
            start = 1;
            colId = Find(rows[0], "会話ID", "ID");
            colChara = Find(rows[0], "キャラ", "キャラクター", "Character");
            colExpr = Find(rows[0], "表情", "Expression");
            colName = Find(rows[0], "名前", "Name", "Speaker");
            colText = Find(rows[0], "本文", "Text", "セリフ");

            if(colId < 0 || colText < 0)
            {
                Debug.LogError("[会話CSV] 1行目に「会話ID」と「本文」の列が見つかりません: " + csvPath);
                if(showDialog) { EditorUtility.DisplayDialog("会話CSV", "1行目に「会話ID」と「本文」の列が見つかりません。", "OK"); }
                return;
            }
        }

        Dictionary<string, CharacterProfile> characters = LoadCharacters();
        var warnings = new List<string>();

        // 会話ID → 文の一覧（CSV に出てきた順）
        var dialogues = new Dictionary<string, List<DialogueData.Line>>();
        var order = new List<string>();
        string currentId = null;

        for(int r = start; r < rows.Count; r++)
        {
            List<string> row = rows[r];
            int rowNumber = r + 1;

            if(row.Count == 0 || (row.Count == 1 && row[0].Trim().Length == 0)) { continue; }
            if(Cell(row, 0).TrimStart().StartsWith("#")) { continue; }

            string id = Cell(row, colId).Trim();
            if(id.Length > 0)
            {
                if(id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    warnings.Add($"{rowNumber}行目：会話ID「{id}」にファイル名に使えない文字があります（飛ばしました）");
                    currentId = null;
                    continue;
                }

                currentId = id;
                if(!dialogues.ContainsKey(id))
                {
                    dialogues.Add(id, new List<DialogueData.Line>());
                    order.Add(id);
                }
                else
                {
                    warnings.Add($"{rowNumber}行目：会話ID「{id}」が2回目に出てきました（前の続きとして追加します）");
                }
            }

            if(currentId == null)
            {
                warnings.Add($"{rowNumber}行目：会話IDがないので飛ばしました");
                continue;
            }

            var line = new DialogueData.Line
            {
                speakerName = Cell(row, colName).Trim(),
                text = Cell(row, colText).Replace("\r\n", "\n"),
            };

            string charaName = Cell(row, colChara).Trim();
            if(charaName.Length > 0)
            {
                if(characters.TryGetValue(charaName, out CharacterProfile chara)) { line.character = chara; }
                else { warnings.Add($"{rowNumber}行目：キャラ「{charaName}」の CharacterProfile が見つかりません（立ち絵なしにしました）"); }
            }

            string exprName = Cell(row, colExpr).Trim();
            if(exprName.Length > 0)
            {
                if(TryParseExpression(exprName, out Expression expr)) { line.expression = expr; }
                else { warnings.Add($"{rowNumber}行目：表情「{exprName}」がありません（Normal にしました）"); }
            }

            dialogues[currentId].Add(line);
        }

        // 書き込み
        string folder = Path.GetDirectoryName(csvPath).Replace('\\', '/') + "/" + Path.GetFileNameWithoutExtension(csvPath);
        if(!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(csvPath).Replace('\\', '/'), Path.GetFileNameWithoutExtension(csvPath));
        }

        int created = 0, updated = 0;
        foreach(string id in order)
        {
            string assetPath = $"{folder}/{id}.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);

            if(data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                data.SetLinesForEditor(dialogues[id].ToArray());
                AssetDatabase.CreateAsset(data, assetPath);
                created++;
            }
            else
            {
                Undo.RecordObject(data, "Import Dialogue CSV");
                data.SetLinesForEditor(dialogues[id].ToArray());
                EditorUtility.SetDirty(data);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();

        string summary = $"{csvPath}\n新規 {created} 件、上書き {updated} 件\n保存先：{folder}";
        if(warnings.Count > 0)
        {
            foreach(string w in warnings) { Debug.LogWarning("[会話CSV] " + w); }
            summary += $"\n\n注意 {warnings.Count} 件（Console を見てください）：\n" + string.Join("\n", warnings.GetRange(0, Mathf.Min(5, warnings.Count)));
        }

        Debug.Log("[会話CSV] 取り込み完了: " + summary.Replace("\n", " "));
        if(showDialog)
        {
            EditorUtility.DisplayDialog("会話CSV", summary, "OK");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder));
        }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    /// <summary>
    /// UTF-8 で読めなければ Shift-JIS（Excel の「CSV（コンマ区切り）」）として読む
    /// </summary>
    private static string ReadText(string fullPath)
    {
        byte[] bytes = File.ReadAllBytes(fullPath);
        try
        {
            string text = new UTF8Encoding(false, true).GetString(bytes);
            return text.TrimStart('﻿');
        }
        catch(DecoderFallbackException)
        {
            return Encoding.GetEncoding(932).GetString(bytes);
        }
    }

    private static void WriteUtf8Bom(string assetPath, string text)
    {
        // BOM 付きにすると、Excel で開いても文字化けしない
        File.WriteAllText(Path.GetFullPath(assetPath), text, new UTF8Encoding(true));
    }

    /// <summary>
    /// CSV を行・セルに分ける（"" で囲んだセルの中のカンマ・改行・"" に対応）
    /// </summary>
    private static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;

        for(int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if(quoted)
            {
                if(c == '"')
                {
                    if(i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else { quoted = false; }
                }
                else { cell.Append(c); }
                continue;
            }

            switch(c)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    row.Add(cell.ToString()); cell.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(cell.ToString()); cell.Clear();
                    rows.Add(row); row = new List<string>();
                    break;
                default:
                    cell.Append(c);
                    break;
            }
        }

        if(cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private static string Escape(string value)
    {
        if(value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) { return value; }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string Cell(List<string> row, int index)
    {
        return index >= 0 && index < row.Count ? row[index] : "";
    }

    private static bool IsHeader(List<string> row)
    {
        return Find(row, "会話ID", "ID") >= 0 && Find(row, "本文", "Text", "セリフ") >= 0;
    }

    private static int Find(List<string> header, params string[] names)
    {
        for(int i = 0; i < header.Count; i++)
        {
            string h = header[i].Trim();
            foreach(string name in names)
            {
                if(string.Equals(h, name, StringComparison.OrdinalIgnoreCase)) { return i; }
            }
        }
        return -1;
    }

    private static bool TryParseExpression(string name, out Expression expression)
    {
        if(expressionNames.TryGetValue(name, out expression)) { return true; }
        return Enum.TryParse(name, true, out expression) && Enum.IsDefined(typeof(Expression), expression);
    }

    /// <summary>
    /// CharacterProfile を「ファイル名」と「表示名」の両方で引けるようにする
    /// </summary>
    private static Dictionary<string, CharacterProfile> LoadCharacters()
    {
        var map = new Dictionary<string, CharacterProfile>();
        foreach(string guid in AssetDatabase.FindAssets("t:CharacterProfile"))
        {
            CharacterProfile chara = AssetDatabase.LoadAssetAtPath<CharacterProfile>(AssetDatabase.GUIDToAssetPath(guid));
            if(chara == null) { continue; }

            if(!map.ContainsKey(chara.name)) { map.Add(chara.name, chara); }
            if(!string.IsNullOrEmpty(chara.DisplayName) && !map.ContainsKey(chara.DisplayName)) { map.Add(chara.DisplayName, chara); }
        }
        return map;
    }

    private static string ToAssetPath(string fullPath)
    {
        string data = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        string full = Path.GetFullPath(fullPath).Replace('\\', '/');
        if(!full.StartsWith(data, StringComparison.OrdinalIgnoreCase)) { return null; }
        return "Assets" + full.Substring(data.Length);
    }
}
