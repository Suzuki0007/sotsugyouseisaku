/*********************************************************************/
// * \file   DarknessEventAction.cs
// * \brief  イベントの部品「暗さ・視界を変える」（StageDarkness の暗さとプレイヤーの明かりの広さ）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「暗さ・視界を変える」
/// </summary>
///
/// ・ランタンを拾ったら部屋が少し明るくなり、プレイヤーの周りが照らされる、など
/// ・シーンの StageDarkness の値を変える（StageDarkness がないシーンでは何もしない）
/// ・セーブから戻ったとき（完了したイベント）は、変えたあとの値にすぐ戻す
public class DarknessEventAction : EventAction
{
    [SerializeField, Label("暗さを変える")]
    private bool changeDarkness = true;

    [SerializeField, Label("暗さ", 0.0f, 1.0f)]
    [Tooltip("0 で普段の明るさ、1 で真っ暗（StageDarkness の Darkness と同じ）")]
    private float darkness = 0.85f;

    [SerializeField, Label("視界を変える")]
    private bool changeRadius = true;

    [SerializeField, Label("プレイヤーの明かりの半径", 0.0f)]
    [Tooltip("プレイヤーの周りの明るい範囲（マス。StageDarkness の Radius と同じ）")]
    private float radius = 3.0f;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("0 ならすぐに変わる")]
    private float duration = 1.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public override void Execute(GameObject gameObject)
    {
        StageDarkness stage = StageDarkness.Active;
        if(stage == null) { return; }

        if(duration <= 0.0f) { ApplyFinal(stage); return; }
        stage.StartCoroutine(Fade(stage));
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        StageDarkness stage = StageDarkness.Active;
        if(stage == null) { yield break; }

        if(duration <= 0.0f) { ApplyFinal(stage); yield break; }

        Coroutine fade = stage.StartCoroutine(Fade(stage));
        if(waitUntilDone) { yield return fade; }
    }

    public override void Restore(GameObject gameObject)
    {
        StageDarkness stage = StageDarkness.Active;
        if(stage != null) { ApplyFinal(stage); }
    }

    // 暗さと半径を同時に変える（StageDarkness の SetDarkness・SetRadius は、続けて呼ぶと前の変化を止めるので、ここで少しずつ変える）
    private IEnumerator Fade(StageDarkness stage)
    {
        float fromDarkness = stage.Darkness;
        float fromRadius = stage.Radius;
        float time = 0.0f;

        while(time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            if(changeDarkness) { stage.SetDarkness(Mathf.Lerp(fromDarkness, darkness, t)); }
            if(changeRadius)   { stage.SetRadius(Mathf.Lerp(fromRadius, radius, t)); }
            yield return null;
        }

        ApplyFinal(stage);
    }

    private void ApplyFinal(StageDarkness stage)
    {
        if(changeDarkness) { stage.SetDarkness(darkness); }
        if(changeRadius)   { stage.SetRadius(radius); }
    }
}
