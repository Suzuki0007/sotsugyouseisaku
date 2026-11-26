/*********************************************************************/
// * \file   WeatherArea.cs
// * \brief  天候の範囲：操作中のキャラがこの四角に入ると天候を変える・止める（室内に入ると雨が止む など）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天候の範囲（入ると天候が変わる・止む）
/// </summary>
///
/// ・四角（BoxCollider2D）の範囲に操作中のキャラが入ると、天候をここで決めた物にする（空なら止める）
/// ・出ると元の天候（シーンの既定・イベントで変えた天候）に戻る。範囲が重なる所は「優先度」の大きいほうが勝つ
/// ・イベントで変えた天候より強い（室内では、外で降らせた雨も止む）。回想の天候よりは弱い
/// ・Hierarchy 右クリック > 演出 > 天候の範囲 で作れる
[AddComponentMenu("演出/天候の範囲")]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class WeatherArea : MonoBehaviour
{
    [SerializeField, Label("天候")]
    [Tooltip("範囲の中の天候。空なら天候を止める（室内など）")]
    private WeatherPreset weather;

    [SerializeField, Label("優先度")]
    [Tooltip("範囲が重なっている所では、大きいほうになる")]
    private int priority = 0;

    [SerializeField, Label("切り替わる秒数", 0.0f)]
    [Tooltip("入った・出たときに、この秒数をかけて量を変える")]
    private float blendSeconds = 1.0f;

    private Rect area;

    private static readonly List<WeatherArea> active = new List<WeatherArea>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
    }

    public WeatherPreset Weather => weather;
    public int Priority => priority;
    public float BlendSeconds => blendSeconds;

    /// <summary> 今シーンにある天候の範囲 </summary>
    public static IReadOnlyList<WeatherArea> Active => active;

    public bool Contains(Vector2 point) => area.Contains(point);

    private void OnEnable()
    {
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
        Gizmos.color = new Color(0.4f, 0.7f, 1.0f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
