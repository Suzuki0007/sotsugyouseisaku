/*********************************************************************/
// * \file   StandardSignals.cs
// * \brief  標準の合図：ゲームが自動で出す合図の一覧と、今あるプログラムの通知から合図を出す橋渡し
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標準の合図
/// </summary>
///
/// ・ペグ・調律・守り・アイテム・敵・戦闘・ダメージなど、プログラムの中の出来事を決まった ID の合図にして出す
/// ・合図のアセットは Tools > 合図 > 標準の合図を作る（Assets/Data/Signals/標準/）。アセットの ID がここの ID と同じなら受けられる
/// ・ID は変えない（受ける側のアセットが ID で比べている）。新しく足すときは一覧（All）と出す所の両方に足す
/// ・戦闘（BattleArea）・ダメージ（BarkWatcher）・ペグが進まない（TimpaniRoom）は、その場所から直接出す
public static class StandardSignals
{
    //=====================================================================
    // ID
    //=====================================================================

    public const string PegHit       = "timpani/peg_hit";
    public const string PegMiss      = "timpani/peg_miss";
    public const string PegTightened = "timpani/peg_tightened";
    public const string Calmed       = "timpani/calmed";
    public const string AllCalmed    = "timpani/all_calmed";
    public const string Stalled      = "timpani/stalled";
    public const string GuardEmpty   = "guard/empty";
    public const string GuardReady   = "guard/ready";
    public const string ItemGuard    = "item/appeared_guard";
    public const string ItemHeal     = "item/appeared_heal";
    public const string ItemPicked   = "item/picked";
    public const string EnemyDefeated = "enemy/defeated";
    public const string BattleStarted = "battle/started";
    public const string BattleWon    = "battle/won";
    public const string BattleLost   = "battle/lost";
    public const string Damaged      = "player/damaged";
    public const string LowHealth    = "player/low_health";
    public const string AutoSaved    = "game/autosaved";

    /// <summary> 一覧の1件（ID・アセットの名前・説明） </summary>
    public struct Info
    {
        public string id;
        public string name;
        public string memo;

        public Info(string id, string name, string memo) { this.id = id; this.name = name; this.memo = memo; }
    }

    /// <summary> 標準の合図の一覧（アセットを作るメニュー・早見表・シーンチェックが使う） </summary>
    public static readonly IReadOnlyList<Info> All = new List<Info>
    {
        new Info(PegHit,        "ペグでタイミングよく押せた",   "まだ締まっていないとき。送り元＝ペグ。数＝成功した回数"),
        new Info(PegMiss,       "ペグでタイミングを外した",     "送り元＝ペグ"),
        new Info(PegTightened,  "ペグが締まった",               "周りの攻撃が消えたとき。そのティンパニの最後のペグ（＝調律できた）は除く。送り元＝ティンパニ。数＝締まった数"),
        new Info(Calmed,        "ティンパニを調律できた",       "最後の1台（＝全部調律できた）は除く。送り元＝ティンパニ。数＝調律できた数"),
        new Info(AllCalmed,     "ティンパニを全部調律できた",   "送り元＝ティンパニの部屋"),
        new Info(Stalled,       "しばらくペグが進まない",       "戦闘中、ティンパニの部屋の「ヒントの合図を出すまでの時間」ペグが進まなかったとき。送り元＝ティンパニの部屋"),
        new Info(GuardEmpty,    "守りのゲージが切れた",         "送り元＝守りのゲージ"),
        new Info(GuardReady,    "守りのゲージが戻った",         "空になったあと、また出せるようになったとき。送り元＝守りのゲージ"),
        new Info(ItemGuard,     "守りのアイテムが出てきた",     "すぐ効くアイテム（守りのゲージ）が出たとき。送り元＝アイテム"),
        new Info(ItemHeal,      "回復のアイテムが出てきた",     "すぐ効くアイテム（体力の回復）が出たとき。送り元＝アイテム"),
        new Info(ItemPicked,    "すぐ効くアイテムを拾った",     "送り元＝拾ったキャラ"),
        new Info(EnemyDefeated, "敵を倒した",                   "送り元＝敵"),
        new Info(BattleStarted, "戦闘が始まった",               "送り元＝戦闘エリア"),
        new Info(BattleWon,     "戦闘に勝った",                 "送り元＝戦闘エリア"),
        new Info(BattleLost,    "戦闘に負けた",                 "戦闘エリアの負け方を満たしたとき（体力が 0 のゲームオーバーは含まない）。送り元＝戦闘エリア"),
        new Info(Damaged,       "ダメージを受けた",             "ノア・シグル。送り元＝受けたキャラ（一緒のときは体力を分けているシグルになることがある）。数＝残りの体力"),
        new Info(LowHealth,     "体力が少ない",                 "ダメージを受けて、体力が最大の 1/3 以下のとき（受けるたびに出る。「ダメージを受けた」より先に出る）。送り元＝受けたキャラ。数＝残りの体力"),
        new Info(AutoSaved,     "オートセーブした",             "送り元なし"),
    };

    //=====================================================================
    // 橋渡し（今あるプログラムの通知 → 合図）
    //=====================================================================

    // 各クラスの static イベントは SubsystemRegistration で空になるので、そのあと（BeforeSceneLoad）でつなぐ
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        TimpaniPeg.Succeeded += peg => { if(!peg.IsTightened) { Send(PegHit, peg.gameObject, peg.SuccessCount); } };
        TimpaniPeg.Missed += peg => Send(PegMiss, peg.gameObject);
        TimpaniUnit.AttacksCleared += (unit, peg) =>
        {
            if(!unit.IsCalmed && unit.TightenedCount < unit.Pegs.Count) { Send(PegTightened, unit.gameObject, unit.TightenedCount); }
        };
        TimpaniUnit.Calmed += unit =>
        {
            // 部屋の数え直しより先に呼ばれることがあるので、自分で数える
            int calmed = 0;
            int total = 0;
            foreach(TimpaniUnit u in Object.FindObjectsByType<TimpaniUnit>())
            {
                total++;
                if(u.IsCalmed) { calmed++; }
            }
            if(calmed < total) { Send(Calmed, unit.gameObject, calmed); }
        };
        TimpaniRoom.AllCalmed += () => Send(AllCalmed, TimpaniRoom.Active != null ? TimpaniRoom.Active.gameObject : null);
        GuardGauge.Emptied += () => Send(GuardEmpty, GuardSource());
        GuardGauge.Ready += () => Send(GuardReady, GuardSource());
        InstantPickup.Appeared += item => Send(item.Effect == InstantPickup.Kind.Guard ? ItemGuard : ItemHeal, item.gameObject);
        InstantPickup.Picked += (item, who) => Send(ItemPicked, who);
        Enemy.Defeated += enemy => Send(EnemyDefeated, enemy != null ? enemy.gameObject : null);
        GameSession.AutoSaved += () => Send(AutoSaved, null);
    }

    private static GameObject GuardSource()
    {
        GuardGauge g = Object.FindAnyObjectByType<GuardGauge>();
        return g != null ? g.gameObject : null;
    }

    /// <summary> 合図を出す（送り元と数） </summary>
    public static void Send(string id, GameObject source, float value = 0.0f)
    {
        Signals.Send(id, SignalContext.From(source, value));
    }

    /// <summary> ID から一覧の1件を探す（なければ名前は空） </summary>
    public static Info Find(string id)
    {
        foreach(Info i in All) { if(i.id == id) { return i; } }
        return new Info(id, "", "");
    }
}
