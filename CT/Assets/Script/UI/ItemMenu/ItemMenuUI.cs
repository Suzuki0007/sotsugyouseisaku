/*********************************************************************/
// * \file   ItemMenuUI.cs
// * \brief  取得したアイテムを確認する画面
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 取得したアイテムを確認する画面
/// </summary>
///
/// ・Tab キーで開閉する（Esc でも閉じる）。↑↓ / W・S で選択、マウスでも選択できる
/// ・左に所持アイテムの一覧、右に選んだアイテムの名前・説明・アイコンを表示する
/// ・開いている間はゲームを止める（Time.timeScale = 0）
/// ・開いている間は IsBusy が true になるので、プレイヤーの操作側はこれを見て入力を止める
/// ・Resources/UI/ItemMenu のプレハブから自動で生成され、シーンをまたいで存続する
///   （プレハブは Tools > アイテム画面 > UIプレハブを作成 で作る）
public class ItemMenuUI : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary>
    /// 自動生成に使うプレハブの Resources 内のパス
    /// </summary>
    public const string PrefabResourcePath = "UI/ItemMenu";

    //=====================================================================
    // 変数
    //=====================================================================

    [Header("UI")]

    [Tooltip("開閉する画面全体")]
    [SerializeField]
    private GameObject panel;

    [Tooltip("一覧の行を並べる親（VerticalLayoutGroup）")]
    [SerializeField]
    private RectTransform listRoot;

    [Tooltip("一覧の1行のひな形（非表示にしておく）")]
    [SerializeField]
    private ItemSlotUI slotTemplate;

    [Tooltip("一覧のスクロール（任意）")]
    [SerializeField]
    private ScrollRect listScroll;

    [Tooltip("詳細表示の親")]
    [SerializeField]
    private GameObject detailRoot;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text descriptionText;

    [Tooltip("詳細のアイコン（任意）")]
    [SerializeField]
    private Image iconImage;

    [Tooltip("アイテムを1つも持っていないときに表示する")]
    [SerializeField]
    private GameObject emptyMessage;

    [Header("入力")]

    [SerializeField]
    private Key toggleKey = Key.Tab;

    [SerializeField]
    private Key closeKey = Key.Escape;

    [Header("設定")]

    [Tooltip("開いている間ゲームを止めるか")]
    [SerializeField]
    private bool pauseGame = true;

    [Header("SE")]

    [SerializeField, Tooltip("開いたとき")]
    private SeData openSe;

    [SerializeField, Tooltip("閉じたとき")]
    private SeData closeSe;

    [SerializeField, Tooltip("選択を動かしたとき")]
    private SeData selectSe;

    private static ItemMenuUI instance;

    private readonly List<ItemSlotUI> slots = new List<ItemSlotUI>();

    private bool isOpen;

    private int selectedIndex;

    // 開く前の Time.timeScale（閉じたときに戻す）
    private float previousTimeScale = 1f;

    private bool isPausing;

    // 開いた／閉じたフレーム（同じフレームのキー入力を無視するため）
    private int lastToggleFrame = -1;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// アイテム画面を開いているか
    /// </summary>
    public static bool IsOpen => instance != null && instance.isOpen;

    /// <summary>
    /// 開いている、または開閉した直後か（プレイヤーの入力を止める判定に使う）
    /// </summary>
    public static bool IsBusy =>
        instance != null && (instance.isOpen || Time.frameCount == instance.lastToggleFrame);

    //=====================================================================
    // 自動生成
    //=====================================================================

    /// <summary>
    /// 最初のシーンにアイテム画面がなければ、プレハブから生成する
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if(instance != null) { return; }

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if(prefab == null)
        {
            Debug.LogWarning(
                $"アイテム画面のプレハブ Resources/{PrefabResourcePath} がありません。" +
                "Tools > アイテム画面 > UIプレハブを作成 で作成してください");
            return;
        }

        Instantiate(prefab).name = prefab.name;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        // 2つ目以降は破棄する
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if(panel == null)       { Debug.LogError($"{name}: Panelが設定されていません", this); }
        if(listRoot == null)    { Debug.LogError($"{name}: List Rootが設定されていません", this); }
        if(slotTemplate == null){ Debug.LogError($"{name}: Slot Templateが設定されていません", this); }

        if(slotTemplate != null) { slotTemplate.gameObject.SetActive(false); }
        if(panel != null) { panel.SetActive(false); }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if(instance != this) { return; }

        // 開いたまま破棄されても、ゲームが止まったままにならないようにする
        ResumeGame();
        UnsubscribeInventory();
        instance = null;
    }

    /// <summary>
    /// シーンが切り替わったら閉じる
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Close(false);
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
            if(Keyboard.current[toggleKey].wasPressedThisFrame && CanOpen())
            {
                Open();
            }
            return;
        }

        // シーン遷移が始まったら閉じる
        if(SceneFader.IsBusy) { Close(false); return; }

        if(Keyboard.current[toggleKey].wasPressedThisFrame ||
           Keyboard.current[closeKey].wasPressedThisFrame)
        {
            Close(true);
            return;
        }

        UpdateSelectInput();
    }

    /// <summary>
    /// ↑↓ / W・S で選択を動かす
    /// </summary>
    private void UpdateSelectInput()
    {
        if(slots.Count == 0) { return; }

        Keyboard keyboard = Keyboard.current;

        if(keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            MoveSelection(-1);
        }
        else if(keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            MoveSelection(1);
        }
    }

    /// <summary>
    /// 選択を動かす（端まで行ったら反対側へ回る）
    /// </summary>
    private void MoveSelection(int delta)
    {
        int next = (selectedIndex + delta + slots.Count) % slots.Count;
        if(next == selectedIndex) { return; }

        Select(next);
        SeManager.PlaySe(selectSe);
    }

    //=====================================================================
    // 開閉
    //=====================================================================

    /// <summary>
    /// 開けるかどうか（ゲーム中で、会話・シーン遷移中でないとき）
    /// </summary>
    private bool CanOpen()
    {
        if(panel == null) { return false; }
        if(InventoryManager.Instance == null) { return false; }

        // プレイヤーがいるシーン（ゲーム中）だけ開ける
        if(PlayerSwitchManager.Instance == null || PlayerSwitchManager.Instance.CurrentPlayer == null) { return false; }

        if(DialogueManager.IsBusy || SceneFader.IsBusy) { return false; }

        return true;
    }

    /// <summary>
    /// アイテム画面を開く
    /// </summary>
    public void Open()
    {
        if(isOpen) { return; }

        isOpen = true;
        lastToggleFrame = Time.frameCount;

        PauseGame();
        EnsureEventSystem();

        panel.SetActive(true);

        InventoryManager.Instance.OnChanged += Refresh;
        Refresh();

        SeManager.PlaySe(openSe);
    }

    /// <summary>
    /// アイテム画面を閉じる
    /// </summary>
    /// <param name="playSe"> 閉じるSEを鳴らすか </param>
    public void Close(bool playSe = true)
    {
        if(!isOpen) { return; }

        isOpen = false;
        lastToggleFrame = Time.frameCount;

        UnsubscribeInventory();

        if(panel != null) { panel.SetActive(false); }

        ResumeGame();

        if(playSe) { SeManager.PlaySe(closeSe); }
    }

    private void UnsubscribeInventory()
    {
        if(InventoryManager.Instance != null) { InventoryManager.Instance.OnChanged -= Refresh; }
    }

    //=====================================================================
    // 一時停止
    //=====================================================================

    private void PauseGame()
    {
        if(!pauseGame || isPausing) { return; }

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

    /// <summary>
    /// マウスで選択できるように、EventSystem がなければ作る
    /// </summary>
    private void EnsureEventSystem()
    {
        if(EventSystem.current != null) { return; }
        if(FindAnyObjectByType<EventSystem>() != null) { return; }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    //=====================================================================
    // 表示
    //=====================================================================

    /// <summary>
    /// 所持アイテムに合わせて一覧を作り直す
    /// </summary>
    private void Refresh()
    {
        if(listRoot == null || slotTemplate == null) { return; }

        // 前の行を消す
        foreach(ItemSlotUI slot in slots)
        {
            if(slot == null) { continue; }

            // Destroy はフレームの最後に消えるので、先に非表示にしてレイアウトから外す
            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }
        slots.Clear();

        IReadOnlyList<ItemData> items = InventoryManager.Instance != null
            ? InventoryManager.Instance.Items
            : new List<ItemData>();

        for(int i = 0; i < items.Count; i++)
        {
            ItemSlotUI slot = Instantiate(slotTemplate, listRoot);
            slot.gameObject.SetActive(true);
            slot.name = $"Slot_{items[i].Id}";
            slot.Setup(items[i], InventoryManager.Instance.GetCount(items[i]), i, this);
            slots.Add(slot);
        }

        bool isEmpty = items.Count == 0;
        if(emptyMessage != null) { emptyMessage.SetActive(isEmpty); }
        if(detailRoot != null)   { detailRoot.SetActive(!isEmpty); }

        if(isEmpty) { return; }

        Select(Mathf.Clamp(selectedIndex, 0, items.Count - 1));
    }

    /// <summary>
    /// 一覧の index 番目を選択して、詳細を表示する
    /// </summary>
    public void Select(int index)
    {
        if(InventoryManager.Instance == null) { return; }

        IReadOnlyList<ItemData> items = InventoryManager.Instance.Items;
        if(index < 0 || index >= items.Count || index >= slots.Count) { return; }

        selectedIndex = index;

        for(int i = 0; i < slots.Count; i++)
        {
            slots[i].SetSelected(i == index);
        }

        ShowDetail(items[index]);
        ScrollToSelected();
    }

    /// <summary>
    /// 右側に選んだアイテムの詳細を表示する
    /// </summary>
    private void ShowDetail(ItemData item)
    {
        int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(item) : 0;

        if(nameText != null)        { nameText.text = FormatName(item, count); }
        if(descriptionText != null) { descriptionText.text = item.Description; }

        if(iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = item.Icon != null;
        }
    }

    /// <summary>
    /// 表示用の名前を作る（2個以上なら「鍵 ×2」のように個数を付ける）
    /// </summary>
    public static string FormatName(ItemData item, int count)
    {
        if(item == null) { return ""; }

        return count >= 2 ? $"{item.DisplayName} ×{count}" : item.DisplayName;
    }

    /// <summary>
    /// 選択中の行が見えるように一覧をスクロールする
    /// </summary>
    private void ScrollToSelected()
    {
        if(listScroll == null || listScroll.content == null || listScroll.viewport == null) { return; }
        if(slots.Count <= 1) { return; }

        Canvas.ForceUpdateCanvases();

        float contentHeight = listScroll.content.rect.height;
        float viewportHeight = listScroll.viewport.rect.height;
        if(contentHeight <= viewportHeight) { return; }

        RectTransform slotRect = (RectTransform)slots[selectedIndex].transform;

        // content の上端からの行の位置
        float slotTop = -slotRect.anchoredPosition.y - slotRect.rect.height * (1f - slotRect.pivot.y);
        float slotBottom = slotTop + slotRect.rect.height;

        float scrollable = contentHeight - viewportHeight;
        float viewTop = (1f - listScroll.verticalNormalizedPosition) * scrollable;

        if(slotTop < viewTop)
        {
            viewTop = slotTop;
        }
        else if(slotBottom > viewTop + viewportHeight)
        {
            viewTop = slotBottom - viewportHeight;
        }

        listScroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(viewTop / scrollable);
    }
}
