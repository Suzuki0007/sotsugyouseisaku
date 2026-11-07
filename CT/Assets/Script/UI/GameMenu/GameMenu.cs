/*********************************************************************/
// * \file   GameMenu.cs
// * \brief  ゲーム中のメニュー画面（アイテム・ロード・タイトルに戻る）とセーブの流れ
// *
// * \author
/*********************************************************************/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲーム中のメニュー画面
/// </summary>
///
/// ・Tab で開閉（ステージにいるときだけ。会話・イベント・暗転中は開かない）。Esc でも閉じる
/// ・アイテム：アイテム画面を開く（閉じるとメニューに戻る）
/// ・ロード：セーブデータ選択画面 → 確認 → そのデータで再開
/// ・タイトルに戻る：確認 → タイトルへ
/// ・セーブポイントから OpenSave で呼ばれると「セーブしますか？」→ セーブデータ選択画面 → セーブ
/// ・開いている間はゲームを止める（Time.timeScale = 0）。IsBusy の間はプレイヤーの操作が止まる（InputBlock）
/// ・シーンに置かなくても、Resources/UI/GameMenu のプレハブから自動で作られ、シーンをまたいで残る
public class GameMenu : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    public const string PrefabResourcePath = "UI/GameMenu";

    //=====================================================================
    // 変数
    //=====================================================================

    [Header("メニュー")]

    [SerializeField]
    private GameObject menuPanel;

    [SerializeField]
    private Button itemButton;

    [SerializeField]
    private Button loadButton;

    [SerializeField]
    private Button titleButton;

    [SerializeField]
    private Button closeButton;

    [Header("共通の画面")]

    [SerializeField]
    private SaveSlotPanel slotPanel;

    [SerializeField, Tooltip("タイトルに戻る・セーブしますか？ の確認")]
    private ConfirmDialog confirm;

    [Header("メッセージ（「セーブしました」など）")]

    [SerializeField]
    private GameObject messagePanel;

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField, Tooltip("メッセージを出しておく秒数")]
    private float messageDuration = 1.5f;

    [Header("操作")]

    [SerializeField]
    private Key toggleKey = Key.Tab;

    [SerializeField]
    private Key closeKey = Key.Escape;

    [Header("SE（任意。空なら共通の UI 効果音：Resources/UISounds）")]

    [SerializeField]
    private SeData openSe;

    [SerializeField]
    private SeData closeSe;

    [SerializeField]
    private SeData decideSe;

    private static GameMenu instance;

    private bool isOpen;
    private int lastToggleFrame = -1;
    private bool isPausing;
    private float previousTimeScale = 1f;
    private Coroutine messageRoutine;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public static GameMenu Instance => instance;

    /// <summary>
    /// メニュー（セーブの流れを含む）を開いているか
    /// </summary>
    public static bool IsOpen => instance != null && instance.isOpen;

    /// <summary>
    /// 開いている、または開閉した直後か（プレイヤーの入力を止める判定に使う）
    /// </summary>
    public static bool IsBusy => instance != null && (instance.isOpen || Time.frameCount == instance.lastToggleFrame);

    //=====================================================================
    // 自動生成
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if(instance != null) { return; }

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if(prefab == null)
        {
            Debug.LogWarning($"メニュー画面のプレハブ Resources/{PrefabResourcePath} がありません。Tools > メニュー画面 > メニューのプレハブを作成 で作成してください");
            return;
        }

        Instantiate(prefab).name = prefab.name;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if(itemButton != null)  { itemButton.onClick.AddListener(OnItem); }
        if(loadButton != null)  { loadButton.onClick.AddListener(OnLoad); }
        if(titleButton != null) { titleButton.onClick.AddListener(OnTitle); }
        if(closeButton != null) { closeButton.onClick.AddListener(() => CloseAll(true)); }

        SetActive(menuPanel, false);
        SetActive(messagePanel, false);
        if(slotPanel != null) { slotPanel.Close(); }
        if(confirm != null)   { confirm.Hide(); }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if(instance != this) { return; }

        ResumeGame();
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CloseAll(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        instance = null;
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(Keyboard.current == null) { return; }

        // 開いた／閉じたフレームのキー入力では、すぐに切り替えない
        if(Time.frameCount == lastToggleFrame) { return; }

        if(!isOpen)
        {
            if(Keyboard.current[toggleKey].wasPressedThisFrame && CanOpen()) { OpenMenu(); }
            return;
        }

        // シーン遷移が始まったら閉じる
        if(SceneFader.IsBusy) { CloseAll(false); return; }

        // アイテム画面・セーブデータ選択・確認の間は、それぞれの画面が操作を受け持つ
        if(ItemMenuUI.IsOpen) { return; }
        if(slotPanel != null && slotPanel.IsOpenOrJustClosed) { return; }
        if(confirm != null && confirm.IsOpenOrJustClosed) { return; }
        if(menuPanel == null || !menuPanel.activeSelf) { return; }

        if(Keyboard.current[toggleKey].wasPressedThisFrame || Keyboard.current[closeKey].wasPressedThisFrame)
        {
            CloseAll(true);
            return;
        }

        // マウスで選択が外れたら、キーボードで操作できるように戻す
        if(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            Select(itemButton);
        }
    }

    /// <summary>
    /// 開けるかどうか（ステージにいて、会話・暗転・イベント・アイテム画面の間でない）
    /// </summary>
    private static bool CanOpen()
    {
        if(PlayerSwitchManager.Instance == null || PlayerSwitchManager.Instance.CurrentPlayer == null) { return false; }
        if(DialogueManager.IsBusy || SceneFader.IsBusy || EventSequence.IsBusy || ItemMenuUI.IsBusy) { return false; }
        return true;
    }

    //=====================================================================
    // メニュー
    //=====================================================================

    /// <summary>
    /// メニューを開く
    /// </summary>
    public void OpenMenu()
    {
        if(isOpen) { return; }

        Begin();
        UISound.Play(openSe, UISoundType.MenuOpen);
        ShowMenu(itemButton);
    }

    private void ShowMenu(Button select)
    {
        SetActive(menuPanel, true);
        if(loadButton != null) { loadButton.interactable = AnySlotExists(); }
        Select(select != null && select.interactable ? select : itemButton);
    }

    private void OnItem()
    {
        if(!isOpen) { return; }

        UISound.Play(decideSe, UISoundType.Decide);
        SetActive(menuPanel, false);

        bool opened = ItemMenuUI.OpenFromMenu(() =>
        {
            // アイテム画面を閉じたらメニューに戻る（同じ Tab / Esc でメニューまで閉じないように、このフレームの入力は無視）
            if(!isOpen || SceneFader.IsBusy) { return; }
            lastToggleFrame = Time.frameCount;
            ShowMenu(itemButton);
        });

        if(!opened) { ShowMenu(itemButton); }
    }

    private void OnLoad()
    {
        if(!isOpen || slotPanel == null) { return; }

        UISound.Play(decideSe, UISoundType.Decide);
        SetActive(menuPanel, false);
        slotPanel.Open(SaveSlotPanel.Purpose.Load,
            slot =>
            {
                CloseAll(false);
                GameSession.Continue(slot, GameSession.FallbackScene);
            },
            () => ShowMenu(loadButton));
    }

    private void OnTitle()
    {
        if(!isOpen || confirm == null) { return; }

        UISound.Play(decideSe, UISoundType.Decide);
        confirm.Show("タイトルに戻りますか？\n<size=80%>（最後にセーブしたあとの進行は失われます）</size>",
            () =>
            {
                CloseAll(false);
                GameSession.ReturnToTitle();
            },
            () => Select(titleButton));
    }

    //=====================================================================
    // セーブ（セーブポイントから）
    //=====================================================================

    /// <summary>
    /// 「セーブしますか？」→ セーブデータ選択 → セーブ の流れを始める
    /// </summary>
    /// <param name="sceneName"> 再開するシーン </param>
    /// <param name="resumePosition"> 再開する位置 </param>
    /// <param name="question"> 最初の確認の文 </param>
    /// <param name="saveSe"> セーブしたときの SE（任意） </param>
    public void OpenSave(string sceneName, Vector2 resumePosition, string question, SeData saveSe)
    {
        if(isOpen || !CanOpen() || confirm == null || slotPanel == null) { return; }

        Begin();
        SetActive(menuPanel, false);
        UISound.Play(UISoundType.Decide);

        // 「いいえ」・もどる の音はそれぞれの画面が鳴らすので、閉じるときは鳴らさない
        confirm.Show(question,
            () => slotPanel.Open(SaveSlotPanel.Purpose.Save,
                slot =>
                {
                    GameSession.SaveAt(slot, sceneName, resumePosition);
                    UISound.Play(saveSe, UISoundType.Save);
                    CloseAll(false);
                    ShowMessage($"スロット {slot} にセーブしました");
                },
                () => CloseAll(false)),
            () => CloseAll(false));
    }

    //=====================================================================
    // 開閉の共通処理
    //=====================================================================

    private void Begin()
    {
        isOpen = true;
        lastToggleFrame = Time.frameCount;
        PauseGame();
        EnsureEventSystem();
    }

    /// <summary>
    /// メニュー・アイテム画面・セーブデータ選択・確認をすべて閉じて、ゲームに戻る
    /// </summary>
    public void CloseAll(bool playSe)
    {
        bool wasOpen = isOpen;

        // 先に閉じた状態にする（アイテム画面を閉じたときの「メニューに戻る」処理が動かないように）
        isOpen = false;
        if(wasOpen) { lastToggleFrame = Time.frameCount; }

        if(ItemMenuUI.IsOpen) { ItemMenuUI.CloseIfOpen(); }
        if(slotPanel != null) { slotPanel.Close(); }
        if(confirm != null)   { confirm.Hide(); }
        SetActive(menuPanel, false);

        ResumeGame();

        if(wasOpen && playSe) { UISound.Play(closeSe, UISoundType.MenuClose); }
    }

    //=====================================================================
    // メッセージ
    //=====================================================================

    /// <summary>
    /// 画面下にメッセージを少しの間出す（ゲームは止めない）
    /// </summary>
    public void ShowMessage(string message)
    {
        if(messagePanel == null) { return; }

        if(messageText != null) { messageText.text = message; }
        if(messageRoutine != null) { StopCoroutine(messageRoutine); }
        messageRoutine = StartCoroutine(MessageRoutine());
    }

    private IEnumerator MessageRoutine()
    {
        messagePanel.SetActive(true);
        yield return new WaitForSecondsRealtime(messageDuration);
        messagePanel.SetActive(false);
        messageRoutine = null;
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private void PauseGame()
    {
        if(isPausing) { return; }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPausing = true;
    }

    private void ResumeGame()
    {
        if(!isPausing) { return; }

        Time.timeScale = previousTimeScale;
        isPausing = false;
    }

    private static void EnsureEventSystem()
    {
        if(EventSystem.current != null) { return; }
        if(FindAnyObjectByType<EventSystem>() != null) { return; }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static void Select(Button button)
    {
        if(EventSystem.current == null || button == null) { return; }
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private static bool AnySlotExists()
    {
        for(int slot = 1; slot <= Save.SlotCount; slot++)
        {
            if(Save.SlotExists(slot)) { return true; }
        }
        return false;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if(target != null && target.activeSelf != active) { target.SetActive(active); }
    }
}
