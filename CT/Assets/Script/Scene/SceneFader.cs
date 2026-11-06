/*********************************************************************/
// * \file   SceneFader.cs
// * \brief  暗転演出付きシーン遷移クラス
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 暗転(フェードアウト) → シーン読み込み → 明転(フェードイン) を行うクラス
/// シーンに配置しなくても、初回アクセス時に自動生成され、シーンをまたいで存続する
/// </summary>
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    /// <summary>
    /// インスタンス (存在しなければ自動生成)
    /// </summary>
    public static SceneFader Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new GameObject(nameof(SceneFader)).AddComponent<SceneFader>();
            }
            return instance;
        }
    }

    [SerializeField, Tooltip("暗転にかける時間(秒)")]
    private float fadeOutDuration = 0.3f;

    [SerializeField, Tooltip("明転にかける時間(秒)")]
    private float fadeInDuration = 0.3f;

    [SerializeField, Tooltip("暗転しきった状態で待つ時間(秒)")]
    private float blackHoldDuration = 0.1f;

    [SerializeField, Tooltip("フェードの色")]
    private Color fadeColor = Color.black;

    private Image fadeImage;

    /// <summary>
    /// 遷移中かどうか (遷移中の多重呼び出しを防ぐ)
    /// </summary>
    public bool IsFading { get; private set; }

    /// <summary>
    /// シーン遷移中(暗転〜明転)かどうか
    /// インスタンスを生成せずに参照できるので、毎フレームの入力チェックに使う
    /// </summary>
    public static bool IsBusy => instance != null && instance.IsFading;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        CreateFadeCanvas();
    }

    private void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 最前面に表示するフェード用キャンバスを生成する
    /// </summary>
    private void CreateFadeCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;// 全てのUIより手前に表示

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(transform, false);

        fadeImage = imageObject.AddComponent<Image>();
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetAlpha(0.0f);
    }

    /// <summary>
    /// 暗転してからシーンを遷移する
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    /// <param name="leaveSe">  暗転を始めるときに鳴らすSE (任意) </param>
    /// <param name="arriveSe"> 遷移先で明転を始めるときに鳴らすSE (任意) </param>
    public void FadeAndLoad(string sceneName, SeData leaveSe = null, SeData arriveSe = null)
    {
        if(IsFading) { return; }
        StartCoroutine(FadeAndLoadRoutine(sceneName, leaveSe, arriveSe));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, SeData leaveSe, SeData arriveSe)
    {
        IsFading = true;
        fadeImage.raycastTarget = true;// 遷移中はUI操作をブロック

        // 出発のSE (SeManagerはシーンをまたいで残るので、読み込み中も途切れない)
        SeManager.PlaySe(leaveSe);

        // 暗転
        yield return Fade(0.0f, 1.0f, fadeOutDuration);

        // 前のシーンで鳴らしていたループSE (環境音など) を止める
        SeManager.StopAllLoopSe();

        // 暗転中にシーン読み込み
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if(operation == null)
        {
            Debug.LogError($"シーン「{sceneName}」の読み込みに失敗しました (Build Settingsに登録されているか確認してください)");
        }
        else
        {
            while(!operation.isDone) { yield return null; }
        }

        // 新しいシーンの初期化を1フレーム待つ
        yield return null;
        yield return new WaitForSecondsRealtime(blackHoldDuration);

        // 到着のSE
        SeManager.PlaySe(arriveSe);

        // 明転
        yield return Fade(1.0f, 0.0f, fadeInDuration);

        fadeImage.raycastTarget = false;
        IsFading = false;
    }

    /// <summary>
    /// アルファ値を補間する (Time.timeScaleの影響を受けない)
    /// </summary>
    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0.0f;
        while(time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(time / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeColor;
        color.a = alpha;
        fadeImage.color = color;
    }
}
