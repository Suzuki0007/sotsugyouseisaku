/*********************************************************************/
// * \file   LensSun.cs
// * \brief  太陽（レンズフレア）：置いた場所に太陽があるとして、画面にレンズフレアを出す
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 太陽（レンズフレア）
/// </summary>
///
/// ・置いた場所を太陽として、まわりの白い光・画面の中心を通る線の上の六角形や玉・虹の輪・全体の白いかぶりを出す
/// ・太陽が画面の外でも、近ければ少し出る（「画面の外でも出る距離」）
/// ・強さは、この部品の強さ × 色調のプリセットの「レンズフレアの強さ」（夜・室内の色調で 0 にすると消える）
/// ・1 つの場面に 1 つだけ置く（複数あるときは強い方を使う）。表示・非表示で出す・消す
/// ・使いすぎると安っぽくなるので、太陽が見える場面だけに置く
public class LensSun : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("強さ", 0.0f, 2.0f)]
    [Tooltip("フレア全体の強さ。1 がふつう")]
    private float strength = 1.0f;

    [SerializeField, Label("太陽の色")]
    [Tooltip("太陽のまわりの光の色。昼は白に近い黄、夕方は橙")]
    private Color color = new Color(1.0f, 0.96f, 0.86f, 1.0f);

    [SerializeField, Label("太陽の光の大きさ", 0.1f, 2.0f)]
    [Tooltip("太陽のまわりの白い光の大きさ（マップの映る範囲の高さに対する割合）")]
    private float glowSize = 0.8f;

    [SerializeField, Label("ゴーストの強さ", 0.0f, 2.0f)]
    [Tooltip("画面の中心を通る線の上に並ぶ六角形・玉・虹の輪の濃さ。0 で出さない")]
    private float ghostStrength = 1.0f;

    [SerializeField, Label("白いかぶり", 0.0f, 1.0f)]
    [Tooltip("太陽が画面に近いほど、画面全体が白っぽく（黒が浮いて）見える強さ。0 でなし")]
    private float veil = 0.35f;

    [SerializeField, Label("かぶりの色")]
    [Tooltip("全体にかぶる色。写真の手本のような橙のかぶりにするなら薄い橙")]
    private Color veilColor = new Color(1.0f, 0.8f, 0.6f, 1.0f);

    [SerializeField, Label("画面の外でも出る距離", 0.0f, 1.0f)]
    [Tooltip("太陽が画面の外にあるとき、どこまで離れてもフレアが出るか（マップの映る範囲の大きさに対する割合）。0 で画面の中にあるときだけ")]
    private float reach = 0.35f;

    //=====================================================================
    // 公開
    //=====================================================================

    public float Strength => strength;
    public Color Color => color;
    public float GlowSize => glowSize;
    public float GhostStrength => ghostStrength;
    public float Veil => veil;
    public Color VeilColor => veilColor;
    public float Reach => reach;

    private static readonly List<LensSun> active = new List<LensSun>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        active.Clear();
    }

    /// <summary> 使う太陽（強い方。なければ null） </summary>
    public static LensSun Current
    {
        get
        {
            LensSun best = null;
            for(int i = 0; i < active.Count; i++)
            {
                if(active[i] == null) { continue; }
                if(best == null || active[i].strength > best.strength) { best = active[i]; }
            }
            return best;
        }
    }

    private void OnEnable()
    {
        if(!active.Contains(this)) { active.Add(this); }
        PresentationDirector.RefreshLens();
    }

    private void OnDisable()
    {
        active.Remove(this);
        PresentationDirector.RefreshLens();
    }

    private void OnValidate()
    {
        if(Application.isPlaying && isActiveAndEnabled) { PresentationDirector.RefreshLens(); }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1.0f, 0.85f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}
