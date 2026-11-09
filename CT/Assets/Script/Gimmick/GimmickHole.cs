/*********************************************************************/
// * \file   GimmickHole.cs
// * \brief  穴（押せる箱を落とすと埋まって通れるようになる）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 穴
/// </summary>
///
/// ・当たり判定（Is Trigger オフの Collider2D）で通れない。押せる箱を押し込むと埋まり、当たり判定が消えて通れるようになる
/// ・埋まったかは仕掛けの状態（GimmickState）の ON。セーブされ、扉などの条件にも使える
[RequireComponent(typeof(GimmickState))]
public class GimmickHole : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("穴の当たり判定")]
    [Tooltip("埋まるまで通れなくする Collider2D（Is Trigger オフ）。空なら同じオブジェクトのもの")]
    private Collider2D blocker;

    [SerializeField, Label("埋まった絵")]
    [Tooltip("埋まったあとの絵。空なら色だけ変える")]
    private Sprite filledSprite;

    [SerializeField, Label("埋まったときの色")]
    private Color filledColor = new Color(0.75f, 0.65f, 0.5f, 1.0f);

    private GimmickState state;
    private SpriteRenderer spriteRenderer;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 埋まっているか </summary>
    public bool Filled => state != null && state.IsOn;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        state = GetComponent<GimmickState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer == null) { spriteRenderer = GetComponentInChildren<SpriteRenderer>(); }
        if(blocker == null)
        {
            foreach(Collider2D c in GetComponents<Collider2D>()) { if(!c.isTrigger) { blocker = c; break; } }
        }
        state.Changed += (s, v) => Apply();
    }

    private void Start()
    {
        Apply();
    }

    /// <summary> 埋める（押せる箱が落ちたとき） </summary>
    public void Fill()
    {
        state.Set(1, true);
        Apply();
    }

    private void Apply()
    {
        bool filled = Filled;
        if(blocker != null) { blocker.enabled = !filled; }
        if(spriteRenderer != null && filled)
        {
            if(filledSprite != null) { spriteRenderer.sprite = filledSprite; }
            else                     { spriteRenderer.color = filledColor; }
        }
    }
}
