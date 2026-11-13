/*********************************************************************/
// * \file   Save.cs
// * \brief  セーブクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/


using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Unity.Collections;

public static class  Save
{
    // Jsonに登録するためのクラス
    [Serializable]
    private class  Entry
    {
        public string key;
        public string value;
    }

    // Jsonのデータを格納するためのクラス
    [Serializable]
    private class  FileData
    {
        public List<Entry> list = new List<Entry>(); 
    }

    // 別スレッドに渡す書き込みの内容
    private class  WriteJob
    {
        public string path;
        public string json;
    }

    // 確定済みのデータ
    private static readonly Dictionary<string, string> data = new Dictionary<string, string>();

    // 仮置きのデータ
    private static readonly Dictionary<string, string> pending = new Dictionary<string, string>();

    // 書き込みが複数しないようにするためのオブジェクト
    private static readonly object fileLock = new object();

    // 前回の書き込み以降に変更があったか
    private static bool dirty = false;

    //=====================================================================
    // セーブスロット
    //=====================================================================

    /// <summary>
    /// 手動セーブのスロットの数
    /// </summary>
    public const int SlotCount = 3;

    /// <summary>
    /// オートセーブ枠の番号（ファイルは save_auto.json）
    /// ・自動の書き込み（Write / WriteAsync）は、いつもオートセーブ枠に書く
    /// ・手動の枠（1〜SlotCount）に書くのは、セーブポイントの手動セーブ（SaveToSlot）だけ
    /// </summary>
    public const int AutoSlot = -1;

    /// <summary>
    /// 最後に読み込んだ・手動セーブした枠（1〜SlotCount、オートセーブ枠なら AutoSlot）。0 ならプレイしていない
    /// （画面の「プレイ中」の表示に使う。書き込み先ではない）
    /// </summary>
    public static int CurrentSlot { get; private set; }

    /// <summary>
    /// プレイ中か（false の間は、仮置き・確定はできるが、ファイルには書き込まない＝エディタでステージから直接再生したときなど）
    /// </summary>
    public static bool HasSlot => sessionActive;

    private static bool sessionActive;

    /// <summary>
    /// 確定する直前に呼ばれる（プレイ時間や保存日時など、毎回記録したいものを仮置きするため）
    /// </summary>
    public static event Action BeforeCommit;

    // 自動の書き込み先（オートセーブ枠）。メインスレッドでしか取得できないので、別スレッドから呼ばないようにする
    private static string FilePath
    {
        get { return SlotPath(AutoSlot); }
    }

    /// <summary>
    /// スロットのファイルのパス（AutoSlot ならオートセーブ枠）
    /// </summary>
    // Application.persistentDataPathは、Unityが提供する、アプリケーションのデータを保存するためのディレクトリのパスです。
    // Path.Combineは、複数のパスを結合するための関数です。
    public static string SlotPath(int slot)
    {
        if(slot == AutoSlot) { return Path.Combine(Application.persistentDataPath, "save_auto.json"); }
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    // スロット導入前のファイル（初回にスロット1へ引き継ぐ）
    private static string LegacyPath
    {
        get { return Path.Combine(Application.persistentDataPath, "save.json"); }
    }

    // 再生開始時に中身を空にする
    // エディタでドメインリロードを無効にしていると、static の中身が前回の再生から残るため
    // （ビルドしたゲームは起動のたびに空から始まるので、動作は変わらない）
    // SubsystemRegistration は他のどの初期化よりも先に呼ばれる
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        data.Clear();
        pending.Clear();
        dirty = false;
        CurrentSlot = 0;
        sessionActive = false;
        BeforeCommit = null;
    }

    /// <summary>
    /// スロットにデータがあるか
    /// </summary>
    public static bool SlotExists(int slot)
    {
        return File.Exists(SlotPath(slot));
    }

    /// <summary>
    /// スロット（手動の枠かオートセーブ枠）を読み込んでプレイを始める（つづきから・ロード）
    /// 以後の自動の書き込みはオートセーブ枠に行く（読み込んだ手動の枠は、手動セーブするまで変わらない）
    /// </summary>
    public static void LoadSlot(int slot)
    {
        CurrentSlot = slot;
        sessionActive = true;
        Read();
    }

    /// <summary>
    /// 空の状態でプレイを始める（はじめから）。次の自動の書き込みで、オートセーブ枠が上書きされる
    /// </summary>
    public static void StartNewGame()
    {
        CurrentSlot = AutoSlot;
        sessionActive = true;
        Discard();
        dirty = true;
    }

    /// <summary>
    /// 以前の呼び方（スロットを選んで、はじめから）。今はオートセーブ枠で始める（slot は使わない）
    /// </summary>
    public static void StartNewSlot(int slot)
    {
        StartNewGame();
    }

    /// <summary>
    /// 今の内容（仮置きも確定して）を、指定した手動の枠にすぐ書き込む（セーブポイントの手動セーブ）
    /// オートセーブ枠は変えない
    /// </summary>
    public static void SaveToSlot(int slot)
    {
        Commit();
        sessionActive = true;
        CurrentSlot = slot;
        WriteFile(SlotPath(slot), BuildJson());
    }

    /// <summary>
    /// プレイをやめる（タイトルに戻ったときなど）。読み込んだデータも消す（ファイルには書かない）
    /// </summary>
    public static void CloseSlot()
    {
        CurrentSlot = 0;
        sessionActive = false;
        Discard();
        dirty = false;
    }

    /// <summary>
    /// スロットのファイルを削除する
    /// </summary>
    public static void DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if(File.Exists(path)) { File.Delete(path); }
    }

    /// <summary>
    /// スロット導入前の save.json があれば、スロット1に引き継ぐ（スロット1が空のときだけ）
    /// </summary>
    public static void MigrateLegacy()
    {
        try
        {
            if(File.Exists(LegacyPath) && !SlotExists(1))
            {
                File.Move(LegacyPath, SlotPath(1));
                Debug.Log("[セーブ] 以前のセーブデータ（save.json）をスロット1に引き継ぎました");
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning("[セーブ] 以前のセーブデータを引き継げませんでした: " + e.Message);
        }
    }

    /// <summary>
    /// スロットの中身を、読み込まずにのぞく（ロード画面の表示用）
    /// </summary>
    /// <returns> キーと値の一覧。ファイルがない・読めないときは null </returns>
    public static Dictionary<string, string> PeekSlot(int slot)
    {
        string path = SlotPath(slot);
        if(!File.Exists(path)) { return null; }

        try
        {
            FileData file = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
            if(file == null || file.list == null) { return null; }

            var result = new Dictionary<string, string>();
            foreach(Entry entry in file.list)
            {
                if(entry != null && !string.IsNullOrEmpty(entry.key)) { result[entry.key] = entry.value; }
            }
            return result;
        }
        catch(Exception)
        {
            return null;
        }
    }

    // 仮置きで保存する
    public static void SetPending<T>(string key, T value)
    {
        // Convert.ToStringはC#の「何かを文字列にする」ための関数
        // CultureInfo.InvariantCultureは、PCの地域設定で変わることなく、常に同じ形式で文字列にするための設定
        pending[key] = Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    // 名前と値で保存する
    public static void Set<T>(string key, T value)
    {
        string s = Convert.ToString(value, CultureInfo.InvariantCulture);// 値を文字列に変換する

        pending.Remove(key);// 仮置きのデータを削除する

        // 同じ値なら保存しない
        string old;
        // TryGetValueは、指定したキーが存在する場合に、その値を取得するための関数です。
        if(data.TryGetValue(key, out old) && old == s)
        {
            return;
        }

        data[key] = s;
        dirty = true;
    }

    // 名前で取得する
    public static T Get<T>(string key, T def = default(T))
    {
        string value;
        if((pending.TryGetValue(key, out value) || data.TryGetValue(key, out value)))
        {
            // Convert.ChangeTypeは、指定した型に変換するための関数です。
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        return def;
    }

    // 仮置きを確定する
    public static void Commit()
    {
        // プレイ時間など、確定のたびに記録したいものを仮置きしてもらう
        BeforeCommit?.Invoke();

        if(pending.Count == 0)
        {
            return;
        }

        // 仮置きのデータを確定済みのデータにコピーする
        // KeyValuePairは、キーと値のペアを表す構造体です。
        // forach(型　変数名　in 集まり)は、集まりの中の要素を順番に取り出すための構文です。
        foreach(KeyValuePair<string, string> p in pending)
        {
            data[p.Key] = p.Value;
        }
        pending.Clear();
        dirty = true;
    }

    //　仮置きのデータを破棄する
    public static void DiscardPending()
    {
        pending.Clear();
    }

    // データを仮置き、確定したデータを破棄する
    public static void Discard()
    {
        data.Clear();
        pending.Clear();
    }


    // データをクリアする
    public static void Clear()
    {
        Discard();
        dirty = true;
    }

    // データを1度だけ呼び出す
    public static void Read()
    {
        Discard();
        dirty = false;

        // スロットを使っていなければ読むものはない
        if(!HasSlot) { return; }

        string path = SlotPath(CurrentSlot);
        // ファイルが存在しない場合は何もしない
        if(!File.Exists(path))
        {
            return;
        }

        // JsonUtility.FromJsonは、JSON形式の文字列を指定した型のオブジェクトに変換するための関数です。
        // File.ReadAllTextは、指定したファイルの内容をすべて文字列として読み込むための関数です。
        // ファイルが壊れていても止まらないように、読めなかったら空から始める
        FileData file;
        try
        {
            file = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
        }
        catch(Exception e)
        {
            Debug.LogWarning("セーブデータを読み込めませんでした。最初から始めます: " + e.Message);
            return;
        }
        if(file == null || file.list == null) { return; }

        // データを復元する
        for(int i = 0; i < file.list.Count; i++)
        {
            if(file.list[i] == null || string.IsNullOrEmpty(file.list[i].key)) { continue; }
            data[file.list[i].key] = file.list[i].value;
        }
    }

    /// <summary>
    /// 確定済みのデータの件数（デバッグ表示用）
    /// </summary>
    public static int Count => data.Count;

    // データをオートセーブ枠に保存する（自動の書き込み。手動の枠は SaveToSlot）
    public static void Write()
    {
        if(!dirty)
        {
            return;
        }

        // スロットを使っていない（エディタでステージから直接再生したときなど）はファイルに書かない
        if(!HasSlot) { return; }

        dirty = false;

        WriteFile(FilePath, BuildJson());// 同期的に書き込む
    }

    // データをオートセーブ枠に非同期で保存する
    public static void WriteAsync()
    {
        if(!dirty)
        {
            return;
        }

        if(!HasSlot) { return; }

        dirty = false;

        WriteJob job = new WriteJob();// 別スレッドに渡す書き込みの内容を格納するためのクラス
        job.path = FilePath;// 書き込み先のパスを設定する
        job.json = BuildJson();// 書き込むJSON文字列を設定する

        // ThreadPool.QueueUserWorkItemは、別スレッドに依頼するための関数
        ThreadPool.QueueUserWorkItem(WriteWorker, job);// 別スレッドで書き込みを行う
    }

    // 別スレッドで呼ばれる
    private static void WriteWorker(object state)
    {
        WriteJob job = (WriteJob)state;// 別スレッドに渡す書き込みの内容を取得する
        WriteFile(job.path, job.json);// 同期的に書き込む
    }

    // ファイルに書き込む
    private static string BuildJson()
    {
        FileData file = new FileData();

        foreach (KeyValuePair<string, string> pair in data)
        {
            Entry entry = new Entry();
            entry.key = pair.Key;
            entry.value = pair.Value;
            file.list.Add(entry);
        }

        return JsonUtility.ToJson(file);
    }

    private static void WriteFile(string path, string json)
    {
        lock(fileLock)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);// 一時ファイルに書き込む

            // ファイルが存在する場合は削除する
            if(File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tmp, path);// 一時ファイルを本来のファイル名に変更する
        }
    }
}