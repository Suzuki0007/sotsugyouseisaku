/*********************************************************************/
// * \file   SendSignalEventAction.cs
// * \brief  イベントの部品「合図を出す」
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントの部品「合図を出す」
/// </summary>
///
/// ・会話のあと・仕掛けを解いたときなどに、自分で決めた合図を出す。受けるのは「合図を受けたら」と相棒の一言
/// ・出した物（合図の送り元）は、この部品のオブジェクト（受ける側の「送り元の絞り込み」に使う）
public class SendSignalEventAction : EventAction
{
    [SerializeField, Label("出す合図")]
    [Tooltip("出す合図（Assets/Data/Signals/ の合図）")]
    private GameSignal signal;

    [SerializeField, Label("数")]
    [Tooltip("合図に付ける数（受ける側で使うときだけ。ふだんは 0）")]
    private float value;

    public GameSignal Signal => signal;

    public override void Execute(GameObject player)
    {
        Signals.Send(signal, SignalContext.From(gameObject, value));
    }
}
