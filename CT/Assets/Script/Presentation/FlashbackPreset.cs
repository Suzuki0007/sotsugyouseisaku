/*********************************************************************/
// * \file   FlashbackPreset.cs
// * \brief  回想のプリセット：過去の場面の見た目（色調・天候・光・回想中フラグ・入り方・コマ落ち）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 回想のプリセット（過去の場面を、今と違う見た目にする）
/// </summary>
///
/// ・部品「回想に入る」で選ぶ。暗転せずに、色調・天候・光が回想の物に変わる
/// ・回想の間は回想中フラグが立つ（「フラグでなめらかに出す」で三人の頃の家具・人影を出す）
/// ・部品「回想から戻る」で、入る前の色調・光・天候に戻る
/// ・置き場所は Assets/Resources/Presentation/Flashback/
[CreateAssetMenu(menuName = "CT/演出/回想のプリセット", fileName = "Flashback_")]
public class FlashbackPreset : ScriptableObject
{
    public enum WeatherUse
    {
        [InspectorName("入る前のまま")]
        Keep = 0,

        [InspectorName("止める")]
        None = 1,

        [InspectorName("下の天候にする")]
        Use = 2,
    }

    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [SerializeField, Label("色調")]
    [Tooltip("回想の間の色調（ふつうは「回想」）。空なら色調は変えない")]
    private MoodPreset mood;

    [SerializeField, Label("光")]
    [Tooltip("回想の間の光。空なら入る前のまま")]
    private LightPreset light;

    [SerializeField, Label("天候をどうするか")]
    private WeatherUse weatherUse = WeatherUse.Use;

    [SerializeField, Label("天候")]
    [Tooltip("「下の天候にする」のときの天候（光の粒など）")]
    private WeatherPreset weather;

    [SerializeField, Label("回想中に立てるフラグ")]
    [Tooltip("回想の間だけ ON（セーブしないフラグ。ふつうは Flag_Flashback_Active）")]
    private GameFlag flag;

    [SerializeField, Label("入り方")]
    private FlashbackTransition transition = FlashbackTransition.GrayThenTint;

    [SerializeField, Label("コマ落ち（1秒のコマ数）", 0.0f, 30.0f)]
    [Tooltip("回想の間、キャラ・物のアニメ（SpriteAnimator）を1秒にこのコマ数でだけ動かす（少しカクつく）。0 で落とさない")]
    private float frameStepFps = 8.0f;

    public string Memo => memo;
    public MoodPreset Mood => mood;
    public LightPreset Light => light;
    public WeatherUse Weather => weatherUse;
    public WeatherPreset WeatherPreset => weather;
    public GameFlag Flag => flag;
    public FlashbackTransition Transition => transition;
    public float FrameStepFps => frameStepFps;

#if UNITY_EDITOR
    public void EditorSet(string memoText, MoodPreset m, LightPreset l, WeatherUse wu, WeatherPreset w, GameFlag f, FlashbackTransition tr, float step)
    {
        memo = memoText; mood = m; light = l; weatherUse = wu; weather = w; flag = f; transition = tr; frameStepFps = step;
    }
#endif
}
