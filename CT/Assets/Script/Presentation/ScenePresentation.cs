/*********************************************************************/
// * \file   ScenePresentation.cs
// * \brief  シーンの演出の既定：このシーンがどの色調・光・天候で始まるかを決める（フラグの条件で変えられる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーンの演出の既定（このシーンの色調・光・天候）
/// </summary>
///
/// ・シーンに1つ置く。シーンに入ったとき、ここで決めた色調・光・天候で始まる
/// ・「条件で変える」に、フラグの条件と変える物を書いておくと、条件を満たしている間はそちらになる（優先度の大きいほうが勝つ）
/// ・フラグが変わったら、ルールの秒数でなめらかに変わる
/// ・部品「色調を戻す」「光を戻す」「天候を変える（シーンの既定に戻す）」は、ここで決まった物へ戻す
/// ・「全体の光の倍率」：光のプリセットを使っている間だけ、全体の光を何倍にするか（絵が暗いシーンで上げる）
/// ・Hierarchy 右クリック > 演出 > シーンの演出の既定 で作れる
[AddComponentMenu("演出/シーンの演出の既定")]
[DisallowMultipleComponent]
public class ScenePresentation : MonoBehaviour
{
    [System.Serializable]
    public class Rule
    {
        [Label("メモ")]
        [Tooltip("Inspector で見分けるための名前")]
        public string label = "";

        [Tooltip("すべて満たしたときにこのルールを使う")]
        public List<FlagCondition> flagConditions = new List<FlagCondition>();

        [Label("優先度")]
        [Tooltip("満たしているルールが複数あるときは、大きいほうが勝つ")]
        public int priority = 0;

        [Label("色調を変える")]
        public bool changeMood = false;

        [Label("色調")]
        [Tooltip("空なら「通常」（何も加工しない）")]
        public MoodPreset mood;

        [Label("光を変える")]
        public bool changeLight = false;

        [Label("光")]
        [Tooltip("空ならシーンの Global Light 2D のまま")]
        public LightPreset light;

        [Label("天候を変える")]
        public bool changeWeather = false;

        [Label("天候")]
        [Tooltip("空なら天候なし")]
        public WeatherPreset weather;

        [Label("切り替える秒数", 0.0f)]
        [Tooltip("ゲームの途中で条件が変わったときに、この秒数をかけて変える")]
        public float seconds = 2.0f;

        public bool IsMet() => FlagCondition.AllMet(flagConditions);
    }

    [Header("既定")]

    [SerializeField, Label("色調")]
    [Tooltip("このシーンの色調。空なら「通常」（何も加工しない）")]
    private MoodPreset mood;

    [SerializeField, Label("光")]
    [Tooltip("このシーンの光。空ならシーンの Global Light 2D のまま")]
    private LightPreset lightPreset;

    [SerializeField, Label("天候")]
    [Tooltip("このシーンの天候。空なら天候なし")]
    private WeatherPreset weather;

    [SerializeField, Label("外側の空")]
    [Tooltip("マップの絵がない所（塔の外など。今は黒い所）に見せる空。空なら今までどおり（カメラの背景の色）。部品「空を変える」でイベントの途中に変えられる")]
    private SkyPreset sky;

    [SerializeField, Label("外側の見え方")]
    [Tooltip("見上げる：屋外・地上（空・雲・星）／見下ろす：塔の中・高い所（塔を輪切りにして斜め上から見ている。外は、かすみの下のはるか下の地面と、下を流れる雲）")]
    private SkyViewMode skyView = SkyViewMode.LookUp;

    [SerializeField, Label("外壁を下へ続ける")]
    [Tooltip("見下ろすとき、マップの下の端から塔の外壁が下へ伸びて、かすみに消えるように見せる（マップの絵の下の端が外壁でないときはオフ）")]
    private bool towerWall = true;

    [Header("このシーンの光の合わせ方")]

    [SerializeField, Label("全体の光の倍率", 0.1f, 4.0f)]
    [Tooltip("光のプリセット（青い夜など）を使っている間だけ、全体の光の強さをこの倍率にする。絵が暗いシーン（塔の中など）で、同じプリセットでも青い夜に見えるように上げる。光のプリセットを使っていないときは効かない（今の見た目は変わらない）")]
    private float globalLightScale = 1.0f;

    [Header("色の戻り具合（波紋で直すほど色が戻る）")]

    [SerializeField, Label("色が戻ったときの色調")]
    [Tooltip("部品「色の戻り具合を変える」で割合を上げると、既定の色調からこの色調へ近づく（全部直すと 1）。空なら色調は変えない")]
    private MoodPreset restoredMood;

    [SerializeField, Label("色が戻ったときの光")]
    [Tooltip("同じく、既定の光からこの光へ近づく。空なら光は変えない")]
    private LightPreset restoredLight;

    [Header("条件で変える")]

    [SerializeField]
    private List<Rule> rules = new List<Rule>();

    private MoodPreset appliedMood;
    private LightPreset appliedLight;
    private WeatherPreset appliedWeather;
    private bool started;

    private static ScenePresentation current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
    }

    /// <summary> 今のシーンの既定（なければ null） </summary>
    public static ScenePresentation Current => current;

    public MoodPreset DefaultMood => mood;
    public LightPreset DefaultLight => lightPreset;
    public WeatherPreset DefaultWeather => weather;
    public SkyPreset DefaultSky => sky;
    public IReadOnlyList<Rule> Rules => rules;
    public MoodPreset RestoredMood => restoredMood;
    public LightPreset RestoredLight => restoredLight;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void OnEnable()
    {
        current = this;
        PresentationDirector.Ensure();
    }

    private void Start()
    {
        GameFlags.OnChanged += OnFlagsChanged;
        PresentationDirector.SceneGlobalLightScale = globalLightScale;
        Evaluate(out appliedMood, out appliedLight, out appliedWeather, out _);
        PresentationDirector.SetBase(appliedMood, appliedLight, appliedWeather, 0.0f);
        PresentationDirector.SetSkyMode(skyView, towerWall);
        PresentationDirector.SetBaseSky(sky, 0.0f);
        started = true;
    }

    private void OnDisable()
    {
        if(current == this) { current = null; }
    }

    private void OnDestroy()
    {
        GameFlags.OnChanged -= OnFlagsChanged;
    }

    //=====================================================================
    // 判定
    //=====================================================================

    private void OnFlagsChanged()
    {
        if(!started) { return; }
        Evaluate(out MoodPreset m, out LightPreset l, out WeatherPreset w, out float seconds);
        if(m == appliedMood && l == appliedLight && w == appliedWeather) { return; }
        appliedMood = m;
        appliedLight = l;
        appliedWeather = w;
        PresentationDirector.SetBase(m, l, w, seconds);
    }

    /// <summary>
    /// 今の条件での色調・光・天候（優先度の小さいルールから順に上書きする）
    /// </summary>
    public void Evaluate(out MoodPreset m, out LightPreset l, out WeatherPreset w, out float seconds)
    {
        m = mood;
        l = lightPreset;
        w = weather;
        seconds = 2.0f;

        int bestMood = int.MinValue, bestLight = int.MinValue, bestWeather = int.MinValue, best = int.MinValue;
        for(int i = 0; i < rules.Count; i++)
        {
            Rule r = rules[i];
            if(r == null || !r.IsMet()) { continue; }
            if(r.changeMood && r.priority >= bestMood) { m = r.mood; bestMood = r.priority; }
            if(r.changeLight && r.priority >= bestLight) { l = r.light; bestLight = r.priority; }
            if(r.changeWeather && r.priority >= bestWeather) { w = r.weather; bestWeather = r.priority; }
            if(r.priority >= best) { seconds = r.seconds; best = r.priority; }
        }
    }
}
