/*********************************************************************/
// * \file   GimmickSwitch.cs
// * \brief  調べると仕掛けの状態が変わるスイッチ（レバー・ボタン・燭台・ダイヤルなど）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// スイッチを調べたときの動き
/// </summary>
public enum GimmickSwitchAction
{
    [InspectorName("ON / OFF を切り替える（段階があれば次へ）")]
    Toggle = 0,

    [InspectorName("ON にする（もう一度調べても OFF にならない）")]
    TurnOn = 1,

    [InspectorName("OFF にする")]
    TurnOff = 2,

    [InspectorName("次の段階へ")]
    Advance = 3,
}

/// <summary>
/// 調べるスイッチ
/// </summary>
///
/// ・範囲（Is Trigger の Collider2D）に入って E で調べると、仕掛けの状態（GimmickState）を変える
/// ・使えるキャラ（両方・ノアだけ・シグルだけ）と、使える条件（電源が入っている など）を決められる
/// ・使えないキャラ・条件を満たさないときは、それぞれのイベント（会話など）を実行する（空なら「できない」音だけ）
/// ・調べられるのは、範囲の中にいる「操作しているキャラ」だけ（ついてくる相棒・待たせている相棒では反応しない）
[RequireComponent(typeof(GimmickState))]
public class GimmickSwitch : GimmickInteractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("調べたとき")]
    private GimmickSwitchAction action = GimmickSwitchAction.Toggle;

    [SerializeField, Label("使えるキャラ")]
    [Tooltip("ノアだけ：ノアの装置・魔法陣 など ／ シグルだけ：重いレバー など")]
    private CharacterRestriction user = CharacterRestriction.Both;

    [SerializeField, Label("使える条件")]
    [Tooltip("満たしているときだけ使える（空なら いつでも）。例：電源のスイッチが ON のとき")]
    private GameCondition condition = new GameCondition();

    [SerializeField, Label("調べたときの SE")]
    private SeData useSe;

    [Header("使えないとき")]

    [SerializeField, Label("使えないキャラで調べたとき")]
    [Tooltip("例：ノアで調べたら「重くて動かない……シグルなら」。空なら「できない」音だけ")]
    private EventSequence wrongCharacter;

    [SerializeField, Label("条件を満たさないとき")]
    [Tooltip("例：「電気が来ていない」。空なら「できない」音だけ")]
    private EventSequence conditionNotMet;

    [SerializeField, Label("固定されているとき")]
    [Tooltip("答え合わせが済んで動かせないとき。空なら何もしない")]
    private EventSequence lockedSequence;

    private GimmickState state;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public GimmickState State => state != null ? state : GetComponent<GimmickState>();
    public CharacterRestriction User => user;
    public GameCondition Condition => condition;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        state = GetComponent<GimmickState>();
    }

    protected override void OnInteract(GameObject current)
    {
        if(!user.Allows(current))
        {
            Refuse(wrongCharacter);
            return;
        }
        if(condition != null && !condition.IsEmpty && !condition.IsMet())
        {
            Refuse(conditionNotMet);
            return;
        }
        if(state.Locked)
        {
            if(lockedSequence != null) { lockedSequence.Play(gameObject); }
            return;
        }

        bool changed;
        switch(action)
        {
            case GimmickSwitchAction.TurnOn:  changed = state.TurnOn(); break;
            case GimmickSwitchAction.TurnOff: changed = state.TurnOff(); break;
            case GimmickSwitchAction.Advance: changed = state.Advance(); break;
            default:                          changed = state.Toggle(); break;
        }

        if(changed) { SeManager.PlaySe(useSe, transform.position); }
    }
}
