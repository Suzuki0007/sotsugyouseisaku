/*********************************************************************/
// * \file   NoahAnimation.cs
// * \brief  ノアのアニメーションクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ノアの弾丸削除エリアコントローラー
/// </summary>
public class NoahBulletDeleteAreaController : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private BulletDeleteArea bulletDeleteArea;

    private GameObject followTargetObject;

    private bool isBulletDeleteAreaActive = false;

    [Header("デバッグ設定")]

    [SerializeField]
    private bool isDebugMode = false;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool IsBulletDeleteAreaActive => isBulletDeleteAreaActive;

    public bool IsDebugMode => isDebugMode;

    //=====================================================================
    // 更新
    //=====================================================================

    void Update()
    {
        if(bulletDeleteArea == null || followTargetObject == null) { return; }

        bool isActive = isDebugMode || isBulletDeleteAreaActive;

        bulletDeleteArea.SetAreaActive(isActive);

        // 追従ターゲットの位置にエリアを追従させる
        bulletDeleteArea.transform.position = followTargetObject.transform.position;
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetBulletDeleteArea(BulletDeleteArea newBulletDeleteArea)
    {
        bulletDeleteArea = newBulletDeleteArea;
        if(bulletDeleteArea == null) { Debug.LogError("弾丸削除エリアの設定に失敗しました"); }
    }

    public void SetBulletDeleteAreaActive(bool isActive)
    {
        isBulletDeleteAreaActive = isActive;

        //Debug.Log("弾削除エリアの有効化状態を設定: " + isActive);
    }

    public void SetFollowTarget(GameObject targetObject)
    {
        followTargetObject = targetObject;
        if(followTargetObject == null) { Debug.LogError("追従ターゲットの設定に失敗しました"); }
    }
}
