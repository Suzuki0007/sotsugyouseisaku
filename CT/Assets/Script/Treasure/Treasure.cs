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
    public static Treasure instance;
    public bool getkey;

    /// <summary>
    /// 宝箱のスプライトレンダラー
    /// </summary>
    /// 
    /// SerializeField属性を使用して、Inspector上で設定できるようにする
    [SerializeField]
    private SpriteRenderer beforeRenderer;

    /// <summary>
    /// 宝箱を開けた後のスプライト
    /// </summary>
    /// 
    /// SerializeField属性を使用して、Inspector上で設定できるようにする
    [SerializeField]
    private Sprite afterSprite;

    /// <summary>
    /// Awake関数は、オブジェクトが有効化されたときに一度だけ呼ばれる
    /// </summary>
    private void Awake()
    {
        // シングルトンのインスタンスを設定
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
            // プレイヤーが鍵を取得していない場合
            if(getkey == false)
            {
                // 宝箱のスプライト変更、鍵を取得したことを示すフラグを立てる
                beforeRenderer.sprite = afterSprite;
                getkey = true;
            }
        }
    }
}
