/*********************************************************************/
// * \file   ItemSocket.cs
// * \brief  置き台（アイテムをはめると仕掛けが ON になる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
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
    [Tooltip("1 種類だけのとき。下の「選べるアイテム」を使うときは空でよい")]
    private ItemData item;

    [SerializeField]
    [Tooltip("選べるアイテム（何をはめるか選ぶ）。入れると、調べたときに持っている物から選ぶ画面が出る。仕掛けの状態は「選んだアイテムがこの並びの何番目か（1 から）」になる → 答え合わせ（決めた組み合わせ）で、どの台に何をはめたかを確かめられる")]
    private List<ItemData> choices = new List<ItemData>();

    [SerializeField, Label("選ぶ画面の見出し")]
    private string choiceTitle = "何をはめますか？";

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
    public IReadOnlyList<ItemData> Choices => choices;
    private bool ChoiceMode => choices != null && choices.Count > 0;

    /// <summary> 今はめている物（なければ null） </summary>
    public ItemData PlacedItem
    {
        get
        {
            if(!Placed) { return null; }
            if(!ChoiceMode) { return item; }
            int i = state.Value - 1;
            return i >= 0 && i < choices.Count ? choices[i] : null;
        }
    }
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
        if(!user.Allows(current)) { RefuseCharacter(wrongCharacter); return; }

        InventoryManager inventory = InventoryManager.Instance;
        if(inventory == null || (item == null && !ChoiceMode)) { return; }

        if(Placed)
        {
            if(!removable || state.Locked) { return; }
            ItemData placed = PlacedItem;
            if(consume && placed != null) { inventory.AddItem(placed, Mathf.Max(1, count)); }
            state.Set(0);
            SeManager.PlaySe(removeSe, transform.position);
            return;
        }

        if(state.Locked) { return; }

        if(ChoiceMode) { OpenChoice(inventory); return; }

        if(inventory.GetCount(item) < Mathf.Max(1, count)) { Refuse(noItem); return; }

        if(consume) { inventory.RemoveItem(item, Mathf.Max(1, count)); }
        state.Set(1);
        SeManager.PlaySe(placeSe, transform.position);
    }

    /// <summary>
    /// 持っている「選べるアイテム」から選ぶ画面を出す
    /// </summary>
    private void OpenChoice(InventoryManager inventory)
    {
        var owned = new List<int>();
        var labels = new List<string>();
        var icons = new List<Sprite>();
        for(int i = 0; i < choices.Count; i++)
        {
            ItemData c = choices[i];
            if(c == null || inventory.GetCount(c) < Mathf.Max(1, count)) { continue; }
            owned.Add(i);
            labels.Add(c.DisplayName);
            icons.Add(c.Icon);
        }
        if(owned.Count == 0) { Refuse(noItem); return; }

        PuzzleUI.OpenChoice(choiceTitle, labels, icons, 0, index =>
        {
            int choice = owned[index];
            ItemData chosen = choices[choice];
            if(consume) { inventory.RemoveItem(chosen, Mathf.Max(1, count)); }
            state.Set(choice + 1);
            SeManager.PlaySe(placeSe, transform.position);
        }, null);
    }

    private void ApplyVisual()
    {
        if(placedRenderer == null) { return; }
        ItemData shown = PlacedItem != null ? PlacedItem : item;
        Sprite icon = shown != null ? shown.Icon : null;
        placedRenderer.sprite = icon;
        placedRenderer.enabled = Placed && icon != null;

        // アイコンの大きさはまちまちなので、決めた大きさに合わせる
        if(icon != null)
        {
            float native = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            if(native > 0.0001f) { placedRenderer.transform.localScale = Vector3.one * (placedSize / native); }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 選べるアイテムの数に合わせて、仕掛けの状態の「段階の数」を直す（0 = 何もない、1〜 = 何番目の物）
    /// </summary>
    private void OnValidate()
    {
        if(choices == null || choices.Count == 0) { return; }
        GimmickState s = GetComponent<GimmickState>();
        if(s == null) { return; }
        var so = new UnityEditor.SerializedObject(s);
        var p = so.FindProperty("stateCount");
        if(p != null && p.intValue < choices.Count + 1)
        {
            p.intValue = choices.Count + 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
#endif
}
