/*********************************************************************/
// * \file   SceneStartEvent.cs
// * \brief  シーンが始まったときに、条件を満たしていればイベントを実行する
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// シーンが始まったときに、条件を満たしていれば EventSequence を実行する
/// </summary>
///
/// ・OP（はじめから のときだけ）、ステージに初めて来たときの会話などに使う
/// ・「1回だけ」にしたいときは、イベントの最後でフラグを立て、条件に「そのフラグが OFF」を入れる
/// ・条件はシーンが始まった瞬間に調べる（フラグ・アイテムの条件向け）
/// ・「最初から暗転しておく」をオンにすると、マップが一瞬見えることなく真っ暗から始まる
///   （イベントの中で「画面の暗転」の部品で明転する）
public class SceneStartEvent : MonoBehaviour
{
    [SerializeField, Label("実行するイベント")]
    [Tooltip("実行する EventSequence（空なら同じオブジェクトのもの）")]
    private EventSequence sequence;

    [SerializeField, Label("条件")]
    [Tooltip("すべて満たしたときだけ実行する（空ならいつも）。例：フラグ「OP を見た」が OFF")]
    private GameCondition condition = new GameCondition();

    [SerializeField, Label("始めるまでの時間（秒）", 0.0f)]
    [Tooltip("シーンに着いてから、イベントを始めるまで待つ時間")]
    private float delay = 0.0f;

    [SerializeField, Label("最初から暗転しておく")]
    [Tooltip("条件を満たしたとき、シーンの最初から画面を真っ暗にしておく（イベントの部品「画面の暗転」の明転で明るくする）")]
    private bool startDark = false;

    private bool willPlay;

    private void Awake()
    {
        if(sequence == null) { sequence = GetComponent<EventSequence>(); }

        willPlay = sequence != null && (condition == null || condition.IsMet());
        if(willPlay && startDark) { ScreenCurtain.Fade(1.0f, 0.0f, Color.black); }

        if(sequence == null) { Debug.LogWarning($"{name}: 実行するイベント（EventSequence）がありません", this); }
    }

    private IEnumerator Start()
    {
        if(!willPlay) { yield break; }

        // プレイヤーの生成など、ほかの Start が終わるのを待つ
        yield return null;
        if(delay > 0.0f) { yield return new WaitForSeconds(delay); }

        sequence.Play(gameObject);
    }
}
