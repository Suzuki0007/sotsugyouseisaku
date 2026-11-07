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
