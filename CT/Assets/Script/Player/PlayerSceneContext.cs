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

    [SerializeField]
    private MapBounds mapBounds;

    [Header("弾")]
    [SerializeField]
    private PlayerBulletPool playerBulletPool;

    [Header("フォーカスカメラ")]
    [SerializeField]
    private FocusCamera focusCamera;

    [Header("シーン切り替え")]
    [SerializeField]
    private SceneChange sceneChange;

    [Header("UI")]
    [SerializeField]
    private TimingGaugeUI timingGaugeUI;

    [SerializeField]
    private LifeUIController lifeUIController;

    [Header("カメラ")]
    [SerializeField]
    private Camera targetCamera;

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

    public MapBounds MapBounds => mapBounds;

    public PlayerBulletPool PlayerBulletPool => playerBulletPool;

    public FocusCamera FocusCamera => focusCamera;

    public SceneChange SceneChange => sceneChange;

    public TimingGaugeUI TimingGaugeUI => timingGaugeUI;

    public LifeUIController LifeUIController => lifeUIController;

    public Camera TargetCamera => targetCamera;

    public AreaAttack AreaAttack => areaAttack;

    public BulletDeleteArea BulletDeleteArea => bulletDeleteArea;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        // 参照が正しく設定されているかを検証する
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        bool isValid = true;

        // isValid = isValid && ValidateReference(...);
        isValid &= ValidateReference(playerLimitTilemap, nameof(playerLimitTilemap));
        isValid &= ValidateReference(mapBounds, nameof(mapBounds));
        isValid &= ValidateReference(playerBulletPool, nameof(playerBulletPool));
        isValid &= ValidateReference(focusCamera, nameof(focusCamera));
        isValid &= ValidateReference(sceneChange, nameof(sceneChange));
        isValid &= ValidateReference(timingGaugeUI, nameof(timingGaugeUI));
        isValid &= ValidateReference(lifeUIController, nameof(lifeUIController));
        isValid &= ValidateReference(targetCamera, nameof(targetCamera));
        isValid &= ValidateReference(areaAttack, nameof(areaAttack));
        isValid &= ValidateReference(bulletDeleteArea, nameof(bulletDeleteArea));

        if(!isValid)
        {
            Debug.LogError($"{name}: PlayerSceneContextの参照設定が不足しています", this);
        }
    }

    private bool ValidateReference(Object reference, string referenceName)
    {
        if(reference != null) { return true; }

        Debug.LogError($"{name}: {referenceName}が設定されていません", this);

        return false;
    }
}
