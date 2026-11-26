/*********************************************************************/
// * \file   MoodChangeEventAction.cs
// * \brief  イベントの部品「色調を変える」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「色調を変える」（画面全体の色で場面の気持ちを変える）
/// </summary>
///
/// ・色調のプリセット（夜・一人／温かい日常／喪失／回想／夜明け など）へ、指定の秒数でなめらかに変える
/// ・会話・メニュー・HUD の色は変わらない
/// ・回想の中で使うと、回想から戻ったときに回想に入る前の色調へ戻る
/// ・ロードしたときは、セーブしたときの色調に戻る
public class MoodChangeEventAction : EventAction
{
    [SerializeField, Label("色調")]
    [Tooltip("変える先の色調のプリセット（Assets/Resources/Presentation/Mood/）。空なら「通常」（何も加工しない）")]
    private MoodPreset preset;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("0 ならすぐに変わる")]
    private float seconds = 2.0f;

    [SerializeField, Label("変わり方")]
    private PresentationCurve curve = PresentationCurve.Smooth;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public MoodPreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        PresentationDirector.SetMood(PresentationDirector.EventLayer, preset, seconds, curve);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        while(PresentationDirector.IsMoodBlending) { yield return null; }
    }
}
