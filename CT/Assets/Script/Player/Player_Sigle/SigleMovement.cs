/*********************************************************************/
// * \file   SigleMovement.cs
// * \brief  シグルの移動クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 移動クラス
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class SigleMovement : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private PlayerMovement playerMovement;

    private PlayerHistory targetHistory;

    private SigleAnimation sigleAnimation;

    [Header("追従設定")]

    [SerializeField] 
    private GameObject targetPlayer;

    [SerializeField] 
    private int followDistance = 10;

    //=============================================================
    // 関数
    //=============================================================

    private void Start()
    {
        // 初期位置を追従対象の位置に設定
        if(targetPlayer != null)
        {
            transform.position = targetPlayer.transform.position;
        }

        playerMovement = GetComponent<PlayerMovement>();
        if(playerMovement == null) { Debug.LogError("PlayerMovementが付いていません"); return; }

        sigleAnimation = GetComponent<SigleAnimation>(); 
        if(sigleAnimation == null) { Debug.LogError("SigleAnimationが付いていません"); return; }

        if(targetPlayer == null) { Debug.LogError("追従対象が設定されていません"); return; }

        // Inspectorで指定された追従対象を設定
        SetTarget(targetPlayer); 
    }

    private void LateUpdate()
    {
        // 操作できるキャラクターは追従しない
        if(playerMovement == null || targetHistory == null || playerMovement.CanControl)
        {
            return;
        }

        // 追従対象の履歴を取得
        var history = targetHistory.PosHistory;
        int targetIndex = history.Count - 1 - followDistance;
        if(targetIndex < 0) { return; }

        // 追従対象の履歴から位置を取得
        Vector3 targetPosition = history[targetIndex];
        Vector3 direction = targetPosition - transform.position;

        if(direction != Vector3.zero && sigleAnimation != null)
        {
            sigleAnimation.SetDirection(direction);
        }

        transform.position = targetPosition;
    }

    /// <summary>
    /// 追従対象を設定する関数
    /// </summary>
    /// <param name="newTarget"></param>
    public void SetTarget(GameObject newTarget)
    {
        if(newTarget == null)
        {
            Debug.LogError("追従対象が設定されていません");
            targetHistory = null;
            targetPlayer = null;
            return;
        }

        PlayerHistory newHistory = newTarget.GetComponent<PlayerHistory>();
        if(newHistory == null)
        {
            Debug.LogError("追従対象にPlayerHistoryが付いていません");
            return;
        }

        targetPlayer = newTarget;
        targetHistory = newHistory;
    }
}
