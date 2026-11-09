/*********************************************************************/
// * \file   DeathEventAction.cs
// * \brief  イベントで即死させるアクション（やり直す／ゲームオーバー）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 即死
/// </summary>
///
/// 例：選択を間違えた会話のあと ／ 時計が 0 時になったら ／ 答え合わせを間違えたとき
/// ・この部品の前に並べた演出（会話・SE）は、そのまま流れてから即死する
public class DeathEventAction : EventAction
{
    [SerializeField, Label("即死したあと")]
    private DeathResult result = DeathResult.Retry;

    public override void Execute(GameObject gameObject)
    {
        // イベントの EventSequence が終わるのを待ってから暗転する
        InstantDeath.Kill(this, result, null);
    }
}
