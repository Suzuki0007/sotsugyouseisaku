/*********************************************************************/
// * \file   AutoSave.cs
// * \brief  自動セーブクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

public static class  AutoSave
{
    /// <summary>
    /// エディタで再生するときにセーブを読み込むかの設定名（EditorPrefs。デバッグウィンドウで切り替える）
    /// </summary>
    public static string EditorLoadOnPlayKey => "CT.LoadSaveOnPlay." + Application.productName;

    // 起動時（最初のシーンを読み込む前）にセーブデータを読み込む
    // 最初のシーンの Awake（宝箱など）や InventoryManager より先に読む必要があるので BeforeSceneLoad で呼ぶ
    // （Save の ResetOnPlay は SubsystemRegistration なので、それより後に呼ばれる）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadOnStartup()
    {
#if UNITY_EDITOR
        // エディタでは、デバッグウィンドウの設定で「読み込まない」にできる（毎回まっさらな状態で確認したいとき）
        if(!UnityEditor.EditorPrefs.GetBool(EditorLoadOnPlayKey, true))
        {
            Debug.Log("[セーブ] デバッグウィンドウの設定により、セーブを読み込まずに始めます");
            return;
        }
#endif

        Save.Read();

        if(Save.Count > 0) { Debug.Log($"[セーブ] セーブデータを読み込みました（{Save.Count} 件）"); }
    }

    // RuntimeInitializeOnLoadMethodは1度だけstaticの関数を自動で呼び出せる
    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        // ドメインリロード無効のエディタでは、前回の再生の登録が残っているので先に外す
        // （外さないと、再生するたびに同じ処理が二重・三重に呼ばれる）
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnQuitting;

        SceneManager.sceneLoaded += OnSceneLoaded;

        Application.quitting += OnQuitting;// Application.quittingはアプリケーションが終了する時に呼ばれるイベント
    }

    // 終了時に呼ばれるイベントの解除
    private static void OnQuitting()
    {
        Save.Write();
    }

    // シーンがロードされた時に呼ばれるイベント
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Save.WriteAsync();
    }
}
