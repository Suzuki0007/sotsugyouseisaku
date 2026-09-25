/*********************************************************************/
// * \file   NoahMovement.cs
// * \brief  ノアの移動クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 移動クラス
/// </summary>
public class NoahMovement : CharaBase
{
    // 移動速度
    public float moveSpeed = 0.1f;

    // 別機能クラスの参照
    NoahAnimation noahAnimation;
    NoahHistory noahHistory;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        Application.targetFrameRate = 60;

        noahAnimation = GetComponent<NoahAnimation>();
        noahHistory = GetComponent<NoahHistory>();
    }

    /// <summary>
    /// 毎フレーム呼ばれる関数
    /// </summary>
    void Update()
    {
        Vector2 moveDirection = Vector2.zero;

        if(Keyboard.current.dKey.isPressed ||
       Keyboard.current.rightArrowKey.isPressed)
        {
            moveDirection.x = 1;
        }
        else if(Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
        {
            moveDirection.x = -1;
        }

        if(Keyboard.current.wKey.isPressed ||
           Keyboard.current.upArrowKey.isPressed)
        {
            moveDirection.y = 1;
        }
        else if(Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
        {
            moveDirection.y = -1;
        }

        if(moveDirection != Vector2.zero)
        {
            transform.Translate(moveDirection * moveSpeed);
            noahAnimation.SetDirection(moveDirection);
            noahHistory.RecordPosition(transform.position);
        }
    }
}
