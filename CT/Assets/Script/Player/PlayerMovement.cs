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
public class PlayerMovement : CharacterMovement
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("操作設定")]

    [SerializeField] 
    private bool canControl = false;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool CanControl => canControl;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 操作可能状態を変更する
    /// </summary>
    /// 操作できなくなったときは、その場で止める
    /// （移動の向きは操作中のキャラにだけ毎フレーム入れ直すので、消さないと切り替えた後も歩き続ける）
    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        if(!enabled) { Stop(); ClearVelocity(); }
    }

    //=====================================================================
    // 移動処理
    //=====================================================================

    /// <summary>
    /// プレイヤーは物理の速さを使わない（動きは MovePosition と追従だけ）
    /// ぶつかって付いた速さが残ると滑り続けるので、毎回 0 に戻してから動かす
    /// </summary>
    protected override void FixedUpdate()
    {
        ClearVelocity();
        base.FixedUpdate();
    }

    /// <summary>
    /// 行き先をマップの範囲内に収める（端の外へ出てから戻されると、ついてくる相棒が震える）
    /// </summary>
    protected override Vector2 LimitNextPosition(Vector2 nextPosition)
    {
        if(mapLimit == null) { mapLimit = GetComponent<PlayerMapLimit>(); }
        return mapLimit != null ? mapLimit.ClampToMap(nextPosition) : nextPosition;
    }

    private PlayerMapLimit mapLimit;

    private void ClearVelocity()
    {
        if(Rb != null) { Rb.linearVelocity = Vector2.zero; Rb.angularVelocity = 0.0f; }
    }
}
