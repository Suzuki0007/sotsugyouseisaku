/*********************************************************************/
// * \file   EnemyCommandEventAction.cs
// * \brief  イベントの部品「敵に命令」（止める・動かす・段階を変える・倒す など）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵に命令する
/// </summary>
public enum EnemyCommand
{
    [InspectorName("止める（ずっと）")]
    Stop = 0,

    [InspectorName("○秒だけ止める")]
    StopFor = 1,

    [InspectorName("動かす（止めたのを戻す）")]
    Resume = 2,

    [InspectorName("今の行動をやめさせる")]
    Interrupt = 3,

    [InspectorName("段階を変える")]
    SetPhase = 4,

    [InspectorName("倒す（倒されたときの演出あり）")]
    Kill = 5,

    [InspectorName("消す（演出なし）")]
    Remove = 6,
}

/// <summary>
/// イベントの部品「敵に命令」
/// </summary>
///
/// ・ギミックを解いたら敵が止まる、攻撃の発生源（見えない敵）が弱くなる・止まる、などに使う
/// ・ItemInteraction・EventZone などの EventSequence に、ほかの部品と同じように付ける
/// ・セーブから戻ったとき（ItemInteraction の完了の復元）は、ずっと続く命令（止める・動かす・段階・倒す・消す）だけをやり直す
public class EnemyCommandEventAction : EventAction
{
    [SerializeField]
    [Tooltip("命令する敵（シーンに置いた敵）")]
    private List<Enemy> enemies = new List<Enemy>();

    [SerializeField]
    [Tooltip("命令する攻撃の発生源（見えない敵）")]
    private List<AttackEmitter> emitters = new List<AttackEmitter>();

    [SerializeField, Label("命令")]
    private EnemyCommand command = EnemyCommand.Stop;

    [SerializeField, Label("秒数", 0.0f)]
    [Tooltip("「○秒だけ止める」のときの秒数")]
    private float seconds = 3.0f;

    [SerializeField, Label("段階の番号", 1)]
    [Tooltip("「段階を変える」のとき。行動パターンの段階の番号（1 から）")]
    private int phase = 2;

    [SerializeField, Label("止まっている間の色を変える")]
    private bool tintWhenStopped = true;

    [SerializeField, Label("止まっている間の色")]
    [Tooltip("止まっていることが見て分かるように、敵の絵にかける色")]
    private Color stoppedTint = new Color(0.55f, 0.6f, 0.85f, 1.0f);

    [SerializeField, Label("エフェクト")]
    [Tooltip("命令したときに、それぞれの敵の位置に出す")]
    private EffectData effect;

    [SerializeField, Label("SE")]
    private SeData se;

    public override void Execute(GameObject gameObject)
    {
        Apply(false);
    }

    public override void Restore(GameObject gameObject)
    {
        // 一時的な命令（○秒だけ止める・今の行動をやめさせる）は戻さない
        if(command == EnemyCommand.StopFor || command == EnemyCommand.Interrupt) { return; }
        Apply(true);
    }

    private void Apply(bool restoring)
    {
        Color? tint = tintWhenStopped ? stoppedTint : (Color?)null;

        foreach(Enemy enemy in enemies)
        {
            if(enemy == null) { continue; }
            if(!restoring) { PlayFeedback(enemy.transform.position); }

            switch(command)
            {
                case EnemyCommand.Stop:      enemy.SetPaused(true, tint); break;
                case EnemyCommand.StopFor:   enemy.PauseFor(seconds, tint); break;
                case EnemyCommand.Resume:    enemy.SetPaused(false); break;
                case EnemyCommand.Interrupt: if(enemy.Brain != null) { enemy.Brain.Interrupt(); } break;
                case EnemyCommand.SetPhase:  if(enemy.Brain != null) { enemy.Brain.SetPhase(phase - 1); } break;
                case EnemyCommand.Kill:      if(restoring) { enemy.Remove(); } else { enemy.Kill(); } break;
                case EnemyCommand.Remove:    enemy.Remove(); break;
            }
        }

        foreach(AttackEmitter emitter in emitters)
        {
            if(emitter == null) { continue; }
            if(!restoring) { PlayFeedback(emitter.transform.position); }

            switch(command)
            {
                case EnemyCommand.Stop:      emitter.SetStopped(true); break;
                case EnemyCommand.StopFor:   emitter.StopFor(seconds); break;
                case EnemyCommand.Resume:    emitter.SetStopped(false); break;
                case EnemyCommand.Interrupt: emitter.Interrupt(); break;
                case EnemyCommand.SetPhase:  emitter.SetPhase(phase - 1); break;
                case EnemyCommand.Kill:
                case EnemyCommand.Remove:    Destroy(emitter.gameObject); break;
            }
        }
    }

    private void PlayFeedback(Vector3 position)
    {
        EffectManager.Play(effect, position);
        SeManager.PlaySe(se, position);
    }
}
