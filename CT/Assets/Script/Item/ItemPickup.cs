/*********************************************************************/
// * \file   ItemPickup.cs
// * \brief  調べると手に入るアイテムのクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 調べると手に入るアイテムのクラス
/// </summary>
///
/// ・インタラクトキー（E）で調べると、設定した ItemData を所持アイテムに追加して消える
/// ・一度拾ったものは Save に記録し、シーンに戻っても出てこない（使って消えたあとも）
/// ・同じアイテムを持っていても拾える（個数が増える）
public class ItemPickup : Intaractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("手に入るアイテム")]
    [SerializeField]
    private ItemData item;

    [Tooltip("拾ったことを記録するためのID。空ならヒエラルキー上の名前を使う（同じシーンで重複しないようにする）")]
    [SerializeField]
    private string pickupId;

    [Header("SE")]

    [SerializeField, Tooltip("アイテムを拾ったとき")]
    private SeData pickupSe;

    private string saveKey;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        saveKey = MakeSaveKey(gameObject, pickupId);
    }

    private void Start()
    {
        if(item == null) { Debug.LogError($"{name}: ItemDataが設定されていません", this); return; }

        // 拾ったことがあるなら消す
        if(Save.Get(saveKey, false))
        {
            gameObject.SetActive(false);
        }
    }

    //=====================================================================
    // インタラクション関数
    //=====================================================================

    /// <summary>
    /// プレイヤーがアイテムを調べたときに呼ばれる
    /// </summary>
    protected override void Interact()
    {
        if(item == null) { return; }
        if(InventoryManager.Instance == null) { return; }

        if(!InventoryManager.Instance.AddItem(item)) { return; }

        // 拾ったことを仮置きで記録（チェックポイント・シーン遷移で確定）
        Save.SetPending(saveKey, true);

        // オブジェクトを消す前に鳴らす (SeManager側で鳴るので消えても途切れない)
        SeManager.PlaySe(pickupSe, transform.position);

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    //=====================================================================
    // セーブ用の名前
    //=====================================================================

    /// <summary>
    /// 拾ったことを記録するセーブ用の名前を作る
    /// </summary>
    /// <param name="target"> 拾われるオブジェクト </param>
    /// <param name="id"> 指定のID（空ならヒエラルキー上のパスを使う） </param>
    public static string MakeSaveKey(GameObject target, string id)
    {
        string name = string.IsNullOrEmpty(id) ? GetHierarchyPath(target.transform) : id;
        return "Pickup/" + target.scene.name + "/" + name;
    }

    /// <summary>
    /// "親/子/孫" の形でヒエラルキー上のパスを返す
    /// </summary>
    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }
}
