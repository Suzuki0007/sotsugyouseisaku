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

    private GameObject player;

    private Health playerHealth;

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
        if(playerHealth == null) { return; }

        // プレイヤーの現在のライフ数に応じてUI画像を更新する
        UpdateLifeUI(playerHealth.CurrentHealth);
    }

    //=====================================================================
    // 体力UI更新関数
    //=====================================================================

    public void UpdateLifeUI(int currentLife)
    {
        for(int i = 0; i < lifeImages.Length; i++)
        {
            // 現在のライフ数に応じてUI画像を更新する
            if(i < currentLife)
            {
                lifeImages[i].sprite = fullLifeSprite;
            }
            else
            {
                lifeImages[i].sprite = emptyLifeSprite;
            }
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
