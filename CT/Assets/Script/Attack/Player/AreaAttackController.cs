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
    private AreaAttack areaAttack;

    [SerializeField]
    private float damageMultiplier = 1.0f;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(areaAttack == null) { Debug.LogError("範囲攻撃の設定が設定されていません"); }
    }

    protected override void StartAttack()
    {
        areaAttack.StartAttack(damageMultiplier);
    }
}
