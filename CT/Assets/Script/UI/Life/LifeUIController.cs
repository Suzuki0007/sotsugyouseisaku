/*********************************************************************/
// * \file   LifeUIController.cs
// * \brief  ライフUIコントローラークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ライフUIコントローラークラス
/// </summary>
public class LifeUIController : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("UI画像設定")]

    [SerializeField]
    private Image[] lifeImages;

    [SerializeField]
    private Sprite fullLifeSprite;

    [SerializeField]
    private Sprite emptyLifeSprite;

    [Header("2人の体力の見分け")]

    [SerializeField, Label("一緒のとき（2人で共有）の色")]
    [Tooltip("一緒にいるときは、シグルの体力を2人で共有している（シグルがノアを守っている）")]
    private Color togetherColor = Color.white;

    [SerializeField, Label("離れているときのノアの色")]
    private Color noahColor = new Color(1.0f, 0.78f, 0.88f, 1.0f);

    [SerializeField, Label("離れているときのシグルの色")]
    private Color sigleColor = new Color(0.75f, 0.88f, 1.0f, 1.0f);

    private GameObject player;

    private Health playerHealth;

    // 足りないハートを複製した分も含めた一覧
    private readonly System.Collections.Generic.List<Image> hearts = new System.Collections.Generic.List<Image>();

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(lifeImages == null || lifeImages.Length == 0)
        {
            Debug.LogError("LifeImagesが設定されていません", this);
        }

        if(fullLifeSprite == null || emptyLifeSprite == null)
        {
            Debug.LogError("ライフ画像が設定されていません", this);
        }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    void Update()
    {
        // 今の体力（一緒：シグルの体力を共有 ／ 離れている：操作しているキャラの体力）。なければ設定されたプレイヤー
        Health health = PlayerCharacters.PartyHealth;
        if(health == null) { health = playerHealth; }
        if(health == null) { return; }

        // プレイヤーの現在のライフ数に応じてUI画像を更新する
        UpdateLifeUI(health.CurrentHealth, health.MaxHealth, CurrentColor());
    }

    private Color CurrentColor()
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null || manager.IsTogether) { return togetherColor; }
        return manager.CurrentPlayer == manager.Noah ? noahColor : sigleColor;
    }

    //=====================================================================
    // 体力UI更新関数
    //=====================================================================

    public void UpdateLifeUI(int currentLife)
    {
        UpdateLifeUI(currentLife, lifeImages != null ? lifeImages.Length : 0, togetherColor);
    }

    /// <summary>
    /// ハートを最大の体力の数だけ出し、今の体力の分を満たす
    /// （置いてあるハートより最大の体力が多いときは、最後のハートを複製して並べる）
    /// </summary>
    public void UpdateLifeUI(int currentLife, int maxLife, Color color)
    {
        if(lifeImages == null || lifeImages.Length == 0) { return; }
        EnsureHearts(maxLife);

        for(int i = 0; i < hearts.Count; i++)
        {
            Image heart = hearts[i];
            bool visible = i < maxLife;
            if(heart.gameObject.activeSelf != visible) { heart.gameObject.SetActive(visible); }
            if(!visible) { continue; }

            // 現在のライフ数に応じてUI画像を更新する
            heart.sprite = i < currentLife ? fullLifeSprite : emptyLifeSprite;
            heart.color = color;
        }
    }

    private void EnsureHearts(int count)
    {
        if(hearts.Count == 0)
        {
            foreach(Image image in lifeImages) { if(image != null) { hearts.Add(image); } }
            if(hearts.Count == 0) { return; }
        }

        // 並べる間隔は、置いてあるハートの間隔に合わせる（1つしかなければ幅の分）
        RectTransform first = hearts[0].rectTransform;
        Vector2 step = hearts.Count >= 2
            ? hearts[1].rectTransform.anchoredPosition - first.anchoredPosition
            : new Vector2(first.rect.width * 1.1f, 0.0f);

        while(hearts.Count < count)
        {
            Image last = hearts[hearts.Count - 1];
            Image copy = Instantiate(last, last.transform.parent);
            copy.name = $"{lifeImages[0].name}_{hearts.Count + 1}";
            copy.rectTransform.anchoredPosition = last.rectTransform.anchoredPosition + step;
            hearts.Add(copy);
        }
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    /// <summary>
    /// プレイヤーを設定する
    /// </summary>
    /// <param name="targetPlayer">対象プレイヤー</param>
    public void SetPlayer(GameObject targetPlayer)
    {
        if(targetPlayer == null)
        {
            Debug.LogError("LifeUIControllerの対象プレイヤーがnullです", this);
            playerHealth = null;
            return;
        }

        player = targetPlayer;
        playerHealth = player.GetComponent<Health>();

        if(playerHealth == null)
        {
            Debug.LogError($"{targetPlayer.name}にHealthコンポーネントがありません", targetPlayer);
        }
    }
}
