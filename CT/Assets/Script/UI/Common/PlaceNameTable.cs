/*********************************************************************/
// * \file   PlaceNameTable.cs
// * \brief  シーン名と、セーブデータ一覧に出す場所の名前の対応表
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン名と、セーブデータ一覧に出す場所の名前の対応表
/// </summary>
///
/// Assets/Resources/PlaceNames.asset に1つだけ置く（タイトル・メニュー・セーブポイントで共通）
/// 表にないシーンは、シーン名をそのまま表示する
[CreateAssetMenu(menuName = "UI/PlaceNameTable", fileName = "PlaceNames")]
public class PlaceNameTable : ScriptableObject
{
    public const string ResourcePath = "PlaceNames";

    [Serializable]
    public class Entry
    {
        [SceneName, Tooltip("シーン")]
        public string scene;

        [Tooltip("セーブデータ一覧に出す名前（例：地下水路）")]
        public string displayName;
    }

    [SerializeField]
    private List<Entry> entries = new List<Entry>();

    private static PlaceNameTable cached;

    /// <summary>
    /// シーンの表示名（表になければシーン名、シーン名が空なら「場所不明」）
    /// </summary>
    public static string GetName(string sceneName)
    {
        if(string.IsNullOrEmpty(sceneName)) { return "場所不明"; }

        if(cached == null) { cached = Resources.Load<PlaceNameTable>(ResourcePath); }
        if(cached != null)
        {
            foreach(Entry e in cached.entries)
            {
                if(e != null && e.scene == sceneName && !string.IsNullOrEmpty(e.displayName)) { return e.displayName; }
            }
        }
        return sceneName;
    }

    /// <summary>
    /// 1行追加する（エディタのひな形作成用）
    /// </summary>
    public void AddForEditor(string scene, string displayName)
    {
        entries.Add(new Entry { scene = scene, displayName = displayName });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        cached = null;
    }
}
