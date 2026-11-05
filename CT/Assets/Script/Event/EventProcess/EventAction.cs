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
    /// アクションを実行する仮想関数
    /// </summary>
    /// <param name="gameObject"></param>
    public abstract void Execute(GameObject gameObject);

    /// <summary>
    /// アクションが終了したときに呼ばれる仮想関数
    /// </summary>
    /// <param name="gameObject"></param>
    public abstract void OnExit(GameObject gameObject);
}
