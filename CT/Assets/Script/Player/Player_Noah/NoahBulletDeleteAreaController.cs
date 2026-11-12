/*********************************************************************/
// * \file   NoahAnimation.cs
// * \brief  ノアのアニメーションクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ノアの弾丸削除エリアコントローラー
/// </summary>
public class NoahBulletDeleteAreaController : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private BulletDeleteArea bulletDeleteArea;

    private GameObject followTargetObject;

    private bool isBulletDeleteAreaActive = false;

    [Header("SE")]

    [SerializeField, Tooltip("エリアを展開したとき")]
    private SeData areaStartSe;

    [SerializeField, Tooltip("エリア展開中に鳴らし続ける (SeData の loop をオンにする)")]
    private SeData areaLoopSe;

    [SerializeField, Tooltip("エリアを閉じたとき")]
    private SeData areaEndSe;

    private bool wasAreaActive = false;
    private SeHandle areaLoopHandle;

    [Header("デバッグ設定")]

    [SerializeField]
    private bool isDebugMode = false;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool IsBulletDeleteAreaActive => isBulletDeleteAreaActive;

    public bool IsDebugMode => isDebugMode;

    //=====================================================================
    // 更新
    //=====================================================================

    void Update()
    {
        if(bulletDeleteArea == null || followTargetObject == null) { return; }

        bool isActive = isDebugMode || isBulletDeleteAreaActive;

        // 守りのゲージ（GuardGauge）があるシーンでは、ゲージが残っている間だけ出せる（ないシーンは今までどおり）
        if(isActive && !isDebugMode) { isActive = GuardGauge.Use(Time.deltaTime); }

        bulletDeleteArea.SetAreaActive(isActive);

        // 追従ターゲットの位置にエリアを追従させる
        bulletDeleteArea.transform.position = followTargetObject.transform.position;

        UpdateAreaSe(isActive);
    }

    private void OnDisable()
    {
        SeManager.StopSe(areaLoopHandle);
        areaLoopHandle = SeHandle.None;
        wasAreaActive = false;
    }

    /// <summary>
    /// エリアの開閉に合わせてSEを鳴らす
    /// </summary>
    private void UpdateAreaSe(bool isActive)
    {
        if(isActive == wasAreaActive) { return; }
        wasAreaActive = isActive;

        if(isActive)
        {
            SeManager.PlaySe(areaStartSe, transform.position);
            areaLoopHandle = SeManager.PlaySe(areaLoopSe, followTargetObject.transform);
        }
        else
        {
            SeManager.StopSe(areaLoopHandle);
            areaLoopHandle = SeHandle.None;
            SeManager.PlaySe(areaEndSe, transform.position);
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetBulletDeleteArea(BulletDeleteArea newBulletDeleteArea)
    {
        bulletDeleteArea = newBulletDeleteArea;
        if(bulletDeleteArea == null) { Debug.LogError("弾丸削除エリアの設定に失敗しました"); }
    }

    public void SetBulletDeleteAreaActive(bool isActive)
    {
        isBulletDeleteAreaActive = isActive;

        //Debug.Log("弾削除エリアの有効化状態を設定: " + isActive);
    }

    public void SetFollowTarget(GameObject targetObject)
    {
        followTargetObject = targetObject;
        if(followTargetObject == null) { Debug.LogError("追従ターゲットの設定に失敗しました"); }
    }
}
