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

        // タイミングゲージの判定待ちの間は、同じキー（Space）が判定に使われるので通常攻撃は出さない
        // （同時に出すと、ゲージの倍率つきの攻撃が「攻撃中」で弾かれてしまう）
        if(TimingGaugeUI.IsJudging) { return; }

        areaAttack.StartAttack(damageMultiplier);
        PlayAttackSe();
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
