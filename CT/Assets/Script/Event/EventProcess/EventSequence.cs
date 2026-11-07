/*********************************************************************/
// * \file   EventSequence.cs
// * \brief  イベントシーケンスのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベントのシーケンスを管理するクラス
/// </summary>
///
/// ・順番に実行：Actions を上から順に実行し、会話や待機など「終わるまで待つ」部品は終わってから次へ進む
/// ・すべて同時に実行：Actions を一度に実行する（以前の動き）
/// ・Actions が空なら、同じオブジェクトに付いている EventAction を上から順に使う
public class EventSequence : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum PlayMode
    {
        [InspectorName("順番に実行（会話などが終わるのを待つ）")]
        Sequential,

        [InspectorName("すべて同時に実行")]
        Simultaneous,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("実行のしかた")]
    private PlayMode playMode = PlayMode.Sequential;

    [SerializeField, Tooltip("実行中はプレイヤーを動けなくする（演出の待ち時間に歩き回らせたくないとき）")]
    private bool lockPlayer = false;

    [SerializeField, Tooltip("実行する部品。上から順に実行する（空なら同じオブジェクトの部品を上から順に使う）")]
    private List<EventAction> actions = new List<EventAction>();

    private Coroutine running;

    // プレイヤーを止めているシーケンスの数
    private static int lockCount;

    private bool isLocking;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// 実行中かどうか
    /// </summary>
    public bool IsPlaying => running != null;

    /// <summary>
    /// プレイヤーを止めるシーケンスが実行中か（プレイヤーの操作側はこれを見て入力を止める）
    /// </summary>
    public static bool IsBusy => lockCount > 0;

    //=====================================================================
    // アクションの実行関数
    //=====================================================================

    /// <summary>
    /// アクションを実行する関数
    /// </summary>
    /// <param name="gameObject"> 実行するGameObject </param>
    public void Play(GameObject gameObject)
    {
        if(playMode == PlayMode.Simultaneous)
        {
            foreach(EventAction action in GetActions())
            {
                if(action == null) { continue; }
                action.Execute(gameObject);
            }
            return;
        }

        // 実行中にもう一度呼ばれたら無視する（会話中に同じイベントが重なるのを防ぐ）
        if(IsPlaying) { return; }

        if(!isActiveAndEnabled)
        {
            Debug.LogWarning($"{name}: 非表示のオブジェクトの EventSequence は順番に実行できません", this);
            return;
        }

        running = StartCoroutine(PlayRoutine(gameObject));
    }

    private IEnumerator PlayRoutine(GameObject gameObject)
    {
        SetLock(lockPlayer);

        // 実行中に Actions が変わっても大丈夫なようにコピーしておく
        List<EventAction> list = new List<EventAction>(GetActions());
        foreach(EventAction action in list)
        {
            if(action == null) { continue; }
            yield return action.Run(gameObject);
        }

        SetLock(false);
        running = null;
    }

    /// <summary>
    /// アクションが終了したときに呼ばれる関数
    /// </summary>
    /// <param name="gameObject"></param>
    public void Exit(GameObject gameObject)
    {
        foreach(EventAction action in GetActions())
        {
            if(action == null) { continue; }
            action.OnExit(gameObject);
        }
    }

    /// <summary>
    /// セーブから復元するときに呼ばれる関数（見た目だけを実行後の状態にする。待機はしない）
    /// </summary>
    /// <param name="gameObject"></param>
    public void Restore(GameObject gameObject)
    {
        foreach(EventAction action in GetActions())
        {
            if(action == null) { continue; }
            action.Restore(gameObject);
        }
    }

    /// <summary>
    /// 実行中のシーケンスを止める
    /// </summary>
    public void Stop()
    {
        if(running != null) { StopCoroutine(running); running = null; }
        SetLock(false);
    }

    /// <summary>
    /// アクションを追加する（エディタのひな形作成用）
    /// </summary>
    public void AddAction(EventAction action)
    {
        if(action == null || actions.Contains(action)) { return; }
        actions.Add(action);
    }

    //=====================================================================
    // 補助
    //=====================================================================

    /// <summary>
    /// 実行する部品（Actions が空なら同じオブジェクトの部品）
    /// </summary>
    private IList<EventAction> GetActions()
    {
        if(actions.Count > 0) { return actions; }

        return GetComponents<EventAction>();
    }

    private void SetLock(bool value)
    {
        if(isLocking == value) { return; }

        isLocking = value;
        lockCount += value ? 1 : -1;
        if(lockCount < 0) { lockCount = 0; }
    }

    // 実行中に非表示・破棄されるとコルーチンが止まるので、ロックを解除しておく
    private void OnDisable()
    {
        running = null;
        SetLock(false);
    }

    // ドメインリロードを無効にしていても、再生のたびに数え直す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        lockCount = 0;
    }
}
