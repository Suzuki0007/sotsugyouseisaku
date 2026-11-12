/*********************************************************************/
// * \file   TimpaniUnit.cs
// * \brief  ティンパニ1台：ペグが締まったら周りの攻撃を消す、全部締まったら真ん中の敵をおとなしくする
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ティンパニ1台
/// </summary>
///
/// ・子のペグ（TimpaniPeg）と、子の敵（Enemy＝音楽生物）を自動で見つける（Inspector で入れてもよい）
/// ・ペグが1つ締まるたびに、周りの敵の攻撃を一度消す（弾を消し、攻撃をやめさせて少し止める）
/// ・ペグが全部締まると、真ん中の敵がおとなしくなる（敵のデータの「倒されたあと：おとなしくなる」）
///   → 調律できた印（♪）・エフェクト・回復アイテムを1つ出す → 部屋（TimpaniRoom）に知らせる
public class TimpaniUnit : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("中身（空なら子から自動で探す）")]

    [SerializeField, Label("真ん中の敵")]
    private Enemy creature;

    [SerializeField]
    [Tooltip("このティンパニのペグ（空なら子のペグを全部）")]
    private List<TimpaniPeg> pegs = new List<TimpaniPeg>();

    [Header("ペグが締まったとき")]

    [SerializeField, Label("攻撃を消す範囲（マス）", 0.0f)]
    [Tooltip("締まったペグからこの距離の中の敵の弾を消し、敵の攻撃をやめさせる。0 なら消さない")]
    private float clearRadius = 9.0f;

    [SerializeField, Label("敵を止める時間（秒）", 0.0f)]
    [Tooltip("攻撃をやめさせた敵が、また動き出すまでの時間")]
    private float stunSeconds = 1.0f;

    [SerializeField, Label("攻撃を消したときのエフェクト")]
    [Tooltip("締まったペグの位置に出す（空でもよい）")]
    private EffectData clearEffect;

    [Header("全部締まったとき（調律できた）")]

    [SerializeField, Label("出す回復アイテム")]
    [Tooltip("InstantPickup のプレハブ（空なら出さない）")]
    private GameObject healItem;

    [SerializeField, Label("アイテムを出す位置（敵から）")]
    private Vector2 healItemOffset = new Vector2(0.0f, -1.6f);

    [SerializeField, Label("調律できたあとの絵")]
    [Tooltip("おとなしくなった敵の絵に差し替える（空なら差し替えない）")]
    private Sprite calmSprite;

    [SerializeField, Label("調律できたときのエフェクト")]
    private EffectData calmEffect;

    [SerializeField, Label("調律できたときの音")]
    private SeData calmSe;

    [SerializeField, Label("調律できた印の色")]
    private Color calmMarkColor = new Color(0.4f, 1.0f, 0.7f, 1.0f);

    [SerializeField, Label("調律できたときのイベント")]
    [Tooltip("このティンパニを調律できたときに実行する（空でよい）")]
    private EventSequence onCalmed;

    private bool calmed;
    private TextMeshPro calmMark;
    private float markTime;

    //=====================================================================
    // プロパティ・イベント
    //=====================================================================

    /// <summary> 調律できた（真ん中の敵がおとなしくなった）か </summary>
    public bool IsCalmed => calmed;

    public Enemy Creature => creature;

    public IReadOnlyList<TimpaniPeg> Pegs => pegs;

    /// <summary> 締まっているペグの数 </summary>
    public int TightenedCount
    {
        get
        {
            int count = 0;
            foreach(TimpaniPeg peg in pegs) { if(peg != null && peg.IsTightened) { count++; } }
            return count;
        }
    }

    /// <summary> 調律できたとき（部屋・一言が聞く） </summary>
    public static event Action<TimpaniUnit> Calmed;

    /// <summary> ペグが締まって、周りの攻撃を消したとき </summary>
    public static event Action<TimpaniUnit, TimpaniPeg> AttacksCleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Calmed = null;
        AttacksCleared = null;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(creature == null) { creature = GetComponentInChildren<Enemy>(true); }
        if(pegs.Count == 0) { pegs.AddRange(GetComponentsInChildren<TimpaniPeg>(true)); }
        pegs.RemoveAll(p => p == null);
    }

    private void OnEnable()
    {
        TimpaniPeg.Tightened += OnPegTightened;
    }

    private void OnDisable()
    {
        TimpaniPeg.Tightened -= OnPegTightened;
    }

    private void OnPegTightened(TimpaniPeg peg)
    {
        if(calmed || !pegs.Contains(peg)) { return; }

        ClearAttacksAround(peg.transform.position);
        AttacksCleared?.Invoke(this, peg);

        if(TightenedCount >= pegs.Count) { Calm(); }
    }

    //=====================================================================
    // 攻撃を消す
    //=====================================================================

    /// <summary>
    /// 周りの敵の弾を消し、敵の攻撃をやめさせて少し止める
    /// </summary>
    public void ClearAttacksAround(Vector2 center)
    {
        if(clearRadius <= 0.0f) { return; }

        float sqr = clearRadius * clearRadius;

        foreach(EnemyBullet bullet in FindObjectsByType<EnemyBullet>())
        {
            if(bullet == null || !bullet.gameObject.activeInHierarchy) { continue; }
            if(((Vector2)bullet.transform.position - center).sqrMagnitude > sqr) { continue; }
            bullet.Vanish();
        }

        foreach(Enemy enemy in Enemy.All)
        {
            if(enemy == null || enemy.IsDead) { continue; }
            if(((Vector2)enemy.transform.position - center).sqrMagnitude > sqr) { continue; }

            if(enemy.Brain != null) { enemy.Brain.Interrupt(); }
            if(stunSeconds > 0.0f) { enemy.PauseFor(stunSeconds); }
        }

        if(clearEffect != null) { EffectManager.Play(clearEffect, center, Vector2.zero, 0.0f, null, false); }
    }

    //=====================================================================
    // 調律できた
    //=====================================================================

    private void Calm()
    {
        if(calmed) { return; }
        calmed = true;

        Vector2 pos = creature != null ? (Vector2)creature.transform.position : (Vector2)transform.position;

        if(creature != null && !creature.IsDead) { creature.Kill(); }

        // 調律できた絵に差し替える（倒れたときの演出のあとも残るように、少しあとでもう一度）
        if(creature != null && calmSprite != null)
        {
            SetCreatureSprite();
            Invoke(nameof(SetCreatureSprite), 0.6f);
        }

        SeManager.PlaySe(calmSe, pos);
        if(calmEffect != null) { EffectManager.Play(calmEffect, pos, Vector2.zero, 0.0f, null, false); }

        if(healItem != null) { InstantPickup.Spawn(healItem, pos + healItemOffset); }

        ShowCalmMark(pos);

        Calmed?.Invoke(this);
        if(onCalmed != null) { onCalmed.Play(gameObject); }
    }

    private void SetCreatureSprite()
    {
        if(creature == null) { return; }

        SpriteAnimator anim = creature.GetComponentInChildren<SpriteAnimator>();
        if(anim != null) { anim.enabled = false; }

        foreach(SpriteRenderer r in creature.GetComponentsInChildren<SpriteRenderer>())
        {
            r.sprite = calmSprite;
            r.color = Color.white;
        }
    }

    private void ShowCalmMark(Vector2 pos)
    {
        var go = new GameObject("調律できた印");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(pos.x, pos.y + 1.4f, 0.0f);

        calmMark = go.AddComponent<TextMeshPro>();
        calmMark.text = "♪";
        calmMark.fontSize = 8.0f;
        calmMark.alignment = TextAlignmentOptions.Center;
        calmMark.rectTransform.sizeDelta = new Vector2(3.0f, 2.0f);
        calmMark.color = calmMarkColor;
        calmMark.sortingOrder = 202;
    }

    private void Update()
    {
        if(calmMark == null) { return; }

        // ♪ をゆらゆらさせる
        markTime += Time.deltaTime;
        calmMark.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(markTime * 3.0f) * 12.0f);
    }
}
