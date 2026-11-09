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
    /// エディタでステージのシーンから直接再生するときに使うスロットの設定名
    /// （EditorPrefs。0 = 使わない。デバッグウィンドウで切り替える）
    /// </summary>
    public static string EditorSlotOnPlayKey => "CT.SaveSlotOnPlay." + Application.productName;

    // 起動時（最初のシーンを読み込む前）の処理
    // ・スロット導入前の save.json をスロット1に引き継ぐ
    // ・ゲームはタイトル画面から始まり、そこで選んだスロットを読み込む（ここでは読み込まない）
    // ・エディタでステージから直接再生したときだけ、デバッグウィンドウで選んだスロットを読み込む
    //   （最初のシーンの Awake（宝箱など）や InventoryManager より先に読む必要があるので BeforeSceneLoad で呼ぶ）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadOnStartup()
    {
        Save.MigrateLegacy();

#if UNITY_EDITOR
        if(SceneManager.GetActiveScene().name == GameSession.TitleSceneName) { return; }

        int slot = UnityEditor.EditorPrefs.GetInt(EditorSlotOnPlayKey, 0);
        if(slot < 1 || slot > Save.SlotCount)
        {
            Debug.Log("[セーブ] スロットを使わずに始めます（この再生の内容はファイルに保存されません。デバッグウィンドウで変更できます）");
            return;
        }

        Save.LoadSlot(slot);
        GameSession.PlayTime = Save.Get(SaveSlotInfo.PlayTimeKey, 0f);
        Debug.Log($"[セーブ] デバッグウィンドウの設定により、スロット{slot}を読み込みました（{Save.Count} 件）");
#endif
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
