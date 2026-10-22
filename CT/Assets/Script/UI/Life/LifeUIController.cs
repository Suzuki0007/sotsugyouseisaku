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

    [SerializeField]
    private GameObject player;

    private Health playerHealth;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(player == null) { Debug.LogError("プレイヤーが設定されていません"); }

        playerHealth = player.GetComponent<Health>();
        if(playerHealth == null) { Debug.LogError("プレイヤーにHealthコンポーネントがアタッチされていません"); }
    }

    void Update()
    {
        // プレイヤーの現在のライフ数に応じてUI画像を更新する
        UpdateLifeUI(playerHealth.CurrentHealth);
    }
     
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
}
