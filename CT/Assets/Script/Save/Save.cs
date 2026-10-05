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

    // メインスレッドでしか取得できないので、別スレッドから呼ばないようにする
    private static string FilePath
    {
        // Application.persistentDataPathは、Unityが提供する、アプリケーションのデータを保存するためのディレクトリのパスです。
        // Path.Combineは、複数のパスを結合するための関数です。
        get { return Path.Combine(Application.persistentDataPath, "save.json"); }
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

        string path = FilePath;
        // ファイルが存在しない場合は何もしない
        if(!File.Exists(path))
        {
            return;
        }

        // JsonUtility.FromJsonは、JSON形式の文字列を指定した型のオブジェクトに変換するための関数です。
        // File.ReadAllTextは、指定したファイルの内容をすべて文字列として読み込むための関数です。
        FileData file = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
        // データを復元する
        for(int i = 0; i < file.list.Count; i++)
        {
            data[file.list[i].key] = file.list[i].value;
        }
    }

    // データを保存する
    public static void Write()
    {
        if(!dirty)
        {
            return;
        }

        dirty = false;

        WriteFile(FilePath, BuildJson());// 同期的に書き込む
    }

    // データを非同期で保存する
    public static void WriteAsync()
    {
        if(!dirty)
        {
            return;
        }

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