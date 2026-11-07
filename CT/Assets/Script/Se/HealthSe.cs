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

    private void OnDamaged(int damage)
    {
        if(health.IsDead) { return; }
        SeManager.PlaySe(damageSe, transform.position);
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
