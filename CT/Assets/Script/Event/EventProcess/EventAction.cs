/*********************************************************************/
// * \file   EventAction.cs
// * \brief  イベントアクションのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント時に実行されるアクションのクラス
/// </summary>
public abstract class EventAction : MonoBehaviour
{
    /// <summary>
    /// アクションを実行する純粋仮想関数
    /// </summary>
    /// <param name="gameObject"></param>
    public abstract void Execute(GameObject gameObject);

    /// <summary>
    /// アクションが終了したときに呼ばれる仮想関数
    /// </summary>
    /// <param name="gameObject"></param>
    public virtual void OnExit(GameObject gameObject) { }

    /// <summary>
    /// セーブから復元するときに呼ばれる仮想関数
    /// </summary>
    /// <param name="gameObject"></param>
    ///
    /// 会話やSEは鳴らさず、見た目（表示・非表示、スプライトなど）だけを
    /// 実行後の状態にしたいアクションがオーバーライドする
    public virtual void Restore(GameObject gameObject) { }
}
