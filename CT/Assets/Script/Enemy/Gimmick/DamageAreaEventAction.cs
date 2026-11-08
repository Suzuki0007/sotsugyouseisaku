/*********************************************************************/
// * \file   DamageAreaEventAction.cs
// * \brief  イベントの部品「ダメージエリアを出す」（攻撃のデータを、指定した場所から出す）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「ダメージエリアを出す」
/// </summary>
///
/// ・攻撃のデータ（AttackData）を、指定した場所から出す。予告・形・発動のしかたはデータのとおり
/// ・「当たる相手：敵」にすると、ギミックで敵を攻撃するエリアになる（例：仕掛けを動かすと床に電気が流れる）
///   向きの「プレイヤーの方向」は、プレイヤーを狙う（敵を狙いたいときは「決まった角度」か、円の形を使う）
/// ・ずっと出しておきたいときは、攻撃の発生源（AttackEmitter）を置いて「敵に命令」で動かす・止めるほうがよい
public class DamageAreaEventAction : EventAction
{
    [SerializeField, Label("攻撃")]
    [Tooltip("出す攻撃（AttackData）。攻撃の種類を「ギミック」にしておくと、敵の効き方で区別できる")]
    private AttackData attack;

    [SerializeField, Label("出す場所")]
    [Tooltip("ここを「敵の位置」として攻撃を出す（右向きが「敵の向いている方向」）。空ならこのオブジェクトの位置")]
    private Transform place;

    [SerializeField, Label("回数", 1)]
    private int repeat = 1;

    [SerializeField, Label("間隔（秒）", 0.0f)]
    [Tooltip("続けて出すときの間隔（攻撃の後隙のあと）")]
    private float interval = 0.5f;

    [SerializeField, Label("終わるまで次の部品を待たせる")]
    [Tooltip("オン：攻撃が全部終わってから、イベントの次の部品へ進む（順番に実行のとき）")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        if(attack == null) { return; }
        StartCoroutine(Play());
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        if(attack == null) { yield break; }

        if(waitUntilDone) { yield return Play(); }
        else              { StartCoroutine(Play()); }
    }

    private IEnumerator Play()
    {
        // 攻撃を出すための、一時的な発生源を作る（終わったら消す）
        Transform at = place != null ? place : transform;
        var go = new GameObject("ダメージエリア（一時的）");
        go.transform.SetPositionAndRotation(at.position, at.rotation);
        var emitter = go.AddComponent<AttackEmitter>();

        // イベント（プレイヤーを止めている間）の中から出すので、敵が止まる条件では止めない
        emitter.IgnoreEnemyBlock = true;

        for(int i = 0; i < Mathf.Max(1, repeat); i++)
        {
            yield return emitter.RunAttack(attack);
            if(i < repeat - 1) { yield return emitter.Wait(interval); }
        }

        Destroy(go);
    }
}
