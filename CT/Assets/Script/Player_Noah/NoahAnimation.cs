/*********************************************************************/
// * \file   NoahAnimation.cs
// * \brief  ノアのアニメーションクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// アニメーションクラス
/// </summary>
public class NoahAnimation : CharaBase
{
    SpriteRenderer spriteRenderer;

    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 毎フレーム呼ばれる関数
    /// </summary>
    void Update()
    {

    }

    /// <summary>
    /// 向きを設定する関数
    /// </summary>
    /// 
    /// <param name="direction"></param>
    public void SetDirection(Vector2 direction)
    {
        // 移動方向によって画像を変更
        if(direction.x > 0)
        {
            spriteRenderer.sprite = rightSprite;
        }
        else if(direction.x < 0)
        {
            spriteRenderer.sprite = leftSprite;
        }
        else if(direction.y > 0)
        {
            spriteRenderer.sprite = upSprite;
        }
        else if(direction.y < 0)
        {
            spriteRenderer.sprite = downSprite;
        }
    }
}
