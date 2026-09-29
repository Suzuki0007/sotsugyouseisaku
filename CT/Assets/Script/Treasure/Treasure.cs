/*********************************************************************/
// * \file   Treasure.cs
// * \brief  宝箱クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 宝箱クラス
/// </summary>
public class Treasure : Intaractable
{
    [SerializeField] private static Treasure instance;
    [SerializeField] private bool needKey;
    [SerializeField] private bool getKey;

    /// <summary>
    /// 宝箱のスプライトレンダラー
    /// </summary>
    /// 
    /// SerializeField属性を使用して、Inspector上で設定できるようにする
    [SerializeField] private SpriteRenderer beforeRenderer;

    /// <summary>
    /// 宝箱を開けた後のスプライト
    /// </summary>
    /// 
    /// SerializeField属性を使用して、Inspector上で設定できるようにする
    [SerializeField] private Sprite afterSprite;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Interact関数は、プレイヤーが宝箱を開けるときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        Debug.Log("宝箱にインタラクトした");

        if(needKey)
        {
            if(!HasKey())
            {
                Debug.Log("宝箱を開けるには鍵が必要です");
                return;
            }
            else
            {
                Debug.Log("宝箱を開けました");

                // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
                beforeRenderer.sprite = afterSprite;
                getKey = true;
            }
        }
        else
        {
            // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
            beforeRenderer.sprite = afterSprite;
            getKey = true;
        }
    }

    private bool HasKey()
    {
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
