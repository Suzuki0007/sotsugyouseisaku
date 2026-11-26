/*********************************************************************/
// * \file   LightChangeEventAction.cs
// * \brief  イベントの部品「光を変える」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「光を変える」（2D の光で時間帯を変える）
/// </summary>
///
/// ・光のプリセット（昼／夜／月明かり／夕日／夜明け／塔の中の暗がり など）へ、指定の秒数でなめらかに変える
/// ・全体の光（Global Light 2D）と、シーンの「場面の光」のグループの強さが変わる。画面全体の色の加工はしない（それは色調）
/// ・暗いステージ（StageDarkness）の暗さ・プレイヤーの明かりはそのまま（この光の上に掛かる）
/// ・回想の中で使うと、回想から戻ったときに回想に入る前の光へ戻る。ロードしたときは、セーブしたときの光に戻る
public class LightChangeEventAction : EventAction
{
    [SerializeField, Label("光")]
    [Tooltip("変える先の光のプリセット（Assets/Resources/Presentation/Light/）。空ならシーンの Global Light 2D の値")]
    private LightPreset preset;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("0 ならすぐに変わる")]
    private float seconds = 2.0f;

    [SerializeField, Label("変わり方")]
    private PresentationCurve curve = PresentationCurve.Smooth;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public LightPreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        PresentationDirector.SetLight(PresentationDirector.EventLayer, preset, seconds, curve);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        while(PresentationDirector.IsLightBlending) { yield return null; }
    }
}
