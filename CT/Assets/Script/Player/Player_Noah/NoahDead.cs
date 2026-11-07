/*********************************************************************/
// * \file   NoahDead.cs
// * \brief  ノアの死亡クラス
// *
// * \author 成田悠真
/*********************************************************************/

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ノアの死亡クラス
/// </summary>
public class NoahDead : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private Health health;

    private SceneChange sceneChange;

    [Header("SE")]

    [SerializeField, Tooltip("ゲームオーバー画面へ遷移し始めたとき")]
    private SeData gameOverLeaveSe;

    [SerializeField, Tooltip("ゲームオーバー画面に着いたとき")]
    private SeData gameOverArriveSe;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        health = GetComponent<Health>();
        if(health == null) { Debug.Log("Healthコンポーネントが見つかりません"); }
    }

    /// <summary>
    /// オブジェクトが有効化されたときに呼ばれる関数
    /// </summary>
    private void OnEnable()
    {
        health.OnDied += OnPlayerDied;
    }

    /// <summary>
    /// オブジェクトが無効化されたときに呼ばれる関数
    /// </summary>
    /// 
    /// 例えば、プレイヤーの体力が
    private void OnDisable()
    {
        health.OnDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        Debug.Log("プレイヤーが死亡しました！");

        // リトライで今のステージに戻れるように記録しておく
        SceneChange.LastStageScene = SceneManager.GetActiveScene().name;

        // 借りている SceneChange (ドアなど) のSEではなく、ゲームオーバー用のSEを鳴らす
        sceneChange.ChangeScene("SceneGameOver", null, gameOverLeaveSe, gameOverArriveSe);
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetSceneChange(SceneChange sceneChange)
    {
        this.sceneChange = sceneChange;
        if(this.sceneChange == null) { Debug.LogError($"SceneChangeの設定に失敗しました"); }
    }
}
