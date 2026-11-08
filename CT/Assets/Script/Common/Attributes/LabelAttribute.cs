/*********************************************************************/
// * \file   LabelAttribute.cs
// * \brief  Inspector に出す項目名を日本語にする属性
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// Inspector の項目名を変える
/// </summary>
///
/// ・[Label("体力")]                  … 名前だけ
/// ・[Label("速さ", min: 0)]          … 0 より小さくできない（Min の代わり）
/// ・[Label("割合", 0, 1)]            … スライダー（Range の代わり）
/// ・InspectorName は enum の値にしか効かないので、項目（フィールド）にはこれを使う
/// ・Range / Min とは一緒に付けない（どちらか片方の表示になる）。上のように引数で指定する
/// ・リスト（List / 配列）には付けない（中の要素ごとに名前が変わってしまう）。リストは Header と Tooltip で説明する
public class LabelAttribute : PropertyAttribute
{
    public readonly string label;
    public readonly float min;
    public readonly float max;
    public readonly bool hasMin;
    public readonly bool isRange;

    public LabelAttribute(string label)
    {
        this.label = label;
    }

    public LabelAttribute(string label, float min)
    {
        this.label = label;
        this.min = min;
        hasMin = true;
    }

    public LabelAttribute(string label, float min, float max)
    {
        this.label = label;
        this.min = min;
        this.max = max;
        hasMin = true;
        isRange = true;
    }
}
