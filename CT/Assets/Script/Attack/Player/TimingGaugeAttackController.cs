/*********************************************************************/
// * \file   TimingGaugeAttackController.cs
// * \brief  タイミングゲージ攻撃コントローラークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// タイミングゲージ攻撃コントローラークラス
/// </summary>
public class TimingGaugeAttackController : AttackController
{
    //=====================================================================
    // 変数
    //=====================================================================

    private TimingGaugeUI timingGauge;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 攻撃を開始する
    /// </summary>
    protected override void StartAttack()
    {
        if(timingGauge.gameObject.activeSelf) { return; }

        timingGauge.StartGauge();
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetTimingGauge(TimingGaugeUI newTimingGauge)
    {
        timingGauge = newTimingGauge;
        if(timingGauge == null) { Debug.LogError("タイミングゲージの設定に失敗しました"); }
    }
}
