/*********************************************************************/
// * \file   PlayerSceneContext.cs
// * \brief  プレイヤーのシーンコンテキストクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// プレイヤーのシーンコンテキストクラス
/// </summary>
public class PlayerSceneContext : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("マップ")]

    [SerializeField]
    private Tilemap playerLimitTilemap;

    [Header("弾")]

    [SerializeField]
    private PlayerBulletPool playerBulletPool;

    [Header("カメラ")]

    [SerializeField]
    private Camera targetCamera;

    [Header("UI")]

    [SerializeField]
    private TimingGaugeUI timingGaugeUI;

    [SerializeField]
    private LifeUIController lifeUIController;

    [Header("フォーカスカメラ")]

    [SerializeField]
    private FocusCamera focusCamera;

    [Header("シーン切り替え")]

    [SerializeField]
    private SceneChange sceneChange;

    [Header("範囲攻撃")]

    [SerializeField]
    private AreaAttack areaAttack;

    [Header("弾丸削除エリア")]

    [SerializeField]
    private BulletDeleteArea bulletDeleteArea;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public Tilemap PlayerLimitTilemap => playerLimitTilemap;

    public PlayerBulletPool PlayerBulletPool => playerBulletPool;

    public Camera TargetCamera => targetCamera;

    public TimingGaugeUI TimingGaugeUI => timingGaugeUI;

    public LifeUIController LifeUIController => lifeUIController;

    public FocusCamera FocusCamera => focusCamera;

    public SceneChange SceneChange => sceneChange;

    public AreaAttack AreaAttack => areaAttack;

    public BulletDeleteArea BulletDeleteArea => bulletDeleteArea;
}
