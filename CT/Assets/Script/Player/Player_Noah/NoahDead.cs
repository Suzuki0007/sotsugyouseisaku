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
        if(partnerHealth != null) { partnerHealth.OnDied += OnPartnerDied; }
    }

    /// <summary>
    /// オブジェクトが無効化されたときに呼ばれる関数
    /// </summary>
    /// 
    /// 例えば、プレイヤーの体力が
    private void OnDisable()
    {
        health.OnDied -= OnPlayerDied;
        if(partnerHealth != null) { partnerHealth.OnDied -= OnPartnerDied; }
    }

    //=====================================================================
    // 相棒（シグル）
    //=====================================================================

    private GameObject partner;
    private Health partnerHealth;

    /// <summary>
    /// 相棒（シグル）を設定する。相棒が倒れてもゲームオーバーにする
    /// （一緒のときはシグルの体力を2人で共有、離れているときはどちらが倒れてもゲームオーバー）
    /// </summary>
    public void SetPartner(GameObject partnerObject)
    {
        if(partnerHealth != null) { partnerHealth.OnDied -= OnPartnerDied; }

        partner = partnerObject;
        partnerHealth = partner != null ? partner.GetComponent<Health>() : null;
        if(partnerHealth != null && isActiveAndEnabled) { partnerHealth.OnDied += OnPartnerDied; }
    }

    private void OnPlayerDied() => BeginGameOver(gameObject);

    private void OnPartnerDied() => BeginGameOver(partner);

    /// <summary>
    /// 倒れたキャラの死亡アニメを流してから、ゲームオーバー画面へ
    /// </summary>
    private void BeginGameOver(GameObject fallen)
    {
        if(dying) { return; }
        dying = true;

        Debug.Log($"{(fallen != null ? fallen.name : "プレイヤー")}が倒れました");

        // 2人とも動けなくする
        StopMovement(gameObject);
        StopMovement(partner);

        // 死亡アニメがあれば、流し終わるのを待つ
        SpriteAnimator anim = fallen != null ? fallen.GetComponent<SpriteAnimator>() : null;
        if(anim != null && anim.Play(deathAnimation, true))
        {
            StartCoroutine(GameOverAfterAnimation(anim));
            return;
        }

        GoToGameOver();
    }

    private static void StopMovement(GameObject target)
    {
        PlayerMovement movement = target != null ? target.GetComponent<PlayerMovement>() : null;
        if(movement == null) { return; }
        movement.Stop();
        movement.SetControlEnabled(false);
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
