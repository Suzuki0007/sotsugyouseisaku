/*********************************************************************/
// * \file   BarkData.cs
// * \brief  相棒の一言のデータ（ScriptableObject。Resources/Barks/ に置く）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一言が出るきっかけ
/// </summary>
/// 番号はデータに保存されるので、途中に足さない（末尾に足す）
public enum BarkTrigger
{
    [InspectorName("イベント・範囲からだけ")]
    Manual = 0,

    [InspectorName("使えないキャラで調べた")]
    WrongCharacter = 1,

    [InspectorName("調べたけど使えなかった（条件・アイテムが足りない）")]
    CannotUse = 2,

    [InspectorName("相棒の出番の仕掛けに近づいた（目印が出た）")]
    PartnerGimmickNear = 3,

    [InspectorName("呼んでも来られない（待て／ついてこいができない）")]
    CannotFollow = 4,

    [InspectorName("ダメージを受けた")]
    Damaged = 5,

    [InspectorName("体力が少なくなった")]
    LowHealth = 6,

    [InspectorName("敵を倒した")]
    EnemyDefeated = 7,

    [InspectorName("相棒を待たせた")]
    Wait = 8,

    [InspectorName("相棒を長く待たせている")]
    LongWait = 9,

    [InspectorName("待たせていた相棒を呼び戻した")]
    CalledBack = 10,

    [InspectorName("離れ離れになった（別行動）")]
    Separated = 11,

    [InspectorName("別行動から合流した")]
    Reunited = 12,

    [InspectorName("操作するキャラを切り替えた")]
    Switched = 13,

    [InspectorName("しばらく止まっている")]
    Idle = 14,

    [InspectorName("シーンに着いた")]
    SceneEnter = 15,

    [InspectorName("謎を解いた（答え合わせ・番号錠の正解）")]
    PuzzleSolved = 16,

    [InspectorName("答えを間違えた")]
    PuzzleWrong = 17,

    [InspectorName("合図が来た（下の「合図」）")]
    Signal = 18,
}

/// <summary>
/// 話すキャラ
/// </summary>
public enum BarkSpeaker
{
    [InspectorName("相棒（操作していない方）")]
    Partner = 0,

    [InspectorName("操作しているキャラ")]
    Current = 1,

    [InspectorName("ノア")]
    Noah = 2,

    [InspectorName("シグル")]
    Sigle = 3,

    [InspectorName("きっかけになったキャラ（ダメージを受けた・調べたキャラ）")]
    Subject = 4,

    [InspectorName("きっかけになったキャラの相棒")]
    SubjectPartner = 5,
}

/// <summary>
/// 何回出すか
/// </summary>
public enum BarkRepeat
{
    [InspectorName("何度でも（待ち時間をあけて）")]
    Always = 0,

    [InspectorName("シーンに入るたびに1回")]
    OncePerScene = 1,

    [InspectorName("ゲームの中で1回だけ（セーブする）")]
    OnceEver = 2,
}

/// <summary>
/// 一言の1行
/// </summary>
[System.Serializable]
public class BarkLine
{
    [Label("話すキャラ")]
    public BarkSpeaker speaker = BarkSpeaker.Partner;

    [TextArea(1, 3)]
    [Tooltip("吹き出しに出す文。短めに（1〜2行）")]
    public string text = "";

    [Label("出している秒数", 0.0f)]
    [Tooltip("0 なら文の長さで決める（一言の設定の「1文字あたりの秒数」）")]
    public float seconds = 0.0f;

    [Label("次の行までの間（秒）", 0.0f)]
    [Tooltip("この行が消えてから次の行が出るまで")]
    public float gap = 0.2f;

    [Label("SE")]
    [Tooltip("この行を出すときに鳴らす（空なら鳴らさない）")]
    public SeData se;
}

/// <summary>
/// 一言の候補（1行だけか、掛け合い）
/// </summary>
[System.Serializable]
public class BarkVariant
{
    [Tooltip("上から順に出す。2行以上で掛け合いになる（例：シグル「大丈夫か」→ ノア「平気」）")]
    public List<BarkLine> lines = new List<BarkLine>();

    [Label("選ばれやすさ", 0.0f)]
    [Tooltip("候補の中からこの重みで選ぶ（同じなら同じ確率）")]
    public float weight = 1.0f;
}

/// <summary>
/// 相棒の一言のデータ
/// </summary>
///
/// ・Resources/Barks/ に置くと、きっかけが起きたときに自動で選ばれる（シーンに置く物はない）
/// ・同じきっかけのデータが複数あれば、条件を満たすものから「優先度」の高いものを選ぶ（同じなら重みでランダム）
/// ・吹き出しはゲームを止めない。会話・メニューの間は出ない
[CreateAssetMenu(menuName = "仕掛け/相棒の一言", fileName = "Bark_")]
public class BarkData : ScriptableObject
{
    [Label("使う")]
    [Tooltip("オフにすると、この一言は出ない（消さずに止めておきたいとき）")]
    public bool active = true;

    [Label("きっかけ")]
    [Tooltip("いつ出すか。「イベント・範囲からだけ」は、イベントの部品「一言」と一言の範囲から出す")]
    public BarkTrigger trigger = BarkTrigger.Manual;

    [Label("合図")]
    [Tooltip("きっかけが「合図が来た」のとき：この合図で出す（標準の合図は Assets/Data/Signals/標準/）。戦闘中だけにしたいときは、条件に戦闘中のフラグなどを入れる")]
    public GameSignal signal;

    [Label("秒数", 0.0f)]
    [Tooltip("「長く待たせている」：待たせてからこの秒数で出す\n「しばらく止まっている」：止まってからこの秒数で出す\n「体力が少なくなった」では使わない")]
    public float seconds = 20.0f;

    [Label("体力の割合", 0.0f, 1.0f)]
    [Tooltip("「体力が少なくなった」：受けたあとの体力（最大を 1）がこの割合以下になったら出す")]
    public float healthRatio = 0.34f;

    [Label("倒した敵")]
    [Tooltip("「敵を倒した」：この敵のときだけ出す（空ならどの敵でも）")]
    public EnemyData enemy;

    [Tooltip("出す条件（フラグ・アイテム・操作中のキャラ・2人の状態など）。すべて満たすとき。空なら判定しない")]
    public GameCondition condition = new GameCondition();

    [Label("優先度")]
    [Tooltip("同じきっかけで複数出せるとき、大きい方を出す。出している一言より大きければ割り込む")]
    public int priority = 0;

    [Label("出る確率", 0.0f, 1.0f)]
    [Tooltip("1 ならいつも。0.3 なら 3 回に 1 回くらい（毎回出るとうるさい一言に）")]
    public float chance = 1.0f;

    [Label("何回出すか")]
    public BarkRepeat repeat = BarkRepeat.Always;

    [Label("次に出せるまで（秒）", 0.0f)]
    [Tooltip("一度出したら、この秒数は同じ一言を出さない")]
    public float cooldown = 30.0f;

    [Label("一言どうしの間を待たない")]
    [Tooltip("オンにすると、前の一言のすぐあとでも出す（一言の設定の「一言どうしの間」を待たない）。大事な一言（戦闘の始まり・体力が少ない・ヒントなど）に")]
    public bool ignoreGap = false;

    [Label("出すまでの間（秒）", 0.0f)]
    [Tooltip("きっかけから少し間をおいて出す（間に会話などが始まったら出さない）")]
    public float delay = 0.3f;

    [Tooltip("候補。どれか1つを選んで出す（同じことを毎回言わないように、言い方を変えて並べる）")]
    public List<BarkVariant> variants = new List<BarkVariant>();

    [TextArea(1, 4)]
    [Tooltip("メモ（ゲームには出ない）")]
    public string memo = "";

    /// <summary> ゲームの中で1回だけのときのセーブのキー </summary>
    public string SaveKey => "Bark/" + name;

    /// <summary> 出せる行がある候補があるか </summary>
    public bool HasLines
    {
        get
        {
            foreach(BarkVariant v in variants)
            {
                if(v == null) { continue; }
                foreach(BarkLine l in v.lines) { if(l != null && !string.IsNullOrEmpty(l.text)) { return true; } }
            }
            return false;
        }
    }

    /// <summary> 重みで候補を1つ選ぶ（なければ null） </summary>
    public BarkVariant PickVariant()
    {
        float total = 0.0f;
        foreach(BarkVariant v in variants) { if(v != null && v.lines.Count > 0) { total += Mathf.Max(0.0f, v.weight); } }
        if(total <= 0.0f)
        {
            foreach(BarkVariant v in variants) { if(v != null && v.lines.Count > 0) { return v; } }
            return null;
        }

        float r = Random.value * total;
        foreach(BarkVariant v in variants)
        {
            if(v == null || v.lines.Count == 0) { continue; }
            r -= Mathf.Max(0.0f, v.weight);
            if(r <= 0.0f) { return v; }
        }
        return variants[variants.Count - 1];
    }
}
