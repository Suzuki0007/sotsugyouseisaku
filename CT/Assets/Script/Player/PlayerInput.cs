/*********************************************************************/
// * \file   PlayerInput.cs
// * \brief  プレイヤーの入力を管理するクラス
// *
// * \author 成田悠真
/*********************************************************************/

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力を管理するクラス
/// </summary>
public class PlayerInput : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private PlayerSwitchManager playerSwitchManager;

    [Header("参照")]

    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private SigleShooter sigleShooter;

    [Header("入力設定")]

    [SerializeField]
    private Key bulletDeleteAreaKey = Key.Q;

    private GameObject currentPlayer;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(playerSwitchManager == null)
        {
            Debug.LogError($"{name}: PlayerSwitchManagerが設定されていません");
            enabled = false;
            return;
        }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(Keyboard.current == null) { return; }

        currentPlayer = playerSwitchManager.CurrentPlayer;
        if(currentPlayer == null) { Debug.LogError($"{name}: CurrentPlayerがnullです"); return; }

        // 会話中・シーン遷移中・イベント演出中・メニュー画面などでは移動と弾削除エリアを止める
        if(InputBlock.IsBlocked)
        {
            StopPlayerActions(currentPlayer);
            return;
        }

        // 移動入力を更新
        UpdateMoveInput(currentPlayer);

        // 弾削除エリアの有効化を更新
        UpdateBulletDeleteAreaInput(currentPlayer);

        // 射撃（マウスの左クリック）
        UpdateShotInput(currentPlayer);
    }

    //=====================================================================
    // アクション開始関数
    //=====================================================================

    /// <summary>
    /// 移動入力を更新する
    /// </summary>
    /// <param name="currentPlayer"></param>
    private void UpdateMoveInput(GameObject currentPlayer)
    {
        PlayerMovement movement = currentPlayer?.GetComponent<PlayerMovement>();
        if(movement == null) { return; }
        if(!movement.CanControl) { return; }

        // 移動入力を取得して、移動方向を設定
        movement.SetMoveDirection(GetMoveInput());
    }

    /// <summary>
    /// 弾削除エリアの有効化を更新する
    /// </summary>
    /// <param name="currentPlayer"></param>
    private void UpdateBulletDeleteAreaInput(GameObject currentPlayer)
    {
        GameObject noah = playerSwitchManager.Noah;
        if(noah == null) { return; }

        NoahBulletDeleteAreaController areaController =
            currentPlayer?.GetComponent<NoahBulletDeleteAreaController>();
        if(areaController == null) { return; }

        bool isPressed = Keyboard.current[bulletDeleteAreaKey].isPressed;

        // 弾削除エリアを有効化
        areaController.SetBulletDeleteAreaActive(isPressed);
    }

    /// <summary>
    /// 会話中などに、プレイヤーの移動と弾削除エリアを止める
    /// </summary>
    /// <param name="currentPlayer"></param>
    private void StopPlayerActions(GameObject currentPlayer)
    {
        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if(movement != null) { movement.Stop(); }

        NoahBulletDeleteAreaController areaController =
            currentPlayer.GetComponent<NoahBulletDeleteAreaController>();
        if(areaController != null) { areaController.SetBulletDeleteAreaActive(false); }
    }

    /// <summary>
    /// マウスの左クリックで、操作中のキャラが SigleShooter を持っていれば撃つ
    /// </summary>
    /// 以前は Input System の PlayerInput コンポーネントのイベント（OnShot）から呼ぶ想定だったが、
    /// そのコンポーネントがどこにもなく、一度も呼ばれていなかった。移動・Q と同じく、ここで直接読む
    private void UpdateShotInput(GameObject currentPlayer)
    {
        if(Mouse.current == null) { return; }
        if(!Mouse.current.leftButton.wasPressedThisFrame) { return; }

        ShootAtMouse(currentPlayer);
    }

    /// <summary>
    /// 弾の発射入力が行われたときに呼ばれる関数（Input System の PlayerInput コンポーネントを使う場合用。今は使っていない）
    /// </summary>
    /// <param name="context"></param>
    public void OnShot(InputAction.CallbackContext context)
    {
        // 入力が開始されていないなら処理を行わない
        if(!context.performed) { return; }

        // 会話中・シーン遷移中は撃たない
        if(InputBlock.IsBlocked) { return; }

        ShootAtMouse(playerSwitchManager.CurrentPlayer);
    }

    /// <summary>
    /// マウスの位置へ撃つ
    /// </summary>
    private void ShootAtMouse(GameObject currentPlayer)
    {
        if(currentPlayer == null) { return; }
        if(Mouse.current == null) { return; }

        SigleShooter shooter = currentPlayer.GetComponent<SigleShooter>();
        if(shooter == null) { return; }

        // Target Camera が空なら、メインカメラを使う（シーンで設定し忘れても撃てるように）
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if(cam == null) { return; }

        // マウスのスクリーン座標を取得
        Vector2 screenPosition = Mouse.current.position.ReadValue();

        // カメラの位置を考慮してスクリーン座標をワールド座標に変換するために、カメラのZ座標を使用
        Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z);

        // スクリーン座標をワールド座標に変換
        Vector3 worldPoint = cam.ScreenToWorldPoint(screenPoint);

        shooter.Shoot(worldPoint);
    }

    //=====================================================================
    // 入力取得関数
    //=====================================================================

    /// <summary>
    /// 移動入力を取得する
    /// </summary>
    /// <returns> 移動入力 </returns>
    public Vector2 GetMoveInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if(Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal = 1f;
        }
        else if(Keyboard.current.aKey.isPressed ||
                 Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal = -1f;
        }

        if(Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical = 1f;
        }
        else if(Keyboard.current.sKey.isPressed ||
                 Keyboard.current.downArrowKey.isPressed)
        {
            vertical = -1f;
        }

        return new Vector2(horizontal, vertical).normalized;
    }
}
