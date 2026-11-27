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
        MakeMaterial("Mat_UI_Screen", "CT/UI Screen", log);

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

        // 青と黄色の哀愁（11-26 依頼書 CT_mood_blue_yellow。明るさ → 色の塗り替え。値は presets.json から）
        BuildBlueYellow(overwrite, log);
        BuildBlueYellowLights(overwrite, log);
        if(overwrite) { SoftenBlueYellowMoods(); TintBlueYellowMoods(); }

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

        // 青と黄色の色調に合わせる光：全体の光は白で暗め（白い物でも黄色の段に届かない）、場面の光（窓・ランプ・月・頂上・時計）は強く点ける
        var byLights = new List<LightPreset.SceneLightSetting>();
        foreach(string g in new[] { "窓", "ランプ", "月", "頂上", "時計" }) { byLights.Add(new LightPreset.SceneLightSetting { group = g, on = true, intensity = 1.0f }); }
        LightPreset by = LightPreset("Light_青い夜（塗り替え用）", "青と黄色の色調（P1〜P6）と組み合わせる。全体の光は白・暗め、窓・ランプ・月・頂上・時計の光を点ける、キャラの周りを少し明るく", Color.white, 0.25f, byLights, overwrite, log);
        if(by != null && (overwrite || by.PlayerGlow <= 0.0f)) { by.EditorSetPlayerGlow(0.35f, 1.6f); EditorUtility.SetDirty(by); }

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
    // 青と黄色の哀愁（P1〜P6）
    //=====================================================================

    private static void BuildBlueYellow(bool overwrite, System.Text.StringBuilder log)
    {
        MoodValues v;
        v = MoodValues.Neutral;
        RemapMood("Mood_P1_ファミレス型", "塔を止める・中庭の光（段階・青と平らな黄色・青緑の縁・加工なし）", v, RemapMode.Step, 0.0f,
            S(0.0f, "#111F2B"), S(0.18f, "#36689B"), S(0.55f, "#5E8FA6"), S(0.72f, "#F0E478"), overwrite, log);
        v = MoodValues.Neutral; v.vignette = 0.18f; v.grain = 0.06f; v.grainType = FilmGrainLookup.Thin2; v.bloom = 0.2f; v.bloomThreshold = 0.8f;
        RemapMood("Mood_P2_夏のうちに型", "第一層・日常、カイロスとの再会（なめらか・藍と杏色・物の色が少し残る）", v, RemapMode.Smooth, 0.15f,
            S(0.0f, "#0E1436"), S(0.35f, "#3C486C"), S(0.6f, "#6A6F96"), S(0.78f, "#C9B9A8"), S(1.0f, "#FCD8A8"), overwrite, log);
        v = MoodValues.Neutral; v.vignette = 0.25f; v.bloom = 0.15f; v.bloomThreshold = 0.85f;
        RemapMood("Mood_P3_月夜の砂浜", "上陸、時計、埋葬の終わり（段階・紺と淡い黄色）", v, RemapMode.Step, 0.0f,
            S(0.0f, "#0B1626"), S(0.2f, "#2A4A72"), S(0.55f, "#4C77A0"), S(0.85f, "#E6E2A8"), overwrite, log);
        v = MoodValues.Neutral; v.vignette = 0.35f; v.grain = 0.12f; v.grainType = FilmGrainLookup.Thin2; v.bloom = 0.2f; v.bloomThreshold = 0.8f;
        RemapMood("Mood_P4_回想ぬくもり", "地下の記憶、埋葬の始まり（なめらか・明るい所が広く温かい）", v, RemapMode.Smooth, 0.1f,
            S(0.0f, "#2B3550"), S(0.3f, "#5A6A8A"), S(0.55f, "#E8D9B0"), S(1.0f, "#FFF3C4"), overwrite, log);
        v = MoodValues.Neutral; v.vignette = 0.35f; v.grain = 0.06f; v.grainType = FilmGrainLookup.Thin2;
        RemapMood("Mood_P5_喪失", "カイロスの死のあと、中庭（なめらか・黄色が消える）", v, RemapMode.Smooth, 0.0f,
            S(0.0f, "#0D1622"), S(0.5f, "#2E4A6C"), S(1.0f, "#7E9AB2"), overwrite, log);
        v = MoodValues.Neutral; v.vignette = 0.18f; v.bloom = 0.4f; v.bloomThreshold = 0.8f;
        RemapMood("Mood_P6_夜明け", "ヴェルツィーの墓、エピローグ（なめらか・薄紫と桃色から黄色へ）", v, RemapMode.Smooth, 0.1f,
            S(0.0f, "#1C2340"), S(0.35f, "#4A5C8C"), S(0.6f, "#A69BC0"), S(0.8f, "#F2C9B0"), S(1.0f, "#F8EBA0"), overwrite, log);
    }

    //=====================================================================
    // 手触り・波紋（11-27 指示書 CT_presentation_ripple_subtitle_gesture）
    //=====================================================================

    [MenuItem("Tools/演出/手触りと波紋のプリセットを作る（ある物はそのまま）", false, 3)]
    private static void BuildGestureRippleMenu()
    {
        var log = new System.Text.StringBuilder();
        BuildGestureAndRipple(false, log);
        Debug.Log(log.Length > 0 ? log.ToString() : "手触りと波紋：すべてそろっている");
    }

    public static string BuildGestureAndRipple(bool overwrite, System.Text.StringBuilder log)
    {
        EnsureFolder(Root + "/Gesture");
        EnsureFolder(Root + "/Ripple");
        var flat = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        // 最後の 10% はとても重い
        var lastHeavy = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(0.85f, 0.8f), new Keyframe(0.9f, 0.18f), new Keyframe(1.0f, 0.12f));
        // 最初は重く、近づくほど軽い
        var heavyToLight = new AnimationCurve(new Keyframe(0.0f, 0.35f), new Keyframe(1.0f, 1.4f));
        Gesture("Gesture_カイロスと調律", "長押し・軽い。二人の光がゆっくり重なる", GesturePreset.Kind.Hold, 2.5f, flat, GesturePreset.Release.Return, 6.0f, 1, 0.0f, GesturePreset.Look.None, "", 0.02f, false, overwrite, log);
        Gesture("Gesture_手を伸ばす", "長押し・最初は重く、ノアに近づくほど軽い（中庭）", GesturePreset.Kind.Hold, 4.0f, heavyToLight, GesturePreset.Release.Return, 5.0f, 1, 0.0f, GesturePreset.Look.None, "", 0.05f, false, overwrite, log);
        Gesture("Gesture_墓を掘る", "繰り返し 5 回・1 回ごとに少し間がある（埋葬の記憶）", GesturePreset.Kind.Repeat, 1.0f, flat, GesturePreset.Release.Stop, 4.0f, 5, 0.8f, GesturePreset.Look.None, "", 0.08f, false, overwrite, log);
        Gesture("Gesture_二人で墓を掘る", "繰り返し 5 回（ヴェルツィーの墓。隣のノアも一緒に動く＝1 回ごとのイベントで）", GesturePreset.Kind.Repeat, 1.0f, flat, GesturePreset.Release.Stop, 4.0f, 5, 0.8f, GesturePreset.Look.None, "", 0.08f, false, overwrite, log);
        Gesture("Gesture_最後の一音", "長押し・押し始めるとまわりの動きと音が止まる", GesturePreset.Kind.Hold, 3.0f, flat, GesturePreset.Release.Stop, 4.0f, 1, 0.0f, GesturePreset.Look.None, "", 0.03f, true, overwrite, log);
        Gesture("Gesture_最後のひと巻き", "ゆっくり回す・最後の 10% はとても重い。離すと止まるだけ", GesturePreset.Kind.Rotate, 4.0f, lastHeavy, GesturePreset.Release.Stop, 4.0f, 1, 0.0f, GesturePreset.Look.Ring, "", 0.03f, false, overwrite, log);

        Ripple("Ripple_色が戻る", "直した場所から元の色が戻る（夏のうちに型・回想など、元の色を残す色調で）", RipplePreset.Look.RestoreColor, Color.white, 0.95f, 6.0f, 2.5f, 0.75f, 1.0f, new Color(0.9f, 0.95f, 1.0f), 0.08f, RipplePreset.After.Stay, 3.0f, overwrite, log);
        Ripple("Ripple_光が灯る", "直した場所から黄色い光が灯る（ファミレス型・月夜の砂浜など）", RipplePreset.Look.LightUp, new Color(1.0f, 0.86f, 0.55f), 1.0f, 5.0f, 2.0f, 0.75f, 1.3f, new Color(1.0f, 0.95f, 0.75f), 0.08f, RipplePreset.After.Stay, 3.0f, overwrite, log);
        Ripple("Ripple_小さな光", "手元から小さく広がって、ゆっくり消える（懐中時計・中庭で掴んだ瞬間）", RipplePreset.Look.LightUp, new Color(1.0f, 0.9f, 0.65f), 1.2f, 2.2f, 1.2f, 0.85f, 1.2f, new Color(1.0f, 0.97f, 0.85f), 0.1f, RipplePreset.After.FadeOut, 4.0f, overwrite, log);
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    private static void Gesture(string name, string memo, GesturePreset.Kind kind, float secs, AnimationCurve w, GesturePreset.Release r, float ret, int cnt, float interval,
                                GesturePreset.Look look, string prompt, float shake, bool freeze, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Gesture/{name}.asset";
        GesturePreset p = LoadOrCreate<GesturePreset>(path, out bool created);
        if(created || overwrite) { p.EditorSet(memo, kind, secs, w, r, ret, cnt, interval, look, prompt, shake, freeze); EditorUtility.SetDirty(p); log.Append(created ? "作った " : "戻した ").Append(name).Append('\n'); }
    }

    private static void Ripple(string name, string memo, RipplePreset.Look look, Color c, float i, float r, float secs, float ell, float edgeI, Color edgeC, float edgeW, RipplePreset.After af, float fade, bool overwrite, System.Text.StringBuilder log)
    {
        string path = $"{Root}/Ripple/{name}.asset";
        RipplePreset p = LoadOrCreate<RipplePreset>(path, out bool created);
        if(created || overwrite) { p.EditorSet(memo, look, c, i, r, secs, ell, edgeI, edgeC, edgeW, af, fade); EditorUtility.SetDirty(p); log.Append(created ? "作った " : "戻した ").Append(name).Append('\n'); }
    }

    /// <summary>
    /// 案ごとの光（11-26 ユーザーの指示「元の絵に光で色味と雰囲気を加える」）：全体の光を案の青に、場面の光（窓・ランプ・月・頂上・時計）を案の黄色にする
    /// </summary>
    public static void BuildBlueYellowLights(bool overwrite, System.Text.StringBuilder log)
    {
        CaseLight("Light_P1_ファミレス型", "ファミレス型の光：明るめの青い夜にレモン色の明かり", new Color(0.32f, 0.50f, 0.95f), 0.72f, "#F6E87A", 0.55f, 0.2f, overwrite, log);
        CaseLight("Light_P2_夏のうちに型", "夏のうちに型の光：藍色に沈む部屋に杏色の明かり", new Color(0.36f, 0.40f, 0.82f), 0.65f, "#FCD8A8", 0.55f, 0.2f, overwrite, log);
        CaseLight("Light_P3_月夜の砂浜", "月夜の砂浜の光：紺の夜に淡い黄色の月・明かり", new Color(0.30f, 0.45f, 0.86f), 0.58f, "#E6E2A8", 0.45f, 0.2f, overwrite, log);
        CaseLight("Light_P4_回想ぬくもり", "回想の光：やわらかい青に温かいクリーム色の明かり", new Color(0.55f, 0.62f, 0.90f), 0.82f, "#FFF0C4", 0.60f, 0.15f, overwrite, log);
        CaseLight("Light_P5_喪失", "喪失の光：黄色が消える。明かりも青白く弱い", new Color(0.38f, 0.52f, 0.74f), 0.58f, "#9FB8CC", 0.30f, 0.0f, overwrite, log);
        CaseLight("Light_P6_夜明け", "夜明けの光：薄紫の空に桃色〜黄色の明かり", new Color(0.55f, 0.52f, 0.86f), 0.78f, "#F8D0B0", 0.55f, 0.15f, overwrite, log);
    }

    private static void CaseLight(string name, string memo, Color global, float globalIntensity, string lightHex, float lightFactor, float glow, bool overwrite, System.Text.StringBuilder log)
    {
        ColorUtility.TryParseHtmlString(lightHex, out Color lc);
        var lights = new List<LightPreset.SceneLightSetting>();
        foreach(string g in new[] { "窓", "ランプ", "月", "頂上", "時計" })
        {
            lights.Add(new LightPreset.SceneLightSetting { group = g, on = true, intensity = lightFactor, changeColor = true, color = lc });
        }
        LightPreset p = LightPreset(name, memo, global, globalIntensity, lights, overwrite, log);
        if(p != null && (overwrite || p.PlayerGlow <= 0.0f)) { p.EditorSetPlayerGlow(glow, 1.6f); EditorUtility.SetDirty(p); }
    }

    /// <summary>
    /// P1〜P6 の色調に「暗い所を案の青・明るい所を案の黄色」へ寄せる色分けを入れる（11-26：暗い絵の場所では、青い光を掛けても青くならなかったため）
    /// </summary>
    public static string TintBlueYellowMoods()
    {
        var sb = new System.Text.StringBuilder();
        Tint("Mood_P1_ファミレス型",   new Color(0.20f, 0.36f, 0.85f), new Color(0.70f, 0.66f, 0.38f), 30f, new Color(0.88f, 0.93f, 1.0f), new Color(0.10f, 0.22f, 0.42f), sb);
        Tint("Mood_P2_夏のうちに型",   new Color(0.18f, 0.30f, 0.90f), new Color(0.66f, 0.56f, 0.40f), 35f, new Color(0.86f, 0.90f, 1.0f), new Color(0.10f, 0.12f, 0.38f), sb);
        Tint("Mood_P3_月夜の砂浜",     new Color(0.16f, 0.28f, 0.82f), new Color(0.66f, 0.64f, 0.45f), 35f, new Color(0.85f, 0.90f, 1.0f), new Color(0.06f, 0.14f, 0.32f), sb);
        Tint("Mood_P4_回想ぬくもり",   new Color(0.36f, 0.42f, 0.74f), new Color(0.72f, 0.64f, 0.46f), 20f, new Color(0.95f, 0.95f, 1.0f), new Color(0.18f, 0.22f, 0.36f), sb);
        Tint("Mood_P5_喪失",           new Color(0.20f, 0.32f, 0.72f), new Color(0.50f, 0.56f, 0.62f), 30f, new Color(0.86f, 0.90f, 1.0f), new Color(0.06f, 0.12f, 0.22f), sb);
        Tint("Mood_P6_夜明け",         new Color(0.32f, 0.30f, 0.76f), new Color(0.74f, 0.58f, 0.50f), 25f, new Color(0.92f, 0.90f, 1.0f), new Color(0.14f, 0.14f, 0.34f), sb);
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    private static void Tint(string name, Color shadows, Color highlights, float balance, Color filter, Color lift, System.Text.StringBuilder sb)
    {
        var p = AssetDatabase.LoadAssetAtPath<MoodPreset>($"{Root}/Mood/{name}.asset");
        if(p == null) { return; }
        var so = new SerializedObject(p);
        so.FindProperty("shadowsColor").colorValue = shadows;
        so.FindProperty("highlightsColor").colorValue = highlights;
        so.FindProperty("splitBalance").floatValue = balance;
        so.FindProperty("colorFilter").colorValue = filter;
        // 暗い所の色（黒を案の紺に沈める。11-26 ユーザーの指示「青の色合いで暗くなるように」）
        so.FindProperty("shadowLiftColor").colorValue = lift;
        so.FindProperty("shadowLift").floatValue = 0.45f;
        so.ApplyModifiedPropertiesWithoutUndo();
        sb.Append(name).Append(' ');
    }

    /// <summary> P1〜P6 の色調を「光が主役」に合わせて控えめにする（塗り替えの強さ 0.25・元の色を残す 0.5） </summary>
    public static string SoftenBlueYellowMoods()
    {
        var sb = new System.Text.StringBuilder();
        foreach(string n in new[] { "Mood_P1_ファミレス型", "Mood_P2_夏のうちに型", "Mood_P3_月夜の砂浜", "Mood_P4_回想ぬくもり", "Mood_P5_喪失", "Mood_P6_夜明け" })
        {
            var p = AssetDatabase.LoadAssetAtPath<MoodPreset>($"{Root}/Mood/{n}.asset");
            if(p == null) { continue; }
            var so = new SerializedObject(p);
            so.FindProperty("remapStrength").floatValue = 0.25f;
            so.FindProperty("keepOriginal").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            sb.Append(n).Append(' ');
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    private static RemapStop S(float from, string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return new RemapStop { from = from, color = c };
    }

    private static void RemapMood(string name, string memo, MoodValues v, RemapMode mode, float keep, params object[] rest)
    {
        var stops = new List<RemapStop>();
        bool overwrite = false;
        System.Text.StringBuilder log = null;
        foreach(object o in rest)
        {
            if(o is RemapStop s) { stops.Add(s); }
            else if(o is bool b) { overwrite = b; }
            else if(o is System.Text.StringBuilder sb) { log = sb; }
        }
        string path = $"{Root}/Mood/{name}.asset";
        MoodPreset p = LoadOrCreate<MoodPreset>(path, out bool created);
        if(created || overwrite)
        {
            p.EditorSet(memo, v);
            // 11-26 ユーザーの指示「元の絵に光で色味と雰囲気を加える」：光が主役なので、色調の塗り替えは控えめ（強さ 0.25・元の色を半分以上残す）
            p.EditorSetRemap(mode, stops, Mathf.Max(keep, 0.5f));
            var so = new SerializedObject(p);
            so.FindProperty("remapStrength").floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(p);
            log?.Append(created ? "作った " : "戻した ").Append(name).Append('\n');
        }
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
