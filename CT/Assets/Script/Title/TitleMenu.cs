/*********************************************************************/
// * \file   TitleMenu.cs
// * \brief  タイトル画面（はじめから・つづきから・ゲーム終了）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// タイトル画面
/// </summary>
///
/// ・はじめから：保存するスロットを選ぶ（使用中なら上書きの確認）→ 最初のシーンへ
/// ・つづきから：スロットを選ぶ → 記録した場所から再開
/// ・ゲーム終了
/// スロットの選択は共通の SaveSlotPanel を使う（メニュー画面のロード・セーブポイントと同じ画面）
/// 場所の表示名は Assets/Resources/PlaceNames.asset で変える
public class TitleMenu : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("ゲームの始まり")]

    [SerializeField, SceneName, Tooltip("「はじめから」で最初に入るシーン（場所の記録がない古いセーブを「つづきから」で読んだときも、ここから始まる）")]
    private string firstScene = "Stage1_1";

    [SerializeField, SpawnPointId(nameof(firstScene)), Tooltip("最初のシーンでの出現位置（デフォルトならシーンの既定の位置）")]
    private string firstSpawnPointId = "";

    [Header("メインメニュー")]

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private Button newGameButton;

    [SerializeField]
    private Button continueButton;

    [SerializeField]
    private Button quitButton;

    [Header("セーブデータ選択")]

    [SerializeField]
    private SaveSlotPanel slotPanel;

    [Header("SE（任意）")]

    [SerializeField, Tooltip("決定したとき（空なら共通の UI 効果音：Resources/UISounds）")]
    private SeData decideSe;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        // 以前のセーブデータ（save.json）があればスロット1に引き継ぐ
        Save.MigrateLegacy();

        // 場所の記録がないセーブを読んだときの行き先
        GameSession.FallbackScene = firstScene;

        if(newGameButton != null)  { newGameButton.onClick.AddListener(OpenNewGame); }
        if(continueButton != null) { continueButton.onClick.AddListener(OpenContinue); }
        if(quitButton != null)     { quitButton.onClick.AddListener(QuitGame); }

        if(slotPanel != null) { slotPanel.Close(); }
        ShowMain();
    }

    private void Update()
    {
        // マウスでどこかをクリックして選択が外れたら、キーボードで操作できるように戻す
        if(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
           Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if(slotPanel == null || !slotPanel.IsOpen) { SelectDefault(); }
        }
    }

    //=====================================================================
    // 画面の切り替え
    //=====================================================================

    private void ShowMain()
    {
        if(mainMenu != null) { mainMenu.SetActive(true); }
        if(continueButton != null) { continueButton.interactable = AnySlotExists(); }
        SelectDefault();
    }

    /// <summary>
    /// 「はじめから」：保存するスロットを選ぶ
    /// </summary>
    public void OpenNewGame()
    {
        if(slotPanel == null || SceneFader.IsBusy) { return; }

        PlaySe(decideSe);
        if(mainMenu != null) { mainMenu.SetActive(false); }
        slotPanel.Open(SaveSlotPanel.Purpose.NewGame,
            slot => GameSession.StartNewGame(slot, firstScene, firstSpawnPointId),
            ShowMain);
    }

    /// <summary>
    /// 「つづきから」：読み込むスロットを選ぶ
    /// </summary>
    public void OpenContinue()
    {
        if(slotPanel == null || SceneFader.IsBusy || !AnySlotExists()) { return; }

        PlaySe(decideSe);
        if(mainMenu != null) { mainMenu.SetActive(false); }
        slotPanel.Open(SaveSlotPanel.Purpose.Continue,
            slot => GameSession.Continue(slot, firstScene),
            ShowMain);
    }

    /// <summary>
    /// ゲームを終了する
    /// </summary>
    public void QuitGame()
    {
        PlaySe(decideSe);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //=====================================================================
    // 補助
    //=====================================================================

    /// <summary>
    /// 最初に選ぶボタンを選ぶ（キーボード操作用）
    /// </summary>
    private void SelectDefault()
    {
        if(EventSystem.current == null) { return; }

        GameObject target = continueButton != null && continueButton.interactable ? continueButton.gameObject
                          : newGameButton != null ? newGameButton.gameObject : null;
        EventSystem.current.SetSelectedGameObject(target);
    }

    private static bool AnySlotExists()
    {
        // オートセーブ枠か手動の枠のどれかにデータがあるか
        return SaveSlotPanel.AnySaveExists();
    }

    // 決定の SE（個別の SE が空なら共通の UI 効果音）
    private static void PlaySe(SeData se)
    {
        UISound.Play(se, UISoundType.Decide);
    }
}
