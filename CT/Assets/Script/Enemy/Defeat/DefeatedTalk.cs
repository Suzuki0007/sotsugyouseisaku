/*********************************************************************/
// * \file   DefeatedTalk.cs
// * \brief  倒した敵を調べたとき（会話・アイテムをもらう など）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 倒した敵を調べる
/// </summary>
///
/// ・敵（Enemy）の「倒した後に調べたとき」が入っているとき、倒れた姿で残る・おとなしくなる敵に自動で付く
/// ・E で調べると、そのイベント（会話・ItemGive など）を実行する
public class DefeatedTalk : Intaractable
{
    private EventSequence sequence;

    public static DefeatedTalk Attach(Enemy enemy, EventSequence sequence, float radius)
    {
        var go = new GameObject("調べる範囲（倒した後）");
        go.transform.SetParent(enemy.transform, false);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        var talk = go.AddComponent<DefeatedTalk>();
        talk.sequence = sequence;
        return talk;
    }

    protected override void Interact()
    {
        if(sequence != null) { sequence.Play(gameObject); }
    }
}
