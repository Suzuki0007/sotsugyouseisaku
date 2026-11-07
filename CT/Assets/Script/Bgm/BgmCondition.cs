/*********************************************************************/
// * \file   BgmCondition.cs
// * \brief  BGM切り替え条件の基底クラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// BGMを切り替える条件の基底クラス
/// 継承して IsMet() を実装すると、SceneBgm のルールに設定できる
/// </summary>
public abstract class BgmCondition : MonoBehaviour
{
    [SerializeField, Tooltip("チェックすると条件を反転する (〜でないとき)")]
    private bool invert;

    /// <summary>
    /// 条件を満たしているか (反転を適用した結果)
    /// </summary>
    public bool Evaluate()
    {
        return IsMet() != invert;
    }

    /// <summary>
    /// 条件を満たしているかを返す純粋仮想関数
    /// </summary>
    protected abstract bool IsMet();
}
