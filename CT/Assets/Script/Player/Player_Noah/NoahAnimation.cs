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
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
