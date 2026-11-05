/*********************************************************************/
// * \file   CharacterMovement.cs
// * \brief  キャラクターの移動クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キャラクターの移動クラス
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMovement : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private float moveSpeed = 5f;

    private Rigidbody2D rb;

    private Vector2 moveDirection;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public Vector2 MoveDirection => moveDirection;

    //=====================================================================
    // 初期化
    //=====================================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    //=====================================================================
    // 移動処理
    //=====================================================================

    protected virtual void FixedUpdate()
    {
        if(rb == null) { return; }
        if(moveDirection == Vector2.zero) { return; }

        Vector2 nextPosition =
            rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

    /// <summary>
    /// 移動方向を設定する関数
    /// </summary>
    /// <param name="direction"></param>
    public virtual void SetMoveDirection(Vector2 direction)
    {
        // 移動方向を正規化して設定する
        moveDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// 移動を停止する関数
    /// </summary>
    public virtual void Stop()
    {
        moveDirection = Vector2.zero;
    }
}
