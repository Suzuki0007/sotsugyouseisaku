/*********************************************************************/
// * \file   InventoryManager.cs
// * \brief  インベントリ管理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// インベントリ管理クラス
/// </summary>
///
/// ・シーンをまたいで存続する（シーンに置いていなくても自動で生成される）
/// ・取得したアイテムは取得順・個数つきで保持し、Save に仮置きで記録する
///   （チェックポイントやシーン遷移で確定される）
public class InventoryManager : Singleton<InventoryManager>
{
    //=====================================================================
    // 定数
    //=====================================================================

    // セーブに使う名前（値は「アイテムID:個数」のカンマ区切り）
    private const string SaveKey = "Inventory/Items";

    //=====================================================================
    // 変数
    //=====================================================================

    // 所持しているアイテムの種類（取得順）
    private readonly List<ItemData> items = new List<ItemData>();

    // アイテムごとの個数（1以上）
    private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();

    //=====================================================================
    // イベント
    //=====================================================================

    /// <summary>
    /// 所持アイテムが変わったときに呼ばれる
    /// </summary>
    public event Action OnChanged;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// ノアのデータ
    /// </summary>
    ///
    /// getはこのクラスのインスタンスのHasKeyプロパティの値を返す
    /// setはこのクラスのインスタンスのHasKeyプロパティに値を代入する
    /// たとえるなら、ゲッターとセッターが合体したようなもの
    ///
    /// setをprivateにすることで、外部からはnoahDataプロパティの値を変更できなくする
    public NoahData NoahData { get; private set; }

    /// <summary>
    /// 鍵を持っているかどうか
    /// </summary>
    public bool HasKey { get; set; }

    /// <summary>
    /// 所持しているアイテムの種類の一覧（取得順）。個数は GetCount で取得する
    /// </summary>
    public IReadOnlyList<ItemData> Items => items;

    //=====================================================================
    // 初期化
    //=====================================================================

    protected override void Awake()
    {
        // 重複したInventoryManagerが存在する場合は破棄する
        if(Instance != null && Instance != this)
        {
            Debug.Log("重複したInventoryManagerを削除します");
            Destroy(gameObject);
            return;
        }
        base.Awake();

        // シーンに置いたときの設定に関わらず、所持品はシーンをまたいで残す
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // NoahDataの生成
        NoahData = new NoahData();

        // セーブから所持アイテムを復元する
        LoadItems();

        //SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 最初のシーンにInventoryManagerが置かれていなければ自動で生成する
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if(Instance != null) { return; }

        new GameObject(nameof(InventoryManager)).AddComponent<InventoryManager>();
    }

    //=====================================================================
    // アイテム操作
    //=====================================================================

    /// <summary>
    /// アイテムを追加する（持っていれば個数が増える）
    /// </summary>
    /// <param name="item"> 追加するアイテム </param>
    /// <param name="amount"> 追加する個数 </param>
    /// <returns> 追加できたら true（null・個数が0以下なら false） </returns>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if(item == null) { Debug.LogWarning("追加するアイテムがnullです", this); return false; }
        if(amount <= 0) { return false; }

        AddCount(item, amount);
        SaveItems();

        Debug.Log($"{item.DisplayName}を手に入れた！（所持数：{GetCount(item)}）");
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// アイテムを消費する（個数を減らし、0になったら一覧から消す）
    /// </summary>
    /// <param name="item"> 消費するアイテム </param>
    /// <param name="amount"> 消費する個数 </param>
    /// <returns> 消費できたら true（足りない・null なら false） </returns>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if(item == null) { return false; }
        if(amount <= 0) { return false; }

        int current = GetCount(item);
        if(current < amount) { return false; }

        int remaining = current - amount;
        if(remaining > 0)
        {
            counts[item] = remaining;
        }
        else
        {
            counts.Remove(item);
            items.Remove(item);
        }

        // 鍵を消費したら、他に鍵が残っているかで鍵フラグを決め直す
        if(item.IsKey) { NoahData.hasKey = FindKeyItem() != null; }

        SaveItems();

        Debug.Log($"{item.DisplayName}を使った（残り：{remaining}）");
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 鍵を1つ消費する（鍵付きの宝箱を開けたときに使う）
    /// </summary>
    /// <returns> 鍵を持っていて消費できたら true </returns>
    public bool ConsumeKey()
    {
        ItemData keyItem = FindKeyItem();
        if(keyItem != null) { return RemoveItem(keyItem); }

        // ItemData なしで鍵フラグだけ立っている場合（旧方式）
        if(!NoahData.hasKey) { return false; }

        NoahData.hasKey = false;
        Debug.Log("鍵を使った");
        return true;
    }

    /// <summary>
    /// アイテムの所持数を返す（持っていなければ 0）
    /// </summary>
    public int GetCount(ItemData item)
    {
        if(item == null) { return 0; }

        return counts.TryGetValue(item, out int count) ? count : 0;
    }

    /// <summary>
    /// アイテムを持っているかどうか
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return GetCount(item) > 0;
    }

    /// <summary>
    /// IDでアイテムを持っているかどうか
    /// </summary>
    public bool HasItem(string itemId)
    {
        if(string.IsNullOrEmpty(itemId)) { return false; }

        foreach(ItemData item in items)
        {
            if(item.Id == itemId) { return true; }
        }
        return false;
    }

    /// <summary>
    /// 個数を増やす（初めてなら一覧の最後に追加する）
    /// </summary>
    private void AddCount(ItemData item, int amount)
    {
        if(counts.TryGetValue(item, out int current))
        {
            counts[item] = current + amount;
        }
        else
        {
            counts[item] = amount;
            items.Add(item);
        }

        if(item.IsKey) { NoahData.hasKey = true; }
    }

    /// <summary>
    /// 所持アイテムの中から鍵を探す（取得順で最初のもの）
    /// </summary>
    private ItemData FindKeyItem()
    {
        foreach(ItemData item in items)
        {
            if(item.IsKey) { return item; }
        }
        return null;
    }

    //=====================================================================
    // セーブ
    //=====================================================================

    /// <summary>
    /// 所持アイテムを仮置きで記録する（チェックポイント・シーン遷移で確定）
    /// </summary>
    private void SaveItems()
    {
        List<string> entries = new List<string>();
        foreach(ItemData item in items) { entries.Add(item.Id + ":" + counts[item]); }

        Save.SetPending(SaveKey, string.Join(",", entries));
    }

    /// <summary>
    /// セーブの内容で所持アイテムを入れ替える（はじめから・つづきからのとき。GameSession から呼ぶ）
    /// </summary>
    public void ReloadFromSave()
    {
        NoahData.hasKey = false;
        LoadItems();
        OnChanged?.Invoke();
    }

    /// <summary>
    /// セーブから所持アイテムを復元する
    /// </summary>
    private void LoadItems()
    {
        items.Clear();
        counts.Clear();

        string saved = Save.Get(SaveKey, "");
        if(string.IsNullOrEmpty(saved)) { return; }

        foreach(string entry in saved.Split(','))
        {
            // 「ID:個数」。個数がない古い形式は1個として扱う
            string itemId = entry;
            int count = 1;

            int separator = entry.LastIndexOf(':');
            if(separator >= 0)
            {
                itemId = entry.Substring(0, separator);
                if(!int.TryParse(entry.Substring(separator + 1), out count)) { count = 1; }
            }

            if(count <= 0) { continue; }

            ItemData item = ItemData.Find(itemId);
            if(item == null)
            {
                Debug.LogWarning($"セーブにあるアイテム {itemId} が Resources/{ItemData.ResourcesFolder} に見つかりません", this);
                continue;
            }

            AddCount(item, count);
        }
    }

    //=====================================================================
    // デバッグ
    //=====================================================================

    /// <summary>
    /// シーンがロードされたときに呼び出されるメソッド
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    ///
    /// シーンがロードされたときにログに出力する。
    /// 現在のシーン名、
    /// ロードモード、
    /// InventoryManagerのID、
    /// NoahDataのハッシュコード、
    /// 鍵を持っているかどうかを
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
            $"========== SCENE LOAD ==========\n" +
            $"Scene : {scene.name}\n" +
            $"Mode : {mode}\n" +
            $"Manager ID : {GetEntityId()}\n" +
            $"Data ID     : {NoahData.GetHashCode()}\n" +
            $"HasKey : {NoahData.hasKey}\n" +
            $"================================"
        );
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
