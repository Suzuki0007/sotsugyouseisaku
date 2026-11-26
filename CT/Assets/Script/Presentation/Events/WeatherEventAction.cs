/*********************************************************************/
// * \file   WeatherEventAction.cs
// * \brief  イベントの部品「天候を変える」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「天候を変える」（雨・雪・砂ぼこり・霧・光の粒・花びら）
/// </summary>
///
/// ・天候のプリセットへ、指定の秒数で量をなめらかに増やす・減らす（今の天候は同じ秒数で減っていく）
/// ・「なし」で止める。「シーンの既定に戻す」で、シーンの演出の既定の天候へ戻す
/// ・天候の範囲（室内など）の中では、範囲の天候のほうが勝つ（出ると、ここで変えた天候になる）
/// ・回想の中で使うと、回想から戻ったときに回想に入る前の天候へ戻る。ロードしたときは、セーブしたときの天候に戻る
public class WeatherEventAction : EventAction
{
    public enum Mode
    {
        [InspectorName("指定した天候にする")]
        Set = 0,

        [InspectorName("止める（なし）")]
        None = 1,

        [InspectorName("シーンの既定に戻す")]
        Reset = 2,
    }

    [SerializeField, Label("どうするか")]
    private Mode mode = Mode.Set;

    [SerializeField, Label("天候")]
    [Tooltip("「指定した天候にする」のときの天候のプリセット（Assets/Resources/Presentation/Weather/）")]
    private WeatherPreset preset;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("量を増やす・減らす時間。0 ならすぐ（最初から降っている）")]
    private float seconds = 3.0f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、量が変わり終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    public Mode CurrentMode => mode;
    public WeatherPreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        switch(mode)
        {
            case Mode.Set:   PresentationDirector.SetWeather(PresentationDirector.EventLayer, preset, seconds); break;
            case Mode.None:  PresentationDirector.SetWeather(PresentationDirector.EventLayer, null, seconds); break;
            case Mode.Reset: PresentationDirector.ResetWeatherForEvent(seconds); break;
        }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }
        yield return null;
        while(PresentationDirector.IsWeatherBlending) { yield return null; }
    }
}
