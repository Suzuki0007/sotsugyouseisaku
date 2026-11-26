/*********************************************************************/
// * \file   PresentationStatusView.cs
// * \brief  演出の今の状態を表示（試し用）：画面の右上に、今の色調・光・天候・回想・暗さの名前を小さく出す
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;

/// <summary>
/// 演出の今の状態を表示（試し用。画面の右上に今の色調・光・天候・回想の名前）
/// </summary>
///
/// ・シーンに置くと、再生中に画面の右上へ小さく出す。台座を調べたあと、どのプリセットになったかを見るのに使う
/// ・エディタと Development Build だけ（本番のビルドでは何もしない）
[AddComponentMenu("演出/演出の今の状態を表示（試し用）")]
[DisallowMultipleComponent]
public class PresentationStatusView : MonoBehaviour
{
    [SerializeField, Label("文字の大きさ", 10.0f, 40.0f)]
    private float fontSize = 20.0f;

    private TextMeshProUGUI text;
    private GameObject canvasObject;
    private string lastMood, lastLight, lastWeather;
    private bool lastFlashback, lastDark;
    private float wait;

    private void Start()
    {
        if(!Debug.isDebugBuild) { enabled = false; return; }

        canvasObject = new GameObject("演出の今の状態（表示）");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;
        var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        var go = new GameObject("文", typeof(RectTransform));
        go.transform.SetParent(canvasObject.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1.0f, 1.0f);
        rt.pivot = new Vector2(1.0f, 1.0f);
        rt.anchoredPosition = new Vector2(-16.0f, -60.0f);
        rt.sizeDelta = new Vector2(330.0f, 200.0f);
        text = go.AddComponent<TextMeshProUGUI>();
        if(TMP_Settings.defaultFontAsset != null) { text.font = TMP_Settings.defaultFontAsset; }
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopRight;
        text.color = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        text.raycastTarget = false;
        Refresh(true);
    }

    private void LateUpdate()
    {
        // 名前が変わったときだけ文を作り直す（0.2 秒ごとに見る）
        wait -= Time.unscaledDeltaTime;
        if(wait > 0.0f) { return; }
        wait = 0.2f;
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        if(text == null) { return; }
        string m = PresentationDirector.CurrentMoodName;
        string l = PresentationDirector.CurrentLightName;
        string w = PresentationDirector.CurrentWeatherName;
        bool f = PresentationDirector.InFlashback;
        bool d = StageDarkness.IsDarkStage;
        if(!force && m == lastMood && l == lastLight && w == lastWeather && f == lastFlashback && d == lastDark) { return; }
        lastMood = m; lastLight = l; lastWeather = w; lastFlashback = f; lastDark = d;

        text.text = "色調：" + Short(m, "通常") + "\n光：" + Short(l, "シーンのまま") + "\n天候：" + Short(w, "なし") +
                    "\n回想：" + (f ? "中" : "－") + "\n暗さ：" + (d ? "暗い" : "－");
    }

    private static string Short(string name, string empty)
    {
        if(string.IsNullOrEmpty(name)) { return empty; }
        int u = name.IndexOf('_');
        return u >= 0 ? name.Substring(u + 1) : name;
    }
}
