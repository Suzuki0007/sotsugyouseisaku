/*********************************************************************/
// * \file   PlayerPartySpawner.cs
// * \brief  プレイヤーパーティのスポナー
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// プレイヤーパーティのスポナー
/// </summary>
public class PlayerPartySpawner : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("プレイヤープレハブ")]

    [SerializeField]
    private GameObject noahPrefab;

    [SerializeField]
    private GameObject siglePrefab;

    [Header("生成位置")]

    [SerializeField]
    private Transform noahSpawnPoint;

    [SerializeField]
    private Transform sigleSpawnPoint;

    [Header("管理クラス")]

    [SerializeField]
    private PlayerSwitchManager playerSwitchManager;

    [Header("シーンコンテキスト")]

    [SerializeField]
    private PlayerSceneContext sceneContext;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(noahPrefab == null ||
            siglePrefab == null ||
            noahSpawnPoint == null ||
            sigleSpawnPoint == null ||
            playerSwitchManager == null || 
            sceneContext == null)
        {
            Debug.LogError("プレイヤー生成に必要な参照が設定されていません", this);
            return;
        }
    }

    private void Start()
    {
        // 出現位置を決める
        // ・つづきから（チェックポイント）で座標が指定されていればその座標
        // ・遷移元で出現位置IDが指定されていればその地点
        // ・どちらもなければデフォルトの地点

        Vector3 noahPos = noahSpawnPoint.position;
        Quaternion noahRot = noahSpawnPoint.rotation;
        Vector3 siglePos = sigleSpawnPoint.position;
        Quaternion sigleRot = sigleSpawnPoint.rotation;

        Vector2? pendingPosition = PlayerSpawnPoint.ConsumePendingPosition();
        PlayerSpawnPoint spawnPoint = null;

        if(pendingPosition.HasValue)
        {
            PlayerSpawnPoint.PendingSpawnPointId = null;
            noahPos = new Vector3(pendingPosition.Value.x, pendingPosition.Value.y, noahPos.z);
            siglePos = noahPos;
        }
        else
        {
            spawnPoint = PlayerSpawnPoint.ConsumePending();
            if(spawnPoint != null)
            {
                noahPos = spawnPoint.NoahPoint.position;
                noahRot = spawnPoint.NoahPoint.rotation;
                siglePos = spawnPoint.SiglePoint.position;
                sigleRot = spawnPoint.SiglePoint.rotation;
            }
        }

        // NoahとSigleを生成する

        GameObject noah = Instantiate(noahPrefab, noahPos, noahRot);

        GameObject sigle = Instantiate(siglePrefab, siglePos, sigleRot);

        // 「つづきから」で再開する場所として、着いた場所を記録して保存する
        // （スロットを使っていないときはファイルに書かれない）
        string sceneName = gameObject.scene.name;
        if(pendingPosition.HasValue) { GameSession.RecordResumePosition(sceneName, pendingPosition.Value); }
        else                         { GameSession.RecordResumePoint(sceneName, spawnPoint != null ? spawnPoint.SpawnPointId : ""); }
        Save.Commit();
        Save.WriteAsync();

        // プレイヤーの初期化

        PlayerRuntimeInitializer noahInitializer = noah.GetComponent<PlayerRuntimeInitializer>();
        if(noahInitializer == null) { Debug.LogError("NoahにPlayerRuntimeInitializerがアタッチされていません", noah); return; }

        PlayerRuntimeInitializer sigleInitializer = sigle.GetComponent<PlayerRuntimeInitializer>();
        if(sigleInitializer == null) { Debug.LogError("SigleにPlayerRuntimeInitializerがアタッチされていません", sigle); return; }

        noahInitializer.InitializeSceneReferences(sceneContext);
        sigleInitializer.InitializeSceneReferences(sceneContext);

        noahInitializer.InitializePlayer(sigle);
        sigleInitializer.InitializePlayer(noah);

        sceneContext.FocusCamera.SetTarget(noah.transform);
        sceneContext.FocusCamera.SetTilemap(sceneContext.PlayerLimitTilemap);

        sceneContext.LifeUIController.SetPlayer(noah);

        sceneContext.AreaAttack.SetAttackerPos(noah.transform);

        noah.GetComponent<NoahDead>().SetSceneChange(sceneContext.SceneChange);

        noah.GetComponent<NoahBulletDeleteAreaController>().SetBulletDeleteArea(sceneContext.BulletDeleteArea);
        noah.GetComponent<NoahBulletDeleteAreaController>().SetFollowTarget(noah);

        noah.GetComponent<AreaAttackController>().SetAreaAttack(sceneContext.AreaAttack);

        noah.GetComponent<TimingGaugeAttackController>().SetTimingGauge(sceneContext.TimingGaugeUI);

        sigle.GetComponent<CharacterSorting>().SetPlayer(noah);

        // 体力：一緒のときはシグルの体力を2人で共有（シグルがノアを守っている）、離れたら別々
        // 共有の設定は PlayerSwitchManager が2人の状態に合わせて行う。ゲームオーバーは、どちらが倒れても起きるようにする
        Health sigleHealth = sigle.GetComponent<Health>();
        if(sigleHealth == null) { sigleHealth = sigle.AddComponent<Health>(); }
        noah.GetComponent<NoahDead>().SetPartner(sigle);

        // Sigle の出現位置
        // ・出現位置に Sigle 用の位置が設定されていれば、そこに出して、ノアが動き出したらそこから歩いて付いてくる
        // ・なければ今まで通りノアの位置に出す（このスポナーの Sigle Spawn Point は、ノアと重ねる前提で使わない）
        if(spawnPoint != null && spawnPoint.HasSiglePoint)
        {
            PlayerHistory noahHistory = noah.GetComponent<PlayerHistory>();
            PlayerFollower sigleFollower = sigle.GetComponent<PlayerFollower>();
            if(noahHistory != null && sigleFollower != null)
            {
                noahHistory.SeedPath(siglePos, noahPos, sigleFollower.FollowFrames);
            }
        }
        else
        {
            sigle.GetComponent<SigleMovement>().SetStartPos(noah.transform);
        }

        playerSwitchManager.InitializePlayers(noah, sigle);

        // 2人の状態（離れ離れ）を、同じシーンのセーブから戻す（つづきから・やり直しなど）
        PartyPersistence.Restore(playerSwitchManager, sceneName);
    }
}
