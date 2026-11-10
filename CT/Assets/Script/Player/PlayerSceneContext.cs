/*********************************************************************/
// * \file   PlayerSceneContext.cs
// * \brief  プレイヤーのシーンコンテキストクラス
// *
// * \author 成田悠真
/*********************************************************************/

using System;
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

    [Header("マップ範囲")]

    [SerializeField]
    private MapBounds mapBounds;

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

    public MapBounds MapBounds => mapBounds;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(PlayerLimitTilemap == null) { playerLimitTilemap = FindAnyObjectByType<Tilemap>(); }

        if(MapBounds == null)
        {
            // もしタグがMapFloorのオブジェクトがある場合、そのMapBoundsを使用する
            GameObject mapFloorObject = GameObject.FindGameObjectWithTag("MapFloor");
            if(mapFloorObject != null)
            {
                MapBounds mapBoundsFromFloor = mapFloorObject.GetComponent<MapBounds>();
                if(mapBoundsFromFloor != null)
                {
                    mapBounds = mapBoundsFromFloor;
                    return;
                }
            }

            // タグでも見つからなかった場合は、FindAnyObjectByTypeで探す
            mapBounds = FindAnyObjectByType<MapBounds>();
        }
    }

    private void Start()
    {
        if(PlayerLimitTilemap == null) { Debug.LogError($"{name}: PlayerLimitTilemapが見つかりません", this); }
        if(PlayerBulletPool == null) { Debug.LogError($"{name}: PlayerBulletPoolが見つかりません", this); }
        if(TargetCamera == null) { Debug.LogError($"{name}: TargetCameraが見つかりません", this); }
        if(TimingGaugeUI == null) { Debug.LogError($"{name}: TimingGaugeUIが見つかりません", this); }
        if(LifeUIController == null) { Debug.LogError($"{name}: LifeUIControllerが見つかりません", this); }
        if(FocusCamera == null) { Debug.LogError($"{name}: FocusCameraが見つかりません", this); }
        if(SceneChange == null) { Debug.LogError($"{name}: SceneChangeが見つかりません", this); }
        if(AreaAttack == null) { Debug.LogError($"{name}: AreaAttackが見つかりません", this); }
        if(BulletDeleteArea == null) { Debug.LogError($"{name}: BulletDeleteAreaが見つかりません", this); }
        if(MapBounds == null) { Debug.LogError($"{name}: MapBoundsが見つかりません", this); }
    }
}
