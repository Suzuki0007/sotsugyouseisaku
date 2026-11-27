/*********************************************************************/
// * \file   SkyEventAction.cs
// * \brief  イベントの部品「空を変える」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「空を変える」（塔の外の空：黒・青空・暗い空・夜明け・夕焼け・夜…）
/// </summary>
///
/// ・マップの外に見える空を、指定の秒数でなめらかに変える（暗い空 → 夜明け など）
/// ・「なし」で空を消す（カメラの背景の色に戻る）。「シーンの既定に戻す」で、シーンの演出の既定の「外側の空」へ
/// ・ロードしたときは、セーブしたときの空に戻る
public class SkyEventAction : EventAction
{
    public enum Mode
    {
        [InspectorName("指定した空にする")]
        Set = 0,

        [InspectorName("空を消す（なし）")]
        None = 1,

        [InspectorName("シーンの既定に戻す")]
        Reset = 2,
    }

    [SerializeField, Label("どうするか")]
    private Mode mode = Mode.Set;

    [SerializeField, Label("空")]
    [Tooltip("「指定した空にする」のときの空のプリセット（Assets/Resources/Presentation/Sky/）")]
    private SkyPreset preset;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("空が変わりきるまでの秒数。0 ならすぐ")]
    private float seconds = 4.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、空が変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public Mode CurrentMode => mode;
    public SkyPreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        switch(mode)
        {
            case Mode.Set:   PresentationDirector.SetEventSky(preset, seconds); break;
            case Mode.None:  PresentationDirector.SetEventSky(null, seconds); break;
            case Mode.Reset: PresentationDirector.ResetSkyForEvent(seconds); break;
        }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        yield return null;
        while(PresentationDirector.IsSkyBlending) { yield return null; }
    }
}
