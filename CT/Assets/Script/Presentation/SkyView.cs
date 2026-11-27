/*********************************************************************/
// * \file   SkyView.cs
// * \brief  空（塔の外・マップの外）を描く：見上げる（空・雲・星）／見下ろす（かすみの下の地面・下を流れる雲・下へ続く外壁）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 空（演出の窓口 PresentationDirector が自動で付ける。シーンに置かなくてよい）
/// </summary>
///
/// ・マップのカメラの一番奥（並び順 -32000〜）に、カメラについてくる空の絵を置く。マップの絵がない所にだけ見える
/// ・見え方はシーンごと（ScenePresentation の「外側の見え方」）：
///   見上げる＝屋外・地上。上から地平線への空のグラデーション・流れる雲・星
///   見下ろす＝塔の中（塔を輪切りにして斜め上から見ている）。外は「はるか下の地面」をかすみ越しに見る：
///     遠い地面ほど画面の上で薄く（空気遠近）、地面はゆっくり・下を流れる雲は少し速くずれる（遠いほど動きが小さい＝奥行き）、
///     星は見えない、マップの手前（下の端）には塔の外壁が下へ続いて、かすみに消える
/// ・層：シーンの既定（「外側の空」）＜ イベント（部品「空を変える」）。切り替えはなめらか
/// ・イベントの層はセーブする（Presentation/<シーン>/Sky）
/// ・空は光の影響を受けない（Unlit）。色調は受ける
public class SkyView : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary> 空の並び順（マップの何よりも奥） </summary>
    public const int SortingOrder = -32000;

    private const int GradientSize = 64;
    private const int CloudW = 256, CloudH = 128;
    private const float CloudPpu = 16.0f;
    private const int StarSize = 128;
    private const float StarPpu = 32.0f;
    private const int GroundSize = 256;
    private const float GroundPpu = 8.0f;
    private const int WallH = 64;

    // 奥行き：カメラが動いたとき、床（1）に比べてどれだけ画面上でずれるか（小さいほど遠い）
    private const float UpCloudParallax = 0.12f, UpStarParallax = 0.04f;
    private const float DownGroundParallax = 0.3f, DownCloudParallax = 0.6f;

    /// <summary> 見下ろすときの雲の大きさ（遠く下にあるので小さい） </summary>
    private const float DownCloudScale = 0.75f;

    /// <summary> 下へ続く外壁の高さ（マス） </summary>
    private const float WallDrop = 6.0f;

    /// <summary> 空の絵を置く奥行き（カメラから） </summary>
    private const float Depth = 50.0f;

    //=====================================================================
    // 変数
    //=====================================================================

    private SkyPreset basePreset;
    private bool eventSet;
    private SkyPreset eventPreset;
    private SkyViewMode mode = SkyViewMode.LookUp;
    private bool towerWall = true;

    private SkyValues current = SkyValues.None;
    private SkyValues from = SkyValues.None;
    private SkyValues to = SkyValues.None;
    private float time, duration;
    private bool blending;

    private Camera cam;
    private GameObject root;
    private SpriteRenderer gradientRenderer, cloudRenderer, starRenderer, groundRenderer, wallRenderer;
    private Texture2D gradientTexture, cloudTexture, starTexture, groundTexture, wallTexture;
    private Sprite gradientSprite, cloudSprite, starSprite, groundSprite, wallSprite;
    private Color32[] gradientPixels;
    private float cloudScroll;
    private float lastOrtho = -1.0f, lastAspect = -1.0f;
    private bool dirty = true;

    /// <summary> 今の一番上の空の名前（なければ ""） </summary>
    public string CurrentName => Top != null ? Top.name : "";

    /// <summary> 空が出ている割合（確かめ用） </summary>
    public float Presence => current.presence;

    /// <summary> 今の見え方 </summary>
    public SkyViewMode Mode => mode;

    public bool IsBlending => blending;

    private SkyPreset Top => eventSet ? eventPreset : basePreset;

    private bool Down => mode == SkyViewMode.LookDown;

    //=====================================================================
    // 層
    //=====================================================================

    public void SetBase(SkyPreset preset, float seconds)
    {
        basePreset = preset;
        StartBlend(seconds);
    }

    /// <summary> シーンの見え方（見上げる／見下ろす）と、下へ続く外壁を出すか </summary>
    public void SetMode(SkyViewMode viewMode, bool wall)
    {
        mode = viewMode;
        towerWall = wall;
        // 見え方が変わると、絵の重ね方・置く物が変わるので作り直す
        if(root != null) { Destroy(root); root = null; }
        dirty = true;
        Apply();
    }

    public void SetEvent(SkyPreset preset, float seconds)
    {
        eventSet = true;
        eventPreset = preset;
        StartBlend(seconds);
    }

    /// <summary> イベントで変えた空を外す（シーンの既定へ戻す） </summary>
    public void ClearEvent(float seconds)
    {
        eventSet = false;
        eventPreset = null;
        StartBlend(seconds);
    }

    /// <summary> セーブに書く文字（""＝イベントの層なし、"*"＝空なしを指定） </summary>
    public string EncodeEvent()
    {
        if(!eventSet) { return ""; }
        return eventPreset != null ? eventPreset.name : "*";
    }

    /// <summary> プリセットを Inspector で変えたとき（今使っている物なら、すぐ反映） </summary>
    public void NotifyChanged(SkyPreset preset)
    {
        if(preset != null && preset == Top) { StartBlend(0.0f); }
    }

    private void StartBlend(float seconds)
    {
        from = current;
        SkyPreset top = Top;
        to = top != null ? top.Values : SkyValues.None;
        time = 0.0f;
        duration = Mathf.Max(0.0f, seconds);
        blending = duration > 0.0001f;
        if(!blending) { current = to; }
        dirty = true;
        Apply();
    }

    //=====================================================================
    // シーン
    //=====================================================================

    /// <summary> シーンが変わったとき：層を空にして、新しいカメラに空の絵を付ける。イベントの層はセーブから戻す </summary>
    public void ResetForScene(Camera camera, string savedEvent)
    {
        basePreset = null;
        eventSet = false;
        eventPreset = null;
        mode = SkyViewMode.LookUp;
        towerWall = true;
        if(!string.IsNullOrEmpty(savedEvent))
        {
            eventSet = true;
            eventPreset = savedEvent == "*" ? null : PresentationDirector.Find<SkyPreset>(savedEvent);
        }
        current = SkyValues.None;
        blending = false;

        // 前のシーンの絵はカメラ・シーンと一緒に消えている（root はカメラの子、外壁はシーンの物）
        root = null;
        wallRenderer = null;
        cam = camera;
        lastOrtho = -1.0f;
        StartBlend(0.0f);
    }

    //=====================================================================
    // 毎フレーム（窓口の LateUpdate から）
    //=====================================================================

    public void Tick(float dt)
    {
        if(blending)
        {
            time += dt;
            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3.0f - 2.0f * t);
            current = SkyValues.Lerp(from, to, t);
            dirty = true;
            if(time >= duration) { current = to; blending = false; }
        }
        if(dirty) { Apply(); }
        Animate(dt);
    }

    /// <summary> 値を絵に入れる（値が変わったときだけ） </summary>
    private void Apply()
    {
        dirty = false;
        bool show = current.presence > 0.001f && cam != null;
        if(!show)
        {
            if(root != null && root.activeSelf) { root.SetActive(false); }
            if(wallRenderer != null) { wallRenderer.gameObject.SetActive(false); }
            return;
        }
        if(root == null) { Build(); }
        if(!root.activeSelf) { root.SetActive(true); }

        // グラデーション（64 画素だけ書き直す）
        float h = Mathf.Clamp(current.horizon, 0.0f, 0.95f);
        for(int y = 0; y < GradientSize; y++)
        {
            float v = (y + 0.5f) / GradientSize;
            Color c;
            float a;
            if(Down)
            {
                // 見下ろす：画面の上ほど遠い地面＝かすみが厚い（地平線の色・濃い）、下ほど真下＝地面が透けて見える
                c = Color.Lerp(Color.Lerp(current.horizonColor, current.topColor, 0.35f), current.horizonColor, v);
                a = Mathf.Lerp(1.0f - 0.95f * current.groundVisibility, 1.0f - 0.35f * current.groundVisibility, v * v);
            }
            else
            {
                float k = v <= h ? 0.0f : (v - h) / (1.0f - h);
                k = k * k * (3.0f - 2.0f * k);
                c = Color.Lerp(current.horizonColor, current.topColor, k);
                a = 1.0f;
            }
            gradientPixels[y] = new Color32((byte)(c.r * 255.0f), (byte)(c.g * 255.0f), (byte)(c.b * 255.0f), (byte)(a * 255.0f));
        }
        gradientTexture.SetPixels32(gradientPixels);
        gradientTexture.Apply(false);
        gradientRenderer.color = new Color(1.0f, 1.0f, 1.0f, current.presence);

        cloudRenderer.enabled = current.clouds > 0.001f;
        starRenderer.enabled = !Down && current.stars > 0.001f;
        if(groundRenderer != null)
        {
            groundRenderer.enabled = Down;
            Color g = current.groundColor;
            g.a = current.presence;
            groundRenderer.color = g;
        }
        if(wallRenderer != null)
        {
            wallRenderer.gameObject.SetActive(true);
            // 外壁は暗い石の色を、かすみの色へ少し寄せる（下へ行くほど絵の中でかすみに消える）
            Color w = Color.Lerp(new Color(0.16f, 0.16f, 0.19f, 1.0f), current.horizonColor, 0.15f);
            w.a = current.presence;
            wallRenderer.color = w;
        }
    }

    /// <summary> 大きさ合わせ・雲を流す・地面をずらす・星をまたたかせる（確保なし） </summary>
    private void Animate(float dt)
    {
        if(root == null || !root.activeSelf || cam == null) { return; }

        float ortho = cam.orthographicSize, aspect = cam.aspect;
        float hgt = ortho * 2.0f, wid = hgt * aspect;
        if(!Mathf.Approximately(ortho, lastOrtho) || !Mathf.Approximately(aspect, lastAspect))
        {
            lastOrtho = ortho;
            lastAspect = aspect;
            gradientRenderer.transform.localScale = new Vector3(wid * GradientSize, hgt, 1.0f);
            float cw = CloudW / CloudPpu, ch = CloudH / CloudPpu;
            // 見下ろすときの雲は遠く下にあるので小さく見える
            float cs = Down ? DownCloudScale : 1.0f;
            cloudRenderer.transform.localScale = new Vector3(cs, cs, 1.0f);
            cloudRenderer.size = new Vector2(wid / cs + cw * 2.0f, hgt / cs + ch * 2.0f);
            float sw = StarSize / StarPpu;
            starRenderer.size = new Vector2(wid + sw * 2.0f, hgt + sw * 2.0f);
            if(groundRenderer != null)
            {
                float gw = GroundSize / GroundPpu;
                groundRenderer.size = new Vector2(wid + gw * 2.0f, hgt + gw * 2.0f);
            }
        }

        Vector3 cp = cam.transform.position;

        if(cloudRenderer.enabled)
        {
            cloudScroll += current.cloudSpeed * dt;
            float k = Down ? DownCloudParallax : UpCloudParallax;
            float cs = Down ? DownCloudScale : 1.0f;
            float cw = CloudW / CloudPpu * cs, ch = CloudH / CloudPpu * cs;
            float x = -Mathf.Repeat(cloudScroll + cp.x * k, cw);
            float y = -Mathf.Repeat(cp.y * k, ch);
            cloudRenderer.transform.localPosition = new Vector3(x + cw * 0.5f, y + ch * 0.5f, Depth - 0.02f);
            Color c = current.cloudColor;
            // 見下ろすときの雲は、下にあるのでかすみ越しに少し薄い
            c.a = Mathf.Clamp01(current.clouds) * current.presence * (Down ? 0.75f : 1.0f);
            cloudRenderer.color = c;
        }

        if(groundRenderer != null && groundRenderer.enabled)
        {
            float gw = GroundSize / GroundPpu;
            float x = -Mathf.Repeat(cp.x * DownGroundParallax, gw);
            float y = -Mathf.Repeat(cp.y * DownGroundParallax, gw);
            groundRenderer.transform.localPosition = new Vector3(x + gw * 0.5f, y + gw * 0.5f, Depth + 0.02f);
        }

        if(starRenderer.enabled)
        {
            float sw = StarSize / StarPpu;
            float x = -Mathf.Repeat(cp.x * UpStarParallax, sw);
            float y = -Mathf.Repeat(cp.y * UpStarParallax, sw);
            starRenderer.transform.localPosition = new Vector3(x + sw * 0.5f, y + sw * 0.5f, Depth - 0.01f);
            float twinkle = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.3f);
            Color c = current.starColor;
            c.a = Mathf.Clamp01(current.stars) * current.presence * twinkle;
            starRenderer.color = c;
        }
    }

    //=====================================================================
    // 作る
    //=====================================================================

    private void Build()
    {
        root = new GameObject("空（演出・自動）");
        root.transform.SetParent(cam.transform, false);
        root.transform.localPosition = Vector3.zero;

        Material mat = Resources.Load<Material>("Presentation/Materials/Mat_Weather_Unlit");

        if(gradientTexture == null)
        {
            gradientTexture = new Texture2D(1, GradientSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "空のグラデーション（実行中）" };
            gradientPixels = new Color32[GradientSize];
            gradientSprite = Sprite.Create(gradientTexture, new Rect(0, 0, 1, GradientSize), new Vector2(0.5f, 0.5f), GradientSize);
        }
        if(starTexture == null) { BuildStars(); }
        if(cloudTexture == null) { BuildClouds(); }

        if(Down)
        {
            // 見下ろす：地面（一番奥）→ かすみ（グラデーション）→ 下を流れる雲
            if(groundTexture == null) { BuildGround(); }
            groundRenderer = NewRenderer("下に見える地面", groundSprite, mat, SortingOrder, SpriteDrawMode.Tiled);
            gradientRenderer = NewRenderer("かすみ", gradientSprite, mat, SortingOrder + 1, SpriteDrawMode.Simple);
            starRenderer = NewRenderer("星", starSprite, mat, SortingOrder + 2, SpriteDrawMode.Tiled);
            cloudRenderer = NewRenderer("下を流れる雲", cloudSprite, mat, SortingOrder + 3, SpriteDrawMode.Tiled);
            BuildWall(mat);
        }
        else
        {
            // 見上げる：空の色 → 星 → 雲
            groundRenderer = null;
            gradientRenderer = NewRenderer("空の色", gradientSprite, mat, SortingOrder, SpriteDrawMode.Simple);
            starRenderer = NewRenderer("星", starSprite, mat, SortingOrder + 1, SpriteDrawMode.Tiled);
            cloudRenderer = NewRenderer("雲", cloudSprite, mat, SortingOrder + 2, SpriteDrawMode.Tiled);
            if(wallRenderer != null) { Destroy(wallRenderer.gameObject); wallRenderer = null; }
        }
        gradientRenderer.transform.localPosition = new Vector3(0.0f, 0.0f, Depth);

        lastOrtho = -1.0f;
    }

    /// <summary>
    /// 下へ続く外壁：マップ（カメラの映る範囲）の下の端から、塔の外壁が下へ伸びてかすみに消える（斜め上から見た塔の手前の面）
    /// マップの物なので、カメラではなくワールドに置く（シーンと一緒に消える）
    /// </summary>
    private void BuildWall(Material mat)
    {
        if(wallRenderer != null) { Destroy(wallRenderer.gameObject); wallRenderer = null; }
        if(!towerWall || !MapArea.TryGetViewRect(out Rect rect)) { return; }

        if(wallTexture == null)
        {
            // 上が不透明 → 下へ透明（石の横の目地をうっすら）
            wallTexture = new Texture2D(4, WallH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "下へ続く外壁（実行中）" };
            var px = new Color32[4 * WallH];
            for(int y = 0; y < WallH; y++)
            {
                float v = (y + 0.5f) / WallH;            // 0＝下（消える）、1＝上（マップの端）
                float a = Mathf.Clamp01(v * 1.4f);
                a = a * a * (3.0f - 2.0f * a);
                float joint = (y % 8 == 0) ? 0.8f : 1.0f; // 石の段の目地
                byte s = (byte)(255.0f * joint);
                for(int x = 0; x < 4; x++) { px[y * 4 + x] = new Color32(s, s, s, (byte)(a * 255.0f)); }
            }
            wallTexture.SetPixels32(px);
            wallTexture.Apply(false, true);
            wallSprite = Sprite.Create(wallTexture, new Rect(0, 0, 4, WallH), new Vector2(0.5f, 1.0f), 4.0f);
        }

        var go = new GameObject("下へ続く外壁（空・自動）");
        go.transform.position = new Vector3(rect.center.x, rect.yMin, 0.0f);
        wallRenderer = go.AddComponent<SpriteRenderer>();
        wallRenderer.sprite = wallSprite;
        if(mat != null) { wallRenderer.sharedMaterial = mat; }
        wallRenderer.sortingOrder = SortingOrder + 10;
        // 絵は 1×(64/4) マス → 横はマップの幅、縦は WallDrop マス
        go.transform.localScale = new Vector3(rect.width, WallDrop / (WallH / 4.0f), 1.0f);
    }

    private SpriteRenderer NewRenderer(string objectName, Sprite sprite, Material mat, int order, SpriteDrawMode drawMode)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(root.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if(mat != null) { sr.sharedMaterial = mat; }
        sr.sortingOrder = order;
        sr.drawMode = drawMode;
        if(drawMode == SpriteDrawMode.Tiled) { sr.tileMode = SpriteTileMode.Continuous; }
        return sr;
    }

    private void BuildClouds()
    {
        cloudTexture = new Texture2D(CloudW, CloudH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "空の雲（実行中）" };
        var px = new Color32[CloudW * CloudH];
        for(int y = 0; y < CloudH; y++)
        {
            for(int x = 0; x < CloudW; x++)
            {
                float u = (float)x / CloudW, v = (float)y / CloudH;
                // 横長の雲：横 4・縦 2 の格子でくり返す値のノイズを重ねる
                float n = Fbm(u * 4.0f, v * 2.0f, 4, 2, 5, 5);
                float a = Mathf.Clamp01((n - 0.48f) / 0.22f);
                a = a * a * (3.0f - 2.0f * a);
                byte shade = (byte)(225 + 30 * Mathf.Clamp01((n - 0.5f) * 3.0f));
                px[y * CloudW + x] = new Color32(shade, shade, shade, (byte)(a * 255.0f));
            }
        }
        cloudTexture.SetPixels32(px);
        cloudTexture.Apply(false, true);
        cloudSprite = Sprite.Create(cloudTexture, new Rect(0, 0, CloudW, CloudH), new Vector2(0.5f, 0.5f), CloudPpu, 0, SpriteMeshType.FullRect);
    }

    /// <summary> 下に見える地面：大きなまだら（陸と海・畑と林のような明るさの違い）。色はプリセットの「地面の色」を掛ける </summary>
    private void BuildGround()
    {
        groundTexture = new Texture2D(GroundSize, GroundSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "下に見える地面（実行中）" };
        var px = new Color32[GroundSize * GroundSize];
        for(int y = 0; y < GroundSize; y++)
        {
            for(int x = 0; x < GroundSize; x++)
            {
                float u = (float)x / GroundSize, v = (float)y / GroundSize;
                float big = Fbm(u * 3.0f, v * 3.0f, 3, 3, 4, 11);
                float fine = Fbm(u * 12.0f, v * 12.0f, 12, 12, 2, 31);
                float land = Mathf.Clamp01((big - 0.47f) / 0.08f);
                float s = Mathf.Lerp(0.4f, 1.0f, land) * (0.75f + 0.35f * fine);
                byte b = (byte)(Mathf.Clamp01(s) * 255.0f);
                px[y * GroundSize + x] = new Color32(b, b, b, 255);
            }
        }
        groundTexture.SetPixels32(px);
        groundTexture.Apply(false, true);
        groundSprite = Sprite.Create(groundTexture, new Rect(0, 0, GroundSize, GroundSize), new Vector2(0.5f, 0.5f), GroundPpu, 0, SpriteMeshType.FullRect);
    }

    private void BuildStars()
    {
        starTexture = new Texture2D(StarSize, StarSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "空の星（実行中）" };
        var px = new Color32[StarSize * StarSize];
        var rnd = new System.Random(23);
        for(int i = 0; i < 26; i++)
        {
            int x = rnd.Next(StarSize), y = rnd.Next(StarSize);
            byte a = (byte)(110 + rnd.Next(146));
            px[y * StarSize + x] = new Color32(255, 255, 255, a);
            if(a > 220)
            {
                byte b = (byte)(a / 3);
                px[y * StarSize + (x + 1) % StarSize] = new Color32(255, 255, 255, b);
                px[y * StarSize + (x + StarSize - 1) % StarSize] = new Color32(255, 255, 255, b);
                px[((y + 1) % StarSize) * StarSize + x] = new Color32(255, 255, 255, b);
                px[((y + StarSize - 1) % StarSize) * StarSize + x] = new Color32(255, 255, 255, b);
            }
        }
        starTexture.SetPixels32(px);
        starTexture.Apply(false, true);
        starSprite = Sprite.Create(starTexture, new Rect(0, 0, StarSize, StarSize), new Vector2(0.5f, 0.5f), StarPpu, 0, SpriteMeshType.FullRect);
    }

    /// <summary> くり返す値のノイズ（横 px・縦 py の格子でつながる）を octaves 回重ねる </summary>
    private static float Fbm(float x, float y, int px, int py, int octaves, int seed)
    {
        float sum = 0.0f, amp = 0.5f, norm = 0.0f;
        for(int o = 0; o < octaves; o++)
        {
            sum += amp * ValueNoise(x, y, px, py, o * 17 + seed);
            norm += amp;
            x *= 2.0f; y *= 2.0f; px *= 2; py *= 2; amp *= 0.5f;
        }
        return sum / norm;
    }

    private static float ValueNoise(float x, float y, int px, int py, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3.0f - 2.0f * fx);
        fy = fy * fy * (3.0f - 2.0f * fy);
        float a = Hash(x0, y0, px, py, seed), b = Hash(x0 + 1, y0, px, py, seed);
        float c = Hash(x0, y0 + 1, px, py, seed), d = Hash(x0 + 1, y0 + 1, px, py, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float Hash(int x, int y, int px, int py, int seed)
    {
        x = ((x % px) + px) % px;
        y = ((y % py) + py) % py;
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695040;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0f;
        }
    }

    private void OnDestroy()
    {
        if(gradientSprite != null) { Destroy(gradientSprite); }
        if(gradientTexture != null) { Destroy(gradientTexture); }
        if(cloudSprite != null) { Destroy(cloudSprite); }
        if(cloudTexture != null) { Destroy(cloudTexture); }
        if(starSprite != null) { Destroy(starSprite); }
        if(starTexture != null) { Destroy(starTexture); }
        if(groundSprite != null) { Destroy(groundSprite); }
        if(groundTexture != null) { Destroy(groundTexture); }
        if(wallSprite != null) { Destroy(wallSprite); }
        if(wallTexture != null) { Destroy(wallTexture); }
    }
}
