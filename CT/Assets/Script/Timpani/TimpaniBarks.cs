/*********************************************************************/
// * \file   TimpaniBarks.cs
// * \brief  ティンパニ戦の一言（ヒント・褒める・注意）。戦闘中のできごとで、ノアやシグルが吹き出しで話す
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ティンパニ戦の一言
/// </summary>
///
/// ・戦闘エリア（BattleArea）が動いている間だけ、できごとに合わせて一言（吹き出し）を出す。ゲームは止まらない
/// ・できごと：ペグの成功・失敗・締まった、調律できた、守りが切れた・戻った、アイテムが落ちてきた・拾った、
///   ダメージ、体力が少ない、しばらくペグが進まない（ヒント）
/// ・できごとごとに、候補の一言（話すキャラと文）・出す確率を決める。候補が2つ以上ならランダム
/// ・一言どうしの間（秒）を空ける。同じできごとの一言は、決めた時間がたつまで出さない
public class TimpaniBarks : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum When
    {
        [InspectorName("ペグでタイミングよく押せた")]
        PegSuccess = 0,

        [InspectorName("ペグでタイミングを外した")]
        PegMiss = 1,

        [InspectorName("ペグが締まった（周りの攻撃が消えた）")]
        PegTightened = 2,

        [InspectorName("ティンパニを調律できた（残りが激しくなる）")]
        Calmed = 3,

        [InspectorName("守りのゲージが切れた")]
        GuardEmpty = 4,

        [InspectorName("守りのゲージが戻った")]
        GuardReady = 5,

        [InspectorName("アイテムが落ちてきた")]
        ItemAppeared = 6,

        [InspectorName("アイテムを拾った")]
        ItemPicked = 7,

        [InspectorName("ダメージを受けた")]
        Damaged = 8,

        [InspectorName("体力が少なくなった")]
        LowHealth = 9,

        [InspectorName("しばらくペグが進まない（ヒント）")]
        NoProgress = 10,

        [InspectorName("戦闘が始まった")]
        BattleStart = 11,
    }

    [Serializable]
    public class Entry
    {
        [Label("できごと")]
        public When when = When.PegSuccess;

        [Label("出す確率", 0.0f, 1.0f)]
        [Tooltip("できごとが起きたときに、この確率で出す（1 なら毎回）")]
        public float chance = 1.0f;

        [Label("次に出せるまで（秒）", 0.0f)]
        [Tooltip("同じできごとの一言を、この時間がたつまで出さない")]
        public float cooldown = 8.0f;

        [Tooltip("候補の一言（ランダムに1つ）")]
        public List<BarkLine> lines = new List<BarkLine>();

        [NonSerialized] public float readyTime;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("戦闘エリア")]
    [Tooltip("この戦闘エリアが動いている間だけ一言を出す（空ならシーンの戦闘エリアがどれか動いている間）")]
    private BattleArea battle;

    [SerializeField, Label("一言どうしの間（秒）", 0.0f)]
    [Tooltip("どのできごとでも、前の一言からこの時間は次を出さない")]
    private float globalGap = 3.0f;

    [SerializeField, Label("体力が少ないとき（この数以下）", 1)]
    private int lowHealth = 2;

    [SerializeField, Label("ヒントを出すまでの時間（秒）", 1.0f)]
    [Tooltip("ペグがこの時間進まなかったら「しばらくペグが進まない」の一言")]
    private float noProgressSeconds = 25.0f;

    [SerializeField]
    private List<Entry> entries = new List<Entry>();

    private float nextAllowed;
    private float lastProgressTime;
    private bool wasActive;
    private Health noahHealth;
    private Health sigleHealth;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void OnEnable()
    {
        TimpaniPeg.Succeeded += OnPegSucceeded;
        TimpaniPeg.Missed += OnPegMissed;
        TimpaniUnit.AttacksCleared += OnAttacksCleared;
        TimpaniUnit.Calmed += OnCalmed;
        GuardGauge.Emptied += OnGuardEmpty;
        GuardGauge.Ready += OnGuardReady;
        InstantPickup.Appeared += OnItemAppeared;
        InstantPickup.Picked += OnItemPicked;
    }

    private void OnDisable()
    {
        TimpaniPeg.Succeeded -= OnPegSucceeded;
        TimpaniPeg.Missed -= OnPegMissed;
        TimpaniUnit.AttacksCleared -= OnAttacksCleared;
        TimpaniUnit.Calmed -= OnCalmed;
        GuardGauge.Emptied -= OnGuardEmpty;
        GuardGauge.Ready -= OnGuardReady;
        InstantPickup.Appeared -= OnItemAppeared;
        InstantPickup.Picked -= OnItemPicked;
        HookHealth(null, null);
    }

    //=====================================================================
    // できごと
    //=====================================================================

    private void OnPegSucceeded(TimpaniPeg peg) { lastProgressTime = Time.time; if(!peg.IsTightened) { Say(When.PegSuccess); } }
    private void OnPegMissed(TimpaniPeg peg) { Say(When.PegMiss); }
    private void OnAttacksCleared(TimpaniUnit unit, TimpaniPeg peg) { lastProgressTime = Time.time; if(!unit.IsCalmed && unit.TightenedCount < unit.Pegs.Count) { Say(When.PegTightened); } }
    private void OnCalmed(TimpaniUnit unit) { lastProgressTime = Time.time; if(TimpaniRoom.Active == null || TimpaniRoom.Active.CalmedCount < TimpaniRoom.Active.UnitCount) { Say(When.Calmed, true); } }
    private void OnGuardEmpty() { Say(When.GuardEmpty); }
    private void OnGuardReady() { Say(When.GuardReady); }
    private void OnItemAppeared(InstantPickup item) { if(item.Effect == InstantPickup.Kind.Guard) { Say(When.ItemAppeared); } }
    private void OnItemPicked(InstantPickup item, GameObject who) { Say(When.ItemPicked); }

    private void OnDamaged(int amount)
    {
        Health h = PlayerCharacters.PartyHealth;
        if(h != null && h.CurrentHealth > 0 && h.CurrentHealth <= lowHealth && Say(When.LowHealth, true)) { return; }
        Say(When.Damaged);
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        bool active = BattleActive();

        if(active && !wasActive)
        {
            lastProgressTime = Time.time;
            Say(When.BattleStart, true);
        }
        wasActive = active;

        // ダメージを見るため、ノア・シグルの体力に登録する（出てくるのはシーンが始まったあと）
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m != null) { HookHealth(m.HealthOf(m.Noah), m.HealthOf(m.Sigle)); }

        if(active && noProgressSeconds > 0.0f && Time.time - lastProgressTime > noProgressSeconds)
        {
            lastProgressTime = Time.time;
            Say(When.NoProgress, true);
        }
    }

    private void HookHealth(Health noah, Health sigle)
    {
        if(noah == noahHealth && sigle == sigleHealth) { return; }

        if(noahHealth != null) { noahHealth.OnDamaged -= OnDamaged; }
        if(sigleHealth != null) { sigleHealth.OnDamaged -= OnDamaged; }

        noahHealth = noah;
        sigleHealth = sigle;

        if(noahHealth != null) { noahHealth.OnDamaged += OnDamaged; }
        if(sigleHealth != null && sigleHealth != noahHealth) { sigleHealth.OnDamaged += OnDamaged; }
    }

    private bool BattleActive()
    {
        return battle != null ? battle.Running : BattleArea.AnyActive;
    }

    //=====================================================================
    // 一言
    //=====================================================================

    /// <summary>
    /// そのできごとの一言を出す
    /// </summary>
    /// <param name="important"> 一言どうしの間を待たずに出す（大事なできごと） </param>
    private bool Say(When when, bool important = false)
    {
        if(!BattleActive()) { return false; }
        if(!important && Time.time < nextAllowed) { return false; }

        foreach(Entry entry in entries)
        {
            if(entry == null || entry.when != when || entry.lines.Count == 0) { continue; }
            if(Time.time < entry.readyTime) { continue; }
            if(UnityEngine.Random.value > entry.chance) { continue; }

            BarkLine line = entry.lines[UnityEngine.Random.Range(0, entry.lines.Count)];
            PlayerSwitchManager m = PlayerSwitchManager.Instance;
            GameObject subject = m != null ? m.CurrentPlayer : null;
            if(!Bark.PlayLines(new List<BarkLine> { line }, BarkContext.Of(subject), 50)) { return false; }

            entry.readyTime = Time.time + entry.cooldown;
            nextAllowed = Time.time + globalGap;
            return true;
        }
        return false;
    }
}
