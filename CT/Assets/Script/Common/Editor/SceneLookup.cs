/*********************************************************************/
// * \file   SceneLookup.cs
// * \brief  シーン名・出現位置IDを調べるエディタ用の補助
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン名・出現位置IDを調べるエディタ用の補助クラス
/// ・Build Settings のシーン一覧
/// ・シーン名からシーンファイルのパス
/// ・シーンにある PlayerSpawnPoint の ID（開いているシーンは今の状態、開いていないシーンはファイルから読む）
/// </summary>
public static class SceneLookup
{
    // ファイルから読んだ出現位置IDのキャッシュ（パス → 更新日時と ID 一覧）
    private static readonly Dictionary<string, (long time, List<string> ids)> spawnCache =
        new Dictionary<string, (long, List<string>)>();

    private static string spawnPointScriptGuid;

    //=====================================================================
    // シーン
    //=====================================================================

    /// <summary>
    /// Build Settings に入っている（有効な）シーンの名前
    /// </summary>
    public static List<string> BuildSceneNames()
    {
        var names = new List<string>();
        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if(!scene.enabled) { continue; }
            names.Add(Path.GetFileNameWithoutExtension(scene.path));
        }
        return names;
    }

    /// <summary>
    /// シーンが Build Settings に入っていて有効か
    /// </summary>
    public static bool IsInBuild(string sceneName)
    {
        return BuildSceneNames().Contains(sceneName);
    }

    /// <summary>
    /// シーン名からシーンファイルのパスを探す（Build Settings を優先、なければプロジェクト全体）
    /// </summary>
    public static string FindScenePath(string sceneName)
    {
        if(string.IsNullOrEmpty(sceneName)) { return null; }

        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if(Path.GetFileNameWithoutExtension(scene.path) == sceneName) { return scene.path; }
        }

        foreach(string guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if(Path.GetFileNameWithoutExtension(path) == sceneName) { return path; }
        }

        return null;
    }

    //=====================================================================
    // 出現位置
    //=====================================================================

    /// <summary>
    /// シーンにある PlayerSpawnPoint の ID 一覧（見つからなければ空）
    /// </summary>
    public static List<string> SpawnPointIds(string scenePath)
    {
        if(string.IsNullOrEmpty(scenePath)) { return new List<string>(); }

        // 開いているシーンは、保存前の変更も含めて今の状態から取る
        Scene opened = SceneManager.GetSceneByPath(scenePath);
        if(opened.IsValid() && opened.isLoaded)
        {
            var ids = new List<string>();
            foreach(GameObject root in opened.GetRootGameObjects())
            {
                foreach(PlayerSpawnPoint point in root.GetComponentsInChildren<PlayerSpawnPoint>(true))
                {
                    if(!string.IsNullOrEmpty(point.SpawnPointId)) { ids.Add(point.SpawnPointId); }
                }
            }
            return ids;
        }

        return ReadSpawnPointIdsFromFile(scenePath);
    }

    /// <summary>
    /// シーンファイル（テキスト形式）から PlayerSpawnPoint の ID を読む
    /// ※ プレハブの中に置いた PlayerSpawnPoint は読めない
    /// </summary>
    private static List<string> ReadSpawnPointIdsFromFile(string scenePath)
    {
        string fullPath = Path.GetFullPath(scenePath);
        if(!File.Exists(fullPath)) { return new List<string>(); }

        long time = File.GetLastWriteTimeUtc(fullPath).Ticks;
        if(spawnCache.TryGetValue(scenePath, out var cached) && cached.time == time) { return cached.ids; }

        var result = new List<string>();
        string guid = SpawnPointScriptGuid();
        if(!string.IsNullOrEmpty(guid))
        {
            string[] lines = File.ReadAllLines(fullPath);
            for(int i = 0; i < lines.Length; i++)
            {
                if(!lines[i].Contains("m_Script:") || !lines[i].Contains(guid)) { continue; }

                // 同じブロック（次の "---" まで）の spawnPointId を探す
                for(int j = i + 1; j < lines.Length && !lines[j].StartsWith("---"); j++)
                {
                    Match match = Regex.Match(lines[j], @"^\s*spawnPointId:\s*(.*)$");
                    if(!match.Success) { continue; }

                    string id = match.Groups[1].Value.Trim().Trim('\'', '"');
                    if(id.Length > 0) { result.Add(id); }
                    break;
                }
            }
        }

        spawnCache[scenePath] = (time, result);
        return result;
    }

    private static string SpawnPointScriptGuid()
    {
        if(spawnPointScriptGuid != null) { return spawnPointScriptGuid; }

        foreach(string guid in AssetDatabase.FindAssets("PlayerSpawnPoint t:MonoScript"))
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if(script != null && script.GetClass() == typeof(PlayerSpawnPoint))
            {
                spawnPointScriptGuid = guid;
                return guid;
            }
        }

        spawnPointScriptGuid = "";
        return spawnPointScriptGuid;
    }

    /// <summary>
    /// シーンを開くとき・保存したときにキャッシュを捨てる
    /// </summary>
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorSceneManager.sceneSaved += _ => spawnCache.Clear();
    }
}
