/*********************************************************************/
// * \file   PresentationStation.cs
// * \brief  演出の台座（試し用）：調べるたびに、色調・光・天候のプリセットを次へ進める／回想・暗さを切り替える／全部戻す
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出の台座（試し用。調べるたびに次のプリセットへ）
/// </summary>
///
/// ・色調・光・天候：調べるたびに「プリセット」の上から順に変わり、最後の次で元（シーンの既定）に戻る。キーの案内に次の名前が出る
/// ・回想：調べると回想に入り、もう一度調べると戻る
/// ・暗さ：調べると暗いステージ（StageDarkness の付いた物を表示）にし、もう一度調べると戻す
/// ・全部戻す：色調・光・天候・回想・暗さを最初の状態へ
/// ・塗り替えの境目を表示：塗り替える色の代わりに、色の段ごとにはっきりした色で塗る（光の中が黄色の段に入っているかを見る）
/// ・色調の台座に「一緒に変える光」を入れると、色調と一緒にその光にする（青と黄色の色調を見るとき）
/// ・部品と同じ「イベント」の層に入れるので、部品の動きと同じ（回想の中なら回想の層）
/// ・見本は試し用のシーン Test_Presentation（レシピ test_presentation）
[AddComponentMenu("演出/演出の台座（試し用）")]
[RequireComponent(typeof(Collider2D))]
public class PresentationStation : GimmickInteractable
{
    public enum Kind
    {
        [InspectorName("色調")]
        Mood = 0,

        [InspectorName("光")]
        Light = 1,

        [InspectorName("天候")]
        Weather = 2,

        [InspectorName("回想")]
        Flashback = 3,

        [InspectorName("暗さ（暗いステージ）")]
        Darkness = 4,

        [InspectorName("全部戻す")]
        ResetAll = 5,

        [InspectorName("塗り替えの境目を表示")]
        Bands = 6,
    }

    [SerializeField, Label("種類")]
    private Kind kind = Kind.Mood;

    [SerializeField]
    [Tooltip("色調・光・天候：調べるたびに上から順に使うプリセット（MoodPreset／LightPreset／WeatherPreset）。回想：1つ目の FlashbackPreset")]
    private List<ScriptableObject> presets = new List<ScriptableObject>();

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    private float seconds = 1.5f;

    [SerializeField, Label("一緒に変える光")]
    [Tooltip("色調の台座：色調と一緒にこの光のプリセットにする（青と黄色の色調は、塗り替え用の暗い全体の光と強い場面の光で見る）。元に戻すときは光も戻す。空なら光は変えない")]
    private LightPreset withLight;

    [SerializeField]
    [Tooltip("色調の台座：色調の 1 つ目・2 つ目… と一緒に使う光（上から順に。足りない所は「一緒に変える光」）。案ごとに青い全体の光と黄色い場面の光を変えるとき")]
    private List<LightPreset> withLights = new List<LightPreset>();

    [SerializeField, Label("暗いステージの物")]
    [Tooltip("暗さ・全部戻す：StageDarkness が付いた物（最初は非表示にしておく。暗さ 0 で置く）")]
    private StageDarkness darkness;

    [SerializeField, Label("暗さ", 0.0f, 1.0f)]
    [Tooltip("暗くしたときの暗さ（StageDarkness の Darkness と同じ）")]
    private float darknessValue = 0.85f;

    private int index = -1;
    private Coroutine darknessRoutine;

    // 全部戻すで、ほかの台座の数え方もそろえる
    private static readonly List<PresentationStation> all = new List<PresentationStation>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
    }

    //=====================================================================
    // キーの案内
    //=====================================================================

    public override string PromptLabel
    {
        get
        {
            switch(kind)
            {
                case Kind.Mood:      return "色調を変える（次：" + NextName("通常") + "）";
                case Kind.Light:     return "光を変える（次：" + NextName("シーンの既定") + "）";
                case Kind.Weather:   return "天候を変える（次：" + NextName("なし") + "）";
                case Kind.Flashback: return PresentationDirector.InFlashback ? "回想から戻る" : "回想に入る";
                case Kind.Darkness:  return IsDark ? "明るく戻す" : "暗くする";
                case Kind.Bands:     return PresentationDirector.ShowRemapBands ? "境目の表示をやめる" : "塗り替えの境目を表示";
                default:             return "全部戻す";
            }
        }
    }

    private bool IsDark => darkness != null && darkness.gameObject.activeSelf && darkness.Darkness > 0.01f;

    private string NextName(string resetName)
    {
        int next = index + 1;
        if(next >= presets.Count || presets[next] == null) { return resetName; }
        string n = presets[next].name;
        int u = n.IndexOf('_');
        return u >= 0 ? n.Substring(u + 1) : n;
    }

    //=====================================================================
    // 調べたとき
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        if(!all.Contains(this)) { all.Add(this); }
    }

    private void OnDestroy()
    {
        all.Remove(this);
    }

    protected override void OnInteract(GameObject user)
    {
        PresentationLayer layer = PresentationDirector.EventLayer;
        switch(kind)
        {
            case Kind.Mood:
                Advance();
                if(index < 0) { PresentationDirector.ResetMoodForEvent(seconds, PresentationCurve.Smooth); }
                else { PresentationDirector.SetMood(layer, presets[index] as MoodPreset, seconds); }
                LightPreset pairLight = index >= 0 && index < withLights.Count && withLights[index] != null ? withLights[index] : withLight;
                if(pairLight != null || withLights.Count > 0)
                {
                    if(index < 0 || pairLight == null) { PresentationDirector.ResetLightForEvent(seconds, PresentationCurve.Smooth); }
                    else { PresentationDirector.SetLight(layer, pairLight, seconds); }
                }
                break;

            case Kind.Light:
                Advance();
                if(index < 0) { PresentationDirector.ResetLightForEvent(seconds, PresentationCurve.Smooth); }
                else { PresentationDirector.SetLight(layer, presets[index] as LightPreset, seconds); }
                break;

            case Kind.Weather:
                Advance();
                if(index < 0) { PresentationDirector.SetWeather(layer, null, seconds); }
                else { PresentationDirector.SetWeather(layer, presets[index] as WeatherPreset, seconds); }
                break;

            case Kind.Flashback:
                if(PresentationDirector.InFlashback) { PresentationDirector.ExitFlashback(seconds, FlashbackTransition.Dissolve); }
                else
                {
                    var fb = presets.Count > 0 ? presets[0] as FlashbackPreset : null;
                    if(fb != null) { PresentationDirector.EnterFlashback(fb, seconds, fb.Transition); }
                }
                break;

            case Kind.Darkness:
                SetDark(!IsDark);
                break;

            case Kind.Bands:
                PresentationDirector.ShowRemapBands = !PresentationDirector.ShowRemapBands;
                break;

            default:
                ResetAll();
                break;
        }
    }

    private void Advance()
    {
        index++;
        if(index >= presets.Count) { index = -1; }
    }

    private void ResetAll()
    {
        if(PresentationDirector.InFlashback) { PresentationDirector.ExitFlashback(0.0f, FlashbackTransition.Instant); }
        PresentationDirector.ClearMood(PresentationLayer.Event, seconds);
        PresentationDirector.ClearLight(PresentationLayer.Event, seconds);
        PresentationDirector.ClearWeather(PresentationLayer.Event, seconds);
        for(int i = 0; i < all.Count; i++)
        {
            all[i].index = -1;
            if(all[i].kind == Kind.Darkness) { all[i].SetDark(false); }
        }
        if(darkness != null) { SetDark(false); }
        PresentationDirector.ShowRemapBands = false;
    }

    //=====================================================================
    // 暗さ
    //=====================================================================

    private void SetDark(bool dark)
    {
        if(darkness == null) { return; }
        if(darknessRoutine != null) { StopCoroutine(darknessRoutine); darknessRoutine = null; }

        if(dark)
        {
            darkness.SetDarkness(0.0f);
            darkness.gameObject.SetActive(true);
            darkness.SetDarkness(darknessValue, seconds);
        }
        else if(darkness.gameObject.activeSelf)
        {
            darkness.SetDarkness(0.0f, seconds);
            darknessRoutine = StartCoroutine(HideAfter(seconds));
        }
    }

    private IEnumerator HideAfter(float wait)
    {
        yield return new WaitForSeconds(wait);
        darkness.gameObject.SetActive(false);
        darknessRoutine = null;
    }
}
