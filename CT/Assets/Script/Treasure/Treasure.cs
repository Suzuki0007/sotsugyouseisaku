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

    [Tooltip("開けたときに鍵を消費する（アイテム欄から鍵が消える）")]
    [SerializeField]
    private bool consumeKey = true;

    [SerializeField] 
    private bool getKey;

    [SerializeField] 
    private SpriteRenderer beforeRenderer;

    [SerializeField]
    private Sprite afterSprite;

    [Header("アニメーション (任意)")]

    [SerializeField, Tooltip("開くアニメを流す SpriteAnimator。空なら画像の差し替え (After Sprite) だけ")]
    private SpriteAnimator openAnimator;

    [SerializeField, Tooltip("開くアニメの動きの名前")]
    private string openAnimation = "Open";

    [Header("SE")]

    [SerializeField, Tooltip("宝箱を開けたとき")]
    private SeData openSe;

    [SerializeField, Tooltip("鍵がなくて開けられなかったとき")]
    private SeData lockedSe;

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
            SetBeforeRenderer(true);
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
                SeManager.PlaySe(lockedSe, transform.position);
                return;
            }
            else
            {
                Debug.Log("宝箱を開けました");

                // 鍵を消費する（アイテム欄から消える）
                if(consumeKey) { InventoryManager.Instance.ConsumeKey(); }

                // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
                SetBeforeRenderer();
                SeManager.PlaySe(openSe, transform.position);

                //Debug.Log("[Treasure] 開封 key=" + key);

                // 開けた瞬間に仮置きで記録
                Save.SetPending(key, true);
            }
        }
        else
        {
            // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
            SetBeforeRenderer();
            SeManager.PlaySe(openSe, transform.position);
        }
    }

    /// <summary>
    /// 画像を変更して、宝箱を開いた状態にする
    /// 開くアニメがあればそれを流す (instant = true ならアニメを飛ばして開いた見た目にする)
    /// </summary>
    private void SetBeforeRenderer(bool instant = false)
    {
        if(openAnimator != null && openAnimator.Has(openAnimation))
        {
            if(instant) { openAnimator.JumpToEnd(openAnimation); }
            else        { openAnimator.Play(openAnimation, true); }
        }
        else
        {
            beforeRenderer.sprite = afterSprite;
        }
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
