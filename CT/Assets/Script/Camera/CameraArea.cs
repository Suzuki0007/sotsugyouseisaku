/*********************************************************************/
// * \file   CameraArea.cs
// * \brief  カメラの範囲（この四角の中にいる間は、カメラがこの範囲の外を映さない）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カメラの範囲
/// </summary>
///
/// ・Is Trigger の BoxCollider2D の四角。カメラが追っているキャラがこの中にいる間は、カメラはこの四角の中だけを映す
///   （ふだんはマップ〔Tilemap〕全体が範囲）
/// ・離れ離れのステージで、同じシーンの中に2つのエリアを作るときに使う（相手のエリアが画面に映り込まないように）
/// ・部屋ごとに区切ったカメラ（ゼルダの部屋切り替え風）にも使える
/// ・重なっているときは「優先度」の大きい方
[RequireComponent(typeof(BoxCollider2D))]
public class CameraArea : MonoBehaviour
{
    [SerializeField, Label("優先度")]
    [Tooltip("範囲が重なっているときは、大きい方を使う")]
    private int priority = 0;

    private BoxCollider2D box;

    private static readonly List<CameraArea> all = new List<CameraArea>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { all.Clear(); }

    public Bounds Bounds => box != null ? box.bounds : new Bounds(transform.position, Vector3.zero);
    public int Priority => priority;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;    // キャラや弾をふさがないように（範囲を表すだけ）
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }
    private void OnEnable()  { if(!all.Contains(this)) { all.Add(this); } }
    private void OnDisable() { all.Remove(this); }

    /// <summary>
    /// その位置を含むカメラの範囲（なければ null）
    /// </summary>
    public static CameraArea Find(Vector2 position)
    {
        CameraArea best = null;
        foreach(CameraArea area in all)
        {
            if(area == null || area.box == null || !area.box.enabled) { continue; }
            if(!area.box.OverlapPoint(position)) { continue; }
            if(best == null || area.priority > best.priority) { best = area; }
        }
        return best;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D b = box != null ? box : GetComponent<BoxCollider2D>();
        if(b == null) { return; }
        Gizmos.color = new Color(0.3f, 0.8f, 1.0f, 0.8f);
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
