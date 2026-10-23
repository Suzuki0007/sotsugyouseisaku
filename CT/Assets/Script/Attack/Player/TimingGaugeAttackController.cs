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

    [Header("タイミングゲージの設定")]

    [SerializeField]
    private TimingGaugeUI timingGauge;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(timingGauge == null) { Debug.LogError("タイミングゲージの設定が設定されていません"); }
    }

    /// <summary>
    /// 攻撃を開始する
    /// </summary>
    protected override void StartAttack()
    {
        if(timingGauge.gameObject.activeSelf) { return; }

        timingGauge.StartGauge();
    }
}
