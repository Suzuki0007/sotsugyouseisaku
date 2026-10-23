/*********************************************************************/
// * \file   TimingGaugeUI.cs
// * \brief  タイミングゲージUIクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// タイミングゲージUIクラス
/// </summary>
public class TimingGaugeUI : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    // UI画像設定
    [Header("Gauge Image")]

    [SerializeField] 
    private GameObject timingGauge;

    [SerializeField]
    private Image leftFill;

    [SerializeField]
    private Image rightFill;

    [SerializeField] 
    private GameObject lightEffect;

    // UI設定
    [Header("Gauge Settings")]

    [SerializeField]
    private float fillDuration = 2f;

    [SerializeField]
    private float resultDisplayDuration = 1f;

    [SerializeField]
    private Key judgeKey = Key.Space;

    [SerializeField]
    private Color gaugeColor = Color.red;

    // ジャッジ
    [Header("Judge Window")]

    //[SerializeField]
    //private float justWindow = 0.01f;

    [SerializeField] 
    private float perfectWindow = 0.05f; 
    
    //[SerializeField]
    //private float greatWindow = 0.12f;
    
    [SerializeField]
    private float goodWindow = 0.22f;

    [SerializeField]
    private float missWindow = 0.15f;

    // リザルトUI
    [Header("Result UI")]

    [SerializeField]
    private TMP_Text resultText;

    private float elapsedTime;

    private bool isRunning; 

    private bool isFinished;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(leftFill == null) { Debug.LogError("Left Fill Imageが設定されていません"); }
        if(rightFill == null) { Debug.LogError("Right Fill Imageが設定されていません"); }
        if(resultText == null) { Debug.LogError("Result Textが設定されていません"); }

        // 初期状態では非表示
        if(timingGauge != null) { timingGauge.SetActive(false); }
        if(lightEffect != null) { lightEffect.SetActive(false); }
    }

    private void Start()
    {
        StartGauge();
    }

    private void Update()
    {
        if(!isRunning || isFinished) { return; }

        elapsedTime += Time.deltaTime;

        // ゲージの進行度を計算（0.0から1.0の間）して、左右のゲージUIのfillAmountに反映
        float progress = Mathf.Clamp01(elapsedTime / fillDuration);
        leftFill.fillAmount = progress;
        rightFill.fillAmount = progress;

        if(Keyboard.current != null && 
           Keyboard.current[judgeKey].wasPressedThisFrame)
        {
            Judge(); 
        }

        if(isRunning && elapsedTime >= fillDuration + missWindow)
        {
            leftFill.color = Color.gray;
            rightFill.color = Color.gray;
            JudgeFinish("MISS");
        }
    }

    /// <summary>
    /// ゲージの初期化と開始
    /// </summary>
    public void StartGauge()
    {
        elapsedTime = 0f;
        isRunning = true; 
        isFinished = false;

        leftFill.fillAmount = 0f; 
        rightFill.fillAmount = 0f;

        leftFill.color = gaugeColor;
        rightFill.color = gaugeColor;

        if(resultText != null) { resultText.text = ""; }
        if(lightEffect != null) lightEffect.SetActive(false);
        if(timingGauge != null) timingGauge.SetActive(true);
    }

    /// <summary>
    /// ジャッジ処理
    /// </summary>
    private void Judge()
    {
        // 中央のタイミングを基準にして、経過時間との差を計算
        float timingError = Mathf.Abs(elapsedTime - fillDuration);

        if(timingError <= perfectWindow)
        {
            JudgeFinish("PERFECT");
        }
        else if(timingError <= goodWindow)
        {
            JudgeFinish("GOOD");
        }
        else
        {
            JudgeFinish("MISS");
        }
    }

    /// <summary>
    /// ジャッジ終了時の処理
    /// </summary>
    /// <param name="result"></param>
    private void JudgeFinish(string result)
    {
        if(isFinished) { return; }

        isFinished = true;
        isRunning = false;
        if(resultText != null) { resultText.text = result; }

        if(lightEffect != null) { lightEffect.SetActive(result == "PERFECT"); }

        // 一定時間後にゲージを非表示にする
        if(resultDisplayDuration > 0f)
        {
            // Invokeを使って、指定した時間後にHideGaugeメソッドを呼び出す
            Invoke("HideGauge", resultDisplayDuration);
        }

        Debug.Log($"タイミング判定: {result}");
    }

    /// <summary>
    /// ゲージを非表示にする関数
    /// </summary>
    public void HideGauge()
    {
        if(timingGauge != null) { timingGauge.SetActive(false); }
    }
}
