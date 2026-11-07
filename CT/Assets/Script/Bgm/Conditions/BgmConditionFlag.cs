/*********************************************************************/
// * \file   BgmConditionFlag.cs
// * \brief  フラグが立っているときのBGM条件
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 指定したフラグが立っているときに満たす条件
/// ・Flag（GameFlag アセット）を入れたらそれを使う（おすすめ）
/// ・空なら Flag Name（BgmFlags の名前）を使う（旧方式）
/// </summary>
public class BgmConditionFlag : BgmCondition
{
    [SerializeField, Tooltip("判定するフラグ（GameFlag アセット）。入れたら下の Flag Name は使わない")]
    private GameFlag flag;

    [SerializeField, Tooltip("旧方式のフラグ名 (例: Boss)")]
    private string flagName;

    protected override bool IsMet()
    {
        if(flag != null) { return flag.Value; }

        return BgmFlags.Get(flagName);
    }
}
