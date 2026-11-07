/*********************************************************************/
// * \file   EventSequence.cs
// * \brief  イベントシーケンスのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// イベントのシーケンスを管理するクラス
/// </summary>
/// 
/// CreateAssetMenu属性を使用して、
/// Unityのアセットメニューに「Event/EventSequence」というメニュー項目を追加する
[CreateAssetMenu(menuName = "Event/EventSequence")]
public class EventSequence : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private List<EventAction> actions = new List<EventAction>();

    //=====================================================================
    // アクションの実行関数
    //=====================================================================

    /// <summary>
    /// アクションを実行する関数
    /// </summary>
    /// <param name="gameObject"> 実行するGameObject </param>
    public void Play(GameObject gameObject)
    {
        foreach(EventAction action in actions)
        {
            if(action == null) { continue; }
            action.Execute(gameObject);
        }
    }

    /// <summary>
    /// アクションが終了したときに呼ばれる関数
    /// </summary>
    /// <param name="gameObject"></param>
    public void Exit(GameObject gameObject)
    {
        foreach(EventAction action in actions)
        {
            if(action == null) { continue; }
            action.OnExit(gameObject);
        }
    }

    /// <summary>
    /// セーブから復元するときに呼ばれる関数（見た目だけを実行後の状態にする）
    /// </summary>
    /// <param name="gameObject"></param>
    public void Restore(GameObject gameObject)
    {
        foreach(EventAction action in actions)
        {
            if(action == null) { continue; }
            action.Restore(gameObject);
        }
    }

    /// <summary>
    /// アクションを追加する（エディタのひな形作成用）
    /// </summary>
    public void AddAction(EventAction action)
    {
        if(action == null || actions.Contains(action)) { return; }
        actions.Add(action);
    }
}
