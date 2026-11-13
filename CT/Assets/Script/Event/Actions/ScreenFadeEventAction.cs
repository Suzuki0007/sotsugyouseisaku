/*********************************************************************/
// * \file   ScreenFadeEventAction.cs
// * \brief  イベントの部品「画面の暗転」（会話の文字は見えたまま画面を暗くする・明るくする）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// イベントの部品「画面の暗転」
/// </summary>
///
/// ・画面を暗くする／明るくする。暗いままでも会話の枠と文字は見える（会話の画面より奥に出す）
/// ・OP のモノローグ、回想、時間が飛ぶ場面などに使う
/// ・暗くしたままシーンを移ると、黒い画面はシーンと一緒に消える
/// ・シーン遷移の暗転（SceneFader）とは別物（あちらは会話の画面も隠す）
public class ScreenFadeEventAction : EventAction
{
    public enum FadeType
    {
        [InspectorName("暗転する（画面を暗くする）")]
        ToBlack = 0,

        [InspectorName("明転する（画面を明るくする）")]
        ToClear = 1,
    }

    [SerializeField, Label("種類")]
    [Tooltip("暗転する：画面を暗くする／明転する：暗くした画面を戻す")]
    private FadeType type = FadeType.ToBlack;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("0 ならすぐに切り替わる")]
    private float duration = 1.0f;

    [SerializeField, Label("色")]
    [Tooltip("暗転の色（ふつうは黒。白くすると「白い光」の演出になる）")]
    private Color color = Color.black;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、暗転・明転が終わってから次の部品へ進む")]
    private bool waitUntilDone = true;

    public override void Execute(GameObject gameObject)
    {
        ScreenCurtain.Fade(type == FadeType.ToBlack ? 1.0f : 0.0f, duration, color);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }

        while(ScreenCurtain.IsFading) { yield return null; }
    }
}

/// <summary>
/// 会話の画面より奥に出す黒い幕（シーンごと。初めて使ったときに作る）
/// </summary>
public class ScreenCurtain : MonoBehaviour
{
    // 会話の画面（DialogueSystem）は 100。HUD より手前・会話より奥
    private const int SortingOrder = 90;

    private static ScreenCurtain instance;

    private Image image;
    private Coroutine fading;

    /// <summary> 暗転・明転の途中か </summary>
    public static bool IsFading => instance != null && instance.fading != null;

    /// <summary> 今の暗さ（0 = 見える、1 = 真っ暗） </summary>
    public static float Alpha => instance != null && instance.image != null ? instance.image.color.a : 0.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    /// <summary>
    /// 暗さを変える
    /// </summary>
    /// <param name="alpha"> 1 = 真っ暗、0 = 見える </param>
    /// <param name="duration"> かける時間（秒）。0 ならすぐ </param>
    public static void Fade(float alpha, float duration, Color color)
    {
        // 明るくするときに幕がなければ何もしない
        if(instance == null && alpha <= 0.0f) { return; }

        ScreenCurtain curtain = GetOrCreate();
        if(curtain.fading != null) { curtain.StopCoroutine(curtain.fading); curtain.fading = null; }

        if(duration <= 0.0f)
        {
            curtain.SetColor(color, alpha);
            return;
        }

        curtain.fading = curtain.StartCoroutine(curtain.FadeRoutine(alpha, duration, color));
    }

    private static ScreenCurtain GetOrCreate()
    {
        if(instance != null) { return instance; }

        var go = new GameObject(nameof(ScreenCurtain));
        instance = go.AddComponent<ScreenCurtain>();

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var imageObject = new GameObject("Black");
        imageObject.transform.SetParent(go.transform, false);
        instance.image = imageObject.AddComponent<Image>();
        instance.image.raycastTarget = false;

        RectTransform rect = instance.image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        instance.SetColor(Color.black, 0.0f);
        return instance;
    }

    private IEnumerator FadeRoutine(float to, float duration, Color color)
    {
        float from = image.color.a;
        float time = 0.0f;
        while(time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetColor(color, Mathf.Lerp(from, to, Mathf.Clamp01(time / duration)));
            yield return null;
        }

        SetColor(color, to);
        fading = null;
    }

    private void SetColor(Color color, float alpha)
    {
        color.a = alpha;
        image.color = color;
        image.enabled = alpha > 0.0f;
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }
}
