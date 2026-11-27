/*********************************************************************/
// * \file   PresentationValueEventAction.cs
// * \brief  イベントの部品「演出の数値を変える」
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントの部品「演出の数値を変える」（掘った回数・巻いた割合などを入れる・足す）
/// </summary>
///
/// ・名前つきの数（演出の数値）を、決めた値にする・足す。「演出の数値で動かす」を付けた物が、その値で動く
/// ・「セーブする」にすると、ロードしたあとも残る
public class PresentationValueEventAction : EventAction
{
    public enum Mode
    {
        [InspectorName("この値にする")]
        Set = 0,

        [InspectorName("この値を足す")]
        Add = 1,
    }

    [SerializeField, Label("数値の名前")]
    [Tooltip("例：掘った回数・巻いた割合・色の戻り（同じ名前を使う部品どうしで値が渡る）")]
    private string valueName = "";

    [SerializeField, Label("どうするか")]
    private Mode mode = Mode.Add;

    [SerializeField, Label("値")]
    private float value = 1.0f;

    [SerializeField, Label("セーブする")]
    [Tooltip("オンにすると、ロードしたあともこの値が残る")]
    private bool save = false;

    public string ValueName => valueName;

    public override void Execute(GameObject gameObject)
    {
        if(mode == Mode.Set) { PresentationValues.Set(valueName, value, save); }
        else { PresentationValues.Add(valueName, value, save); }
    }
}
