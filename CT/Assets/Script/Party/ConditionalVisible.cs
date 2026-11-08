/*********************************************************************/
// * \file   ConditionalVisible.cs
// * \brief  条件で見える物（ノアにだけ見える文字・シグルにだけ見える足場など）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 見えないときの当たり判定
/// </summary>
public enum VisibleCollision
{
    [InspectorName("変えない")]
    Keep = 0,

    [InspectorName("見えている間だけ当たる（見える足場・見える壁）")]
    OnlyWhenVisible = 1,

    [InspectorName("見えていない間だけ当たる（見えると消える壁）")]
    OnlyWhenHidden = 2,
}

/// <summary>
/// 条件で見える物
/// </summary>
///
/// ・条件（初期値：ノアを操作しているとき）を満たしている間だけ、絵（SpriteRenderer・Tilemap）を表示する。切り替わるときはふわっと
///   例：ノアにだけ見える壁の文字（ヒント）／ シグルにだけ見える足場（見えている間だけ渡れる）
/// ・当たり判定も、見えている間だけ・見えていない間だけ有効にできる
/// ・条件は GameCondition なので、フラグ・仕掛けの状態・2人の状態なども使える
public class ConditionalVisible : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("見える条件")]
    [Tooltip("満たしている間だけ見える。初期値は「ノアを操作しているとき」")]
    private GameCondition condition = new GameCondition { character = GameCondition.CharacterCheck.Noah };

    [SerializeField, Label("切り替わる時間（秒）", 0.0f)]
    [Tooltip("ふわっと出る・消える時間。0 なら一瞬")]
    private float fadeTime = 0.3f;

    [SerializeField, Label("見えていないときの濃さ", 0.0f, 1.0f)]
    [Tooltip("0 なら完全に消える。0.15 などにすると、うっすら見える")]
    private float hiddenAlpha = 0.0f;

    [SerializeField, Label("当たり判定")]
    private VisibleCollision collision = VisibleCollision.Keep;

    private readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
    private readonly List<Tilemap> tilemaps = new List<Tilemap>();
    private readonly List<float> spriteAlpha = new List<float>();
    private readonly List<float> tileAlpha = new List<float>();
    private Collider2D[] colliders;
    private float visibility = -1.0f;

    public GameCondition Condition => condition;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true)) { sprites.Add(sr); spriteAlpha.Add(sr.color.a); }
        foreach(Tilemap tm in GetComponentsInChildren<Tilemap>(true)) { tilemaps.Add(tm); tileAlpha.Add(tm.color.a); }
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Update()
    {
        bool visible = condition == null || condition.IsEmpty || condition.IsMet();
        float target = visible ? 1.0f : 0.0f;

        // 最初は一瞬で
        if(visibility < 0.0f) { visibility = target; ApplyAlpha(); ApplyCollision(visible); return; }
        if(Mathf.Approximately(visibility, target)) { return; }

        visibility = fadeTime > 0.0f ? Mathf.MoveTowards(visibility, target, Time.deltaTime / fadeTime) : target;
        ApplyAlpha();
        ApplyCollision(visibility > 0.5f);
    }

    private void ApplyAlpha()
    {
        float a = Mathf.Lerp(hiddenAlpha, 1.0f, visibility);
        for(int i = 0; i < sprites.Count; i++)
        {
            if(sprites[i] == null) { continue; }
            Color c = sprites[i].color;
            c.a = spriteAlpha[i] * a;
            sprites[i].color = c;
        }
        for(int i = 0; i < tilemaps.Count; i++)
        {
            if(tilemaps[i] == null) { continue; }
            Color c = tilemaps[i].color;
            c.a = tileAlpha[i] * a;
            tilemaps[i].color = c;
        }
    }

    private void ApplyCollision(bool visible)
    {
        if(collision == VisibleCollision.Keep) { return; }
        bool on = collision == VisibleCollision.OnlyWhenVisible ? visible : !visible;
        foreach(Collider2D c in colliders) { if(c != null && !c.isTrigger) { c.enabled = on; } }
    }
}
