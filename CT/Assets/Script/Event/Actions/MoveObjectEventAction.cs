/*********************************************************************/
// * \file   MoveObjectEventAction.cs
// * \brief  イベントの部品「物を動かす」（上から落ちてくる・横から入ってくる・置いた位置から動かす）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「物を動かす」
/// </summary>
///
/// ・「ずらした位置から今の位置へ」：シーンに置いた位置へ、ずらした所から動いてくる（敵が上から降ってくる、など）
/// ・「今の位置からずらした位置へ」：置いた位置から、ずらした所へ動いていく（扉が開く・岩がどく、など）
/// ・敵を動かすときは、先に「敵に命令」で止めておく（止めないと自分で歩き出す）
public class MoveObjectEventAction : EventAction
{
    public enum MoveType
    {
        [InspectorName("ずらした位置から今の位置へ（降ってくる・入ってくる）")]
        FromOffset = 0,

        [InspectorName("今の位置からずらした位置へ（どく・開く）")]
        ToOffset = 1,

        [InspectorName("今の位置から目的の物へ（飛んでいく。動いている物も追う）")]
        ToTarget = 2,
    }

    public enum Ease
    {
        [InspectorName("一定の速さ")]
        Linear = 0,

        [InspectorName("だんだん速く（落ちる）")]
        EaseIn = 1,

        [InspectorName("だんだん遅く（止まる）")]
        EaseOut = 2,
    }

    public enum TargetKind
    {
        [InspectorName("下で選んだ物")]
        Object = 0,

        [InspectorName("ノア")]
        Noah = 1,

        [InspectorName("シグル")]
        Sigle = 2,
    }

    [SerializeField, Label("動かすもの")]
    [Tooltip("ノア・シグルは再生中に出てくるので、ここで選ぶ（歩くアニメになる。待機・別行動のときに使う）")]
    private TargetKind targetKind = TargetKind.Object;

    [SerializeField, Label("動かす物")]
    private Transform target;

    [SerializeField, Label("動き方")]
    private MoveType type = MoveType.FromOffset;

    [SerializeField, Label("ずらす量（マス）")]
    [Tooltip("上から降ってくるなら Y = 8 など。「目的の物へ」のときは、着く位置のずらし")]
    private Vector2 offset = new Vector2(0.0f, 8.0f);

    [SerializeField, Label("目的")]
    [Tooltip("「今の位置から目的の物へ」のとき、向かう先：下で選んだ物／ノア／シグル（ずらす量で、その横に止まる）")]
    private TargetKind destinationKind = TargetKind.Object;

    [SerializeField, Label("目的の物")]
    [Tooltip("「今の位置から目的の物へ」のとき、向かう先（敵など。動いていても追う）")]
    private Transform destination;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    private float duration = 0.4f;

    [SerializeField, Label("速さの変わり方")]
    private Ease ease = Ease.EaseIn;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、動き終わってから次の部品へ進む")]
    private bool waitUntilDone = true;

    // 置いた位置（最初に動かすときに覚える）
    private Vector3? home;

    public override void Execute(GameObject gameObject)
    {
        ResolveTarget();
        if(target == null) { return; }
        StartCoroutine(MoveRoutine());
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        ResolveTarget();
        if(target == null) { yield break; }

        if(waitUntilDone) { yield return MoveRoutine(); }
        else              { StartCoroutine(MoveRoutine()); }
    }

    // ノア・シグルは毎回探す（再生中に出てくるため）。位置は動かす直前の位置を使う
    private void ResolveTarget()
    {
        if(destinationKind != TargetKind.Object) { destination = Character(destinationKind); }

        if(targetKind == TargetKind.Object) { return; }

        target = Character(targetKind);
        home = null;
    }

    private static Transform Character(TargetKind kind)
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        GameObject chara = manager == null ? null : (kind == TargetKind.Noah ? manager.Noah : manager.Sigle);
        return chara != null ? chara.transform : null;
    }

    public override void Restore(GameObject gameObject)
    {
        // ノア・シグルはセーブから戻るときに動かさない（出現位置で決まる）
        if(targetKind != TargetKind.Object) { return; }
        if(target == null) { return; }

        Vector3 basePos = home ?? target.position;
        switch(type)
        {
            case MoveType.FromOffset: SetPosition(basePos); break;
            case MoveType.ToOffset:   SetPosition(basePos + (Vector3)offset); break;
            default:                  if(destination != null) { SetPosition(destination.position + (Vector3)offset); } break;
        }
    }

    private IEnumerator MoveRoutine()
    {
        if(!home.HasValue) { home = target.position; }

        Vector3 from = type == MoveType.FromOffset ? home.Value + (Vector3)offset : home.Value;

        SetPosition(from);

        float time = 0.0f;
        while(time < duration)
        {
            time += Time.deltaTime;
            SetPosition(Vector3.LerpUnclamped(from, EndPosition(), Apply(Mathf.Clamp01(time / duration))));
            yield return null;
        }

        SetPosition(EndPosition());
    }

    private Vector3 EndPosition()
    {
        switch(type)
        {
            case MoveType.FromOffset: return home.Value;
            case MoveType.ToOffset:   return home.Value + (Vector3)offset;
            default:                  return destination != null ? destination.position + (Vector3)offset : home.Value;
        }
    }

    private float Apply(float t)
    {
        switch(ease)
        {
            case Ease.EaseIn:  return t * t;
            case Ease.EaseOut: return 1.0f - (1.0f - t) * (1.0f - t);
            default:           return t;
        }
    }

    private void SetPosition(Vector3 position)
    {
        position.z = target.position.z;
        target.position = position;

        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if(body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
    }
}
