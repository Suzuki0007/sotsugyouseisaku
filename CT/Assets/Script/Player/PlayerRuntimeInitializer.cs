/*********************************************************************/
// * \file   PlayerMovement.cs
// * \brief  プレイヤーの移動処理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// プレイヤーにシーン上の初期化処理を行うクラス
/// </summary>
public class PlayerRuntimeInitializer : MonoBehaviour
{
    //=====================================================================
    // 変数   
    //=====================================================================

    private PlayerMapLimit playerMapLimit;

    private PlayerFollower playerFollower;

    private SigleShooter sigleShooter;

    private BulletDeleteArea bulletDeleteArea;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        playerMapLimit = GetComponent<PlayerMapLimit>();
        playerFollower = GetComponent<PlayerFollower>();
        sigleShooter = GetComponent<SigleShooter>();
        bulletDeleteArea = GetComponentInChildren<BulletDeleteArea>();
    }

    /// <summary>
    /// プレイヤー同士の参照を設定する
    /// </summary>
    public void InitializePlayer(GameObject targetPlayer)
    {
        if(playerFollower != null)
        {
            playerFollower.SetTarget(targetPlayer);
        }
    }

    /// <summary>
    /// 生成後にシーン上の参照を設定する関数
    /// </summary>
    public void InitializeSceneReferences(PlayerSceneContext context)
    {
        if(context == null)
        {
            Debug.LogError($"{name}: PlayerSceneContextがnullです");
            return;
        }

        if(playerMapLimit != null)
        {
            playerMapLimit.SetTilemap(context.PlayerLimitTilemap);
        }

        if(sigleShooter != null)
        {
            sigleShooter.InitializeReference(context.PlayerBulletPool);
        }

        if(bulletDeleteArea != null)
        {
            bulletDeleteArea.SetDefenderPos(transform);
        }
    }
}
