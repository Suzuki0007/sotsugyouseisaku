/*********************************************************************/
// * \file   DropShadow2D.cs
// * \brief  絵（SpriteRenderer）に、同じ形の影を自動で付ける
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 影を付ける（絵と同じ形の影）
/// </summary>
///
/// ・絵（SpriteRenderer）の付いた物に付けるだけで、同じ形を黒く半透明にした影を、少し右下にずらして描く（物が地面から浮いて見えなくなる）
/// ・絵を差し替えても・反転しても・アニメで変わっても、影も同じ形についてくる
/// ・エディタで置いたときから見える（影はシーンに保存しない。部品を外すと消える）
/// ・影の並び順は、キャラ（100）より下の 4 が基本（マップチップの層の決まり。Docs/マップチップの置き方.md）
[AddComponentMenu("マップの見た目/影を付ける（DropShadow2D）")]
[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow2D : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("影の濃さ", 0.0f, 1.0f)]
    [Tooltip("0 で見えない、1 でまっ黒。立っている物は 0.3 くらい、床に転がる物は 0.2 くらい")]
    private float strength = 0.3f;

    [SerializeField, Label("ずらす量（マス）")]
    [Tooltip("影を本体からずらす量（右が +x、下が -y）。大きい物ほど大きく")]
    private Vector2 offset = new Vector2(0.1f, -0.15f);

    [SerializeField, Label("縦の長さ", 0.1f, 1.0f)]
    [Tooltip("1 で本体と同じ形。小さくすると、つぶれて地面に落ちた影らしくなる（足元を基準に縮む）")]
    private float squash = 1.0f;

    [SerializeField, Label("本体の並び順の1つ下にする")]
    [Tooltip("オン：影は本体のすぐ下に描く（本体と一緒に前後する）。オフ：下の「影の並び順」で描く（床の上・キャラの下）")]
    private bool justBelowBody = false;

    [SerializeField, Label("影の並び順")]
    [Tooltip("「本体の並び順の1つ下」がオフのときの並び順。マップの層の決まりでは 4（床の影）")]
    private int shadowOrder = 4;

    private SpriteRenderer body;
    private SpriteRenderer shadow;

    //=====================================================================
    // 初期化・後片付け
    //=====================================================================

    private void OnEnable()
    {
        body = GetComponent<SpriteRenderer>();
        CreateShadow();
        Sync();
    }

    private void OnDisable()
    {
        if(shadow == null) { return; }
        if(Application.isPlaying) { Destroy(shadow.gameObject); }
        else                      { DestroyImmediate(shadow.gameObject); }
        shadow = null;
    }

    private void OnValidate()
    {
        if(shadow != null) { Sync(); }
    }

    private void CreateShadow()
    {
        if(shadow != null) { return; }

        var go = new GameObject("影（自動）");
        // エディタでは保存しない・Hierarchy に出さない（部品の設定から毎回作り直す）
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        shadow = go.AddComponent<SpriteRenderer>();
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        if(shadow == null || body == null) { return; }

        // 絵・向きが変わったときだけ合わせる（アニメ・反転）
        if(shadow.sprite != body.sprite || shadow.flipX != body.flipX || shadow.flipY != body.flipY || shadow.enabled != body.enabled)
        {
            Sync();
        }
    }

    /// <summary>
    /// 影の絵・色・位置・並び順を本体に合わせる
    /// </summary>
    private void Sync()
    {
        if(shadow == null || body == null) { return; }

        shadow.sprite = body.sprite;
        shadow.flipX = body.flipX;
        shadow.flipY = body.flipY;
        shadow.enabled = body.enabled;
        shadow.sharedMaterial = body.sharedMaterial;
        shadow.color = new Color(0.0f, 0.0f, 0.0f, Mathf.Clamp01(strength));
        shadow.sortingLayerID = body.sortingLayerID;
        shadow.sortingOrder = justBelowBody ? body.sortingOrder - 1 : shadowOrder;

        // ずらす量はワールドの向き（本体が回っていても、影はいつも右下）
        Transform t = shadow.transform;
        float h = body.sprite != null ? body.sprite.bounds.size.y * transform.lossyScale.y : 0.0f;
        float drop = (1.0f - squash) * h * 0.5f;   // 縦を縮めた分、足元へ下げる
        t.position = transform.position + new Vector3(offset.x, offset.y - drop, 0.0f);
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(1.0f, Mathf.Clamp(squash, 0.1f, 1.0f), 1.0f);
    }
}
