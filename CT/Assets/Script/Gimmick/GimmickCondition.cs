/*********************************************************************/
// * \file   GimmickCondition.cs
// * \brief  仕掛けの状態の条件（GameCondition の中で使う）
// *
// * \author
/*********************************************************************/

using System;
using UnityEngine;

/// <summary>
/// 仕掛けの状態の比べ方
/// </summary>
public enum GimmickCompare
{
    [InspectorName("ON のとき")]
    On = 0,

    [InspectorName("OFF のとき")]
    Off = 1,

    [InspectorName("この値のとき")]
    Equal = 2,

    [InspectorName("この値でないとき")]
    NotEqual = 3,

    [InspectorName("この値以上のとき")]
    AtLeast = 4,

    [InspectorName("この値以下のとき")]
    AtMost = 5,
}

/// <summary>
/// 仕掛けの状態の条件 1 つ
/// </summary>
[Serializable]
public class GimmickCondition
{
    [Label("仕掛け")]
    [Tooltip("シーンに置いた「仕掛けの状態」（GimmickState）。スイッチ・答え合わせなどをドラッグ")]
    public GimmickState target;

    [Label("比べ方")]
    public GimmickCompare compare = GimmickCompare.On;

    [Label("値", 0)]
    [Tooltip("「この値のとき」などで使う（0 = OFF、1 = ON、ダイヤルなら 0, 1, 2 …）")]
    public int value = 1;

    /// <summary> 満たしているか（仕掛けが空なら満たさない） </summary>
    public bool IsMet()
    {
        if(target == null) { return false; }

        int v = target.Value;
        switch(compare)
        {
            case GimmickCompare.On:       return v != 0;
            case GimmickCompare.Off:      return v == 0;
            case GimmickCompare.Equal:    return v == value;
            case GimmickCompare.NotEqual: return v != value;
            case GimmickCompare.AtLeast:  return v >= value;
            case GimmickCompare.AtMost:   return v <= value;
        }
        return false;
    }

    /// <summary> 説明（配線の表示などに使う） </summary>
    public string Summary
    {
        get
        {
            string name = target != null ? target.DisplayName : "（空）";
            switch(compare)
            {
                case GimmickCompare.On:       return $"{name} が ON";
                case GimmickCompare.Off:      return $"{name} が OFF";
                case GimmickCompare.Equal:    return $"{name} が {value}";
                case GimmickCompare.NotEqual: return $"{name} が {value} でない";
                case GimmickCompare.AtLeast:  return $"{name} が {value} 以上";
                default:                      return $"{name} が {value} 以下";
            }
        }
    }
}
