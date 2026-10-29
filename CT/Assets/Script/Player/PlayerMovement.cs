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
        if(rb == null) { Debug.LogError($"{name}: Rigidbody2Dがアタッチされていません。", this); }

        if(moveSpeed <= 0f) { Debug.LogWarning($"{name}: 移動速度が正しく設定されていません。", this); }
    }

    private void FixedUpdate()
    {
        if(!canControl || moveDirection == Vector2.zero) { return; }

        Vector2 nextPosition =
            rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

    /// <summary>
    /// 移動方向を設定するメソッド
    /// </summary>
    /// <param name="direction"></param>
    public void SetMoveDirection(Vector2 direction)
    {
        if(!canControl) 
        {
            moveDirection = Vector2.zero;
            return;
        }

        //moveDirection = direction.normalized;

        // 移動方向を正規化して、その大きさを1に制限する
        moveDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// 操作可能状態を変更する
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;

        if(!canControl) { moveDirection = Vector2.zero; }
    }

    /// <summary>
    /// 移動方向を強制的にリセットする。
    /// </summary>
    public void ResetMoveDirection()
    {
        moveDirection = Vector2.zero;
    }
}
