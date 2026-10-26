/*********************************************************************/
// * \file   PlayerSwitchManager.cs
// * \brief  プレイヤー切り替え管理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー切り替え管理クラス
/// </summary>
public class PlayerSwitchManager : Singleton<PlayerSwitchManager>
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("プレイヤーキャラクター")]

    [SerializeField]
    private GameObject noah;

    [SerializeField]
    private GameObject sigle;

    [Header("カメラ")]

    [SerializeField]
    private FocusCamera focusCamera;

    [Header("設定")]

    [SerializeField]
    private Key switchKey = Key.C;

    private PlayerMovement noahMovement;
    private PlayerMovement sigleMovement;

    private PlayerHistory noahHistory;
    private PlayerHistory sigleHistory;

    private PlayerFollower noahFollower;
    private PlayerFollower sigleFollower;

    // 現在操作しているキャラクター
    private GameObject currentPlayer;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public GameObject CurrentPlayer => currentPlayer;

    public GameObject Noah => noah;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this) { return; }

        // 初期参照設定
        if(!InitializeReferences()) { enabled = false; return; }

        // 初期状態はノアを操作する
        currentPlayer = noah;
        noahMovement.SetControlEnabled(true);
        sigleMovement.SetControlEnabled(false);

        // 追従対象を設定
        noahFollower.SetTarget(sigle);
        sigleFollower.SetTarget(noah);

        // カメラはノアを追いかける
        focusCamera.SetTarget(noah.transform);
    }

    private void Update()
    {
        if(Keyboard.current == null) { return; }

        if(Keyboard.current[switchKey].wasPressedThisFrame)
        {
            SwitchPlayer();
        }
    }

    /// <summary>
    /// 初期参照設定
    /// </summary>
    /// <returns></returns>
    private bool InitializeReferences()
    {
        if(noah == null || sigle == null) { Debug.LogError("NoahまたはSigleが設定されていません"); return false; }
        if(noah == sigle) { Debug.LogError("NoahとSigleに同じオブジェクトが設定されています"); return false; }
        if(focusCamera == null) { Debug.LogError("FocusCameraが設定されていません"); return false; }

        noahMovement = noah.GetComponent<PlayerMovement>();
        sigleMovement = sigle.GetComponent<PlayerMovement>();
        if(noahMovement == null || sigleMovement == null) { Debug.LogError("NoahまたはSigleに、PlayerMovementが付いていません"); return false; }

        noahHistory = noah.GetComponent<PlayerHistory>();
        sigleHistory = sigle.GetComponent<PlayerHistory>();
        if(noahHistory == null || sigleHistory == null) { Debug.LogError("NoahまたはSigleにPlayerHistoryが付いていません"); return false; }

        noahFollower = noah.GetComponent<PlayerFollower>();
        sigleFollower = sigle.GetComponent<PlayerFollower>();
        if(noahFollower == null || sigleFollower == null) { Debug.LogError("NoahまたはSigleにPlayerFollowerが付いていません"); return false; }

        return true;
    }

    /// <summary>
    /// プレイヤー切り替え関数
    /// </summary>
    public void SwitchPlayer()
    {
        if(currentPlayer == noah)
        {
            SwitchTo(sigle, noah);
        }
        else
        {
            SwitchTo(noah, sigle);
        }
    }

    /// <summary>
    /// プレイヤー切り替え処理
    /// </summary>
    /// <param name="newPlayer"> 切り替え後のプレイヤー </param>
    /// <param name="oldPlayer"> 切り替え前のプレイヤー </param>
    private void SwitchTo(GameObject newPlayer, GameObject oldPlayer)
    {
        // 2人の現在位置を保存
        Vector3 oldPlayerPosition = oldPlayer.transform.position;
        Vector3 newPlayerPosition = newPlayer.transform.position; 
        
        // 位置を交換
        oldPlayer.transform.position = newPlayerPosition;
        newPlayer.transform.position = oldPlayerPosition;
        
        // 操作権を交換
        noahMovement.SetControlEnabled(newPlayer == noah);
        sigleMovement.SetControlEnabled(newPlayer == sigle);
        
        // 現在の操作キャラクターを更新
        currentPlayer = newPlayer;

        // 追従対象を変更
        noahFollower.SetTarget(sigle);
        sigleFollower.SetTarget(noah);

        // カメラの追従先を変更
        focusCamera.SetTarget(currentPlayer.transform);
    }
}