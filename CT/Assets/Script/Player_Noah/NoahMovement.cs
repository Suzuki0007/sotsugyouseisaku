/*********************************************************************/
// * \file   NoahMovement.cs
// * \brief  ノアの移動クラス
// *
// * \author 成田悠真, 鈴木裕稀
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 移動クラス
/// </summary>
public class NoahMovement : CharaBase
{
    [SerializeField] private float moveSpeed;

    Rigidbody2D rb;

    // 別機能クラスの参照
    NoahAnimation noahAnimation;
    NoahHistory noahHistory;

    Vector2 moveDirection;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        if(moveSpeed <= 0){ Debug.LogError("移動速度が設定されていません"); }

        rb = GetComponent<Rigidbody2D>();
        if(rb == null) { Debug.LogError("Rigidbody2Dがアタッチされていません"); }

        noahAnimation = GetComponent<NoahAnimation>();
        noahHistory = GetComponent<NoahHistory>();
    }

    /// <summary>
    /// 毎フレーム呼ばれる関数
    /// </summary>
    void Update()
    {
        moveDirection = Vector2.zero;

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
            // アニメーションの方向を設定
            noahAnimation.SetDirection(moveDirection);
        }
    }

    /// <summary>
    /// 物理演算の更新ごとに呼ばれる関数
    /// </summary>
    void FixedUpdate()
    {
        if(moveDirection != Vector2.zero)
        {
            // 正規化
            moveDirection = moveDirection.normalized;

            // ノアの物理演算による移動
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);

            // 位置を履歴に記録
            noahHistory.RecordPosition(rb.position);
        }
    }
}
