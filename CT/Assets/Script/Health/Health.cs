/*********************************************************************/
// * \file   Health.cs
// * \brief  体力クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 体力クラス
/// </summary>
public class Health : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField] 
    private int maxHealth;

    //=====================================================================
    // イベント
    //=====================================================================

    public event System.Action OnDied;

    /// <summary> ダメージを受けたとき (引数: 受けたダメージ量) </summary>
    public event System.Action<int> OnDamaged;

    /// <summary> 回復したとき (引数: 実際に回復した量) </summary>
    public event System.Action<int> OnHealed;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public int CurrentHealth { get; private set; }

    public int MaxHealth => maxHealth;

    public bool IsDead => CurrentHealth <= 0;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void Damage(int damage)
    {
        if(IsDead) { return; }

        CurrentHealth -= damage;
        if(CurrentHealth < 0) { CurrentHealth = 0; }

        // 被ダメージイベントをだす
        OnDamaged?.Invoke(damage);

        if(CurrentHealth <= 0)
        {
            // 死亡イベントをだす
            OnDied?.Invoke();
        }
    }

    public void Heal(int healAmount)
    {
        int before = CurrentHealth;

        CurrentHealth += healAmount;

        if(CurrentHealth > maxHealth) { CurrentHealth = maxHealth; }

        int healed = CurrentHealth - before;
        if(healed > 0) { OnHealed?.Invoke(healed); }
    }
}
