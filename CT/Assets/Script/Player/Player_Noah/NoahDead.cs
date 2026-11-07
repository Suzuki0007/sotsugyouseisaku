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

    [Header("アニメーション")]

    [SerializeField, Tooltip("死亡アニメの名前。SpriteAnimator にこの名前の動きがあれば、流し終わってからゲームオーバー画面へ移る (無ければすぐ移る)")]
    private string deathAnimation = "Dead";

    private bool dying;

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
        if(dying) { return; }
        dying = true;

        Debug.Log("プレイヤーが死亡しました！");

        // 死亡アニメがあれば、操作を止めて流し終わるのを待つ
        SpriteAnimator anim = GetComponent<SpriteAnimator>();
        if(anim != null && anim.Play(deathAnimation, true))
        {
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if(movement != null)
            {
                movement.Stop();
                movement.SetControlEnabled(false);
            }
            StartCoroutine(GameOverAfterAnimation(anim));
            return;
        }

        GoToGameOver();
    }

    private System.Collections.IEnumerator GameOverAfterAnimation(SpriteAnimator anim)
    {
        yield return anim.WaitForCycle();
        GoToGameOver();
    }

    private void GoToGameOver()
    {
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
