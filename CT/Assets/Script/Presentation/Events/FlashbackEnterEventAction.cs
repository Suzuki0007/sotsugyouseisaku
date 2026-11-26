/*********************************************************************/
// * \file   FlashbackEnterEventAction.cs
// * \brief  イベントの部品「回想に入る」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「回想に入る」（暗転せずに、過去の場面の見た目にする）
/// </summary>
///
/// ・回想のプリセットの色調・光・天候に変わり、回想中フラグが立つ（「フラグでなめらかに出す」で三人の頃の物が現れる）
/// ・入り方：色が抜けてから差す（今の色がいったん抜けて灰色 → 回想の色がにじむ）／ゆっくり溶ける／すぐ
/// ・回想の間の「色調を変える」「光を変える」「天候を変える」は、戻ると取り消される
/// ・回想の途中でセーブすると、回想に入る前の状態で記録される（ロードすると回想の外から）。回想の中にオートセーブを置かない
/// ・「回想から戻る」と対で使う（シーンチェックで確かめる）
public class FlashbackEnterEventAction : EventAction
{
    public enum Entry
    {
        [InspectorName("プリセットのまま")]
        Preset = -1,

        [InspectorName("色が抜けてから差す（おすすめ）")]
        GrayThenTint = 0,

        [InspectorName("ゆっくり溶ける")]
        Dissolve = 1,

        [InspectorName("すぐ")]
        Instant = 2,
    }

    [SerializeField, Label("回想")]
    [Tooltip("回想のプリセット（Assets/Resources/Presentation/Flashback/）")]
    private FlashbackPreset preset;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("回想の見た目に変わるまで")]
    private float seconds = 3.0f;

    [SerializeField, Label("入り方")]
    private Entry entry = Entry.Preset;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、回想の見た目に変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = true;

    public FlashbackPreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        if(preset == null) { Debug.LogWarning("回想に入る：回想のプリセットが空です", this); return; }
        FlashbackTransition t = entry == Entry.Preset ? preset.Transition : (FlashbackTransition)(int)entry;
        PresentationDirector.EnterFlashback(preset, seconds, t);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        yield return null;
        while(PresentationDirector.IsMoodBlending || PresentationDirector.IsLightBlending) { yield return null; }
    }
}
