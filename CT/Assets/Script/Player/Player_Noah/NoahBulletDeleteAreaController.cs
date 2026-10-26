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

    [SerializeField] 
    private GameObject bulletDeleteArea;

    [Header("デバッグ設定")]

    [SerializeField]
    private bool isDebugMode = false;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(bulletDeleteArea == null) { Debug.LogError("弾丸削除エリアが設定されていません"); }
    }

    void Update()
    {
        if(isDebugMode)
        {
            bulletDeleteArea.SetActive(true);
        }
        else
        {
            // Qキーが押されている間はエリアを有効化する
            if(Keyboard.current.qKey.isPressed)
            {
                bulletDeleteArea.SetActive(true);
            }
            else
            {
                bulletDeleteArea.SetActive(false);
            }
        }
    }
}
