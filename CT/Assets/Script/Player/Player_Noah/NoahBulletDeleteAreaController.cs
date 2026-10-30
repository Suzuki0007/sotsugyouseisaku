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

    [Header("デバッグ設定")]

    [SerializeField]
    private bool isDebugMode = false;

    //=====================================================================
    // 更新
    //=====================================================================

    void Update()
    {
        if(bulletDeleteArea == null) { return; }

        // デバッグモードが有効な場合は常にエリアを有効化する
        if(isDebugMode)
        {
            bulletDeleteArea.enabled = true;
        }
        else
        {
            // Qキーが押されている間はエリアを有効化する
            if(Keyboard.current.qKey.isPressed)
            {
                bulletDeleteArea.enabled = true;
            }
            else
            {
                bulletDeleteArea.enabled = false;
            }
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetBulletDeleteArea(BulletDeleteArea newBulletDeleteArea)
    {
        bulletDeleteArea = newBulletDeleteArea;
        if(bulletDeleteArea == null) { Debug.LogError("弾丸削除エリアの設定に失敗しました"); }
    }
}
