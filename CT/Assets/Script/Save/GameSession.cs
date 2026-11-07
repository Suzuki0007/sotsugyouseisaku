/*********************************************************************/
// * \file   GameSession.cs
// * \brief  1回のプレイ（はじめから・つづきから・タイトルに戻る）の管理
// *
// * \author
/*********************************************************************/

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 1回のプレイを管理するクラス
/// </summary>
///
/// ・はじめから：スロットを空にして、最初のシーンへ
/// ・つづきから：スロットを読み込んで、記録した場所（シーン＋出現位置 か 位置）へ
/// ・タイトルに戻る：スロットを閉じてタイトルへ
/// ・プレイ時間を数え、確定（セーブ）のたびに「再開する場所」「プレイ時間」「保存日時」を記録する
public static class GameSession
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary>
    /// タイトル画面のシーン名
    /// </summary>
    public const string TitleSceneName = "Title";

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// プレイ時間（秒）。タイトル画面・暗転中・スロットを使っていない間は数えない
    /// </summary>
    public static float PlayTime { get; set; }

    /// <summary>
    /// 再開する場所の記録がないセーブを読んだときに入るシーン（タイトル画面が「最初のシーン」を入れる）
    /// </summary>
    public static string FallbackScene { get; set; } = "Stage1_1";

    //=====================================================================
    // 初期化
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        PlayTime = 0f;
        FallbackScene = "Stage1_1";
    }

    // Save の ResetOnPlay（SubsystemRegistration）より後に登録する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Save.BeforeCommit -= OnBeforeCommit;
        Save.BeforeCommit += OnBeforeCommit;

        // プレイ時間を数えるためのオブジェクト（シーンをまたいで残る）
        var go = new GameObject(nameof(GameSession));
        go.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<PlayTimeCounter>();
    }

    // 確定のたびに、プレイ時間と保存日時を記録する
    private static void OnBeforeCommit()
    {
        if(!Save.HasSlot) { return; }

        Save.SetPending(SaveSlotInfo.PlayTimeKey, PlayTime);
        Save.SetPending(SaveSlotInfo.SavedAtKey, DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
    }

    //=====================================================================
    // はじめから・つづきから・タイトルへ
    //=====================================================================

    /// <summary>
    /// はじめから：スロットを空にして、最初のシーンから始める（スロットのデータは最初のセーブで上書きされる）
    /// </summary>
    public static void StartNewGame(int slot, string firstScene, string firstSpawnPointId)
    {
        if(SceneFader.IsBusy) { return; }

        Save.StartNewSlot(slot);
        PlayTime = 0f;
        ResetRuntimeState();

        PlayerSpawnPoint.PendingSpawnPointId = firstSpawnPointId;
        PlayerSpawnPoint.PendingPosition = null;
        SceneFader.Instance.FadeAndLoad(firstScene);
    }

    /// <summary>
    /// つづきから：スロットを読み込んで、記録した場所から再開する
    /// </summary>
    /// <param name="fallbackScene"> 再開する場所の記録がないとき（古いセーブなど）のシーン </param>
    public static void Continue(int slot, string fallbackScene)
    {
        if(SceneFader.IsBusy) { return; }

        Save.LoadSlot(slot);
        ResetRuntimeState();

        SaveSlotInfo info = SaveSlotInfo.FromCurrent();
        PlayTime = info.PlayTime;

        string scene = string.IsNullOrEmpty(info.SceneName) ? fallbackScene : info.SceneName;
        if(info.HasPosition)
        {
            PlayerSpawnPoint.PendingSpawnPointId = null;
            PlayerSpawnPoint.PendingPosition = info.Position;
        }
        else
        {
            PlayerSpawnPoint.PendingSpawnPointId = info.SpawnPointId;
            PlayerSpawnPoint.PendingPosition = null;
        }

        Debug.Log($"[セーブ] スロット{slot}を読み込みました（{scene}、プレイ時間 {SaveSlotInfo.FormatPlayTime(PlayTime)}）");
        SceneFader.Instance.FadeAndLoad(scene);
    }

    /// <summary>
    /// タイトルに戻る（確定済みのデータは保存してから、スロットを閉じる）
    /// </summary>
    public static void ReturnToTitle()
    {
        if(SceneFader.IsBusy) { return; }

        Save.Write();
        Save.CloseSlot();
        ResetRuntimeState();
        PlayTime = 0f;

        PlayerSpawnPoint.PendingSpawnPointId = null;
        PlayerSpawnPoint.PendingPosition = null;
        SceneFader.Instance.FadeAndLoad(TitleSceneName);
    }

    /// <summary>
    /// セーブポイントでセーブする：再開する場所をこの位置にして、選んだスロットに書き込む
    /// （ほかのスロットを選んだ場合は、以後そのスロットでプレイを続ける）
    /// </summary>
    public static void SaveAt(int slot, string sceneName, Vector2 position)
    {
        RecordResumePosition(sceneName, position);
        Save.SaveToSlot(slot);
        Debug.Log($"[セーブ] スロット{slot}にセーブしました（{sceneName} {position}）");
    }

    //=====================================================================
    // 再開する場所の記録
    //=====================================================================

    /// <summary>
    /// 再開する場所を「シーン＋出現位置ID」で仮置きする（シーンに着いたとき）
    /// </summary>
    public static void RecordResumePoint(string sceneName, string spawnPointId)
    {
        Save.SetPending(SaveSlotInfo.SceneKey, sceneName);
        Save.SetPending(SaveSlotInfo.SpawnPointKey, spawnPointId ?? "");
        Save.SetPending(SaveSlotInfo.HasPosKey, false);
    }

    /// <summary>
    /// 再開する場所を「シーン＋位置」で仮置きする（チェックポイント）
    /// </summary>
    public static void RecordResumePosition(string sceneName, Vector2 position)
    {
        Save.SetPending(SaveSlotInfo.SceneKey, sceneName);
        Save.SetPending(SaveSlotInfo.SpawnPointKey, "");
        Save.SetPending(SaveSlotInfo.HasPosKey, true);
        Save.SetPending(SaveSlotInfo.PosXKey, position.x);
        Save.SetPending(SaveSlotInfo.PosYKey, position.y);
    }

    //=====================================================================
    // 補助
    //=====================================================================

    /// <summary>
    /// 前のプレイの状態を、今の Save の内容に入れ替える
    /// </summary>
    private static void ResetRuntimeState()
    {
        GameFlags.ClearTemporary();
        SceneChange.LastStageScene = null;

        if(InventoryManager.Instance != null) { InventoryManager.Instance.ReloadFromSave(); }
    }

    /// <summary>
    /// プレイ時間を進める（PlayTimeCounter から毎フレーム呼ばれる）
    /// </summary>
    internal static void Tick(float deltaTime)
    {
        if(!Save.HasSlot) { return; }
        if(SceneFader.IsBusy) { return; }
        if(SceneManager.GetActiveScene().name == TitleSceneName) { return; }

        PlayTime += deltaTime;
    }
}
