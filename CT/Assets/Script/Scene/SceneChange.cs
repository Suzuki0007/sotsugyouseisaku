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

    // 遷移先での出現位置ID (遷移先シーンの PlayerSpawnPoint のIDと合わせる)
    // 空欄なら遷移先のデフォルト位置に出現する
    [SerializeField]
    private string spawnPointId;

    [Header("SE")]

    [SerializeField, Tooltip("遷移を始めたとき (暗転の開始) に鳴らす。例: ドアを開ける音")]
    private SeData leaveSe;

    [SerializeField, Tooltip("遷移先に着いたとき (明転の開始) に鳴らす。例: ドアを閉める音")]
    private SeData arriveSe;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            ChangeScene(sceneName, spawnPointId);
        }
    }

    /// <summary>
    /// シーンを変更するメソッド (暗転演出を挟んで遷移する)
    /// 遷移先ではデフォルトの位置に出現する
    /// </summary>
    /// <param name="sceneName"></param>
    public void ChangeScene(string sceneName)
    {
        ChangeScene(sceneName, null);
    }

    /// <summary>
    /// 出現位置を指定してシーンを変更するメソッド (暗転演出を挟んで遷移する)
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    /// <param name="spawnPointId">遷移先での出現位置ID</param>
    public void ChangeScene(string sceneName, string spawnPointId)
    {
        ChangeScene(sceneName, spawnPointId, leaveSe, arriveSe);
    }

    /// <summary>
    /// SEを指定してシーンを変更するメソッド
    /// (このコンポーネントに設定したSEではなく、呼び出し側のSEを鳴らしたいとき用。例: ゲームオーバー)
    /// </summary>
    public void ChangeScene(string sceneName, string spawnPointId, SeData leaveSe, SeData arriveSe)
    {
        // 遷移中の多重呼び出し(連続接触など)を無視する
        if(SceneFader.Instance.IsFading) { return; }

        Save.Commit();
        PlayerSpawnPoint.PendingSpawnPointId = spawnPointId;
        SceneFader.Instance.FadeAndLoad(sceneName, leaveSe, arriveSe);
    }
}
