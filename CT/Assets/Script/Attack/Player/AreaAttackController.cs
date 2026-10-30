/*********************************************************************/
// * \file   AreaAttackController.cs
// * \brief  範囲攻撃コントローラークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 範囲攻撃コントローラークラス
/// </summary>
public class AreaAttackController : AttackController
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("範囲攻撃の設定")]

    [SerializeField]
    private float damageMultiplier = 1.0f;

    private AreaAttack areaAttack;

    //=====================================================================
    // 更新関数
    //=====================================================================

    protected override void StartAttack()
    {
        if(areaAttack == null) { return; }

        areaAttack.StartAttack(damageMultiplier);
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetAreaAttack(AreaAttack newAreaAttack)
    {
        areaAttack = newAreaAttack;
        if(areaAttack == null) { Debug.LogError("範囲攻撃の設定に失敗しました"); }
    }
}
