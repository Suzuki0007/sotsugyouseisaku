using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移クラス
/// </summary>
public class Scene_Change : MonoBehaviour
{
    public string sceneName; // 遷移先のシーン名をInspectorで設定できるようにする

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("何かがDoorに衝突しました");

        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("PlayerがDoorに衝突しました");

            SceneManager.LoadScene(sceneName);
        }
    }
}
