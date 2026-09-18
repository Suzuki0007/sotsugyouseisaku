using UnityEngine;
using UnityEngine.InputSystem;

public class kari_player : MonoBehaviour
{ 
    // 画像を表示するためのSpriteRendererコンポーネントを取得するための変数
    SpriteRenderer spriteRenderer;

    // 画像を表示するためのSpriteコンポーネントを取得するための変数
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    // 移動速度
    public float moveSpeed = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 60;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            transform.Translate(moveSpeed, 0, 0);

            spriteRenderer.sprite = rightSprite;
        }
        if(Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            transform.Translate(-moveSpeed, 0, 0);

            spriteRenderer.sprite = leftSprite;
        }
        if(Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            transform.Translate(0, moveSpeed, 0);

            spriteRenderer.sprite = upSprite;
        }
        if(Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            transform.Translate(0, -moveSpeed, 0);

            spriteRenderer.sprite = downSprite;
        }
    }
}
