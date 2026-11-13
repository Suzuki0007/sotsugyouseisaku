/*********************************************************************/
// * \file   InstantPickup.cs
// * \brief  触れるとすぐ効くアイテム（体力の回復・守りのゲージ）。持ち物には入らない
// *
// * \author
/*********************************************************************/

using System;
using UnityEngine;

/// <summary>
/// 触れるとすぐ効くアイテム
/// </summary>
///
/// ・ノアかシグルが触れると、その場で効いて消える（持ち物には入らない・セーブしない）
/// ・効き目：体力の回復／守りのゲージ（すぐためる・しばらく速くたまる）
/// ・置いたままでも、攻撃（落石など）・イベントから出してもよい。決めた時間で点滅して消える
/// ・Collider2D（Is Trigger）を付けて使う
[RequireComponent(typeof(Collider2D))]
public class InstantPickup : MonoBehaviour
{
    public enum Kind
    {
        [InspectorName("体力の回復")]
        Heal = 0,

        [InspectorName("守りのゲージ（リチャージを速くする）")]
        Guard = 1,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("効き目")]
    private Kind kind = Kind.Heal;

    [SerializeField, Label("回復する量", 1)]
    [Tooltip("「体力の回復」のとき。ハートいくつ分か")]
    private int healAmount = 1;

    [SerializeField, Label("すぐためる量", 0.0f, 1.0f)]
    [Tooltip("「守りのゲージ」のとき。1 で満タン")]
    private float guardRefill = 0.3f;

    [SerializeField, Label("速くたまる倍率", 1.0f)]
    [Tooltip("「守りのゲージ」のとき。2 なら 2 倍速でたまる（リチャージ時間が半分）")]
    private float guardBoostMultiplier = 2.0f;

    [SerializeField, Label("速くたまる時間（秒）", 0.0f)]
    [Tooltip("「守りのゲージ」のとき。0 なら速くならない")]
    private float guardBoostSeconds = 10.0f;

    [SerializeField, Label("消えるまでの時間（秒）", 0.0f)]
    [Tooltip("出てから消えるまで。0 なら消えない")]
    private float lifeSeconds = 12.0f;

    [SerializeField, Label("点滅を始める残り時間（秒）", 0.0f)]
    [Tooltip("消える前に点滅して知らせる")]
    private float blinkSeconds = 3.0f;

    [SerializeField, Label("出てから拾えるまでの時間（秒）", 0.0f)]
    [Tooltip("落ちてきた瞬間に拾ってしまわないように")]
    private float pickupDelay = 0.3f;

    [SerializeField, Label("拾ったときの音")]
    private SeData pickupSe;

    [SerializeField, Label("拾ったときのエフェクト")]
    private EffectData pickupEffect;

    private float age;
    private SpriteRenderer[] renderers;

    /// <summary> 拾われたとき（一言のきっかけ） </summary>
    public static event Action<InstantPickup, GameObject> Picked;

    /// <summary> 出てきたとき（一言のきっかけ） </summary>
    public static event Action<InstantPickup> Appeared;

    public Kind Effect => kind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Picked = null;
        Appeared = null;
    }

    //=====================================================================
    // 出す
    //=====================================================================

    /// <summary>
    /// プレハブから出す（攻撃・イベントから）
    /// </summary>
    public static InstantPickup Spawn(GameObject prefab, Vector2 position)
    {
        if(prefab == null) { return null; }

        GameObject go = Instantiate(prefab, new Vector3(position.x, position.y, 0.0f), Quaternion.identity);
        return go.GetComponent<InstantPickup>();
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        Appeared?.Invoke(this);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if(lifeSeconds > 0.0f)
        {
            float remain = lifeSeconds - age;
            if(remain <= 0.0f) { Destroy(gameObject); return; }

            bool visible = remain > blinkSeconds || Mathf.Repeat(remain * 6.0f, 1.0f) > 0.4f;
            foreach(SpriteRenderer r in renderers) { if(r != null) { r.enabled = visible; } }
        }

        // ふわふわ浮く
        foreach(SpriteRenderer r in renderers)
        {
            if(r != null && r.transform != transform) { r.transform.localPosition = new Vector3(0.0f, 0.15f + Mathf.Sin(age * 4.0f) * 0.08f, 0.0f); }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { TryPick(other); }

    private void OnTriggerStay2D(Collider2D other) { TryPick(other); }

    private void TryPick(Collider2D other)
    {
        if(age < pickupDelay || !other.CompareTag("Player")) { return; }

        switch(kind)
        {
            case Kind.Heal:
            {
                Health health = PlayerCharacters.PartyHealth;
                if(health != null) { health.Heal(healAmount); }
                break;
            }
            case Kind.Guard:
            {
                GuardGauge gauge = GuardGauge.Active;
                if(gauge != null)
                {
                    gauge.Refill(guardRefill);
                    if(guardBoostSeconds > 0.0f) { gauge.BoostRecharge(guardBoostMultiplier, guardBoostSeconds); }
                }
                break;
            }
        }

        SeManager.PlaySe(pickupSe, transform.position);
        if(pickupSe == null) { UISound.Play(UISoundType.Decide); }
        if(pickupEffect != null) { EffectManager.Play(pickupEffect, transform.position, Vector2.zero, 0.0f, null, false); }

        Picked?.Invoke(this, other.gameObject);
        Destroy(gameObject);
    }
}
