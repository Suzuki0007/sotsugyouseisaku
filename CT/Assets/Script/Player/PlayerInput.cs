/*********************************************************************/
// * \file   PlayerInput.cs
// * \brief  プレイヤーの入力を管理するクラス
// *
// * \author 成田悠真
/*********************************************************************/

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

    //=====================================================================
    // 関数
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

    private void Update()
    {
        if(Keyboard.current == null) { Debug.LogError($"{name}: Keyboard.currentがnullです"); return; }

        currentPlayer = playerSwitchManager.CurrentPlayer;
        if(currentPlayer == null) { Debug.LogError($"{name}: CurrentPlayerがnullです"); return; }

        PlayerMovement movement = currentPlayer?.GetComponent<PlayerMovement>();
        if(movement == null) { Debug.LogError($"{name}: PlayerMovementがnullです"); return; }

        Debug.Log("移動方向入力取得前");
        Vector2 moveInput = GetMoveInput();

        Debug.Log("移動方向設定前");
        movement.SetMoveDirection(moveInput);
    }

    /// <summary>
    /// 移動入力を取得する
    /// </summary>
    /// <returns> 移動入力 </returns>
    public Vector2 GetMoveInput()
    {
        Debug.Log("関数内への侵入");
        float horizontal = 0f;
        float vertical = 0f;

        if(Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            Debug.Log("入力検知: 右");
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
