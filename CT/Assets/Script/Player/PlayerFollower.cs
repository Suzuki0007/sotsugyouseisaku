/*********************************************************************/
// * \file   PlayerFollower.cs
// * \brief  プレイヤー追従クラス
// *
// * \author 成田悠真
/*********************************************************************/

using Unity.AppUI.Redux;
using UnityEngine;

/// <summary>
/// プレイヤー追従クラス
/// </summary>
public class PlayerFollower : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("追従設定")]

    [SerializeField]
    private GameObject targetPlayer;

    [SerializeField]
    private float followDistance = 10;

    private PlayerMovement playerMovement;

    private PlayerHistory targetHistory;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if(playerMovement == null)
        {
            Debug.LogError($"{name}: PlayerMovementが付いていません"); 
            enabled = false;
            return; 
        }

        if(followDistance <= 0)
        {
            Debug.LogError($"{name}: 追従距離は0以上にしてください");
            enabled = false;
            return;
        }

        SetTarget(targetPlayer);
    }

    private void LateUpdate()
    {
        // 操作できるキャラクターは追従しない
        if(playerMovement == null || targetHistory == null || playerMovement.CanControl)
        {
            return;
        }

        // 追従対象の履歴から位置を取得
        var history = targetHistory.PosHistory;
        int targetIndex = (int)(history.Count - 1 - followDistance);
        if(targetIndex < 0) { return; }

        // 追従
        Vector3 targetPos = history[targetIndex];
        transform.position = targetPos;
    }

    public void SetTarget(GameObject newTarget)
    {
        if(newTarget == null)
        {
            Debug.LogError($"{name} の追従対象が設定されていません");
            targetHistory = null;
            targetPlayer = null;
            return;
        }

        if(newTarget == gameObject)
        {
            Debug.LogError($"{name} は自分自身を追従対象にできません");
            return;
        }

        PlayerHistory newHistory = newTarget.GetComponent<PlayerHistory>();
        if(newHistory == null)
        {
            Debug.LogError($"{newTarget.name} にPlayerHistoryがありません");
            return;
        }

        targetPlayer = newTarget;
        targetHistory = newHistory;
    }
}
