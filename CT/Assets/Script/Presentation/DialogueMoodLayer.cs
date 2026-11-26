/*********************************************************************/
// * \file   DialogueMoodLayer.cs
// * \brief  会話画面の色調：色調のプリセットの「会話画面にもかける」がオンのとき、会話の立ち絵・文字に色の加工をかけ、粒子と周辺の暗さを重ねる（窓口が使う）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 会話画面の色調（演出の窓口 PresentationDirector が自動で付ける。シーンに置かなくてよい）
/// </summary>
///
/// ・色調のプリセットの「会話画面にもかける」がオンの間、会話の立ち絵（色を抜く・色をかける・粒子）、名前・本文・キーの案内（色をかける）を変える
/// ・会話を出している間だけ、会話画面より手前（並び順 101）に粒子と周辺の暗さを薄く重ねる
/// ・メニュー・HUD・セーブの画面は変えない。色調の切り替えと同じ秒数でなめらかに変わる
[AddComponentMenu("")]
public class DialogueMoodLayer : MonoBehaviour
{
    private const int OverlaySortingOrder = 101;
    private const float OverlayFadeSeconds = 0.3f;

    // 粒子（11-26 ユーザーの指示「0.16 くらいから急に減る」「0.17 でも会話画面では多すぎる」）：
    // 弱くするときは 1 つの点の濃さを下げず、点の数（割合）を減らす（濃さを下げると画面の色の段 1/255 を下回って急に消えるため）
    // 点の割合 ＝ プリセットの粒子 × 会話画面の強さ × DotDensityPerGrain（粒子 0.15・強さ 0.8 で 1.2%、粒子 1・強さ 1 で 10%）
    private const float DotDensityPerGrain = 0.1f;
    private const float PortraitDotStrength = 0.07f;   // 立ち絵の 1 つの点の明るさの揺れ（±）
    private const float OverlayDotAlpha = 0.07f;       // 重ねる粒子の 1 つの点の濃さ
    private const int GrainSize = 128;

    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    private static readonly int MoodTintId = Shader.PropertyToID("_MoodTint");
    private static readonly int GrainId = Shader.PropertyToID("_Grain");
    private static readonly int GrainSeedId = Shader.PropertyToID("_GrainSeed");
    private static readonly int GrainAmountId = Shader.PropertyToID("_GrainAmount");


    private Material portraitMaterial;

    // 今かけている物（元のマテリアル）
    private readonly List<Image> portraits = new List<Image>();
    private readonly List<Material> portraitOriginals = new List<Material>();
    private readonly List<TMP_Text> texts = new List<TMP_Text>();
    private readonly List<TMP_SubMeshUI> subMeshes = new List<TMP_SubMeshUI>();
    private readonly List<Image> collectPortraits = new List<Image>();
    private readonly List<TMP_Text> collectTexts = new List<TMP_Text>();

    private MoodValues values = MoodValues.Neutral;
    private bool applied;
    private bool wasBusy;

    // 重ねる絵
    private GameObject overlayRoot;
    private CanvasGroup overlayGroup;
    private RawImage grainImage;
    private Image vignetteImage;
    private Texture2D grainTexture;
    private Texture2D vignetteTexture;
    private Sprite vignetteSprite;
    private float overlayShow;
    private float grainWait;
    private byte[] grainThreshold;
    private Color32[] grainPixels;
    private float grainBuiltDensity = -1.0f;
    private float retintWait;

    /// <summary> 粒子の点の割合（0〜1） </summary>
    private float DotDensity => PresentationEase.IsLowQuality ? 0.0f : Mathf.Clamp01(values.grain * values.uiAmount * DotDensityPerGrain);

    /// <summary> 会話画面にかけている強さ（0 でかけていない） </summary>
    public float Amount => values.uiAmount;

    //=====================================================================
    // 窓口から
    //=====================================================================

    /// <summary> 今の色調の値で会話画面を変える（窓口が色調を変えるたびに呼ぶ） </summary>
    public void Apply(MoodValues v)
    {
        values = v;
        bool want = v.uiAmount > 0.001f;
        if(want && !applied) { Collect(); }
        if(!want && applied) { Restore(); }
        if(want) { UpdateMaterials(); }
    }

    /// <summary> 毎フレーム（窓口の LateUpdate から）：会話が始まったら拾い直す・重ねる絵を出す・粒子を動かす </summary>
    public void Tick(float dt)
    {
        bool busy = DialogueManager.IsBusy;
        if(busy && !wasBusy && applied)
        {
            // 会話が始まった瞬間（字幕の文字などが作られたあと）に拾い直す
            Restore();
            Collect();
            UpdateMaterials();
        }
        wasBusy = busy;

        // 会話の途中で文字が足される（アトラスが増えて子の文字ができる）ことがあるので、会話中は 0.25 秒ごとに色をかけ直す
        if(applied && busy)
        {
            retintWait -= dt;
            if(retintWait <= 0.0f)
            {
                retintWait = 0.25f;
                Color face = MoodTint(values, values.uiAmount);
                face.a = 1.0f;
                for(int i = 0; i < texts.Count; i++) { if(texts[i] != null) { TintText(texts[i], face); } }
            }
        }

        float target = busy && values.uiAmount > 0.001f ? 1.0f : 0.0f;
        if(target > 0.0f && overlayRoot == null) { BuildOverlay(); }
        if(overlayRoot == null) { return; }

        overlayShow = Mathf.MoveTowards(overlayShow, target, dt / OverlayFadeSeconds);
        overlayGroup.alpha = overlayShow;
        bool visible = overlayShow > 0.001f;
        if(overlayRoot.activeSelf != visible) { overlayRoot.SetActive(visible); }
        if(!visible) { return; }

        float a = values.uiAmount;
        Color vc = values.vignetteColor;
        vc.a = Mathf.Clamp01(values.vignette * a * 1.4f);
        vignetteImage.color = vc;
        // 粒子：点の数を変える（割合が変わったときだけ絵を作り直す）。1 つの点の濃さは同じ
        float density = DotDensity;
        if(Mathf.Abs(density - grainBuiltDensity) > 0.001f) { RebuildGrain(density); }
        grainImage.color = new Color(1.0f, 1.0f, 1.0f, OverlayDotAlpha);
        grainImage.enabled = density > 0.0001f;

        // 粒子は 1 秒に 12 回ほど位置を変える（ちらつき）
        grainWait -= dt;
        if(grainWait <= 0.0f && grainImage.enabled)
        {
            grainWait = 1.0f / 12.0f;
            grainImage.uvRect = new Rect(Random.value, Random.value, 8.0f, 4.5f);
            if(portraitMaterial != null) { portraitMaterial.SetFloat(GrainSeedId, Random.value * 100.0f); }
        }
    }

    //=====================================================================
    // 立ち絵・文字
    //=====================================================================

    private void Collect()
    {
        if(portraitMaterial == null)
        {
            Material source = Resources.Load<Material>("Presentation/Materials/Mat_UI_Mood");
            if(source == null) { Debug.LogWarning("会話画面の色調：Resources/Presentation/Materials/Mat_UI_Mood がありません（Tools > 演出 > 最初のプリセットを作る）"); return; }
            portraitMaterial = new Material(source) { name = "会話画面の色調（実行中）" };
        }

        collectPortraits.Clear();
        collectTexts.Clear();
        DialogueManager dm = DialogueManager.Instance;
        if(dm != null) { dm.GetMoodTargets(collectPortraits, collectTexts); }
        foreach(CinemaMode cinema in FindObjectsByType<CinemaMode>()) { cinema.GetMoodTexts(collectTexts); }

        for(int i = 0; i < collectPortraits.Count; i++)
        {
            Image img = collectPortraits[i];
            if(img == null || portraits.Contains(img)) { continue; }
            portraits.Add(img);
            portraitOriginals.Add(img.material == img.defaultMaterial ? null : img.material);
            img.material = portraitMaterial;
        }
        for(int i = 0; i < collectTexts.Count; i++)
        {
            TMP_Text t = collectTexts[i];
            if(t == null || texts.Contains(t)) { continue; }
            // 文字は表示の色（CanvasRenderer の色）で色をかける。マテリアルは複製しない
            // （11-26：複製すると、フォントに文字が足されてアトラスが 2 枚目に増えたときに TMP がエラーになり、会話が止まった）
            texts.Add(t);
        }
        applied = true;
    }

    private void Restore()
    {
        for(int i = 0; i < portraits.Count; i++)
        {
            if(portraits[i] != null) { portraits[i].material = portraitOriginals[i]; }
        }
        for(int i = 0; i < texts.Count; i++)
        {
            if(texts[i] != null) { TintText(texts[i], Color.white); }
        }
        portraits.Clear();
        portraitOriginals.Clear();
        texts.Clear();
        applied = false;
    }

    /// <summary> 文字（と、足されたアトラスの分の子の文字）の表示の色を変える </summary>
    private void TintText(TMP_Text t, Color color)
    {
        t.canvasRenderer.SetColor(color);
        t.GetComponentsInChildren(true, subMeshes);
        for(int k = 0; k < subMeshes.Count; k++) { subMeshes[k].canvasRenderer.SetColor(color); }
    }

    private void UpdateMaterials()
    {
        MoodValues v = values;
        float a = v.uiAmount;
        Color tint = MoodTint(v, a);

        if(portraitMaterial != null)
        {
            portraitMaterial.SetFloat(SaturationId, Mathf.Clamp(1.0f + v.saturation / 100.0f * a, 0.0f, 2.0f));
            portraitMaterial.SetFloat(ContrastId, Mathf.Clamp(1.0f + v.contrast / 100.0f * a, 0.0f, 2.0f));
            portraitMaterial.SetFloat(ExposureId, Mathf.Pow(2.0f, v.brightness * a));
            portraitMaterial.SetColor(MoodTintId, tint);
            // 立ち絵の粒子は、11-26 ユーザーの指示「ざらつきが強い」でマップの 3 割に
            portraitMaterial.SetFloat(GrainId, DotDensity);
            portraitMaterial.SetFloat(GrainAmountId, PortraitDotStrength);
        }

        // 文字：色をかける＋彩度を下げる分だけ灰色に寄せる（文字は白が多いので、主に色をかける）
        Color face = tint;
        face.a = 1.0f;
        for(int i = 0; i < texts.Count; i++)
        {
            if(texts[i] != null) { TintText(texts[i], face); }
        }
    }

    /// <summary> 色調の「色をかける」「暖色・寒色」を、UI にかける色にまとめる </summary>
    private static Color MoodTint(MoodValues v, float a)
    {
        Color c = v.colorFilter;
        float temp = v.temperature / 100.0f;
        Color warm = temp >= 0.0f ? Color.Lerp(Color.white, new Color(1.0f, 0.86f, 0.66f), temp) : Color.Lerp(Color.white, new Color(0.78f, 0.88f, 1.0f), -temp);
        c *= warm;
        c.a = 1.0f;
        return Color.Lerp(Color.white, c, a);
    }

    //=====================================================================
    // 重ねる絵（粒子・周辺の暗さ）
    //=====================================================================

    private void BuildOverlay()
    {
        overlayRoot = new GameObject("会話画面の色調（重ねる絵）");
        overlayRoot.transform.SetParent(transform, false);
        var canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;
        var scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;
        overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;
        overlayGroup.alpha = 0.0f;

        // 周辺の暗さ（中心が透明・四隅が濃い）
        const int vs = 128;
        vignetteTexture = new Texture2D(vs, vs, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var vp = new Color32[vs * vs];
        for(int y = 0; y < vs; y++)
        {
            for(int x = 0; x < vs; x++)
            {
                float dx = (x + 0.5f) / vs * 2.0f - 1.0f, dy = (y + 0.5f) / vs * 2.0f - 1.0f;
                float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.75f);
                vp[y * vs + x] = new Color32(255, 255, 255, (byte)(d * d * 255.0f));
            }
        }
        vignetteTexture.SetPixels32(vp);
        vignetteTexture.Apply();
        vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0, 0, vs, vs), new Vector2(0.5f, 0.5f));
        vignetteImage = NewChild("周辺の暗さ").AddComponent<Image>();
        vignetteImage.sprite = vignetteSprite;
        vignetteImage.raycastTarget = false;

        // 粒子（点を繰り返して敷く。点の数は RebuildGrain で変える）
        grainTexture = new Texture2D(GrainSize, GrainSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
        grainThreshold = new byte[GrainSize * GrainSize];
        grainPixels = new Color32[GrainSize * GrainSize];
        var rnd = new System.Random(7);
        for(int i = 0; i < grainThreshold.Length; i++) { grainThreshold[i] = (byte)rnd.Next(0, 256); }
        grainBuiltDensity = -1.0f;
        grainImage = NewChild("粒子").AddComponent<RawImage>();
        grainImage.texture = grainTexture;
        grainImage.raycastTarget = false;

        overlayRoot.SetActive(false);
    }

    /// <summary> 点の割合が density になるように、粒子の絵を作り直す（点ごとの順番は固定＝割合を上げると点が足されていく） </summary>
    private void RebuildGrain(float density)
    {
        grainBuiltDensity = density;
        if(grainTexture == null) { return; }
        int limit = Mathf.RoundToInt(density * 256.0f);
        for(int i = 0; i < grainPixels.Length; i++)
        {
            // 点：明るい点と暗い点を半分ずつ（暗い点は黒い帯の上では見えないので、帯の上はさらにまばらに見える）
            bool on = grainThreshold[i] < limit;
            byte g = (byte)((grainThreshold[i] & 1) == 0 ? 255 : 0);
            grainPixels[i] = new Color32(g, g, g, (byte)(on ? 255 : 0));
        }
        grainTexture.SetPixels32(grainPixels);
        grainTexture.Apply(false);
    }

    private GameObject NewChild(string childName)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(overlayRoot.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    /// <summary> シーンが変わったとき（会話の仕組みが入れ替わる） </summary>
    public void ResetForScene()
    {
        portraits.Clear();
        portraitOriginals.Clear();
        texts.Clear();
        applied = false;
        wasBusy = false;
        overlayShow = 0.0f;
        if(overlayRoot != null) { overlayRoot.SetActive(false); }
        Apply(values);
    }

    private void OnDestroy()
    {
        if(applied) { Restore(); }
        if(portraitMaterial != null) { Destroy(portraitMaterial); }
        if(vignetteSprite != null) { Destroy(vignetteSprite); }
        if(vignetteTexture != null) { Destroy(vignetteTexture); }
        if(grainTexture != null) { Destroy(grainTexture); }
    }
}
