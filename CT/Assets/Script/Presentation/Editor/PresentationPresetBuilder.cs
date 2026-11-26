/*********************************************************************/
// * \file   PresentationPresetBuilder.cs
// * \brief  最初の演出のプリセット（色調 6・光 6・天候 7・回想 2）と、天候の粒の絵・マテリアル・回想中フラグを作る（エディタ用）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 最初の演出のプリセットを作る（Tools > 演出 > 最初のプリセットを作る）
/// </summary>
///
/// ・すでにあるプリセットは上書きしない（調整した値が消えないように）。「作り直す」は上書きする
/// ・置き場所：Assets/Resources/Presentation/（Mood・Light・Weather・Flashback・Materials）、粒の絵は Assets/Images/Presentation/Weather/
public static class PresentationPresetBuilder
{
    public const string Root = "Assets/Resources/Presentation";
    public const string TextureFolder = "Assets/Images/Presentation/Weather";
    public const string FlagPath = "Assets/Data/Flags/Flag_Flashback_Active.asset";

    [MenuItem("Tools/演出/最初のプリセットを作る（ある物はそのまま）", false, 1)]
    private static void BuildMenu()
    {
        Debug.Log(Build(false));
    }

    [MenuItem("Tools/演出/最初のプリセットを作り直す（値を最初に戻す）", false, 2)]
    private static void RebuildMenu()
    {
        if(!EditorUtility.DisplayDialog("演出のプリセット", "最初のプリセットの値を、すべて最初の値に戻します（調整した値は消えます）。", "戻す", "やめる")) { return; }
        Debug.Log(Build(true));
    }

    /// <summary> 作る（overwrite で値を最初に戻す）。結果を文で返す </summary>
    public static string Build(bool overwrite)
    {
        var log = new System.Text.StringBuilder();
        foreach(string f in new[] { Root, Root + "/Mood", Root + "/Light", Root + "/Weather", Root + "/Flashback", Root + "/Materials", TextureFolder })
        {
            EnsureFolder(f);
        }

        // 粒の絵
        Texture2D rain = MakeTexture("weather_rain", 8, 64, (x, y) => Soft(Mathf.Abs(x - 0.5f) * 2.0f) * Mathf.Sin(y * Mathf.PI));
        Texture2D dot = MakeTexture("weather_dot", 32, 32, (x, y) => Soft(Dist(x, y) * 2.0f));
        Texture2D glow = MakeTexture("weather_glow", 32, 32, (x, y) => { float d = Dist(x, y) * 2.0f; return Mathf.Clamp01(Soft(d) * 0.6f + Mathf.Clamp01(1.0f - d * 3.0f)); });
        Texture2D sand = MakeTexture("weather_sand", 8, 8, (x, y) => Soft(Dist(x, y) * 2.0f));
        Texture2D fog = MakeTexture("weather_fog", 128, 128, (x, y) => { float d = Dist(x, y) * 2.0f; return Mathf.Pow(Mathf.Clamp01(1.0f - d), 2.0f) * (0.75f + 0.25f * Mathf.PerlinNoise(x * 4.0f, y * 4.0f)); });
        Texture2D petal = MakeTexture("weather_petal", 32, 32, (x, y) => { float dx = (x - 0.5f) / 0.45f, dy = (y - 0.5f) / 0.25f; return Mathf.Clamp01((1.0f - (dx * dx + dy * dy)) * 4.0f); });
        Texture2D ring = MakeTexture("weather_ring", 32, 32, (x, y) => { float d = Dist(x, y) * 2.0f; return Mathf.Clamp01(1.0f - Mathf.Abs(d - 0.8f) * 8.0f); });

        // マテリアル
        MakeMaterial("Mat_Weather_Lit", "Universal Render Pipeline/2D/Sprite-Lit-Default", log);
        MakeMaterial("Mat_Weather_Unlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default", log);
        MakeMaterial("Mat_UI_Mood", "CT/UI Mood", log);

        // 回想中フラグ（セーブしない）
        GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(FlagPath);
        if(flag == null)
        {
            flag = ScriptableObject.CreateInstance<GameFlag>();
            var so = new SerializedObject(flag);
            SetIfExists(so, "id", "Flag_Flashback_Active");
            SetIfExists(so, "memo", "回想の間だけ ON（演出の窓口が立てる・消す）。回想中だけ出す物は「フラグでなめらかに出す」で");
            SetBoolIfExists(so, "saved", false);
            SetBoolIfExists(so, "save", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(flag, FlagPath);
            log.Append("フラグ Flag_Flashback_Active を作った\n");
        }

        // 色調
        var gray = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        Mood("Mood_通常", "既定。何も加工しない", MoodValues.Neutral, overwrite, log);
        MoodValues v;
        v = MoodValues.Neutral; v.saturation = -30; v.temperature = -25; v.colorFilter = new Color(0.92f, 0.95f, 1.0f); v.vignette = 0.3f;
        MoodPreset nightAlone = Mood("Mood_夜・一人", "砂浜に上陸（彩度 -30・寒色・周辺を暗く 弱〜中）", v, overwrite, log);
        v = MoodValues.Neutral; v.saturation = 5; v.temperature = 18; v.vignette = 0.18f;
        Mood("Mood_温かい日常", "第一層の日常・カイロスとの再会（彩度 +5・暖色・周辺を暗く 弱）", v, overwrite, log);
        v = MoodValues.Neutral; v.saturation = -70; v.contrast = -12; v.vignette = 0.35f;
        Mood("Mood_喪失", "カイロスの死のあと・ヴェルツィーと同行（彩度 -70・コントラスト少し下げ・周辺を暗く 中）", v, overwrite, log);
        v = MoodValues.Neutral; v.saturation = -20; v.temperature = 30; v.colorFilter = new Color(1.0f, 0.93f, 0.8f); v.shadows = new Color(0.55f, 0.48f, 0.42f); v.highlights = new Color(0.58f, 0.52f, 0.42f);
        v.vignette = 0.35f; v.grain = 0.15f; v.grainType = FilmGrainLookup.Thin2; v.bloom = 0.3f; v.bloomThreshold = 0.8f; v.uiAmount = 0.8f;
        MoodPreset memory = Mood("Mood_回想", "地下の記憶・中庭の回想・埋葬（セピア寄り・彩度 -20・粒子 弱（細かい）・周辺を暗く 中・にじみ 弱）", v, overwrite, log);
        v = MoodValues.Neutral; v.saturation = -10; v.shadows = new Color(0.45f, 0.5f, 0.6f); v.highlights = new Color(0.62f, 0.5f, 0.55f); v.splitBalance = 10; v.bloom = 0.3f; v.bloomThreshold = 0.8f;
        Mood("Mood_夜明け", "ヴェルツィーの墓・エピローグの手前（暗い所は青・明るい所は淡い桃色・彩度 -10・にじみ 弱）", v, overwrite, log);
        _ = gray; _ = nightAlone;

        // 光
        LightPreset("Light_昼", "白・強さ 1（今の昼の値）", Color.white, 1.0f, null, overwrite, log);
        LightPreset("Light_夜", "今の Stage1_0・Stage1_1 の夜の値（レシピ night_stage1_0_1）", new Color(0.5f, 0.6f, 0.9f, 1.0f), 0.45f, null, overwrite, log);
        LightPreset("Light_月明かり", "夜より少し青く暗い。グループ「月」の場面の光を強める", new Color(0.42f, 0.52f, 0.95f, 1.0f), 0.35f,
            new List<LightPreset.SceneLightSetting> { new LightPreset.SceneLightSetting { group = "月", on = true, intensity = 1.5f } }, overwrite, log);
        LightPreset("Light_夕日", "橙・強さ 0.8。グループ「夕日」の場面の光を点ける", new Color(1.0f, 0.68f, 0.42f, 1.0f), 0.8f,
            new List<LightPreset.SceneLightSetting> { new LightPreset.SceneLightSetting { group = "夕日", on = true, intensity = 1.0f } }, overwrite, log);
        LightPreset dawn = LightPreset("Light_夜明け", "淡い桃色〜水色・強さ 0.7", new Color(0.92f, 0.8f, 0.88f, 1.0f), 0.7f, null, overwrite, log);
        LightPreset("Light_塔の中の暗がり", "暗い灰青・強さ 0.3。グループ「ランタン」の場面の光を点ける", new Color(0.55f, 0.6f, 0.7f, 1.0f), 0.3f,
            new List<LightPreset.SceneLightSetting> { new LightPreset.SceneLightSetting { group = "ランタン", on = true, intensity = 1.0f } }, overwrite, log);
        _ = dawn;

        // 天候
        var rainColor = new Color(0.8f, 0.86f, 1.0f, 0.8f);
        Weather("Weather_雨・弱", "斜めに落ちる細い線（埋葬の記憶・中庭）", rain, 220, 700, 0.6f, 0.05f, 0.07f, rainColor, 16, -3, 0.15f, 0, false, 0.035f, false, null, 0, Color.white, WeatherPreset.Order.Front, true, overwrite, log);
        Weather("Weather_雨・強", "強い雨。地面に小さなはね", rain, 550, 1600, 0.45f, 0.06f, 0.09f, new Color(0.8f, 0.86f, 1.0f, 0.85f), 22, -5, 0.15f, 0, false, 0.04f, true, ring, 0.08f, new Color(0.55f, 0.62f, 0.72f), WeatherPreset.Order.Front, true, overwrite, log);
        Weather("Weather_雪", "ゆっくり揺れながら落ちる粒（埋葬の記憶・冬）", dot, 70, 700, 7.0f, 0.08f, 0.18f, new Color(1, 1, 1, 0.9f), 1.4f, -0.4f, 0.35f, 0.6f, false, 0, false, null, 0, Color.white, WeatherPreset.Order.Front, true, overwrite, log);
        Weather("Weather_砂ぼこり", "横に流れる細かい粒。少し画面がかすむ（砂浜・砂漠）", sand, 380, 1200, 1.6f, 0.05f, 0.12f, new Color(1.0f, 0.9f, 0.7f, 0.9f), 0.5f, 11, 0.35f, 0.8f, false, 0, false, null, 0.1f, new Color(0.9f, 0.78f, 0.58f), WeatherPreset.Order.Front, true, overwrite, log);
        Weather("Weather_霧", "ゆっくり流れる半透明の層（塔の中・夜明け）", fog, 3, 40, 9.0f, 6.0f, 10.0f, new Color(0.85f, 0.88f, 0.92f, 0.18f), 0, 0.5f, 0.4f, 0.3f, false, 0, false, null, 0.06f, new Color(0.8f, 0.84f, 0.9f), WeatherPreset.Order.Front, true, overwrite, log);
        WeatherPreset motes = Weather("Weather_光の粒", "ゆっくり漂う小さな光（回想・地下・夜明け）", glow, 14, 120, 6.0f, 0.06f, 0.16f, new Color(1.0f, 0.92f, 0.7f, 0.85f), -0.15f, 0.1f, 0.5f, 0.5f, false, 0, false, null, 0, Color.white, WeatherPreset.Order.Front, false, overwrite, log);
        Weather("Weather_花びら", "舞う花びら（回想・三人の頃・エピローグ）", petal, 12, 140, 7.0f, 0.16f, 0.26f, new Color(1.0f, 0.82f, 0.88f, 0.95f), 0.9f, 1.2f, 0.4f, 0.8f, true, 0, false, null, 0, Color.white, WeatherPreset.Order.Front, true, overwrite, log);

        // 回想
        Flashback("Flashback_回想", "基本の回想（色調＝回想・天候＝光の粒・色が抜けてから差す・コマ落ち 8）", memory, null, FlashbackPreset.WeatherUse.Use, motes, flag, FlashbackTransition.GrayThenTint, 8, overwrite, log);
        Flashback("Flashback_埋葬", "埋葬の記憶（色調＝回想・天候＝光の粒・色が抜けてから差す。光は部品で夜明けに）", memory, null, FlashbackPreset.WeatherUse.Use, motes, flag, FlashbackTransition.GrayThenTint, 8, overwrite, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log.Length > 0 ? log.ToString() : "演出のプリセット：すべてそろっている（作った物なし）";
    }

    //=====================================================================
    // 作る処理
    //=====================================================================

    private static MoodPreset Mood(string name, string memo, MoodValues v, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Mood/{name}.asset";
        MoodPreset p = LoadOrCreate<MoodPreset>(path, out bool created);
        if(created || overwrite) { p.EditorSet(memo, v); EditorUtility.SetDirty(p); log.Append(created ? "作った " : "戻した ").Append(name).Append('\n'); }
        return p;
    }

    private static LightPreset LightPreset(string name, string memo, Color c, float i, List<LightPreset.SceneLightSetting> lights, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Light/{name}.asset";
        LightPreset p = LoadOrCreate<LightPreset>(path, out bool created);
        if(created || overwrite) { p.EditorSet(memo, c, i, lights); EditorUtility.SetDirty(p); log.Append(created ? "作った " : "戻した ").Append(name).Append('\n'); }
        return p;
    }

    private static WeatherPreset Weather(string name, string memo, Texture2D tex, float rate, int max, float life, float sMin, float sMax, Color c,
                                         float fall, float wind, float variation, float sway, bool spin, float stretch, bool splash, Texture2D splashTex,
                                         float haze, Color hazeColor, WeatherPreset.Order order, bool lit, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Weather/{name}.asset";
        WeatherPreset p = LoadOrCreate<WeatherPreset>(path, out bool created);
        if(created || overwrite)
        {
            p.EditorSet(memo, tex, rate, max, life, sMin, sMax, c, fall, wind, variation, sway, spin, stretch, splash, splashTex, haze, hazeColor, order, lit);
            EditorUtility.SetDirty(p);
            log.Append(created ? "作った " : "戻した ").Append(name).Append('\n');
        }
        return p;
    }

    private static void Flashback(string name, string memo, MoodPreset m, LightPreset l, FlashbackPreset.WeatherUse wu, WeatherPreset w, GameFlag f,
                                  FlashbackTransition tr, float step, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Flashback/{name}.asset";
        FlashbackPreset p = LoadOrCreate<FlashbackPreset>(path, out bool created);
        if(created || overwrite) { p.EditorSet(memo, m, l, wu, w, f, tr, step); EditorUtility.SetDirty(p); log.Append(created ? "作った " : "戻した ").Append(name).Append('\n'); }
    }

    private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
    {
        T p = AssetDatabase.LoadAssetAtPath<T>(path);
        created = p == null;
        if(created)
        {
            p = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(p, path);
        }
        return p;
    }

    private static void MakeMaterial(string name, string shaderName, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Materials/{name}.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(path) != null) { return; }
        Shader shader = Shader.Find(shaderName);
        if(shader == null) { log.Append("!! シェーダーがない: ").Append(shaderName).Append('\n'); return; }
        var mat = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(mat, path);
        log.Append("作った ").Append(name).Append('\n');
    }

    private delegate float AlphaAt(float x, float y);

    /// <summary> 白い粒の絵（不透明さだけを決める）を PNG で作る。あれば読むだけ </summary>
    private static Texture2D MakeTexture(string name, int w, int h, AlphaAt alpha)
    {
        string path = $"{TextureFolder}/{name}.png";
        if(!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for(int y = 0; y < h; y++)
            {
                for(int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255.0f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if(imp != null)
            {
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static float Dist(float x, float y)
    {
        float dx = x - 0.5f, dy = y - 0.5f;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static float Soft(float d)
    {
        float t = Mathf.Clamp01(1.0f - d);
        return t * t * (3.0f - 2.0f * t);
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) { return; }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void SetIfExists(SerializedObject so, string prop, string value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if(p != null && p.propertyType == SerializedPropertyType.String) { p.stringValue = value; }
    }

    private static void SetBoolIfExists(SerializedObject so, string prop, bool value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if(p != null && p.propertyType == SerializedPropertyType.Boolean) { p.boolValue = value; }
    }
}
