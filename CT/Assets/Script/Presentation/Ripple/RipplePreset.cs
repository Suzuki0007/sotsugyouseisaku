/*********************************************************************/
// * \file   RipplePreset.cs
// * \brief  波紋のプリセット：見せ方（色が戻る／光が灯る）・広がる速さ・最後の半径・形・縁・戻り方
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 波紋のプリセット（直した場所から、色または光が波紋のように広がる）
/// </summary>
///
/// ・色が戻る：波紋の中だけ、全体の光の青を打ち消す光を足して、元の色に近づける（夏のうちに型・回想など）
/// ・光が灯る：波紋の中だけ、黄色い光で明るくする（ファミレス型・月夜の砂浜など）
/// ・縁は、少し明るい輪の光（水面の波紋のような線）。品質「軽い」では縁を省く
/// ・広がりきったら、そのまま残す（ロードしても残る＝部品の「残す」）／ゆっくり消える
/// ・置き場所は Assets/Resources/Presentation/Ripple/
[CreateAssetMenu(menuName = "CT/演出/波紋のプリセット", fileName = "Ripple_")]
public class RipplePreset : ScriptableObject
{
    public enum Look
    {
        [InspectorName("色が戻る")]
        RestoreColor = 0,

        [InspectorName("光が灯る")]
        LightUp = 1,
    }

    public enum After
    {
        [InspectorName("ずっと残る")]
        Stay = 0,

        [InspectorName("ゆっくり消える")]
        FadeOut = 1,
    }

    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [SerializeField, Label("見せ方")]
    private Look look = Look.LightUp;

    [SerializeField, Label("光の色")]
    [Tooltip("光が灯る：灯る光の色（杏色・レモン色など）。色が戻る：使わない（全体の光から自動で決める）")]
    private Color color = new Color(1.0f, 0.86f, 0.55f, 1.0f);

    [SerializeField, Label("強さ", 0.0f, 4.0f)]
    [Tooltip("光が灯る：光の強さ。色が戻る：戻す明るさ（1 で全体の光が白の 1 倍になるまで足す）")]
    private float intensity = 1.0f;

    [SerializeField, Label("最後の半径（マス）", 0.2f, 40.0f)]
    private float radius = 6.0f;

    [SerializeField, Label("広がる秒数", 0.0f)]
    [Tooltip("中心から最後の半径まで広がる時間（部品で上書きできる）")]
    private float seconds = 2.0f;

    [SerializeField, Label("縦の割合（楕円）", 0.2f, 1.0f)]
    [Tooltip("1 で円。小さくすると横長の楕円（見下ろしの床に合わせるなら 0.7 くらい）")]
    private float ellipse = 1.0f;

    [Header("縁")]

    [SerializeField, Label("縁の明るさ", 0.0f, 4.0f)]
    [Tooltip("広がっていく輪の縁の明るさ。0 で縁なし")]
    private float edgeIntensity = 1.2f;

    [SerializeField, Label("縁の色")]
    private Color edgeColor = new Color(1.0f, 0.95f, 0.8f, 1.0f);

    [SerializeField, Label("縁の太さ", 0.02f, 0.5f)]
    [Tooltip("半径に対する縁の太さの割合")]
    private float edgeWidth = 0.08f;

    [SerializeField, Label("縁が消える秒数", 0.0f)]
    [Tooltip("広がりきったあと、縁が消えるまで（中の光は残る）")]
    private float edgeFadeSeconds = 1.0f;

    [Header("広がりきったあと")]

    [SerializeField, Label("戻り方")]
    private After after = After.Stay;

    [SerializeField, Label("消える秒数", 0.0f)]
    [Tooltip("ゆっくり消える：広がりきってから消えるまで")]
    private float fadeSeconds = 3.0f;

    public string Memo => memo;
    public Look Mode => look;
    public Color LightColor => color;
    public float Intensity => intensity;
    public float Radius => radius;
    public float Seconds => seconds;
    public float Ellipse => ellipse;
    public float EdgeIntensity => edgeIntensity;
    public Color EdgeColor => edgeColor;
    public float EdgeWidth => edgeWidth;
    public float EdgeFadeSeconds => edgeFadeSeconds;
    public After AfterMode => after;
    public float FadeSeconds => fadeSeconds;

#if UNITY_EDITOR
    public void EditorSet(string memoText, Look lk, Color c, float i, float r, float secs, float ell, float edgeI, Color edgeC, float edgeW, After af, float fade)
    {
        memo = memoText; look = lk; color = c; intensity = i; radius = r; seconds = secs; ellipse = ell;
        edgeIntensity = edgeI; edgeColor = edgeC; edgeWidth = edgeW; after = af; fadeSeconds = fade;
    }
#endif
}
