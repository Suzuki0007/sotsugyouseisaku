/*********************************************************************/
// * \file   CodeLock.cs
// * \brief  番号錠・パスワード（調べると入力の画面が出る。正解で仕掛けが ON）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 番号錠
/// </summary>
///
/// ・調べると入力の画面（←→ で桁、↑↓ で変える）。答えと同じなら正解で、仕掛けの状態が ON（固定・セーブ）
///   例：金庫の 4 桁の番号（バイオハザード）／ 色の並び（使える文字を「赤青黄緑」に）／ 曜日のダイヤル
/// ・ヒントはメモ（NotePickup）や会話で渡す
/// ・扉などは、ほかの仕掛けと同じく「条件」でこの番号錠が ON かを見る
[RequireComponent(typeof(GimmickState))]
public class CodeLock : GimmickInteractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("画面の見出し")]
    [Tooltip("入力の画面の上に出す文（例：「金庫の番号」）")]
    private string title = "番号を合わせる";

    [SerializeField, Label("使える文字")]
    [Tooltip("1 文字ずつ順に回る。数字なら 0123456789、色なら 赤青黄緑 など")]
    private string symbols = "0123456789";

    [SerializeField, Label("答え")]
    [Tooltip("使える文字で書いた答え（桁の数は答えの文字数）。例：0314")]
    private string answer = "0000";

    [SerializeField, Label("前の入力を覚えておく")]
    [Tooltip("オン：もう一度調べたとき、前に入れた並びから始まる（シーンの中だけ）／オフ：毎回はじめの文字から")]
    private bool rememberInput = true;

    [SerializeField, Label("使えるキャラ")]
    private CharacterRestriction user = CharacterRestriction.Both;

    [Header("正解・間違い")]

    [SerializeField, Label("正解の SE")]
    private SeData correctSe;

    [SerializeField, Label("正解したときのイベント")]
    [Tooltip("「カチッ」の会話など。扉を開けるのは、扉の「条件」にこの番号錠を入れる")]
    private EventSequence onCorrect;

    [SerializeField, Label("間違いの SE")]
    private SeData wrongSe;

    [SerializeField, Label("間違えたときのイベント")]
    [Tooltip("「開かない……」の会話・罠など。空なら音だけ")]
    private EventSequence onWrong;

    [SerializeField, Label("開いたあとに調べたとき")]
    [Tooltip("もう開いているときに調べた（「もう開いている」など）。空なら何もしない")]
    private EventSequence afterOpened;

    [SerializeField, Label("使えないキャラで調べたとき")]
    private EventSequence wrongCharacter;

    private GimmickState state;
    private int[] lastInput;

    public string Symbols => symbols;
    public string Answer => answer;
    protected override CharacterRestriction Restriction => user;

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
        if(!user.Allows(current)) { RefuseCharacter(wrongCharacter); return; }

        if(state.IsOn)
        {
            if(afterOpened != null) { afterOpened.Play(gameObject); }
            return;
        }

        int digits = Mathf.Max(1, answer.Length);
        int[] start = rememberInput && lastInput != null && lastInput.Length == digits ? lastInput : new int[digits];
        PuzzleUI.OpenCode(title, symbols, start, OnSubmit, null);
    }

    private void OnSubmit(int[] input)
    {
        lastInput = input;

        if(IsCorrect(input))
        {
            SeManager.PlaySe(correctSe, transform.position);
            state.Set(1, true);
            state.SetLocked(true);
            if(onCorrect != null) { onCorrect.Play(gameObject); }
            Bark.Fire(BarkTrigger.PuzzleSolved, BarkContext.Of(PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null, transform));
            return;
        }

        SeManager.PlaySe(wrongSe, transform.position);
        if(onWrong != null) { onWrong.Play(gameObject); }
        else if(wrongSe == null) { UISound.Play(UISoundType.Cannot); }
        Bark.Fire(BarkTrigger.PuzzleWrong, BarkContext.Of(PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null, transform));
    }

    /// <summary> 入力が答えと同じか </summary>
    public bool IsCorrect(int[] input)
    {
        if(input == null || input.Length != answer.Length) { return false; }
        for(int i = 0; i < input.Length; i++)
        {
            int index = Mathf.Clamp(input[i], 0, symbols.Length - 1);
            if(symbols[index] != answer[i]) { return false; }
        }
        return true;
    }
}
