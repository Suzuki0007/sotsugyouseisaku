/*********************************************************************/
// * \file   SeDataCreator.cs
// * \brief  音声ファイルから SeData を作成するエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Projectウィンドウで音声ファイル (wav / mp3 / ogg など) を選択して右クリック →
///   Create → Audio → SeData (音声ファイルごとに作成)
///   Create → Audio → SeData (選択した音声をまとめて1つに)
/// で SeData を作成する
/// 作成した SeData は音声ファイルと同じフォルダに保存される
/// </summary>
public static class SeDataCreator
{
    private const string MenuEach     = "Assets/Create/Audio/SeData (音声ファイルごとに作成)";
    private const string MenuCombined = "Assets/Create/Audio/SeData (選択した音声をまとめて1つに)";

    //=====================================================================
    // メニュー
    //=====================================================================

    /// <summary>
    /// 選択した音声ファイル1つにつき、SeData を1つ作る
    /// </summary>
    [MenuItem(MenuEach, false, 0)]
    private static void CreateEach()
    {
        List<AudioClip> clips = GetSelectedClips();
        var created = new List<Object>();

        foreach(AudioClip clip in clips)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(clip));
            created.Add(CreateSeData(folder, MakeName(clip.name), new[] { clip }));
        }

        Finish(created);
    }

    /// <summary>
    /// 選択した音声ファイルをまとめて1つの SeData にする (鳴らすたびにランダムで選ばれる)
    /// 足音や打撃音など、同じ種類で数パターンある音に使う
    /// </summary>
    [MenuItem(MenuCombined, false, 1)]
    private static void CreateCombined()
    {
        List<AudioClip> clips = GetSelectedClips();
        if(clips.Count == 0) { return; }

        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(clips[0]));
        Object created = CreateSeData(folder, MakeName(clips[0].name), clips.ToArray());

        Finish(new List<Object> { created });
    }

    // 音声ファイルが選ばれているときだけメニューを押せるようにする
    [MenuItem(MenuEach, true)]
    [MenuItem(MenuCombined, true)]
    private static bool ValidateSelection()
    {
        return Selection.objects.OfType<AudioClip>().Any();
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private static List<AudioClip> GetSelectedClips()
    {
        return Selection.objects
            .OfType<AudioClip>()
            .OrderBy(c => c.name)
            .ToList();
    }

    /// <summary>
    /// SeData のアセットを作成して保存する
    /// </summary>
    private static SeData CreateSeData(string folder, string assetName, AudioClip[] clips)
    {
        SeData data = ScriptableObject.CreateInstance<SeData>();

        // clips は private なので SerializedObject 経由で設定する
        var serialized = new SerializedObject(data);
        SerializedProperty clipsProperty = serialized.FindProperty("clips");
        clipsProperty.arraySize = clips.Length;
        for(int i = 0; i < clips.Length; i++)
        {
            clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 同名のファイルがあれば上書きせず、番号を付ける
        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(folder, assetName + ".asset").Replace('\\', '/'));

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    /// <summary>
    /// "SE_KeyOP" → "Se_KeyOP" のように、アセット名を決める
    /// </summary>
    private static string MakeName(string clipName)
    {
        string name = clipName;
        if(name.StartsWith("SE_", System.StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(3);
        }
        return "Se_" + name;
    }

    private static void Finish(List<Object> created)
    {
        if(created.Count == 0) { return; }

        AssetDatabase.SaveAssets();
        Selection.objects = created.ToArray();
        EditorGUIUtility.PingObject(created[0]);

        Debug.Log($"SeData を {created.Count} 個作成しました");
    }
}
