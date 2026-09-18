using UnityEngine;

public class kari_player2 : MonoBehaviour
{
    public kari_player player;

    // 画像を表示するためのSpriteRendererコンポーネントを取得するための変数
    SpriteRenderer spriteRenderer;

    // 画像を表示するためのSpriteコンポーネントを取得するための変数
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    // 何個前の位置を追いかけるか
    public int followDistance = 10;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 60;

        spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = player.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // プレイヤーの位置に追従する
        if(player.posHistory.Count > followDistance)
        {
            // プレイヤーの位置履歴のfollowDistance個前の位置に追従する
            transform.position = player.posHistory[player.posHistory.Count - followDistance];
        }

        // プレイヤーの位置に応じて画像レイヤーを切り替える
        if(player.transform.position.y < transform.position.y)
        {
            spriteRenderer.sortingOrder = 99; // 自分が後ろにいく　
        }
        else
        {
            spriteRenderer.sortingOrder = 101; // 自分が手前にいく
        }
    }
}
