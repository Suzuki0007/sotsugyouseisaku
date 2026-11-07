/*********************************************************************/
// * \file   Sigle_Animation.cs
// * \brief  シグルのアニメーションクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// アニメーションクラス (古い仕組み : 向きごとに静止画を1枚差し替える)
///
/// 新しい仕組みは SpriteAnimator + CharacterAnimLink
/// (Tools → Animation → サンプルを作成して割り当てる で置き換えられる)
/// SpriteAnimator が付いている場合は、画像を上書きしないように何もしない
/// </summary>
public class SigleAnimation : CharaBase
{
    private SpriteRenderer spriteRenderer;
    private bool replaced;

    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        replaced = GetComponent<SpriteAnimator>() != null;
    }

    public void SetDirection(Vector2 direction)
    {
        if(replaced || spriteRenderer == null) { return; }

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
