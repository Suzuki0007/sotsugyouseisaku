/*********************************************************************/
// * \file   PlayerPartySpawner.cs
// * \brief  プレイヤーパーティのスポナー
// *
// * \author 成田悠真
/*********************************************************************/

using Unity.AppUI.Core;
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
        // 遷移元で出現位置IDが指定されていればその地点、なければデフォルトの地点

        Transform noahPoint = noahSpawnPoint;
        Transform siglePoint = sigleSpawnPoint;

        PlayerSpawnPoint spawnPoint = PlayerSpawnPoint.ConsumePending();
        if(spawnPoint != null)
        {
            noahPoint = spawnPoint.NoahPoint;
            siglePoint = spawnPoint.SiglePoint;
        }

        // NoahとSigleを生成する

        GameObject noah = Instantiate(
            noahPrefab,
            noahPoint.position,
            noahPoint.rotation);

        GameObject sigle = Instantiate(
            siglePrefab,
            siglePoint.position,
            siglePoint.rotation);

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

        sigle.GetComponent<SigleMovement>().SetStartPos(noah.transform);

        playerSwitchManager.InitializePlayers(noah, sigle);
    }
}
