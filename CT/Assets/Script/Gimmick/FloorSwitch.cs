/*********************************************************************/
// * \file   FloorSwitch.cs
// * \brief  床スイッチ（乗ると仕掛けの状態が変わる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 床スイッチの動き
/// </summary>
public enum FloorSwitchMode
{
    [InspectorName("乗っている間だけ ON")]
    WhileOn = 0,

    [InspectorName("一度乗ったら ON のまま")]
    Once = 1,

    [InspectorName("乗るたびに切り替え")]
    Toggle = 2,
}

/// <summary>
/// 床スイッチ
/// </summary>
///
/// ・Is Trigger の Collider2D の上に、決めた物（ノア・シグル・押せる箱・敵）が乗ると、仕掛けの状態（GimmickState）が変わる
///   例：箱を乗せている間だけ開く扉 ／ 相棒を待たせて乗せておき、もう片方で先へ進む（同時押し）／ シグルの重さでだけ沈む床
/// ・「乗っている」は、乗った物の中心が範囲の中にあるとき（かすっただけでは反応しない）
/// ・操作していない相棒（ついてくる・待たせている）も、乗っていれば数える
[RequireComponent(typeof(GimmickState))]
public class FloorSwitch : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("動き")]
    private FloorSwitchMode mode = FloorSwitchMode.WhileOn;

    [Header("反応する物")]

    [SerializeField, Label("ノア")]
    private bool byNoah = true;

    [SerializeField, Label("シグル")]
    private bool bySigle = true;

    [SerializeField, Label("押せる箱")]
    private bool byBlock = true;

    [SerializeField, Label("敵")]
    private bool byEnemy = false;

    [SerializeField, Label("必要な数", 1)]
    [Tooltip("この数以上乗っているときに押される（2 なら、2人とも乗る・箱と1人 など）")]
    private int requiredCount = 1;

    [Header("音")]

    [SerializeField, Label("押されたときの SE")]
    private SeData pressSe;

    [SerializeField, Label("離れたときの SE")]
    private SeData releaseSe;

    private GimmickState state;
    private Collider2D[] areas;
    private bool pressed;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 今押されているか </summary>
    public bool Pressed => pressed;

    /// <summary> 今乗っている数 </summary>
    public int Count { get; private set; }

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        state = GetComponent<GimmickState>();
        areas = GetComponents<Collider2D>();
    }

    private void FixedUpdate()
    {
        Count = CountOnTop();
        bool now = Count >= Mathf.Max(1, requiredCount);
        if(now == pressed) { return; }

        pressed = now;
        if(pressed)
        {
            SeManager.PlaySe(pressSe, transform.position);
            switch(mode)
            {
                case FloorSwitchMode.Toggle: state.Toggle(); break;
                default:                     state.TurnOn(); break;
            }
        }
        else
        {
            SeManager.PlaySe(releaseSe, transform.position);
            if(mode == FloorSwitchMode.WhileOn) { state.TurnOff(); }
        }
    }

    /// <summary>
    /// 乗っている物を数える（同じ物は 1 回）
    /// </summary>
    private int CountOnTop()
    {
        var counted = new HashSet<GameObject>();
        // 何も絞らない（レイヤー・深さ・向きを問わない）。トリガーは数えない
        var filter = new ContactFilter2D { useTriggers = false };

        foreach(Collider2D area in areas)
        {
            if(area == null || !area.enabled || !area.isTrigger) { continue; }

            hits.Clear();
            area.Overlap(filter, hits);
            foreach(Collider2D hit in hits)
            {
                if(hit == null || hit.isTrigger) { continue; }

                GameObject owner = Owner(hit);
                if(owner == null || counted.Contains(owner)) { continue; }

                // 中心が範囲の中にあるときだけ（かすっただけでは押さない）
                if(!area.OverlapPoint(hit.bounds.center)) { continue; }

                counted.Add(owner);
            }
        }
        return counted.Count;
    }

    /// <summary> 反応する物ならその本体、反応しないなら null </summary>
    private GameObject Owner(Collider2D hit)
    {
        GameObject root = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;

        PlayerCharacter character = PlayerCharacters.Of(root);
        if(character == PlayerCharacter.Noah)  { return byNoah ? root : null; }
        if(character == PlayerCharacter.Sigle) { return bySigle ? root : null; }

        PushBlock block = hit.GetComponentInParent<PushBlock>();
        if(block != null) { return byBlock && !block.Fallen ? block.gameObject : null; }

        Enemy enemy = hit.GetComponentInParent<Enemy>();
        if(enemy != null) { return byEnemy && !enemy.IsDead ? enemy.gameObject : null; }

        return null;
    }
}
