/*********************************************************************/
// * \file   SkyPreset.cs
// * \brief  空のプリセット：マップの外（何も描いていない所）に見える空の色・雲・星
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 空のプリセット（塔の外・マップの外に見える空）
/// </summary>
///
/// ・マップの絵がない所（今は黒い所）に、空のグラデーション・流れる雲・星を出す
/// ・シーンの演出の既定の「外側の空」か、部品「空を変える」で選ぶ。切り替えはなめらか（暗い空 → 夜明け など）
/// ・空は光（Global Light）の影響を受けない。色調（画面全体の色の加工）は受ける
/// ・置き場所は Assets/Resources/Presentation/Sky/（セーブから名前で戻すため）
[CreateAssetMenu(menuName = "CT/演出/空のプリセット", fileName = "Sky_")]
public class SkyPreset : ScriptableObject
{
    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [Header("空の色")]

    [SerializeField, Label("上の色")]
    [Tooltip("画面の上のほうの空の色")]
    private Color topColor = new Color(0.18f, 0.42f, 0.85f, 1.0f);

    [SerializeField, Label("地平線の色")]
    [Tooltip("画面の下のほう（地平線）の空の色")]
    private Color horizonColor = new Color(0.62f, 0.82f, 0.98f, 1.0f);

    [SerializeField, Label("地平線の高さ", 0.0f, 0.9f)]
    [Tooltip("ここより下は地平線の色のまま。上へ向かって上の色に変わる（画面の高さに対する割合）")]
    private float horizon = 0.15f;

    [Header("雲")]

    [SerializeField, Label("雲の量", 0.0f, 1.0f)]
    [Tooltip("0 で雲なし、0.5 でところどころ、1 で一面")]
    private float clouds = 0.5f;

    [SerializeField, Label("雲の色")]
    private Color cloudColor = Color.white;

    [SerializeField, Label("雲の速さ", 0.0f, 3.0f)]
    [Tooltip("横に流れる速さ（マス／秒）")]
    private float cloudSpeed = 0.3f;

    [Header("星")]

    [SerializeField, Label("星の量", 0.0f, 1.0f)]
    [Tooltip("0 で星なし。夜・夜明けに")]
    private float stars = 0.0f;

    [SerializeField, Label("星の色")]
    private Color starColor = new Color(1.0f, 0.97f, 0.9f, 1.0f);

    [Header("見下ろすとき（塔の中・高い所。シーンの演出の既定の「外側の見え方」）")]

    [SerializeField, Label("下に見える地面の色")]
    [Tooltip("塔の外のはるか下に見える地面・海の色。かすみ（地平線の色）越しに薄く見える")]
    private Color groundColor = new Color(0.36f, 0.46f, 0.44f, 1.0f);

    [SerializeField, Label("地面の見え方", 0.0f, 1.0f)]
    [Tooltip("下の地面がどれだけはっきり見えるか。0 でかすみだけ（雲の上）、0.5 でうっすら")]
    private float groundVisibility = 0.5f;

    public string Memo => memo;

    /// <summary> 値の一式 </summary>
    public SkyValues Values => new SkyValues
    {
        presence = 1.0f,
        topColor = topColor,
        horizonColor = horizonColor,
        horizon = horizon,
        clouds = clouds,
        cloudColor = cloudColor,
        cloudSpeed = cloudSpeed,
        stars = stars,
        starColor = starColor,
        groundColor = groundColor,
        groundVisibility = groundVisibility,
    };

    private void OnValidate()
    {
        if(Application.isPlaying) { PresentationDirector.NotifySkyPresetChanged(this); }
    }

#if UNITY_EDITOR
    /// <summary> エディタの道具（最初のプリセットを作る）から値を入れる </summary>
    public void EditorSet(string memoText, SkyValues v)
    {
        memo = memoText;
        topColor = v.topColor; horizonColor = v.horizonColor; horizon = v.horizon;
        clouds = v.clouds; cloudColor = v.cloudColor; cloudSpeed = v.cloudSpeed;
        stars = v.stars; starColor = v.starColor;
        groundColor = v.groundColor; groundVisibility = v.groundVisibility;
    }
#endif
}

/// <summary>
/// 空の値の一式（混ぜて使う）。presence＝空が出ている割合（0 で空なし＝今までどおりの背景）
/// </summary>
[System.Serializable]
public struct SkyValues
{
    public float presence;
    public Color topColor;
    public Color horizonColor;
    public float horizon;
    public float clouds;
    public Color cloudColor;
    public float cloudSpeed;
    public float stars;
    public Color starColor;
    public Color groundColor;
    public float groundVisibility;

    /// <summary> 空なし </summary>
    public static SkyValues None => new SkyValues
    {
        presence = 0.0f,
        topColor = Color.black,
        horizonColor = Color.black,
        horizon = 0.15f,
        cloudColor = Color.white,
        starColor = Color.white,
        groundColor = new Color(0.36f, 0.46f, 0.44f, 1.0f),
    };

    public static SkyValues Lerp(SkyValues a, SkyValues b, float t)
    {
        // 空がない側から入る・ない側へ消えるときは、色と形を変えずに薄くする
        if(a.presence <= 0.0001f) { SkyValues v = b; v.presence = Mathf.Lerp(0.0f, b.presence, t); return v; }
        if(b.presence <= 0.0001f) { SkyValues v = a; v.presence = Mathf.Lerp(a.presence, 0.0f, t); return v; }
        return new SkyValues
        {
            presence = Mathf.Lerp(a.presence, b.presence, t),
            topColor = Color.Lerp(a.topColor, b.topColor, t),
            horizonColor = Color.Lerp(a.horizonColor, b.horizonColor, t),
            horizon = Mathf.Lerp(a.horizon, b.horizon, t),
            clouds = Mathf.Lerp(a.clouds, b.clouds, t),
            cloudColor = Color.Lerp(a.cloudColor, b.cloudColor, t),
            cloudSpeed = Mathf.Lerp(a.cloudSpeed, b.cloudSpeed, t),
            stars = Mathf.Lerp(a.stars, b.stars, t),
            starColor = Color.Lerp(a.starColor, b.starColor, t),
            groundColor = Color.Lerp(a.groundColor, b.groundColor, t),
            groundVisibility = Mathf.Lerp(a.groundVisibility, b.groundVisibility, t),
        };
    }
}

/// <summary> 外側の見え方（番号で保存されるので末尾にだけ足す） </summary>
public enum SkyViewMode
{
    [InspectorName("見上げる（屋外・地上。空と雲と星）")]
    LookUp = 0,

    [InspectorName("見下ろす（塔の中・高い所。かすみの下に地面と雲）")]
    LookDown = 1,
}
