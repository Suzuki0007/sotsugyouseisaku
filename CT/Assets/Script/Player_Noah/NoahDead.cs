/*********************************************************************/
// * \file   NoahDead.cs
// * \brief  ノアの死亡クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// ノアの死亡クラス
/// </summary>
public class NoahDead : CharaBase
{
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if(health == null) { Debug.Log("Healthコンポーネントが見つかりません"); }
    }

    /// <summary>
    /// オブジェクトが有効化されたときに呼ばれる関数
    /// </summary>
    private void OnEnable()
    {
        health.OnDied += OnPlayerDied;
    }

    /// <summary>
    /// オブジェクトが無効化されたときに呼ばれる関数
    /// </summary>
    private void OnDisable()
    {
        health.OnDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        Debug.Log("プレイヤーが死亡しました！");
    }
}
