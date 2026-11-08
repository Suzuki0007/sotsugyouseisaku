/*********************************************************************/
// * \file   EnemySpawner.cs
// * \brief  敵のスポナークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using Unity.Behavior;

/// <summary>
/// 敵のスポナークラス
/// </summary>
public class EnemySpawner : ObjectSpawner
{
    //====================================================================================
    // 変数
    //====================================================================================

    [Header("スポーンする敵の設定")]

    [Tooltip("弾のプール")]
    [SerializeField]
    private EnemyBulletPool bulletPool;

    private GameObject player;

    private GameObject spawnedEnemy;

    //====================================================================================
    // 更新関数
    //====================================================================================

    public void Update()
    {
        if(PlayerSwitchManager.Instance == null) { return; }

        GameObject currentPlayer = PlayerSwitchManager.Instance.CurrentPlayer;

        if(currentPlayer == null || currentPlayer == player) { return; }

        player = currentPlayer;

        UpdateEnemyTarget();
    }

    //====================================================================================
    // 生成時の処理
    //====================================================================================

    /// <summary>
    /// オブジェクトがスポーンしたときの処理
    /// </summary>
    /// <param name="spawnedObject"></param>
    protected override void OnObjectSpawned(GameObject spawnedObject)
    {
        base.OnObjectSpawned(spawnedObject);

        spawnedEnemy = spawnedObject;

        if(PlayerSwitchManager.Instance == null)
        {
            Debug.LogError("PlayerSwitchManagerが存在しません", this);
            return;
        }

        player = PlayerSwitchManager.Instance.CurrentPlayer;
        if(player == null)
        {
            Debug.LogError("CurrentPlayerが設定されていません", this);
            return;
        }

        UpdateEnemyTarget();
    }

    /// <summary>
    /// 敵の追跡対象を更新する
    /// </summary>
    private void UpdateEnemyTarget()
    {
        if(spawnedEnemy == null || player == null) { return; }

        // 新しい作りの敵（Enemy）は、狙う相手を自分で探すので何もしなくてよい
        // 古い作りの敵（Behavior Graph + EnemyShooter）には、今までどおり渡す。どちらも付いていなくてもエラーにしない
        BehaviorGraphAgent behaviorAgent = spawnedEnemy.GetComponent<BehaviorGraphAgent>();
        if(behaviorAgent != null)
        {
            bool success = behaviorAgent.SetVariableValue("TargetPlayer", player);
            if(!success) { Debug.LogError("BehaviorGraphAgentのTargetPlayer変数の設定に失敗しました", spawnedEnemy); }
        }

        EnemyShooter enemyShooter = spawnedEnemy.GetComponent<EnemyShooter>();
        if(enemyShooter != null)
        {
            enemyShooter.SetTargetPlayer(player);
            enemyShooter.InitializeReference(player, bulletPool);
        }

        if(behaviorAgent == null && enemyShooter == null && spawnedEnemy.GetComponent<Enemy>() == null)
        {
            Debug.LogWarning("生成したオブジェクトに Enemy（新しい敵）も BehaviorGraphAgent / EnemyShooter（古い敵）も付いていません", spawnedEnemy);
        }
    }

    //====================================================================================
    // 破棄時の処理
    //====================================================================================

    protected override void OnObjectDestroyed(GameObject destroyedObject) { base.OnObjectDestroyed(destroyedObject); }

}
