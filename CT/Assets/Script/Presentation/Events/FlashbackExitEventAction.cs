/*********************************************************************/
// * \file   FlashbackExitEventAction.cs
// * \brief  イベントの部品「回想から戻る」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「回想から戻る」（回想に入る前の色調・光・天候へ戻す）
/// </summary>
///
/// ・シーンの既定ではなく、回想に入る直前の見た目に戻る。回想中フラグが消え、回想中だけの物は消えていく
public class FlashbackExitEventAction : EventAction
{
    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("今の見た目に戻るまで")]
    private float seconds = 3.0f;

    [SerializeField, Label("戻り方")]
    private FlashbackTransition transition = FlashbackTransition.Dissolve;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、戻り終わってから次の部品へ進む")]
    private bool waitUntilDone = true;

    public override void Execute(GameObject gameObject)
    {
        PresentationDirector.ExitFlashback(seconds, transition);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        yield return null;
        while(PresentationDirector.IsMoodBlending || PresentationDirector.IsLightBlending) { yield return null; }
    }
}
