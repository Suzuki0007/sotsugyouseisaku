/*********************************************************************/
// * \file   BgmConditionHealth.cs
// * \brief  体力が一定以下のときのBGM条件
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 体力の割合が指定値以下のときに満たす条件 (ピンチBGMなど)
/// target が空なら、操作中のキャラクターの Health を使う
/// </summary>
public class BgmConditionHealth : BgmCondition
{
    [SerializeField, Tooltip("対象の体力 (空なら操作中のプレイヤー)")]
    private Health target;

    [SerializeField, Tooltip("この割合以下で条件を満たす")]
    [Range(0.0f, 1.0f)]
    private float threshold = 0.3f;

    [SerializeField, Tooltip("死亡中も条件を満たすか")]
    private bool includeDead = false;

    protected override bool IsMet()
    {
        Health health = GetTarget();
        if(health == null || health.MaxHealth <= 0) { return false; }
        if(health.IsDead && !includeDead) { return false; }

        return (float)health.CurrentHealth / health.MaxHealth <= threshold;
    }

    private Health GetTarget()
    {
        if(target != null) { return target; }

        if(PlayerSwitchManager.Instance == null) { return null; }

        GameObject player = PlayerSwitchManager.Instance.CurrentPlayer;
        return player != null ? player.GetComponent<Health>() : null;
    }
}
