/*********************************************************************/
// * \file   PlayerMovement.cs
// * \brief  プレイヤーの移動処理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー共通の移動処理クラス
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("移動設定")]

    [SerializeField]
    private float moveSpeed = 5f;

    [Header("操作設定")]

    [SerializeField] 
    private bool canControl = false;

    private Rigidbody2D rb;

    private Vector2 moveDirection;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool CanControl => canControl;

    public Vector2 MoveDirection => moveDirection;

    //=====================================================================
    // 関数
    //=====================================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if(moveSpeed <= 0f) { Debug.Log($"{name}: 移動速度が正しく設定されていません。", this); }
    }

    private void Update()
    {
        // 操作できない場合は移動方向をリセット
        if(!canControl)
        {
            moveDirection = Vector2.zero;
            return;
        }

        if(Keyboard.current == null)
        {
            moveDirection = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if(Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal = 1f;
        }
        else if(Keyboard.current.aKey.isPressed ||
                 Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal = -1f;
        }

        if(Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical = 1f;
        }
        else if(Keyboard.current.sKey.isPressed ||
                 Keyboard.current.downArrowKey.isPressed)
        {
            vertical = -1f;
        }

        moveDirection = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        if(!canControl || moveDirection == Vector2.zero)
        {
            return;
        }

        Vector2 nextPosition =
            rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

    /// <summary>
    /// 操作可能状態を変更する。
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;

        if(!canControl)
        {
            moveDirection = Vector2.zero;
        }
    }

    /// <summary>
    /// 移動方向を強制的にリセットする。
    /// </summary>
    public void ResetMoveDirection()
    {
        moveDirection = Vector2.zero;
    }
}
