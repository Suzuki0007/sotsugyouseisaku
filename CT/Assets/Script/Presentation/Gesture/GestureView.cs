/*********************************************************************/
// * \file   GestureView.cs
// * \brief  手の動作の画面：押し方の案内（最初の数秒と、止まっているとき）と、必要なときだけ進み具合の輪を出す
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手の動作の画面（部品「手の動作を待つ」が使う。シーンごと。初めて使ったときに作る）
/// </summary>
///
/// ・案内は画面の下に小さく。押し始めて数秒で消え、止まっていると、また出る
/// ・輪はプリセットの見た目が「輪」のときだけ（画面の真ん中の少し下）
public class GestureView : MonoBehaviour
{
    private const int SortingOrder = 96;   // 1枚絵 95 より手前・シネマの帯 97 より奥

    private static GestureView instance;

    private CanvasGroup promptGroup;
    private TextMeshProUGUI promptText;
    private Image ring;
    private Image ringBack;
    private Sprite ringSprite;
    private Texture2D ringTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static GestureView Get()
    {
        if(instance != null) { return instance; }
        var go = new GameObject("GestureView（手の動作）");
        instance = go.AddComponent<GestureView>();
        instance.Build();
        return instance;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        var p = new GameObject("案内", typeof(RectTransform));
        p.transform.SetParent(transform, false);
        var prt = (RectTransform)p.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.0f);
        prt.pivot = new Vector2(0.5f, 0.0f);
        prt.anchoredPosition = new Vector2(0.0f, 300.0f);
        prt.sizeDelta = new Vector2(900.0f, 50.0f);
        promptGroup = p.AddComponent<CanvasGroup>();
        promptText = p.AddComponent<TextMeshProUGUI>();
        if(TMP_Settings.defaultFontAsset != null) { promptText.font = TMP_Settings.defaultFontAsset; }
        promptText.fontSize = 26.0f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(1.0f, 0.97f, 0.9f, 0.9f);
        promptText.outlineWidth = 0.2f;
        promptText.outlineColor = new Color32(10, 16, 40, 200);
        promptText.raycastTarget = false;

        // 輪（白い輪の絵を実行中に作る）
        const int size = 128;
        ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for(int y = 0; y < size; y++)
        {
            for(int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2.0f - 1.0f, dy = (y + 0.5f) / size * 2.0f - 1.0f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1.0f - Mathf.Abs(d - 0.85f) * 14.0f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255.0f));
            }
        }
        ringTexture.SetPixels32(px);
        ringTexture.Apply();
        ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        ringBack = NewRing("輪（下地）", new Color(1.0f, 1.0f, 1.0f, 0.18f), false);
        ring = NewRing("輪", new Color(1.0f, 0.92f, 0.65f, 0.95f), true);
        SetRing(false, 0.0f);
        SetPrompt(null, 0.0f);
    }

    private Image NewRing(string n, Color c, bool filled)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0.0f, -120.0f);
        rt.sizeDelta = new Vector2(150.0f, 150.0f);
        var img = go.AddComponent<Image>();
        img.sprite = ringSprite;
        img.color = c;
        img.raycastTarget = false;
        if(filled)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
        }
        return img;
    }

    /// <summary> 案内を出す（alpha 0 で消す） </summary>
    public void SetPrompt(string text, float alpha)
    {
        if(text != null && promptText.text != text) { promptText.text = text; }
        promptGroup.alpha = alpha;
    }

    /// <summary> 輪を出す・進み具合 </summary>
    public void SetRing(bool show, float progress)
    {
        if(ring.gameObject.activeSelf != show) { ring.gameObject.SetActive(show); ringBack.gameObject.SetActive(show); }
        if(show) { ring.fillAmount = progress; }
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
        if(ringSprite != null) { Destroy(ringSprite); }
        if(ringTexture != null) { Destroy(ringTexture); }
    }
}
