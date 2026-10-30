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
            playerSwitchManager == null)
        {
            Debug.LogError("プレイヤー生成に必要な参照が設定されていません", this);
            return;
        }
    }

    private void Start()
    {
        // NoahとSigleを生成する

        GameObject noah = Instantiate(
            noahPrefab,
            noahSpawnPoint.position,
            noahSpawnPoint.rotation);

        GameObject sigle = Instantiate(
            siglePrefab,
            sigleSpawnPoint.position,
            sigleSpawnPoint.rotation);

        // プレイヤーの初期化

        PlayerRuntimeInitializer noahInitializer = noah.GetComponent<PlayerRuntimeInitializer>();
        PlayerRuntimeInitializer sigleInitializer = sigle.GetComponent<PlayerRuntimeInitializer>();

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

        noah.GetComponent<AreaAttackController>().SetAreaAttack(sceneContext.AreaAttack);

        noah.GetComponent<TimingGaugeAttackController>().SetTimingGauge(sceneContext.TimingGaugeUI);

        sigle.GetComponent<CharacterSorting>().SetPlayer(noah);

        playerSwitchManager.InitializePlayers(noah, sigle);
    }
}
