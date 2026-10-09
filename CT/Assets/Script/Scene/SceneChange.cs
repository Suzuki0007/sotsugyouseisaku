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
        if(collision.gameObject.CompareTag("Player"))
        {
            Save.Commit();

            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// シーンを変更するメソッド
    /// </summary>
    /// <param name="sceneName"></param>
    public void ChangeScene(string sceneName)
    {
        Save.Commit();
        SceneManager.LoadScene(sceneName);
    }
}
