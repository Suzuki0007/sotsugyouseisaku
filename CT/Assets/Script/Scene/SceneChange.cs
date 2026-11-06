/*********************************************************************/
// * \file   SceneChange.cs
// * \brief  シーン遷移クラス
// *
// * \author 成田悠真, 鈴木裕稀
/*********************************************************************/

using UnityEngine;

/// <summary>
/// シーン遷移クラス
/// </summary>
public class SceneChange : MonoBehaviour
{
    // 遷移先のシーン名をInspectorで設定できるようにする
    [SerializeField] 
    private string sceneName;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            ChangeScene(sceneName);
        }
    }

    /// <summary>
    /// シーンを変更するメソッド (暗転演出を挟んで遷移する)
    /// </summary>
    /// <param name="sceneName"></param>
    public void ChangeScene(string sceneName)
    {
        // 遷移中の多重呼び出し(連続接触など)を無視する
        if(SceneFader.Instance.IsFading) { return; }

        Save.Commit();
        SceneFader.Instance.FadeAndLoad(sceneName);
    }
}
