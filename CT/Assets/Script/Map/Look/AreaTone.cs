/*********************************************************************/
// * \file   AreaTone.cs
// * \brief  場所の色味：操作中のキャラがこの四角に入ると、画面全体の明るさ・色（Global Light 2D）をなめらかに変える
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 場所の色味（入ると全体の色・明るさが変わる）
/// </summary>
///
/// ・四角（BoxCollider2D）の範囲に操作中のキャラが入ると、シーンの Global Light 2D の色と明るさを、ここで決めた値へなめらかに変える
/// ・出ると元の値（シーンを始めたときの Global Light 2D）に戻る。範囲が重なる所は「優先度」の大きいほうが勝つ
/// ・例：浜辺は夕方の暖かい色、遺跡の中は暗く冷たい色（明かり〔Light 2D〕が映える）
/// ・マップ・キャラ・物（明かりに反応する絵）だけが変わる。UI は変わらない
/// ・演出の窓口（PresentationDirector）の「範囲」の層：シーンの既定の光より強く、部品「光を変える」・回想より弱い。暗いステージ（StageDarkness）の暗さはこの上に掛かる
[AddComponentMenu("マップの見た目/場所の色味")]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class AreaTone : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("色")]
    [Tooltip("この場所の光の色（白＝そのまま。暖かい色で夕方、青っぽい色で夜・建物の中）")]
    private Color color = Color.white;

    [SerializeField, Label("明るさ", 0.0f, 2.0f)]
    [Tooltip("この場所の全体の明るさ（1＝ふつう。小さいほど暗く、置いた明かりが目立つ）")]
    private float intensity = 1.0f;

    [SerializeField, Label("優先度")]
    [Tooltip("範囲が重なっている所では、大きいほうの色味になる（建物の中を外より大きく）")]
    private int priority = 0;

    [SerializeField, Label("切り替わる秒数", 0.0f)]
    [Tooltip("入った・出たときに、この秒数をかけて色味を変える")]
    private float blendSeconds = 0.8f;

    private Rect area;

    private static readonly List<AreaTone> active = new List<AreaTone>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
    }

    //=====================================================================
    // 公開
    //=====================================================================

    public Color ToneColor => color;
    public float ToneIntensity => intensity;
    public int Priority => priority;
    public float BlendSeconds => blendSeconds;

    /// <summary> 今シーンにある場所の色味 </summary>
    public static IReadOnlyList<AreaTone> Active => active;

    /// <summary> その点がこの範囲の中か </summary>
    public bool Contains(Vector2 point) => area.Contains(point);

    //=====================================================================
    // 初期化
    //=====================================================================

    private void OnEnable()
    {
        // 範囲は四角の形から決める。四角は形を決めるためだけに使うので、再生中は当たり判定を切る
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        Vector2 size = Vector2.Scale(box.size, (Vector2)transform.lossyScale);
        Vector2 center = (Vector2)transform.TransformPoint(box.offset);
        area = new Rect(center - size * 0.5f, size);
        if(Application.isPlaying) { box.enabled = false; }

        if(!active.Contains(this)) { active.Add(this); }
        PresentationDirector.Ensure();
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if(box == null) { return; }
        Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
