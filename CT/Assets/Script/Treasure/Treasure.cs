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
public class Treasure : Intaractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    // 宝箱を識別するためのID
    [SerializeField]
    private string id;

    [SerializeField]
    private bool needKey;

    [SerializeField] 
    private bool getKey;

    [SerializeField] 
    private SpriteRenderer beforeRenderer;

    [SerializeField]
    private Sprite afterSprite;

    private string key; // セーブ用の名前

    //=====================================================================
    // 関数
    //=====================================================================

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
    /// Interact関数は、プレイヤーが宝箱を調べたときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        if(getKey) { return; }

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

    /// <summary>
    /// 画像を変更して、宝箱を開いた状態にする
    /// </summary>
    private void SetBeforeRenderer()
    {
        beforeRenderer.sprite = afterSprite;
        getKey = true;
    }

    /// <summary>
    /// 鍵を持っているかどうかを判定する関数
    /// </summary>
    /// <returns></returns>
    private bool HasKey()
    {
        return InventoryManager.Instance.NoahData.hasKey;
    }
}
