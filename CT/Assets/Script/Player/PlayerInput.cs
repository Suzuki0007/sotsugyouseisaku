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

    private GameObject currentPlayer;

    [Header("参照")]

    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private SigleShooter sigleShooter;

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

    private void Start()
    {
        // PlayerSwitchManagerのAwakeでCurrentPlayerが設定されるため、Startで取得する
        currentPlayer = playerSwitchManager.CurrentPlayer;
        if(currentPlayer == null)
        {
            Debug.LogError($"{name}: CurrentPlayerがnullです");
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

        // 移動入力を更新
        UpdateMoveInput(currentPlayer);
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

        Vector2 moveInput = GetMoveInput();
        if(moveInput.sqrMagnitude <= 0f) { return; }

        movement.SetMoveDirection(moveInput);
    }

    /// <summary>
    /// 弾の発射入力が行われたときに呼ばれる関数
    /// </summary>
    /// <param name="context"></param>
    public void OnShot(InputAction.CallbackContext context)
    {
        // 入力が開始されていないなら処理を行わない
        if(!context.performed) { return; }

        if(targetCamera == null) { return; }

        GameObject currentPlayer = playerSwitchManager.CurrentPlayer;
        if(currentPlayer == null){ return; }

        SigleShooter shooter = currentPlayer.GetComponent<SigleShooter>();
        if(shooter == null) { return; }

        // マウスのスクリーン座標を取得
        // ReadValue()を使用して、現在のマウス位置を取得
        Vector2 screenPosition = Mouse.current.position.ReadValue();

        // カメラの位置を考慮してスク座をワル座に変換するために、カメラのZ座標を使用
        Vector3 screenPoint = 
            new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z);

        // スクリーン座標をワールド座標に変換
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPoint);

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
