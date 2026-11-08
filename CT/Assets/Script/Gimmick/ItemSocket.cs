/*********************************************************************/
// * \file   ItemSocket.cs
// * \brief  置き台（アイテムをはめると仕掛けが ON になる）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 置き台
/// </summary>
///
/// ・調べると、決めたアイテムを持っていればはめる（仕掛けの状態が ON）。はめた物の絵を表示する
///   例：紋章をはめる扉（バイオハザード）／ 4 つの台に宝石をそろえる（答え合わせの「全部 ON」）
/// ・「取り外せる」なら、もう一度調べると外して持ち物に戻す（OFF）
/// ・はめたかどうかは仕掛けの状態としてセーブされる
[RequireComponent(typeof(GimmickState))]
public class ItemSocket : GimmickInteractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("はめるアイテム")]
    private ItemData item;

    [SerializeField, Label("個数", 1)]
    private int count = 1;

    [SerializeField, Label("はめたら持ち物から減らす")]
    [Tooltip("オフ：持っているだけでよい（見せるだけ）")]
    private bool consume = true;

    [SerializeField, Label("取り外せる")]
    [Tooltip("オン：はめたあと、もう一度調べると外して持ち物に戻す")]
    private bool removable = false;

    [SerializeField, Label("使えるキャラ")]
    private CharacterRestriction user = CharacterRestriction.Both;

    [SerializeField, Label("はめた物の表示")]
    [Tooltip("はめたときにアイテムの絵を出す SpriteRenderer（空なら出さない）")]
    private SpriteRenderer placedRenderer;

    [SerializeField, Label("はめた物の大きさ", 0.1f)]
    [Tooltip("はめた物の絵を、この大きさ（マス）に合わせて表示する")]
    private float placedSize = 0.6f;

    [SerializeField, Label("はめたときの SE")]
    private SeData placeSe;

    [SerializeField, Label("外したときの SE")]
    private SeData removeSe;

    [Header("はめられないとき")]

    [SerializeField, Label("アイテムを持っていないとき")]
    [Tooltip("例：「何かをはめるくぼみがある」。空なら「できない」音だけ")]
    private EventSequence noItem;

    [SerializeField, Label("使えないキャラで調べたとき")]
    private EventSequence wrongCharacter;

    private GimmickState state;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public ItemData Item => item;
    public bool Placed => state != null && state.IsOn;

    protected override CharacterRestriction Restriction => user;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        state = GetComponent<GimmickState>();
        state.Changed += (s, v) => ApplyVisual();
    }

    private void Start()
    {
        ApplyVisual();
    }

    protected override void OnInteract(GameObject current)
    {
        if(!user.Allows(current)) { Refuse(wrongCharacter); return; }

        InventoryManager inventory = InventoryManager.Instance;
        if(inventory == null || item == null) { return; }

        if(Placed)
        {
            if(!removable || state.Locked) { return; }
            if(consume) { inventory.AddItem(item, Mathf.Max(1, count)); }
            state.Set(0);
            SeManager.PlaySe(removeSe, transform.position);
            return;
        }

        if(state.Locked) { return; }
        if(inventory.GetCount(item) < Mathf.Max(1, count)) { Refuse(noItem); return; }

        if(consume) { inventory.RemoveItem(item, Mathf.Max(1, count)); }
        state.Set(1);
        SeManager.PlaySe(placeSe, transform.position);
    }

    private void ApplyVisual()
    {
        if(placedRenderer == null) { return; }
        Sprite icon = item != null ? item.Icon : null;
        placedRenderer.sprite = icon;
        placedRenderer.enabled = Placed && icon != null;

        // アイコンの大きさはまちまちなので、決めた大きさに合わせる
        if(icon != null)
        {
            float native = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            if(native > 0.0001f) { placedRenderer.transform.localScale = Vector3.one * (placedSize / native); }
        }
    }
}
