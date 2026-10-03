/*********************************************************************/
// * \file   Treasure.cs
// * \brief  宝箱クラス
// *
// * \author 成田悠真, 鈴木裕稀
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 宝箱クラス
/// </summary>
public class Treasure : MonoBehaviour
{
    [SerializeField] private string id; // 宝箱を識別するためのID

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

    private string key; // セーブ用の名前

    /// <summary>
    /// Awake関数は、オブジェクトが有効化されたときに一度だけ呼ばれる
    /// </summary>
    private void Awake()
    {
        key = gameObject.scene.name + "/" + id;// セーブ用の名前を設定

        // 保存済みで開いていたら、宝箱を開いた状態にする
        if(Save.Get(key, false))
        {
            SetBeforeRenderer();
        }
    }

    /// <summary>
    /// OnCollisionEnter2D関数は、他のオブジェクトと衝突したときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    private void OnCollisionEnter2D(Collision2D other)
    {
        if(getKey)
        {
            return;
        }

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
                    SetBeforeRenderer();

                    //Debug.Log("[Treasure] 開封 key=" + key);

                    // 開けた瞬間に仮置きで記録
                    Save.SetPending(key, true);
                }
            }
            else
            {
                // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
                SetBeforeRenderer();
            }
        }
    }


    // 宝箱のスプライトを変更し、鍵を取得したことを示すフラグを立てる
    private void SetBeforeRenderer()
    {
        beforeRenderer.sprite = afterSprite;
        getKey = true;
    }

    private bool HasKey()
    {
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
