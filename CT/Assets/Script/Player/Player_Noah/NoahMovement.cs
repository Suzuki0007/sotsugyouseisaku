/*********************************************************************/
// * \file   NoahMovement.cs
// * \brief  ノアの移動クラス
// *
// * \author 成田悠真, 鈴木裕稀
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 移動クラス
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class NoahMovement : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private PlayerMovement playerMovement;

    private NoahAnimation noahAnimation;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        noahAnimation = GetComponent<NoahAnimation>();

        if(noahAnimation == null) { Debug.LogError($"{name}: NoahAnimationがアタッチされていません。", this); }
    }

    private void Update()
    {
        if(playerMovement == null || noahAnimation == null) { return; }

        if(playerMovement.MoveDirection != Vector2.zero)
        {
            noahAnimation.SetDirection(playerMovement.MoveDirection);
        }
    }
}
