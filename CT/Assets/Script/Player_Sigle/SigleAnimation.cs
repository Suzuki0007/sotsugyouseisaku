/*********************************************************************/
// * \file   Sigle_Animation.cs
// * \brief  シグルのアニメーションクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// アニメーションクラス
/// </summary>
public class SigleAnimation : CharaBase
{
    SpriteRenderer spriteRenderer;

    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetDirection(Vector2 direction)
    {
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
