/*********************************************************************/
// * \file   DamageReceiver.cs
// * \brief  攻撃を受ける部品（部位・弱点・ギミックの当たり判定に付ける）
// *
// * \author
/*********************************************************************/

using System;
using UnityEngine;

/// <summary>
/// 攻撃を受ける部品
/// </summary>
///
/// ・Collider2D と同じオブジェクトに付ける。攻撃が当たると、指定した Health（空なら自分か親の Health）にダメージを渡す
/// ・敵の子オブジェクト（腕・弱点など）に付ければ、そこに当たった攻撃が本体の体力を減らす
/// ・Health がなくても「当たった」ことは OnHit で分かる（ギミックの反応に使う）
/// 範囲攻撃・弾は DamageUtility.Apply を通して当てるので、これがあればそちらが優先される
[DisallowMultipleComponent]
public class DamageReceiver : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("ダメージを渡す先")]
    [Tooltip("この当たり判定に攻撃が当たったとき、体力を減らす相手。空なら自分か親の Health")]
    private Health target;

    [SerializeField, Label("ダメージの倍率", 0.0f)]
    [Tooltip("ここに当たったときのダメージの倍率（弱点なら 2、硬い所なら 0.5、効かないなら 0）")]
    private float multiplier = 1.0f;

    //=====================================================================
    // イベント
    //=====================================================================

    /// <summary> 攻撃が当たったとき（ダメージが 0 でも呼ばれる。倍率をかける前の情報） </summary>
    public event Action<DamageInfo> OnHit;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> ダメージを渡す先（空なら自分か親の Health） </summary>
    public Health Target => target != null ? target : GetComponentInParent<Health>();

    public float Multiplier { get => multiplier; set => multiplier = Mathf.Max(0.0f, value); }

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 攻撃を受ける
    /// </summary>
    /// <returns> 体力を減らす相手がいたか </returns>
    public bool Receive(DamageInfo info)
    {
        if(!isActiveAndEnabled) { return false; }

        OnHit?.Invoke(info);

        Health health = Target;
        if(health == null) { return false; }

        int amount = Mathf.Max(0, Mathf.FloorToInt(info.amount * multiplier + 0.5f));
        if(amount > 0) { health.Damage(info.WithAmount(amount)); }
        return true;
    }
}

/// <summary>
/// 攻撃を当てる処理の共通部分（範囲攻撃・弾・体当たりなどから使う）
/// </summary>
public static class DamageUtility
{
    /// <summary>
    /// 当たった Collider2D に攻撃を当てる（DamageReceiver があればそれ、なければ同じオブジェクトの Health）
    /// </summary>
    /// <returns> 攻撃を受ける相手がいたか </returns>
    public static bool Apply(Collider2D collider, DamageInfo info)
    {
        if(collider == null) { return false; }

        DamageReceiver receiver = collider.GetComponent<DamageReceiver>();
        if(receiver != null) { return receiver.Receive(info); }

        Health health = collider.GetComponent<Health>();
        if(health == null) { return false; }

        health.Damage(info);
        return true;
    }

    /// <summary>
    /// 1回の攻撃で同じ相手に2度当てないための目印（体力を減らす相手。なければ受ける部品。どちらもなければ null）
    /// </summary>
    /// 本体と部位の両方に範囲攻撃が当たっても、同じ Health なら1回だけにするために使う
    public static UnityEngine.Object HitKey(Collider2D collider)
    {
        if(collider == null) { return null; }

        DamageReceiver receiver = collider.GetComponent<DamageReceiver>();
        if(receiver != null)
        {
            Health target = receiver.Target;
            return target != null ? (UnityEngine.Object)target : receiver;
        }

        return collider.GetComponent<Health>();
    }

    /// <summary>
    /// 体力を減らす相手（表示用。DamageReceiver の渡す先か、同じオブジェクトの Health）
    /// </summary>
    public static Health FindHealth(Collider2D collider)
    {
        if(collider == null) { return null; }

        DamageReceiver receiver = collider.GetComponent<DamageReceiver>();
        if(receiver != null) { return receiver.Target; }

        return collider.GetComponent<Health>();
    }
}
