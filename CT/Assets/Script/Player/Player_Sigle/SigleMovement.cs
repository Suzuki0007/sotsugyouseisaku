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

    private Transform startPos;

    private PlayerMovement playerMovement;

    private SigleAnimation sigleAnimation;

    //=============================================================
    // 関数
    //=============================================================

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if(playerMovement == null) { Debug.LogError("PlayerMovementが付いていません"); return; }

        sigleAnimation = GetComponent<SigleAnimation>(); 
        if(sigleAnimation == null) { Debug.LogError("SigleAnimationが付いていません"); return; }
    }

    private void Update()
    {
        if(playerMovement == null || sigleAnimation == null) { return; }

        if(playerMovement.MoveDirection != Vector2.zero)
        {
            sigleAnimation.SetDirection(playerMovement.MoveDirection);
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetStartPos(Transform target)
    {
        if(target == null) { Debug.LogError("開始位置の設定に失敗しました"); return; }
        startPos = target;
        transform.position = startPos.position;
    }
}
