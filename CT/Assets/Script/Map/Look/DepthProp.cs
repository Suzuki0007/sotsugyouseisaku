/*********************************************************************/
// * \file   DepthProp.cs
// * \brief  奥行きのある物：キャラが後ろに隠れる部分と、当たり判定（入れない足元）の関係をまとめて決める
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 奥行きのある物（後ろに隠れる部分と足元の当たり判定）
/// </summary>
///
/// ・歯車・柱・木・像など、キャラが後ろを通れる物に使う。物を選んで Hierarchy 右クリック > マップの見た目 > 奥行きのある物にする
/// ・約束は 2 つだけ：赤い四角（足元の当たり判定）には入れない。青い線（前後の境目）より上にいるキャラは物の後ろ、下にいるキャラは前に描かれる
/// ・青い線は赤い四角の上の端より少し下にする（同じ高さだと前後が決まらず、キャラの向きなどで入れ替わる）
/// ・この部品の付いた物（足元の親）の位置が青い線。絵は子に入っている。数字を変えたら「当たり判定と境目を作り直す」を押す
/// ・丸い物でも当たり判定は四角でざっくり（細かい形は、引っかかる・押し続けるとぶるぶるするもと）
[AddComponentMenu("マップの見た目/奥行きのある物")]
[DisallowMultipleComponent]
[RequireComponent(typeof(SortingGroup))]
public class DepthProp : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Label("当たり判定の幅", 0.05f, 1.0f)]
    [Tooltip("赤い四角の幅（見た目の幅に対する割合。0.7 で見た目の 7 割）")]
    public float widthRatio = 0.7f;

    [Label("当たり判定の高さ", 0.05f, 1.0f)]
    [Tooltip("赤い四角の高さ（見た目の高さに対する割合。絵の下の端から測る。0.5 で下半分）")]
    public float heightRatio = 0.5f;

    [Label("前後の境目（マス）", 0.05f, 2.0f)]
    [Tooltip("青い線を、赤い四角の上の端からどれだけ下に置くか。0 に近いと前後が入れ替わることがある（0.25 くらい）")]
    public float sortMargin = 0.25f;

    [Label("足元の当たり判定（自動）")]
    [Tooltip("作った足元の当たり判定（歩ける範囲の子）。「当たり判定と境目を作り直す」で入る。物を消すときは、これも消す")]
    public GameObject footprint;

    //=====================================================================
    // 見た目の範囲
    //=====================================================================

    /// <summary>
    /// 子の絵（影など自動で作られる隠れた物は除く）を全部合わせた範囲。絵がなければ false
    /// </summary>
    public bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach(Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if((r.gameObject.hideFlags & HideFlags.DontSave) != 0) { continue; }   // DropShadow2D の影など
            if(!(r is SpriteRenderer)) { continue; }
            if(!found) { bounds = r.bounds; found = true; }
            else       { bounds.Encapsulate(r.bounds); }
        }
        return found;
    }

    /// <summary> 足元の当たり判定の四角（ワールド） </summary>
    public bool TryGetFootprint(out Rect rect)
    {
        rect = default;
        if(!TryGetVisualBounds(out Bounds b)) { return false; }
        float w = b.size.x * Mathf.Clamp01(widthRatio);
        float h = b.size.y * Mathf.Clamp01(heightRatio);
        rect = new Rect(b.center.x - w * 0.5f, b.min.y, w, h);
        return true;
    }

    /// <summary> 前後の境目の高さ（ワールド） </summary>
    public bool TryGetSortLine(out float y)
    {
        y = 0.0f;
        if(!TryGetFootprint(out Rect r)) { return false; }
        y = r.yMax - Mathf.Max(0.0f, sortMargin);
        return true;
    }

    //=====================================================================
    // シーンビューの表示
    //=====================================================================

    private void OnDrawGizmos()
    {
        if(!TryGetFootprint(out Rect r)) { return; }
        Gizmos.color = new Color(1.0f, 0.1f, 0.1f, 0.22f);
        Gizmos.DrawCube(r.center, r.size);
        Gizmos.color = new Color(1.0f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(r.center, r.size);

        float line = r.yMax - Mathf.Max(0.0f, sortMargin);
        if(TryGetVisualBounds(out Bounds b))
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1.0f, 0.95f);
            Gizmos.DrawLine(new Vector3(b.min.x, line, 0.0f), new Vector3(b.max.x, line, 0.0f));
            // 今の親の位置（実際に前後を決めている高さ）が線とずれていたら、黄色で示す（作り直しが要る）
            if(Mathf.Abs(transform.position.y - line) > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(b.min.x, transform.position.y, 0.0f), new Vector3(b.max.x, transform.position.y, 0.0f));
            }
        }
    }
}
