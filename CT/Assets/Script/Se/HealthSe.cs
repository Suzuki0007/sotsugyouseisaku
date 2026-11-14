/*********************************************************************/
// * \file   HealthSe.cs
// * \brief  体力の変化に合わせてSEを鳴らすクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// Health と同じオブジェクトに付けると、被ダメージ・回復・死亡時にSEを鳴らす
/// プレイヤーにも敵にも使える
/// </summary>
///
/// ・ノア・シグルのプレハブに付いている（ダメージの音。2026-11-14）
/// ・体力を共有しているとき（一緒）も、1回の攻撃で鳴るのは当たったキャラの音だけ
[RequireComponent(typeof(Health))]
public class HealthSe : MonoBehaviour
{
    [SerializeField, Tooltip("ダメージを受けたとき (死亡したときは鳴らさない)")]
    private SeData damageSe;

    [SerializeField, Tooltip("回復したとき")]
    private SeData healSe;

    [SerializeField, Tooltip("死亡したとき")]
    private SeData deathSe;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if(health == null) { return; }
        health.OnDamaged += OnDamaged;
        health.OnHealed += OnHealed;
        health.OnDied += OnDied;
    }

    private void OnDisable()
    {
        if(health == null) { return; }
        health.OnDamaged -= OnDamaged;
        health.OnHealed -= OnHealed;
        health.OnDied -= OnDied;
    }

    // 体力を共有しているとき（一緒のノア → シグルの体力）、1回の攻撃で両方の Health が「受けた」を出す。
    // 先に共有先（シグル）が鳴らし、あとから本当に当たった方（ノア）が来るので、同じフレームなら先の音を止めて当たった方の音にする
    private static int lastFrame = -1;
    private static Health lastHealth;
    private static SeHandle lastHandle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lastFrame = -1;
        lastHealth = null;
        lastHandle = SeHandle.None;
    }

    private void OnDamaged(int damage)
    {
        if(health.IsDead) { return; }

        if(lastFrame == Time.frameCount && lastHealth != null)
        {
            if(health.SharedWith == lastHealth)
            {
                // 当たったのは自分（共有先の音を止めて替える）
                if(lastHandle.IsValid) { SeManager.StopSe(lastHandle); }
            }
            else if(lastHealth.SharedWith == health) { return; }   // 当たった方がもう鳴らした
        }

        lastFrame = Time.frameCount;
        lastHealth = health;
        lastHandle = SeManager.PlaySe(damageSe, transform.position);
    }

    private void OnHealed(int amount)
    {
        SeManager.PlaySe(healSe, transform.position);
    }

    private void OnDied()
    {
        SeManager.PlaySe(deathSe, transform.position);
    }
}
