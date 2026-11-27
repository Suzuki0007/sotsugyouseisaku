/*********************************************************************/
// * \file   ScreenLensLayer.cs
// * \brief  画面に重ねる「レンズ」の層：画面の縁（4:3 の端のぼかし・上下のグラデーション）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面のレンズの層（演出の窓口 PresentationDirector が自動で付ける。シーンに置かなくてよい）
/// </summary>
///
/// ・色調のプリセットの「画面の縁」を、マップの映る範囲（中央 4:3）の上に UI の絵で重ねる
/// ・縁をぼかす：4:3 の端を左右の帯の色へなめらかに消す（角の丸みつき）
/// ・上下のグラデーション：上（下）から色をかぶせる（空を落とす・地面を明るくかすませる）
/// ・光漏れ：左右の帯・角の焼け・縦の筋を「スクリーン」（CT/UI Screen）で重ね、ゆっくり揺らし、ときどき一瞬強く光らせる
/// ・レンズフレア：場面の「太陽（LensSun）」の画面の位置に白い光、画面の中心を通る線の上に六角形・玉・虹の輪、全体のかぶり
/// ・並び順はマップの上・HUD の下（-10）。暗転の幕・1枚絵・会話・シネマの帯はこの上に出る
public class ScreenLensLayer : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary> 並び順：帯（-1000）より手前・シーンの HUD（0）より奥 </summary>
    public const int SortingOrder = -10;

    // 縁の絵（9 分割）：角の大きさ（画素）
    private const int EdgeCorner = 64;
    // 角の丸みは 0.05 刻みで絵を作り直す（色調の切り替え中に毎フレーム作り直さないため）
    private const float RoundStep = 0.05f;
    // グラデーションの絵の高さ（画素）
    private const int GradientSize = 64;

    //=====================================================================
    // 変数
    //=====================================================================

    private GameObject root;
    private RectTransform area;
    private Image edgeImage;
    private Image topImage;
    private Image bottomImage;

    private Texture2D edgeTexture;
    private Sprite edgeSprite;
    private Color32[] edgePixels;
    private float edgeBuiltRound = -1.0f;

    private Texture2D gradientTexture;
    private Sprite gradientSprite;

    // 光漏れ
    private const int SideW = 64, SideH = 128;
    private const int BurnSize = 128;
    private const int StreakW = 256, StreakH = 32;
    /// <summary> 角の焼けの大きさ（マップの範囲の高さに対する割合） </summary>
    private const float BurnScale = 0.6f;
    /// <summary> 一瞬光るときの強さ（何倍まで上がるか） </summary>
    private const float FlashBoost = 1.3f;

    private Material screenMaterial;
    private Image leftImage, rightImage, burnImage, streakImage;
    private Texture2D sideTexture, burnTexture, streakTexture;
    private Sprite sideSprite, burnSprite, streakSprite;
    private LeakCorner burnCornerApplied = (LeakCorner)(-1);
    private readonly System.Random random = new System.Random(11);
    private float nextFlash = -1.0f;
    private float flashStart = -10.0f;

    // レンズフレア（太陽）：ゴーストは太陽 → 画面の中心 → 反対側 の線の上に並ぶ
    private enum GhostShape { Hex, Dot, Ring }
    private static readonly float[] GhostT = { 0.55f, 0.25f, -0.15f, -0.45f, -0.62f, -0.95f };
    private static readonly float[] GhostSize = { 0.10f, 0.035f, 0.06f, 0.32f, 0.05f, 0.26f };
    private static readonly GhostShape[] GhostShapes = { GhostShape.Hex, GhostShape.Dot, GhostShape.Dot, GhostShape.Hex, GhostShape.Dot, GhostShape.Ring };
    private static readonly Color[] GhostColor =
    {
        new Color(1.0f, 0.75f, 0.45f, 0.18f),
        new Color(1.0f, 0.6f, 0.3f, 0.5f),
        new Color(0.9f, 0.5f, 1.0f, 0.3f),
        new Color(0.55f, 0.45f, 1.0f, 0.14f),
        new Color(1.0f, 0.55f, 0.35f, 0.35f),
        new Color(1.0f, 1.0f, 1.0f, 0.35f),
    };
    private const int GlowTex = 128, HexTex = 64, DotTex = 32, RingTex = 128;

    private Image flareVeil, flareGlow;
    private readonly Image[] ghosts = new Image[6];
    private Texture2D glowTexture, hexTexture, dotTexture, ringTexture;
    private Sprite glowSprite, hexSprite, dotSprite, ringSprite;

    /// <summary> レンズフレアを出しているか（確かめ用） </summary>
    public bool FlareShown { get; private set; }

    private Camera cam;
    private Rect lastPixelRect;
    private MoodValues values = MoodValues.Neutral;

    /// <summary> 今の光漏れの「一瞬光る」の強さ（0〜1。確かめ用） </summary>
    public float FlashAmount { get; private set; }

    /// <summary> 確かめ用：すぐに一瞬光らせる </summary>
    public void FlashNow()
    {
        flashStart = Time.unscaledTime;
    }

    /// <summary> 縁の絵を出しているか（確かめ用） </summary>
    public bool Visible => root != null && root.activeSelf;

    //=====================================================================
    // 公開
    //=====================================================================

    /// <summary> マップを写すカメラ（シーンが変わったら窓口が入れ直す） </summary>
    public void SetCamera(Camera camera)
    {
        cam = camera;
        lastPixelRect = new Rect(-1, -1, -1, -1);
        Apply(values);
    }

    /// <summary> 色調の値を反映する（窓口の ApplyMood から） </summary>
    public void Apply(MoodValues v)
    {
        values = v;
        bool show = !v.IsLensNeutral || WantsFlare();
        if(!show)
        {
            if(root != null && root.activeSelf) { root.SetActive(false); }
            FlareShown = false;
            return;
        }
        if(root == null) { Build(); }
        if(!root.activeSelf) { root.SetActive(true); }

        FitToCamera(true);

        // 縁
        bool edge = v.edgeFade >= 0.001f;
        edgeImage.enabled = edge;
        if(edge)
        {
            float round = Mathf.Round(Mathf.Clamp01(v.edgeRound) / RoundStep) * RoundStep;
            if(Mathf.Abs(round - edgeBuiltRound) > 0.001f) { BuildEdge(round); }
            Color bar = BarColor();
            bar.a = 1.0f;
            edgeImage.color = bar;
            UpdateEdgeWidth();
        }

        // 上下のグラデーション
        SetGradient(topImage, v.topShade, v.topShadeColor, v.topShadeHeight);
        SetGradient(bottomImage, v.bottomShade, v.bottomShadeColor, v.bottomShadeHeight);

        // 光漏れ
        LayoutLeak();
        AnimateLeak();

        // レンズフレア
        UpdateFlare();
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        if(root == null || !root.activeSelf) { return; }
        FitToCamera(false);
        if(values.leak >= 0.001f) { AnimateLeak(); }
        if(flareGlow != null && (flareGlow.enabled || WantsFlare())) { UpdateFlare(); }
    }

    //=====================================================================
    // レンズフレア（太陽）
    //=====================================================================

    private bool WantsFlare()
    {
        return cam != null && values.flare > 0.001f && LensSun.Current != null;
    }

    /// <summary> 太陽の画面の位置からフレアを並べる（毎フレーム。確保なし） </summary>
    private void UpdateFlare()
    {
        if(flareGlow == null) { return; }
        LensSun sun = WantsFlare() ? LensSun.Current : null;
        float k = sun != null ? sun.Strength * values.flare : 0.0f;

        // 太陽がマップの映る範囲のどこか（0〜1。外は 0 未満・1 より上）
        Vector3 vp = sun != null ? cam.WorldToViewportPoint(sun.transform.position) : Vector3.zero;
        float dx = Mathf.Max(0.0f, Mathf.Abs(vp.x - 0.5f) - 0.5f);
        float dy = Mathf.Max(0.0f, Mathf.Abs(vp.y - 0.5f) - 0.5f);
        float outside = Mathf.Max(dx, dy);
        float vis = sun == null ? 0.0f : (outside <= 0.0f ? 1.0f : (sun.Reach <= 0.001f ? 0.0f : 1.0f - Mathf.Clamp01(outside / sun.Reach)));
        vis = vis * vis * (3.0f - 2.0f * vis);
        float a = Mathf.Clamp01(k) * vis;
        bool on = a > 0.002f;
        FlareShown = on;

        flareGlow.enabled = on;
        flareVeil.enabled = on && sun.Veil > 0.001f;
        bool ghostsOn = on && sun.GhostStrength > 0.001f && !PresentationEase.IsLowQuality;
        for(int i = 0; i < ghosts.Length; i++) { ghosts[i].enabled = ghostsOn; }
        if(!on) { return; }

        float w = Mathf.Max(1.0f, lastPixelRect.width), h = Mathf.Max(1.0f, lastPixelRect.height);
        var local = new Vector2((vp.x - 0.5f) * w, (vp.y - 0.5f) * h);
        float t = Time.unscaledTime;
        float shimmer = 1.0f + 0.06f * (Mathf.PerlinNoise(t * 1.7f, 2.2f) * 2.0f - 1.0f);

        // 太陽のまわりの白い光
        float glow = sun.GlowSize * h * shimmer;
        RectTransform grt = flareGlow.rectTransform;
        grt.anchoredPosition = local;
        grt.sizeDelta = new Vector2(glow, glow);
        Color gc = sun.Color;
        gc.a = Mathf.Clamp01(a * (k > 1.0f ? k : 1.0f));
        flareGlow.color = gc;

        // 全体のかぶり：太陽が画面の真ん中に近いほど強い
        if(flareVeil.enabled)
        {
            float center = Mathf.Clamp01(new Vector2(vp.x - 0.5f, vp.y - 0.5f).magnitude / 0.75f);
            Color vc = sun.VeilColor;
            vc.a = Mathf.Clamp01(sun.Veil * a * (1.0f - 0.5f * center));
            flareVeil.color = vc;
        }

        // ゴースト
        if(ghostsOn)
        {
            float g = a * sun.GhostStrength;
            for(int i = 0; i < ghosts.Length; i++)
            {
                RectTransform rt = ghosts[i].rectTransform;
                rt.anchoredPosition = local * GhostT[i];
                float s = GhostSize[i] * h;
                rt.sizeDelta = new Vector2(s, s);
                Color c = GhostColor[i];
                c.a = Mathf.Clamp01(c.a * g);
                ghosts[i].color = c;
            }
        }
    }

    /// <summary> レンズフレアの絵を作る（実行中に 1 回） </summary>
    private void BuildFlare()
    {
        glowTexture = NewTexture("フレア・太陽の光（実行中）", GlowTex, GlowTex, (u, v) =>
        {
            float x = u * 2.0f - 1.0f, y = v * 2.0f - 1.0f;
            float r = Mathf.Sqrt(x * x + y * y);
            float core = Mathf.Pow(Mathf.Clamp01(1.0f - r * 3.0f), 1.5f);
            float halo = Mathf.Pow(Mathf.Clamp01(1.0f - r), 2.5f) * 0.7f;
            float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(y, x) * 3.0f)), 40.0f) * Mathf.Clamp01(1.0f - r) * 0.3f;
            return Mathf.Clamp01(core + halo + rays);
        });
        glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, GlowTex, GlowTex), new Vector2(0.5f, 0.5f), 100.0f);

        hexTexture = NewTexture("フレア・六角形（実行中）", HexTex, HexTex, (u, v) =>
        {
            float x = Mathf.Abs(u * 2.0f - 1.0f), y = Mathf.Abs(v * 2.0f - 1.0f);
            float hd = Mathf.Max(x * 0.866f + y * 0.5f, y);
            float body = Mathf.Clamp01((0.95f - hd) / 0.22f);
            float rim = 0.6f + 0.4f * Mathf.Clamp01((hd - 0.5f) / 0.4f);
            return body * rim;
        });
        hexSprite = Sprite.Create(hexTexture, new Rect(0, 0, HexTex, HexTex), new Vector2(0.5f, 0.5f), 100.0f);

        dotTexture = NewTexture("フレア・玉（実行中）", DotTex, DotTex, (u, v) =>
        {
            float x = u * 2.0f - 1.0f, y = v * 2.0f - 1.0f;
            return Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Sqrt(x * x + y * y)), 1.5f);
        });
        dotSprite = Sprite.Create(dotTexture, new Rect(0, 0, DotTex, DotTex), new Vector2(0.5f, 0.5f), 100.0f);

        // 虹の輪：外が赤・内が紫（色つきの絵。不透明さは輪の形）
        ringTexture = new Texture2D(RingTex, RingTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "フレア・虹の輪（実行中）" };
        var rp = new Color32[RingTex * RingTex];
        for(int y = 0; y < RingTex; y++)
        {
            for(int x = 0; x < RingTex; x++)
            {
                float fx = (x + 0.5f) / RingTex * 2.0f - 1.0f, fy = (y + 0.5f) / RingTex * 2.0f - 1.0f;
                float r = Mathf.Sqrt(fx * fx + fy * fy);
                float band = Mathf.InverseLerp(0.68f, 0.96f, r);
                float alpha = r < 0.68f || r > 0.96f ? 0.0f : Mathf.Sin(band * Mathf.PI);
                Color c = Color.HSVToRGB(Mathf.Lerp(0.78f, 0.0f, band), 0.85f, 1.0f);
                rp[y * RingTex + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(alpha * 255.0f));
            }
        }
        ringTexture.SetPixels32(rp);
        ringTexture.Apply(false, true);
        ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, RingTex, RingTex), new Vector2(0.5f, 0.5f), 100.0f);

        // かぶり（範囲いっぱいの白。色で決める）→ ゴースト → 太陽の光 の順に重ねる
        flareVeil = NewImage("フレア・かぶり", null);
        Stretch(flareVeil.rectTransform);
        for(int i = 0; i < ghosts.Length; i++)
        {
            Sprite s = GhostShapes[i] == GhostShape.Hex ? hexSprite : (GhostShapes[i] == GhostShape.Ring ? ringSprite : dotSprite);
            ghosts[i] = NewImage("フレア・ゴースト " + i, s);
            SetCentered(ghosts[i].rectTransform);
        }
        flareGlow = NewImage("フレア・太陽の光", glowSprite);
        SetCentered(flareGlow.rectTransform);

        if(screenMaterial != null)
        {
            flareVeil.material = screenMaterial;
            flareGlow.material = screenMaterial;
            for(int i = 0; i < ghosts.Length; i++) { ghosts[i].material = screenMaterial; }
        }
        flareVeil.enabled = false;
        flareGlow.enabled = false;
        for(int i = 0; i < ghosts.Length; i++) { ghosts[i].enabled = false; }
    }

    private static void SetCentered(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    //=====================================================================
    // 光漏れ
    //=====================================================================

    /// <summary> 光漏れの大きさ・置き場所（値か範囲が変わったとき） </summary>
    private void LayoutLeak()
    {
        if(leftImage == null) { return; }
        bool on = values.leak >= 0.001f;
        float w = Mathf.Max(1.0f, lastPixelRect.width), h = Mathf.Max(1.0f, lastPixelRect.height);
        float bandWidth = Mathf.Clamp(values.leakWidth, 0.05f, 0.6f) * w;

        leftImage.enabled = on && values.leakLeft >= 0.001f;
        rightImage.enabled = on && values.leakRight >= 0.001f;
        burnImage.enabled = on && values.leakBurn >= 0.001f;
        streakImage.enabled = on && values.leakStreaks >= 0.001f && !PresentationEase.IsLowQuality;

        leftImage.rectTransform.sizeDelta = new Vector2(bandWidth, 0.0f);
        rightImage.rectTransform.sizeDelta = new Vector2(bandWidth, 0.0f);
        float burn = BurnScale * h;
        burnImage.rectTransform.sizeDelta = new Vector2(burn, burn);

        if(values.leakBurnCorner != burnCornerApplied)
        {
            burnCornerApplied = values.leakBurnCorner;
            bool right = burnCornerApplied == LeakCorner.BottomRight || burnCornerApplied == LeakCorner.TopRight;
            bool top = burnCornerApplied == LeakCorner.TopLeft || burnCornerApplied == LeakCorner.TopRight;
            RectTransform rt = burnImage.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(right ? 1.0f : 0.0f, top ? 1.0f : 0.0f);
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            // 絵の左下が焼けた角。右・上の角は反転して使う
            rt.localScale = new Vector3(right ? -1.0f : 1.0f, top ? -1.0f : 1.0f, 1.0f);
        }
    }

    /// <summary> 光漏れの揺れ・一瞬光る（毎フレーム。確保なし） </summary>
    private void AnimateLeak()
    {
        if(leftImage == null || values.leak < 0.001f) { FlashAmount = 0.0f; return; }
        float t = Time.unscaledTime;
        float sway = Mathf.Clamp01(values.leakSway);

        // 一瞬光る：1 分に leakFlash 回くらい（間はばらつかせる）
        if(values.leakFlash > 0.01f)
        {
            float interval = 60.0f / values.leakFlash;
            if(nextFlash < 0.0f || nextFlash > t + interval * 1.6f) { nextFlash = t + interval * (0.3f + (float)random.NextDouble()); }
            if(t >= nextFlash)
            {
                flashStart = t;
                nextFlash = t + interval * (0.5f + (float)random.NextDouble());
            }
        }
        float dt = t - flashStart;
        float flash = dt < 0.0f || dt > 1.5f ? 0.0f : (dt < 0.06f ? dt / 0.06f : Mathf.Exp(-(dt - 0.06f) * 4.0f));
        FlashAmount = flash;
        float boost = 1.0f + FlashBoost * flash;

        float k = Mathf.Clamp01(values.leak);
        float l = 1.0f + sway * 0.45f * (Mathf.PerlinNoise(t * 0.22f, 1.3f) * 2.0f - 1.0f);
        float r = 1.0f + sway * 0.45f * (Mathf.PerlinNoise(t * 0.19f, 7.1f) * 2.0f - 1.0f);
        float b = 1.0f + sway * 0.35f * (Mathf.PerlinNoise(t * 0.15f, 4.7f) * 2.0f - 1.0f);
        float s = 1.0f + sway * 0.6f * (Mathf.PerlinNoise(t * 0.5f, 9.9f) * 2.0f - 1.0f);

        SetLeakColor(leftImage, values.leakLeftColor, k * values.leakLeft * l * boost);
        SetLeakColor(rightImage, values.leakRightColor, k * values.leakRight * r * boost);
        SetLeakColor(burnImage, values.leakBurnColor, k * values.leakBurn * b * (1.0f + 0.6f * FlashBoost * flash));
        SetLeakColor(streakImage, Color.Lerp(values.leakRightColor, Color.white, 0.45f), k * values.leakStreaks * s);

        // 帯はゆっくり横にずれる（光源が動いているように）
        float bandWidth = leftImage.rectTransform.sizeDelta.x;
        float drift = sway * 0.12f * bandWidth;
        leftImage.rectTransform.anchoredPosition = new Vector2(drift * (Mathf.PerlinNoise(t * 0.08f, 3.3f) * 2.0f - 1.0f), 0.0f);
        rightImage.rectTransform.anchoredPosition = new Vector2(drift * (Mathf.PerlinNoise(t * 0.07f, 5.5f) * 2.0f - 1.0f), 0.0f);
        streakImage.rectTransform.anchoredPosition = new Vector2(lastPixelRect.width * 0.03f * sway * (Mathf.PerlinNoise(t * 0.05f, 8.1f) * 2.0f - 1.0f), 0.0f);
    }

    private static void SetLeakColor(Image image, Color color, float alpha)
    {
        if(!image.enabled) { return; }
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    /// <summary> 光漏れの絵を作る（実行中に 1 回） </summary>
    private void BuildLeak()
    {
        screenMaterial = Resources.Load<Material>("Presentation/Materials/Mat_UI_Screen");

        // 横の帯：左の端（x=0）が濃く、内側へ消える。縦にむらがある
        sideTexture = NewTexture("光漏れ・横の帯（実行中）", SideW, SideH, (u, v) =>
        {
            float fall = Mathf.Pow(1.0f - u, 1.8f);
            float blotch = 0.45f + 0.55f * Mathf.PerlinNoise(v * 2.2f, 0.37f);
            float ends = 0.55f + 0.45f * Mathf.Sin(v * Mathf.PI);
            return fall * blotch * ends;
        });
        sideSprite = Sprite.Create(sideTexture, new Rect(0, 0, SideW, SideH), new Vector2(0.0f, 0.5f), 100.0f);

        // 角の焼け：左下の角（0,0）が白く、丸く広がって消える
        burnTexture = NewTexture("光漏れ・角の焼け（実行中）", BurnSize, BurnSize, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float glow = Mathf.Pow(Mathf.Clamp01(1.0f - d), 1.4f);
            float core = Mathf.Pow(Mathf.Clamp01(1.0f - d * 2.2f), 1.5f);
            float wobble = 0.85f + 0.15f * Mathf.PerlinNoise(u * 3.0f + 2.1f, v * 3.0f);
            return Mathf.Clamp01((glow * 0.8f + core * 0.6f) * wobble);
        });
        burnSprite = Sprite.Create(burnTexture, new Rect(0, 0, BurnSize, BurnSize), Vector2.zero, 100.0f);

        // 縦の筋：細い縦線が何本か。上ほど濃い
        var streakX = new float[7];
        var streakW = new float[7];
        var streakA = new float[7];
        var rnd = new System.Random(5);
        for(int i = 0; i < streakX.Length; i++)
        {
            // 筋は端寄り（中央は空ける）
            float x = (float)rnd.NextDouble() * 0.35f;
            streakX[i] = i % 2 == 0 ? x : 1.0f - x;
            streakW[i] = 0.006f + (float)rnd.NextDouble() * 0.03f;
            streakA[i] = 0.4f + (float)rnd.NextDouble() * 0.6f;
        }
        streakTexture = NewTexture("光漏れ・縦の筋（実行中）", StreakW, StreakH, (u, v) =>
        {
            float a = 0.0f;
            for(int i = 0; i < streakX.Length; i++)
            {
                float d = (u - streakX[i]) / streakW[i];
                a += Mathf.Exp(-d * d) * streakA[i];
            }
            return Mathf.Clamp01(a) * (0.2f + 0.8f * Mathf.Pow(v, 1.3f));
        });
        streakSprite = Sprite.Create(streakTexture, new Rect(0, 0, StreakW, StreakH), new Vector2(0.5f, 0.5f), 100.0f);

        streakImage = NewImage("光漏れ・縦の筋", streakSprite);
        Stretch(streakImage.rectTransform);
        leftImage = NewImage("光漏れ・左の帯", sideSprite);
        SetSide(leftImage.rectTransform, false);
        rightImage = NewImage("光漏れ・右の帯", sideSprite);
        SetSide(rightImage.rectTransform, true);
        burnImage = NewImage("光漏れ・角の焼け", burnSprite);
        burnCornerApplied = (LeakCorner)(-1);

        foreach(Image image in new[] { streakImage, leftImage, rightImage, burnImage })
        {
            if(screenMaterial != null) { image.material = screenMaterial; }
            image.enabled = false;
        }
    }

    private delegate float AlphaAt(float u, float v);

    private static Texture2D NewTexture(string textureName, int w, int h, AlphaAt alpha)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = textureName };
        var px = new Color32[w * h];
        for(int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            for(int x = 0; x < w; x++)
            {
                float a = Mathf.Clamp01(alpha((x + 0.5f) / w, v));
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255.0f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary> 左（右）の端に縦いっぱいで付ける。右は左右を反転して使う </summary>
    private static void SetSide(RectTransform rt, bool right)
    {
        float x = right ? 1.0f : 0.0f;
        rt.anchorMin = new Vector2(x, 0.0f);
        rt.anchorMax = new Vector2(x, 1.0f);
        rt.pivot = new Vector2(0.0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = new Vector3(right ? -1.0f : 1.0f, 1.0f, 1.0f);
    }

    /// <summary> マップの映る範囲（カメラの pixelRect）に合わせる。変わったときだけ </summary>
    private void FitToCamera(bool force)
    {
        Rect r = cam != null ? cam.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        if(!force && r == lastPixelRect) { return; }
        lastPixelRect = r;

        float w = Mathf.Max(1.0f, Screen.width), h = Mathf.Max(1.0f, Screen.height);
        area.anchorMin = new Vector2(r.xMin / w, r.yMin / h);
        area.anchorMax = new Vector2(r.xMax / w, r.yMax / h);
        area.offsetMin = Vector2.zero;
        area.offsetMax = Vector2.zero;

        // 高さが変わると、縁の幅とグラデーションの高さ（画素）も変わる
        if(edgeImage != null && edgeImage.enabled) { UpdateEdgeWidth(); }
        SetGradient(topImage, values.topShade, values.topShadeColor, values.topShadeHeight);
        SetGradient(bottomImage, values.bottomShade, values.bottomShadeColor, values.bottomShadeHeight);
        LayoutLeak();
    }

    private void UpdateEdgeWidth()
    {
        // 9 分割の角の大きさ（画面の単位）＝ 角の画素 / 倍率。幅を画面の高さの割合で決める
        float width = Mathf.Max(0.5f, values.edgeFade * lastPixelRect.height);
        edgeImage.pixelsPerUnitMultiplier = EdgeCorner / width;
    }

    private void SetGradient(Image image, float strength, Color color, float height)
    {
        if(image == null) { return; }
        bool on = strength >= 0.001f;
        image.enabled = on;
        if(!on) { return; }
        color.a = Mathf.Clamp01(strength);
        image.color = color;
        RectTransform rt = image.rectTransform;
        rt.sizeDelta = new Vector2(0.0f, Mathf.Clamp01(height) * lastPixelRect.height);
    }

    private Color BarColor()
    {
        if(cam != null && cam.TryGetComponent(out MapCameraAspect aspect)) { return aspect.BarColor; }
        return Color.black;
    }

    //=====================================================================
    // 作る
    //=====================================================================

    private void Build()
    {
        root = new GameObject("画面のレンズ（縁・光）");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        // CanvasScaler は付けない（画面の画素の単位で縁の幅を決める）

        var areaGo = new GameObject("マップの範囲", typeof(RectTransform));
        areaGo.transform.SetParent(root.transform, false);
        area = (RectTransform)areaGo.transform;
        // 揺れてずれた光漏れが、範囲の外（左右の帯）にはみ出さないように切る
        areaGo.AddComponent<RectMask2D>();

        // グラデーション：上が不透明 → 下が透明の 1×64 の絵（下は上下を反転して使う）
        gradientTexture = new Texture2D(1, GradientSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "上下のグラデーション（実行中）" };
        var gp = new Color32[GradientSize];
        for(int y = 0; y < GradientSize; y++)
        {
            // y=0 が下（透明）、y=上 が不透明。なめらかに（smoothstep）
            float t = (y + 0.5f) / GradientSize;
            float a = t * t * (3.0f - 2.0f * t);
            gp[y] = new Color32(255, 255, 255, (byte)(a * 255.0f));
        }
        gradientTexture.SetPixels32(gp);
        gradientTexture.Apply(false, true);
        gradientSprite = Sprite.Create(gradientTexture, new Rect(0, 0, 1, GradientSize), new Vector2(0.5f, 0.5f), 100.0f);

        topImage = NewImage("上のグラデーション", gradientSprite);
        SetStretchEdge(topImage.rectTransform, true);
        bottomImage = NewImage("下のグラデーション", gradientSprite);
        SetStretchEdge(bottomImage.rectTransform, false);
        bottomImage.rectTransform.localScale = new Vector3(1.0f, -1.0f, 1.0f);

        // 光漏れ（グラデーションの上・縁の下：縁の外へはみ出した光は縁で消える）
        BuildLeak();

        // レンズフレア（光漏れの上・縁の下）
        BuildFlare();

        // 縁（9 分割。中は透明）
        int size = EdgeCorner * 2 + 1;
        edgeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "画面の縁（実行中）" };
        edgePixels = new Color32[size * size];
        edgeImage = NewImage("縁のぼかし", null);
        edgeImage.type = Image.Type.Sliced;
        edgeImage.fillCenter = false;
        RectTransform ert = edgeImage.rectTransform;
        ert.anchorMin = Vector2.zero;
        ert.anchorMax = Vector2.one;
        ert.offsetMin = Vector2.zero;
        ert.offsetMax = Vector2.zero;
        edgeBuiltRound = -1.0f;
    }

    /// <summary> 角の丸み round で縁の絵を作る（中＝透明、外の端＝不透明） </summary>
    private void BuildEdge(float round)
    {
        edgeBuiltRound = round;
        int size = EdgeCorner * 2 + 1;
        // 角の形：u^p + v^p（p が大きいほど四角、2 で円）
        float p = Mathf.Lerp(12.0f, 2.0f, round);
        for(int y = 0; y < size; y++)
        {
            float v = EdgeT(y, size);
            for(int x = 0; x < size; x++)
            {
                float u = EdgeT(x, size);
                float d = (u <= 0.0f && v <= 0.0f) ? 0.0f : Mathf.Pow(Mathf.Pow(u, p) + Mathf.Pow(v, p), 1.0f / p);
                d = Mathf.Clamp01(d);
                float a = d * d * (3.0f - 2.0f * d);
                edgePixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255.0f));
            }
        }
        edgeTexture.SetPixels32(edgePixels);
        edgeTexture.Apply(false);

        if(edgeSprite != null) { Destroy(edgeSprite); }
        var border = new Vector4(EdgeCorner, EdgeCorner, EdgeCorner, EdgeCorner);
        edgeSprite = Sprite.Create(edgeTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, border);
        edgeSprite.name = "画面の縁（実行中）";
        edgeImage.sprite = edgeSprite;
    }

    /// <summary> 画素の位置 → 端からの近さ（0＝ぼかしの内側の始まり、1＝外の端） </summary>
    private static float EdgeT(int i, int size)
    {
        int c = EdgeCorner;
        if(i < c) { return (c - i - 0.5f) / c; }
        if(i > c) { return (i - c - 0.5f) / c; }
        return 0.0f;
    }

    private Image NewImage(string childName, Sprite sprite)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(area, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    /// <summary> 上（下）の端に横いっぱいで付ける。高さは sizeDelta.y で決める </summary>
    private static void SetStretchEdge(RectTransform rt, bool top)
    {
        float y = top ? 1.0f : 0.0f;
        rt.anchorMin = new Vector2(0.0f, y);
        rt.anchorMax = new Vector2(1.0f, y);
        // 下は上下を反転（scale.y = -1）して使うので、どちらも上の端を支点にする（反転すると上へ伸びる）
        rt.pivot = new Vector2(0.5f, 1.0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private void OnDestroy()
    {
        if(edgeSprite != null) { Destroy(edgeSprite); }
        if(edgeTexture != null) { Destroy(edgeTexture); }
        if(gradientSprite != null) { Destroy(gradientSprite); }
        if(gradientTexture != null) { Destroy(gradientTexture); }
        if(sideSprite != null) { Destroy(sideSprite); }
        if(sideTexture != null) { Destroy(sideTexture); }
        if(burnSprite != null) { Destroy(burnSprite); }
        if(burnTexture != null) { Destroy(burnTexture); }
        if(streakSprite != null) { Destroy(streakSprite); }
        if(streakTexture != null) { Destroy(streakTexture); }
        if(glowSprite != null) { Destroy(glowSprite); }
        if(glowTexture != null) { Destroy(glowTexture); }
        if(hexSprite != null) { Destroy(hexSprite); }
        if(hexTexture != null) { Destroy(hexTexture); }
        if(dotSprite != null) { Destroy(dotSprite); }
        if(dotTexture != null) { Destroy(dotTexture); }
        if(ringSprite != null) { Destroy(ringSprite); }
        if(ringTexture != null) { Destroy(ringTexture); }
    }
}
