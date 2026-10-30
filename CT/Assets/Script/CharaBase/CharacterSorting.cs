/*********************************************************************/
// * \file   CharacterSorting.cs
// * \brief  キャラクターのソートクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キャラクターのソートクラス
/// </summary>
public class CharacterSorting : CharaBase
{
    private GameObject player;

    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        if(player == null) { return; }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 更新後に毎フレーム呼ばれる関数
    /// </summary>
    void LateUpdate()
    {
        if(player == null) { return; }

        // プレイヤーの位置に応じてソート順を変更
        if(transform.position.y > player.transform.position.y)
        {
            spriteRenderer.sortingOrder = 99;
        }
        else
        {
            spriteRenderer.sortingOrder = 101;
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
        if(player == null) { Debug.LogError("プレイヤーの設定に失敗しました"); }
    }
}
