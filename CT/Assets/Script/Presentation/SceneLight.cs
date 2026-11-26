/*********************************************************************/
// * \file   SceneLight.cs
// * \brief  場面の光：シーンに置いた Light 2D にグループ名を付け、光のプリセットから点け消し・強さを変えられるようにする
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 場面の光（月明かり・ランタン・窓の光・夕日など）
/// </summary>
///
/// ・Light 2D に付けてグループ名を入れると、光のプリセットの「場面の光」で、そのグループを点ける・消す・強さを変えられる
/// ・同じグループ名の光は一緒に変わる。プリセットに書いていないグループは、置いたときのまま（Light 2D を切って置けば、ふだんは消えている）
/// ・明かりのゆらぎ（LightFlicker）と一緒に付けてもよい（変えた強さを元にゆれる）
/// ・Hierarchy 右クリック > 演出 > 場面の光（点・面・窓の筋）で作れる
[AddComponentMenu("演出/場面の光")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
public class SceneLight : MonoBehaviour
{
    [SerializeField, Label("グループ名")]
    [Tooltip("光のプリセットの「場面の光」で使う名前（月・ランタン・窓・夕日・塔の頂上 など）")]
    private string group = "";

    private Light2D light2D;
    private LightFlicker flicker;
    private float baseIntensity;
    private Color baseColor = Color.white;
    private float factor = 1.0f;
    private bool initialOn = true;
    private bool initialized;

    private static readonly List<SceneLight> all = new List<SceneLight>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
    }

    //=====================================================================
    // 公開
    //=====================================================================

    /// <summary> 今シーンにある場面の光 </summary>
    public static IReadOnlyList<SceneLight> All => all;

    public string Group => group;

    /// <summary> 今の強さ（置いたときの何倍か） </summary>
    public float Factor => factor;

    /// <summary>
    /// 強さを置いたときの何倍かにする（0 で消す）
    /// </summary>
    public void SetFactor(float value)
    {
        Init();
        factor = Mathf.Max(0.0f, value);
        bool on = factor > 0.001f;
        if(light2D.enabled != on) { light2D.enabled = on; }
        float intensity = baseIntensity * factor;
        if(flicker != null) { flicker.SetBaseIntensity(intensity); }
        else { light2D.intensity = intensity; }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Init()
    {
        if(initialized) { return; }
        initialized = true;
        light2D = GetComponent<Light2D>();
        TryGetComponent(out flicker);
        baseIntensity = light2D.intensity;
        baseColor = light2D.color;
        initialOn = light2D.enabled;
        factor = initialOn ? 1.0f : 0.0f;
    }

    /// <summary> 置いたときの色 </summary>
    public Color BaseColor { get { Init(); return baseColor; } }

    /// <summary> 今の色 </summary>
    public Color CurrentColor { get { Init(); return light2D.color; } }

    /// <summary> 光の色を変える（光のプリセットの「色を変える」） </summary>
    public void SetColor(Color c)
    {
        Init();
        c.a = 1.0f;
        light2D.color = c;
    }

    /// <summary>
    /// プリセットの強さ（-1＝書いていない＝置いたときのまま）を、混ぜられる数（置いたときの何倍か）にする
    /// </summary>
    public float Resolve(float presetFactor)
    {
        Init();
        return presetFactor < 0.0f ? (initialOn ? 1.0f : 0.0f) : presetFactor;
    }

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        if(!all.Contains(this)) { all.Add(this); }
    }

    private void OnDisable()
    {
        all.Remove(this);
    }
}
