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

    //=====================================================================
    // プロパティ
    //=====================================================================

    public int CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    //=====================================================================
    // 関数
    //=====================================================================

    void Start()
    {
        CurrentHealth = maxHealth;
    }

    public void Damage(int damage)
    {
        if(IsDead) { return; }

        CurrentHealth -= damage;

        if(CurrentHealth <= 0) 
        {
            CurrentHealth = 0;

            OnDied?.Invoke();
        }
    }

    public void Heal(int healAmount)
    {
        CurrentHealth += healAmount;

        if(CurrentHealth > maxHealth) { CurrentHealth = maxHealth; }
    }
}
