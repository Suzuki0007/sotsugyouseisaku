/*********************************************************************/
// * \file   EnemyDead.cs
// * \brief  敵の死亡クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の死亡クラス
/// </summary>
public class EnemyDead : CharaBase
{
    //======================================================
    // 変数
    //======================================================

    private Health health;

    //======================================================
    // 関数
    //======================================================

    private void Awake()
    {
        health = GetComponent<Health>();
        if(health == null) { Debug.Log("Healthコンポーネントが見つかりません"); }
    }

    void OnEnable()
    {
        health.OnDied += OnEnemyDied;
    }

    void OnDisable()
    {
        health.OnDied -= OnEnemyDied;
    }

    private void OnEnemyDied()
    {
        Debug.Log("敵が死亡しました！");
        Destroy(gameObject);
    }
}
