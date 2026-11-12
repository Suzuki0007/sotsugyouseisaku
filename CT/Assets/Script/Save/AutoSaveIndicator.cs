/*********************************************************************/
// * \file   AutoSaveIndicator.cs
// * \brief  オートセーブしたときに、画面の右下に「オートセーブしました」を少しの間出す
// *
// * \author
/*********************************************************************/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// オートセーブの表示（自動で作られる。シーンをまたいで残る）
/// </summary>
///
/// ・GameSession.AutoSaved が来たら、画面の右下に「オートセーブしました」を出して消す
/// ・プレイしていないとき（エディタでステージから直接再生）は、ファイルに書かないので出さない
public class AutoSaveIndicator : MonoBehaviour
{
    private const string Message = "オートセーブしました";
    private const float ShowSeconds = 1.6f;
    private const float FadeSeconds = 0.4f;

    private static AutoSaveIndicator instance;

    private TextMeshProUGUI text;
    private Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        GameSession.AutoSaved -= OnAutoSaved;
        GameSession.AutoSaved += OnAutoSaved;
    }

    private static void OnAutoSaved()
    {
        if(!Save.HasSlot) { return; }

        if(instance == null)
        {
            var go = new GameObject("オートセーブの表示（自動で作られる）");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AutoSaveIndicator>();
            instance.Build();
        }
        instance.Show();
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        var textObj = new GameObject("文字", typeof(RectTransform));
        textObj.transform.SetParent(transform, false);
        var rect = (RectTransform)textObj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1.0f, 1.0f);
        rect.pivot = new Vector2(1.0f, 1.0f);
        rect.anchoredPosition = new Vector2(-28.0f, -20.0f);
        rect.sizeDelta = new Vector2(400.0f, 40.0f);

        text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = Message;
        text.fontSize = 24.0f;
        text.alignment = TextAlignmentOptions.Right;
        text.raycastTarget = false;
        text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    private void Show()
    {
        if(routine != null) { StopCoroutine(routine); }
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        float t = 0.0f;
        while(t < ShowSeconds + FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float alpha = t < FadeSeconds ? t / FadeSeconds : (t > ShowSeconds ? 1.0f - (t - ShowSeconds) / FadeSeconds : 1.0f);
            text.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha));
            yield return null;
        }
        text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        routine = null;
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }
}
