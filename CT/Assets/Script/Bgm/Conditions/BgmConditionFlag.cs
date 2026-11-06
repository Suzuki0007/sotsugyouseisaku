/*********************************************************************/
// * \file   BgmConditionFlag.cs
// * \brief  フラグが立っているときのBGM条件
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 指定した BgmFlags のフラグが立っているときに満たす条件
/// フラグは BgmFlagEventAction (イベントゾーン) やスクリプトから設定する
/// </summary>
public class BgmConditionFlag : BgmCondition
{
    [SerializeField, Tooltip("フラグ名 (例: Boss)")]
    private string flagName;

    protected override bool IsMet()
    {
        return BgmFlags.Get(flagName);
    }
}
