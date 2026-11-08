/*********************************************************************/
// * \file   DroppedItem.cs
// * \brief  敵が落としたアイテム（触れると拾える）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵が落としたアイテム
/// </summary>
///
/// ・敵のデータの「落とすアイテム」で自動で作られる。シーンに置かなくてよい
/// ・プレイヤーが触れると持ち物に入る。拾わずにシーンを出ると消える（セーブしない）
public class DroppedItem : MonoBehaviour
{
    private ItemData item;
    private int count;
    private float spawnTime;

    /// <summary>
    /// 落とす
    /// </summary>
    public static DroppedItem Drop(ItemData item, int count, Vector2 position)
    {
        var go = new GameObject($"落とした物（{item.DisplayName}）");
        go.transform.position = new Vector3(position.x, position.y, 0.0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = item.Icon;
        sr.sortingOrder = 4;
        if(sr.sprite != null)
        {
            // だいたい 0.6 マスの大きさにそろえる
            float size = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if(size > 0.0f) { go.transform.localScale = Vector3.one * (0.6f / size); }
        }

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = sr.sprite != null ? Mathf.Max(sr.sprite.bounds.extents.x, sr.sprite.bounds.extents.y) : 0.3f;

        var dropped = go.AddComponent<DroppedItem>();
        dropped.item = item;
        dropped.count = Mathf.Max(1, count);
        dropped.spawnTime = Time.time;
        return dropped;
    }

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        // ふわふわ浮く
        transform.position = basePosition + new Vector3(0.0f, Mathf.Sin((Time.time - spawnTime) * 4.0f) * 0.08f, 0.0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 出た直後は拾わない（倒した瞬間に重なっていて、見えないまま拾うのを防ぐ）
        if(Time.time - spawnTime < 0.3f) { return; }
        if(!other.CompareTag("Player")) { return; }
        Pick();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if(Time.time - spawnTime < 0.3f) { return; }
        if(!other.CompareTag("Player")) { return; }
        Pick();
    }

    private void Pick()
    {
        if(item == null) { Destroy(gameObject); return; }
        if(InventoryManager.Instance != null) { InventoryManager.Instance.AddItem(item, count); }
        UISound.Play(UISoundType.Decide);
        Destroy(gameObject);
    }
}
