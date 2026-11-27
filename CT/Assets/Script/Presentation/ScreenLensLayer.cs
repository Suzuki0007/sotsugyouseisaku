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

    private Camera cam;
    private Rect lastPixelRect;
    private MoodValues values = MoodValues.Neutral;

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
        bool show = !v.IsEdgeNeutral;
        if(!show)
        {
            if(root != null && root.activeSelf) { root.SetActive(false); }
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
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        if(root == null || !root.activeSelf) { return; }
        FitToCamera(false);
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
    }
}
