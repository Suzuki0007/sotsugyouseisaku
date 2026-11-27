/*********************************************************************/
// * \file   PresentationValues.cs
// * \brief  演出の数値：名前つきの数（掘った回数・巻いた割合など）。手の動作が書き、波紋・物の動き・あとで作る時代の切り替えなどが読む
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出の数値（名前つきの数。手の動作の進み具合や回数を、ほかの演出に渡す）
/// </summary>
///
/// ・名前（例：「掘った回数」「巻いた割合」）ごとに 1 つの数を持つ。部品「演出の数値を変える」・部品「手の動作を待つ」が書く
/// ・部品「演出の数値で動かす」（物に付ける）や、ほかの部品が読む
/// ・「セーブする」にした名前は、セーブに残る（Value/&lt;名前&gt;）。ほかはシーンを移ると 0 に戻る
public static class PresentationValues
{
    private static readonly Dictionary<string, float> values = new Dictionary<string, float>();
    private static readonly HashSet<string> saved = new HashSet<string>();
    private const string SavePrefix = "Value/";

    /// <summary> どれかの数値が変わったとき（名前・新しい値） </summary>
    public static event Action<string, float> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        values.Clear();
        saved.Clear();
        Changed = null;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // シーンを移ったら、セーブしない数値は 0 に戻す
    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if(mode != UnityEngine.SceneManagement.LoadSceneMode.Single) { return; }
        var keep = new Dictionary<string, float>();
        foreach(var kv in values) { if(saved.Contains(kv.Key)) { keep[kv.Key] = kv.Value; } }
        values.Clear();
        foreach(var kv in keep) { values[kv.Key] = kv.Value; }
    }

    /// <summary> 今の値（なければセーブの値、それもなければ 0） </summary>
    public static float Get(string name)
    {
        if(string.IsNullOrEmpty(name)) { return 0.0f; }
        if(values.TryGetValue(name, out float v)) { return v; }
        return Save.Get(SavePrefix + name, 0.0f);
    }

    /// <summary> 値を入れる（save＝セーブにも残す） </summary>
    public static void Set(string name, float value, bool save = false)
    {
        if(string.IsNullOrEmpty(name)) { return; }
        if(save) { saved.Add(name); }
        if(values.TryGetValue(name, out float old) && Mathf.Approximately(old, value)) { return; }
        values[name] = value;
        if(saved.Contains(name)) { Save.SetPending(SavePrefix + name, value); }
        Changed?.Invoke(name, value);
    }

    /// <summary> 値を足す </summary>
    public static void Add(string name, float delta, bool save = false)
    {
        Set(name, Get(name) + delta, save);
    }

    /// <summary> 今ある数値の一覧（デバッグ用） </summary>
    public static IEnumerable<KeyValuePair<string, float>> All => values;
}
