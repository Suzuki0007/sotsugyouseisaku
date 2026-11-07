/*********************************************************************/
// * \file   ItemInteraction.cs
// * \brief  アイテムの所持状況で結果が変わるオブジェクト
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテムの所持状況で結果が変わるオブジェクト
/// </summary>
///
/// ・「きっかけ」（調べた／エリアに入った）で、上から順にルールの条件（アイテム・フラグ）を調べる
/// ・条件を満たした最初のルールの EventSequence を実行する。どれも満たさなければ「それ以外」を実行する
/// ・ルールに「完了にする」を付けると、以後は「完了後」の EventSequence だけを実行する
///   完了したことは Save に記録し、シーンを読み込み直すと見た目だけを完了後の状態にする
///
/// 例：扉
///   ルール1：鍵 ×1 を持っている → 鍵を消費 → 扉を非表示にする（完了にする）
///   それ以外：「鍵がかかっている」と会話
///
/// Collider2D（Is Trigger）を付けたオブジェクトに付けて使う
[RequireComponent(typeof(Collider2D))]
public class ItemInteraction : Intaractable
{
    //=====================================================================
    // 型
    //=====================================================================

    /// <summary>
    /// きっかけ
    /// </summary>
    public enum TriggerType
    {
        [InspectorName("調べたとき（E キー）")]
        Interact,

        [InspectorName("エリアに入ったとき")]
        EnterArea,
    }

    /// <summary>
    /// ルール（条件と結果の組）
    /// </summary>
    [Serializable]
    public class Rule
    {
        [Tooltip("メモ（Inspector で見分けるための名前。動作には影響しない）")]
        public string label;

        [Tooltip("すべて満たしたときにこのルールを実行する（空なら常に満たす）")]
        public List<ItemCondition> conditions = new List<ItemCondition>();

        [Tooltip("フラグの条件。アイテムの条件と合わせて、すべて満たしたときにこのルールを実行する（空なら常に満たす）")]
        public List<FlagCondition> flagConditions = new List<FlagCondition>();

        [Tooltip("実行するときに、条件のアイテムを消費する（「持っていないとき」の条件は消費しない）")]
        public bool consumeItems;

        [Tooltip("実行する内容")]
        public EventSequence sequence;

        [Tooltip("実行したら完了にする（以後このルールは実行されず、完了状態を保存する）")]
        public bool markCompleted = true;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [Header("きっかけ")]

    [SerializeField]
    private TriggerType trigger = TriggerType.Interact;

    [Header("ルール（上から順に調べ、最初に条件を満たしたものを実行）")]

    [SerializeField]
    private List<Rule> rules = new List<Rule>();

    [Header("どのルールも満たさないとき（任意）")]

    [SerializeField]
    private EventSequence otherwise;

    [Header("完了後（任意）")]

    [Tooltip("完了したあとにもう一度調べた・入ったときに実行する（例：「もう開いている」）")]
    [SerializeField]
    private EventSequence afterCompleted;

    [Header("保存")]

    [Tooltip("完了したことを記録するためのID。空ならヒエラルキー上の名前を使う（同じシーンで重複しないようにする）")]
    [SerializeField]
    private string saveId;

    [Tooltip("完了状態を保存して、シーンを読み込み直しても完了後の状態にする")]
    [SerializeField]
    private bool saveCompleted = true;

    private string saveKey;

    // 完了したルールの番号（-1 なら未完了）
    private int completedRule = -1;

    // 最後に実行した EventSequence（エリアから出たときに Exit を呼ぶため）
    private EventSequence lastPlayed;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// 完了しているか
    /// </summary>
    public bool IsCompleted => completedRule >= 0;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        saveKey = "Interaction/" + gameObject.scene.name + "/" +
                  (string.IsNullOrEmpty(saveId) ? GetHierarchyPath(transform) : saveId);
    }

    private void Start()
    {
        if(!saveCompleted) { return; }

        int saved = Save.Get(saveKey, -1);
        if(saved < 0 || saved >= rules.Count) { return; }

        // 完了済みなら、会話やSEは鳴らさず見た目だけを完了後の状態にする
        completedRule = saved;
        if(rules[saved].sequence != null) { rules[saved].sequence.Restore(gameObject); }
    }

    //=====================================================================
    // きっかけ
    //=====================================================================

    /// <summary>
    /// 調べたとき（Intaractable から呼ばれる）
    /// </summary>
    protected override void Interact()
    {
        if(trigger != TriggerType.Interact) { return; }

        Run(player);
    }

    /// <summary>
    /// エリアに入ったとき
    /// </summary>
    protected override void OnPlayerEnter(GameObject enteredPlayer)
    {
        if(trigger != TriggerType.EnterArea) { return; }

        Run(enteredPlayer);
    }

    /// <summary>
    /// エリアから出たとき（会話を閉じるなど、Exit を呼ぶ）
    /// </summary>
    protected override void OnPlayerExit(GameObject exitedPlayer)
    {
        if(lastPlayed == null) { return; }

        lastPlayed.Exit(exitedPlayer);
        lastPlayed = null;
    }

    //=====================================================================
    // 実行
    //=====================================================================

    /// <summary>
    /// ルールを調べて実行する（スクリプトから呼んでもよい）
    /// </summary>
    public void Run(GameObject target)
    {
        // 完了済みなら「完了後」だけを実行する
        if(IsCompleted)
        {
            Play(afterCompleted, target);
            return;
        }

        for(int i = 0; i < rules.Count; i++)
        {
            Rule rule = rules[i];
            if(rule == null) { continue; }
            if(!ItemCondition.AllMet(rule.conditions)) { continue; }
            if(!FlagCondition.AllMet(rule.flagConditions)) { continue; }

            if(rule.consumeItems) { ItemCondition.ConsumeAll(rule.conditions); }

            if(rule.markCompleted) { Complete(i); }

            Play(rule.sequence, target);
            return;
        }

        Play(otherwise, target);
    }

    /// <summary>
    /// EventSequence を実行する
    /// </summary>
    private void Play(EventSequence sequence, GameObject target)
    {
        if(sequence == null) { return; }

        lastPlayed = sequence;
        sequence.Play(target);
    }

    /// <summary>
    /// 完了にして、仮置きで記録する（チェックポイント・シーン遷移で確定）
    /// </summary>
    private void Complete(int ruleIndex)
    {
        completedRule = ruleIndex;

        if(saveCompleted) { Save.SetPending(saveKey, ruleIndex); }
    }

    //=====================================================================
    // 補助
    //=====================================================================

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

    /// <summary>
    /// エディタのひな形作成用：ルールを1つ追加する
    /// </summary>
    public void AddRuleForEditor(Rule rule)
    {
        if(rule != null) { rules.Add(rule); }
    }

    /// <summary>
    /// エディタのひな形作成用：「それ以外」を設定する
    /// </summary>
    public void SetOtherwiseForEditor(EventSequence sequence)
    {
        otherwise = sequence;
    }

    /// <summary>
    /// エディタのひな形作成用：「完了後」と保存IDを設定する
    /// </summary>
    public void SetAfterCompletedForEditor(EventSequence sequence, string id)
    {
        afterCompleted = sequence;
        saveId = id;
    }
}
