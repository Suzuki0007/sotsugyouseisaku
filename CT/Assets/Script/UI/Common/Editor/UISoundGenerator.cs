/*********************************************************************/
// * \file   UISoundGenerator.cs
// * \brief  仮の UI 効果音（電子音）を作って、UI の効果音の設定に入れるエディタ拡張
// *
// * \author
/*********************************************************************/

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > UI効果音 > 仮の効果音を作成
/// ・Assets/SE/UI/ に仮の電子音（wav）と SeData を作る（すでにあるものは作り直さない）
/// ・Assets/Resources/UISounds.asset（UISoundSettings）を作り、空いている欄に入れる（設定済みの欄は変えない）
/// 本番の音にするときは、UISounds.asset の欄に別の SeData を入れるか、SeData の音声ファイルを差し替える
/// </summary>
public static class UISoundGenerator
{
    private const string Folder = "Assets/SE/UI";
    private const string SettingsPath = "Assets/Resources/" + UISoundSettings.ResourcePath + ".asset";
    private const int Rate = 44100;

    [MenuItem("Tools/UI効果音/仮の効果音を作成")]
    public static void Generate()
    {
        if(!AssetDatabase.IsValidFolder("Assets/SE")) { AssetDatabase.CreateFolder("Assets", "SE"); }
        if(!AssetDatabase.IsValidFolder(Folder))      { AssetDatabase.CreateFolder("Assets/SE", "UI"); }

        var settings = AssetDatabase.LoadAssetAtPath<UISoundSettings>(SettingsPath);
        if(settings == null)
        {
            settings = ScriptableObject.CreateInstance<UISoundSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        if(settings.select == null)    { settings.select    = Make("UI_Select",    Select(),    0.6f); }
        if(settings.decide == null)    { settings.decide    = Make("UI_Decide",    Decide(),    0.7f); }
        if(settings.cancel == null)    { settings.cancel    = Make("UI_Cancel",    Cancel(),    0.7f); }
        if(settings.menuOpen == null)  { settings.menuOpen  = Make("UI_MenuOpen",  Sweep(600f, 1100f, 0.11f), 0.6f); }
        if(settings.menuClose == null) { settings.menuClose = Make("UI_MenuClose", Sweep(1100f, 600f, 0.11f), 0.6f); }
        if(settings.save == null)      { settings.save      = Make("UI_Save",      SaveJingle(), 0.7f); }
        if(settings.cannot == null)    { settings.cannot    = Make("UI_Cannot",    Cannot(),    0.6f); }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[UI効果音] 仮の効果音を " + Folder + " に作り、" + SettingsPath + " に入れました");
    }

    //=====================================================================
    // 音を作る
    //=====================================================================

    // 短い「ピッ」
    private static float[] Select() => Tone(1320f, 0.045f, 0.35f);

    // 上がる2音「ピコッ」
    private static float[] Decide() => Concat(Tone(988f, 0.04f, 0.4f), Tone(1480f, 0.075f, 0.4f));

    // 下がる2音「ポコッ」
    private static float[] Cancel() => Concat(Tone(784f, 0.04f, 0.4f), Tone(523f, 0.08f, 0.4f));

    // 上がる4音（ド・ミ・ソ・ド）
    private static float[] SaveJingle() => Concat(Tone(523f, 0.07f, 0.35f), Tone(659f, 0.07f, 0.35f), Tone(784f, 0.07f, 0.35f), Tone(1047f, 0.25f, 0.4f));

    // 低い「ブブッ」
    private static float[] Cannot() => Concat(Square(196f, 0.06f, 0.25f), Silence(0.03f), Square(196f, 0.08f, 0.25f));

    private static float[] Tone(float freq, float seconds, float amp)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        for(int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
            s[i] = wave * amp * Envelope(i, n);
        }
        return s;
    }

    private static float[] Square(float freq, float seconds, float amp)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        for(int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0f ? 1f : -1f;
            s[i] = wave * amp * Envelope(i, n);
        }
        return s;
    }

    private static float[] Sweep(float from, float to, float seconds)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        double phase = 0;
        for(int i = 0; i < n; i++)
        {
            float freq = Mathf.Lerp(from, to, (float)i / n);
            phase += 2.0 * Math.PI * freq / Rate;
            s[i] = (float)Math.Sin(phase) * 0.35f * Envelope(i, n);
        }
        return s;
    }

    private static float[] Silence(float seconds) => new float[(int)(Rate * seconds)];

    // 立ち上がり 3ms、あとは指数で減衰（プツッという音を防ぐため最後は 0 にする）
    private static float Envelope(int i, int n)
    {
        int attack = Mathf.Max(1, Rate * 3 / 1000);
        float a = Mathf.Min(1f, (float)i / attack);
        float d = Mathf.Exp(-4f * i / n);
        float tail = Mathf.Min(1f, (float)(n - i) / attack);
        return a * d * tail;
    }

    private static float[] Concat(params float[][] parts)
    {
        int total = 0;
        foreach(var p in parts) { total += p.Length; }
        var result = new float[total];
        int offset = 0;
        foreach(var p in parts) { Array.Copy(p, 0, result, offset, p.Length); offset += p.Length; }
        return result;
    }

    //=====================================================================
    // 保存
    //=====================================================================

    /// <summary>
    /// wav と SeData を作る（すでにあればそれを返す）
    /// </summary>
    private static SeData Make(string name, float[] samples, float volume)
    {
        string wavPath = $"{Folder}/{name}.wav";
        string sePath  = $"{Folder}/Se_{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<SeData>(sePath);
        if(existing != null) { return existing; }

        if(!File.Exists(wavPath))
        {
            WriteWav(Path.GetFullPath(wavPath), samples);
            AssetDatabase.ImportAsset(wavPath);
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);

        var se = ScriptableObject.CreateInstance<SeData>();
        AssetDatabase.CreateAsset(se, sePath);
        var so = new SerializedObject(se);
        SerializedProperty clips = so.FindProperty("clips");
        clips.arraySize = 1;
        clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        so.FindProperty("volume").floatValue = volume;
        so.FindProperty("maxConcurrent").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
        return se;
    }

    private static void WriteWav(string fullPath, float[] samples)
    {
        using(var stream = new FileStream(fullPath, FileMode.Create))
        using(var w = new BinaryWriter(stream))
        {
            int dataSize = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataSize);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);            // fmt チャンクの大きさ
            w.Write((short)1);      // PCM
            w.Write((short)1);      // モノラル
            w.Write(Rate);
            w.Write(Rate * 2);      // 1秒あたりのバイト数
            w.Write((short)2);      // 1サンプルのバイト数
            w.Write((short)16);     // 16bit
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataSize);
            foreach(float s in samples)
            {
                w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
