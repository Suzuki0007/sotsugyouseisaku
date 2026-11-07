/*********************************************************************/
// * \file   ItemData.cs
// * \brief  アイテムデータ（ScriptableObject）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテムデータクラス
/// </summary>
///
/// Projectウィンドウで右クリック → Create > Item > ItemData で作成する
/// セーブからの復元で使うため、作成したアセットは Assets/Resources/Items/ に置く
[CreateAssetMenu(menuName = "Item/ItemData", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary>
    /// アイテムデータを置く Resources 内のフォルダ
    /// </summary>
    public const string ResourcesFolder = "Items";

    /// <summary>
    /// 鍵アイテムのID（KeyItem に ItemData が未設定のときに使う）
    /// </summary>
    public const string KeyItemId = "key";

    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("セーブ用のID。空ならアセット名を使う（カンマ「,」とコロン「:」は使わない）")]
    [SerializeField]
    private string id;

    [Tooltip("アイテム画面に表示する名前")]
    [SerializeField]
    private string displayName;

    [Tooltip("アイテム画面に表示する説明")]
    [TextArea(3, 6)]
    [SerializeField]
    private string description;

    [Tooltip("アイテム画面に表示するアイコン（任意）")]
    [SerializeField]
    private Sprite icon;

    [Tooltip("鍵として扱う（持っていると鍵付きの宝箱を開けられる）")]
    [SerializeField]
    private bool isKey;

    // IDからアイテムを探すためのキャッシュ
    private static Dictionary<string, ItemData> cache;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public string Description => description;

    public Sprite Icon => icon;

    public bool IsKey => isKey;

    //=====================================================================
    // 検索
    //=====================================================================

    /// <summary>
    /// IDからアイテムデータを探す（Resources/Items にあるものが対象）
    /// </summary>
    /// <param name="itemId"> アイテムID </param>
    /// <returns> 見つからなければ null </returns>
    public static ItemData Find(string itemId)
    {
        if(string.IsNullOrEmpty(itemId)) { return null; }

        if(cache == null)
        {
            cache = new Dictionary<string, ItemData>();

            foreach(ItemData item in Resources.LoadAll<ItemData>(ResourcesFolder))
            {
                if(cache.ContainsKey(item.Id))
                {
                    Debug.LogWarning($"アイテムIDが重複しています: {item.Id}", item);
                    continue;
                }
                cache.Add(item.Id, item);
            }
        }

        cache.TryGetValue(itemId, out ItemData result);
        return result;
    }

    // ドメインリロードを切っていても、再生のたびにキャッシュを作り直す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        cache = null;
    }
}
