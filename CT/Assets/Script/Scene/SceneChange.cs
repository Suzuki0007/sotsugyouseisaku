/*********************************************************************/
// * \file   SceneChange.cs
// * \brief  シーン遷移クラス
// *
// * \author 成田悠真, 鈴木裕稀
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移クラス
/// </summary>
public class SceneChange : MonoBehaviour
{
    [SerializeField] private string sceneName; // 遷移先のシーン名をInspectorで設定できるようにする

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("何かがDoorに衝突しました");

        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("PlayerがDoorに衝突しました");

            Save.Commit(); // セーブデータを保存する

            SceneManager.LoadScene(sceneName);
        }
    }
}
