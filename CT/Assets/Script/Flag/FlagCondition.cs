/*********************************************************************/
// * \file   FlagCondition.cs
// * \brief  フラグの条件
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フラグの条件（「ボスを倒した が ON」「扉を開けた が OFF」など）
/// </summary>
///
/// ItemInteraction のルールや FlagSwitch で使う
[Serializable]
public class FlagCondition
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum State
    {
        [InspectorName("ON のとき")]
        On,

        [InspectorName("OFF のとき")]
        Off,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("判定するフラグ")]
    public GameFlag flag;

    [Tooltip("ON のときに満たすか、OFF のときに満たすか")]
    public State state = State.On;

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 条件を満たしているか（フラグ未設定なら常に満たす）
    /// </summary>
    public bool IsMet()
    {
        if(flag == null) { return true; }

        return flag.Value == (state == State.On);
    }

    /// <summary>
    /// すべての条件を満たしているか（条件が空なら常に満たす）
    /// </summary>
    public static bool AllMet(List<FlagCondition> conditions)
    {
        if(conditions == null) { return true; }

        foreach(FlagCondition condition in conditions)
        {
            if(condition != null && !condition.IsMet()) { return false; }
        }
        return true;
    }
}
