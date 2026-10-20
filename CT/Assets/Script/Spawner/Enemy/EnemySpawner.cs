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

    [Tooltip("プレイヤー")]
    [SerializeField]
    private GameObject player;

    [Tooltip("弾のプール")]
    [SerializeField]
    private EnemyBulletPool bulletPool;

    //====================================================================================
    // 関数
    //====================================================================================

    /// <summary>
    /// オブジェクトがスポーンしたときの処理
    /// </summary>
    /// <param name="spawnedObject"></param>
    protected override void OnObjectSpawned(GameObject spawnedObject)
    {
        base.OnObjectSpawned(spawnedObject);

        EnemyShooter enemyShooter = spawnedObject.GetComponent<EnemyShooter>();
        if(enemyShooter == null) { Debug.LogError("生成した敵にEnemyShooterがアタッチされていません"); return; }

        BehaviorGraphAgent behaviorAgent = spawnedObject.GetComponent<BehaviorGraphAgent>();
        if(behaviorAgent == null) { Debug.LogError("生成した敵にBehaviorGraphAgentがアタッチされていません"); return; }

        bool success = behaviorAgent.SetVariableValue("TargetPlayer", player.gameObject);
        if(!success) { Debug.LogError("BlackboardにTargetPlayerが見つからないか、型が一致していません。", spawnedObject); return; }

        // 参照の初期化
        enemyShooter.InitializeReference(player, bulletPool);
    }

    protected override void OnObjectDestroyed(GameObject destroyedObject)
    {
        base.OnObjectDestroyed(destroyedObject);

        // 固有の処理が必要な場合ここに追加をできる
    }
}
