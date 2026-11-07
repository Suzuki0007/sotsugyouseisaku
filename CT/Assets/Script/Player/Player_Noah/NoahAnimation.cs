/*********************************************************************/
// * \file   NoahAnimation.cs
// * \brief  ノアのアニメーションクラス
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
public class NoahAnimation : CharaBase
{
    private SpriteRenderer spriteRenderer;
    private bool replaced;

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
        replaced = GetComponent<SpriteAnimator>() != null;
    }

    /// <summary>
    /// 向きを設定する関数
    /// </summary>
    /// 
    /// <param name="direction"></param>
    public void SetDirection(Vector2 direction)
    {
        if(replaced || spriteRenderer == null) { return; }

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
