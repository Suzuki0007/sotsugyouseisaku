/*********************************************************************/
// * \file   GimmickSampleMemo.cs
// * \brief  サンプルの部屋の説明（Inspector で読むだけ。ゲームには出ない）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// サンプルの部屋の説明（Inspector で読むだけ。ゲームには何もしない）
/// </summary>
public class GimmickSampleMemo : MonoBehaviour
{
    [TextArea(3, 10)]
    [Tooltip("この部屋の遊び方と、どの部品をどうつないでいるか")]
    public string text = "";
}
