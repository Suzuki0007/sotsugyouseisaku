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
    /// （EditorPrefs。0 = 使わない、1〜3 = 手動の枠、Save.AutoSlot = オートセーブ枠。デバッグウィンドウで切り替える）
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
        if(slot != Save.AutoSlot && (slot < 1 || slot > Save.SlotCount))
        {
            Debug.Log("[セーブ] スロットを使わずに始めます（この再生の内容はファイルに保存されません。デバッグウィンドウで変更できます）");
            return;
        }

        Save.LoadSlot(slot);
        GameSession.PlayTime = Save.Get(SaveSlotInfo.PlayTimeKey, 0f);
        Debug.Log($"[セーブ] デバッグウィンドウの設定により、スロット{slot}を読み込みました（{Save.Count} 件）");
#endif
    }

    // 終了時・シーンを読み込んだときの書き込みはやめた（2026-11-13）
    // ・ファイルに書くのは、オートセーブ（シーンに着いたとき＝PlayerPartySpawner、イベントの部品「オートセーブ」、
    //   チェックポイント）と、セーブポイントの手動セーブだけ
    // ・終了時・シーンの読み込み（ゲームオーバー画面など）で書くと、「進み具合は新しいのに、再開する場所は古い」
    //   セーブになり、ロードしたときに通れない場所に出ることがあった（Stage1_2 のガレキの向こう側）
    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
    }
}
