/*********************************************************************/
// * \file   ColorRestoreEventAction.cs
// * \brief  イベントの部品「色の戻り具合を変える」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「色の戻り具合を変える」（直した数だけ、層全体の色が少しずつ戻る）
/// </summary>
///
/// ・シーンの演出の既定の「色が戻ったときの色調・光」へ、割合（0〜1）だけ近づける。音楽生物を 1 体直すたびに 0.25 足す、など
/// ・セーブに残る（ロードしても戻った色のまま）
/// ・部品「色調を変える」「光を変える」で変えている間は、そちらが勝つ
public class ColorRestoreEventAction : EventAction
{
    public enum Mode
    {
        [InspectorName("この割合を足す")]
        Add = 0,

        [InspectorName("この割合にする")]
        Set = 1,
    }

    [SerializeField, Label("どうするか")]
    private Mode mode = Mode.Add;

    [SerializeField, Label("割合", 0.0f, 1.0f)]
    [Tooltip("足す・する割合（1 で色が戻りきる）")]
    private float amount = 0.25f;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    private float seconds = 2.0f;

    [SerializeField, Label("終わるまで待つ")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        PresentationDirector.SetRestoreAmount(amount, mode == Mode.Add, seconds);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        while(PresentationDirector.IsMoodBlending || PresentationDirector.IsLightBlending) { yield return null; }
    }
}
