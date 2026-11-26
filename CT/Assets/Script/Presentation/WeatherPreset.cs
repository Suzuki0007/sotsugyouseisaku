/*********************************************************************/
// * \file   WeatherPreset.cs
// * \brief  天候のプリセット：雨・雪・砂ぼこり・霧・光の粒・舞うものを、画面の範囲に降らせる粒（パーティクル）で決める
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 天候のプリセット（雨・雪・砂ぼこり・霧・光の粒・花びら）
/// </summary>
///
/// ・画面（カメラ）の範囲にいつも降る。マップの端でも途切れない
/// ・量・風・落ちる速さ・色・大きさ・キャラの前か後ろか・光の影響を受けるかを決める
/// ・部品「天候を変える」・シーンの既定・天候の範囲で選ぶ。量はなめらかに増える・減る
/// ・品質「軽い」では粒の量が半分になる
/// ・置き場所は Assets/Resources/Presentation/Weather/（セーブから名前で戻すため）
[CreateAssetMenu(menuName = "CT/演出/天候のプリセット", fileName = "Weather_")]
public class WeatherPreset : ScriptableObject
{
    public enum Order
    {
        [InspectorName("キャラの前")]
        Front = 0,

        [InspectorName("キャラの後ろ（床の上）")]
        Behind = 1,
    }

    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [Header("粒")]

    [SerializeField, Label("粒の絵")]
    [Tooltip("粒の画像（Assets/Images/Presentation/Weather/ にある物。白い絵に「色」をかける）")]
    private Texture2D texture;

    [SerializeField, Label("量（1秒に出る粒）", 0.0f, 3000.0f)]
    [Tooltip("1 秒に画面の中へ出す粒の数")]
    private float rate = 200.0f;

    [SerializeField, Label("粒の上限", 10.0f, 5000.0f)]
    [Tooltip("同時に出ている粒の最大（重くならないように）")]
    private int maxParticles = 1000;

    [SerializeField, Label("粒の寿命（秒）", 0.1f, 20.0f)]
    [Tooltip("1 つの粒が出てから消えるまで")]
    private float lifetime = 1.0f;

    [SerializeField, Label("大きさ（最小）", 0.01f, 20.0f)]
    [Tooltip("粒の大きさ（マス）")]
    private float sizeMin = 0.1f;

    [SerializeField, Label("大きさ（最大）", 0.01f, 20.0f)]
    private float sizeMax = 0.2f;

    [SerializeField, Label("色")]
    [Tooltip("粒の色と不透明さ")]
    private Color color = Color.white;

    [Header("動き")]

    [SerializeField, Label("落ちる速さ", -20.0f, 40.0f)]
    [Tooltip("画面の上から下へ動く速さ（マス／秒）。0 で落ちない、マイナスで上へ")]
    private float fallSpeed = 10.0f;

    [SerializeField, Label("風（横の速さ）", -40.0f, 40.0f)]
    [Tooltip("プラスで右へ、マイナスで左へ流れる（マス／秒）")]
    private float wind = -2.0f;

    [SerializeField, Label("速さのばらつき", 0.0f, 1.0f)]
    [Tooltip("粒ごとの速さの違い（0.2 で ±20%）")]
    private float speedVariation = 0.2f;

    [SerializeField, Label("ゆれ", 0.0f, 5.0f)]
    [Tooltip("ふわふわ揺れる強さ（雪・光の粒）。0 でまっすぐ")]
    private float sway = 0.0f;

    [SerializeField, Label("回る")]
    [Tooltip("粒がくるくる回る（花びら・葉）")]
    private bool spin = false;

    [SerializeField, Label("線のように伸ばす", 0.0f, 0.3f)]
    [Tooltip("動く向きに伸ばす（雨）。0 で伸ばさない")]
    private float stretch = 0.0f;

    [Header("そのほか")]

    [SerializeField, Label("地面のはね")]
    [Tooltip("地面に小さな輪のはねを出す（強い雨）")]
    private bool splash = false;

    [SerializeField, Label("はねの絵")]
    [Tooltip("はねの画像（輪）")]
    private Texture2D splashTexture;

    [SerializeField, Label("画面のかすみ", 0.0f, 0.6f)]
    [Tooltip("画面全体をうっすら色で覆う（強い砂ぼこり・霧）。0 でなし")]
    private float haze = 0.0f;

    [SerializeField, Label("かすみの色")]
    private Color hazeColor = new Color(0.85f, 0.75f, 0.6f, 1.0f);

    [SerializeField, Label("描く順番")]
    [Tooltip("キャラの前（雨・霧）か、後ろ（床の上）か")]
    private Order order = Order.Front;

    [SerializeField, Label("光の影響を受ける")]
    [Tooltip("オンで夜は粒も暗くなる（雨・雪・砂・霧）。オフでいつも明るい（光の粒）")]
    private bool lit = true;

    [SerializeField, Label("環境音（ループ）")]
    [Tooltip("この天候の間ずっと鳴らす音（ループの SeData）。空なら鳴らさない")]
    private SeData loopSe;

    //=====================================================================
    // 公開
    //=====================================================================

    public string Memo => memo;
    public Texture2D Texture => texture;
    public float Rate => rate;
    public int MaxParticles => maxParticles;
    public float Lifetime => lifetime;
    public float SizeMin => Mathf.Min(sizeMin, sizeMax);
    public float SizeMax => Mathf.Max(sizeMin, sizeMax);
    public Color ParticleColor => color;
    public float FallSpeed => fallSpeed;
    public float Wind => wind;
    public float SpeedVariation => speedVariation;
    public float Sway => sway;
    public bool Spin => spin;
    public float Stretch => stretch;
    public bool Splash => splash;
    public Texture2D SplashTexture => splashTexture;
    public float Haze => haze;
    public Color HazeColor => hazeColor;
    public Order DrawOrder => order;
    public bool Lit => lit;
    public SeData LoopSe => loopSe;

#if UNITY_EDITOR
    public void EditorSet(string memoText, Texture2D tex, float rateValue, int max, float life, float sMin, float sMax, Color c,
                          float fall, float windValue, float variation, float swayValue, bool spinValue, float stretchValue,
                          bool splashValue, Texture2D splashTex, float hazeValue, Color hazeC, Order orderValue, bool litValue)
    {
        memo = memoText; texture = tex; rate = rateValue; maxParticles = max; lifetime = life; sizeMin = sMin; sizeMax = sMax; color = c;
        fallSpeed = fall; wind = windValue; speedVariation = variation; sway = swayValue; spin = spinValue; stretch = stretchValue;
        splash = splashValue; splashTexture = splashTex; haze = hazeValue; hazeColor = hazeC; order = orderValue; lit = litValue;
    }
#endif
}
