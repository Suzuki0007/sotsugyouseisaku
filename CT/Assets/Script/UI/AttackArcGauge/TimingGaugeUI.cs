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

public class TimingGaugeUI : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    // UI画像設定
    [Header("Gauge Image")]

    [SerializeField]
    private Image leftFill;

    [SerializeField]
    private Image rightFill;

    // UI設定
    [Header("Gauge Settings")]

    [SerializeField]
    private float fillDuration = 2f;

    [SerializeField]
    private Key judgeKey = Key.Space;

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
    }

    private void Start()
    {
        StartGauge();
    }

    private void Update()
    {
        if(!isRunning || isFinished) { return; }

        elapsedTime += Time.deltaTime;

        // ゲージの進行度を計算（0.0から1.0の間）
        float progress = Mathf.Clamp01(elapsedTime / fillDuration);

        // 左右のゲージを更新
        leftFill.fillAmount = progress;
        rightFill.fillAmount = progress;

        if(Keyboard.current[judgeKey].isPressed) { Judge(); }

        if(elapsedTime >= fillDuration + missWindow) { JudgeFinish("MISS"); }
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

        if(resultText != null) { resultText.text = ""; }
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
        isFinished = true;
        isRunning = false;
        if(resultText != null) { resultText.text = result; }

        Debug.Log($"タイミング判定: {result}");
    }
}
