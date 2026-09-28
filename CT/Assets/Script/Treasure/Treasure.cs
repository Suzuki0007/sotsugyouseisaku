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
public class Treasure : MonoBehaviour
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

    /// <summary>
    /// Awake関数は、オブジェクトが有効化されたときに一度だけ呼ばれる
    /// </summary>
    private void Awake()
    {
        instance = this;
    }
    
    /// <summary>
    /// OnCollisionEnter2D関数は、他のオブジェクトと衝突したときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionEnter2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
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
    }

    private bool HasKey()
    {
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
