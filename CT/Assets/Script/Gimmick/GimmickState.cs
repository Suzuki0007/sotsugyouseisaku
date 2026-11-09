/*********************************************************************/
// * \file   GimmickState.cs
// * \brief  仕掛けの状態（ON / OFF、または 0〜n の段階）。セーブする
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 仕掛けの状態
/// </summary>
///
/// ・謎解きの「入力」（スイッチ・レバー・燭台・ダイヤルなど）が持つ状態。ON / OFF（段階の数 2）か、0〜n の段階
/// ・扉などの「出力」は、条件（GameCondition の「仕掛けの状態」）でこの状態を見て動く
/// ・状態はセーブする（Gimmick/<シーン>/<ID か階層パス>）。セーブはほかの物と同じく「仮置き → 確定」
/// ・答え合わせが済んだら「固定」して、もう変えられないようにできる
[DisallowMultipleComponent]
public class GimmickState : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("表示名")]
    [Tooltip("デバッグウィンドウや配線の表示で出る名前。空ならオブジェクトの名前")]
    private string displayName = "";

    [SerializeField, Label("段階の数", 2)]
    [Tooltip("2 なら OFF（0）/ ON（1）。3 以上にすると 0, 1, 2 … の段階（ダイヤル・像の向きなど）")]
    private int stateCount = 2;

    [SerializeField, Label("最初の状態", 0)]
    [Tooltip("0 = OFF。セーブがなければこの状態から始まる")]
    private int initialValue = 0;

    [SerializeField, Label("最後の次は最初に戻る")]
    [Tooltip("オン：「次の段階へ」で最後の次は 0 に戻る（ダイヤル）／オフ：最後で止まる")]
    private bool wrap = true;

    [SerializeField, Label("変わったときのイベント")]
    [Tooltip("状態が変わるたびに実行する（音・見た目の切り替えなど）。空でもよい")]
    private EventSequence onChanged;

    [Header("時間で戻る")]

    [SerializeField, Label("戻るまでの秒数", 0.0f)]
    [Tooltip("最初の状態から変わったら、この秒数で最初の状態に戻る（時間制限のあるスイッチ）。0 なら戻らない。\n会話中などは時間が進まない。時間で戻る仕掛けは、シーンに入り直すと最初の状態から")]
    private float resetAfter = 0.0f;

    [SerializeField, Label("残り時間の音")]
    [Tooltip("戻るまでの間、1 秒ごとに鳴らす（カチカチ）。空なら鳴らさない")]
    private SeData tickSe;

    [SerializeField, Label("戻ったときの音")]
    private SeData resetSe;

    [Header("セーブ")]

    [SerializeField, Label("セーブする")]
    [Tooltip("オフにすると、シーンを読み込むたびに最初の状態に戻る（その場限りの仕掛け）")]
    private bool save = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する（名前や親を変えると記録がリセットされる）")]
    private string saveId = "";

    private int value;
    private bool locked;
    private string saveKey;
    private float resetTimer;
    private int lastTick;

    private static readonly List<GimmickState> all = new List<GimmickState>();

    //=====================================================================
    // プロパティ・イベント
    //=====================================================================

    /// <summary> 状態が変わったとき（引数：新しい値） </summary>
    public event System.Action<GimmickState, int> Changed;

    /// <summary> 今の状態（0 = OFF） </summary>
    public int Value => value;

    /// <summary> ON か（0 以外） </summary>
    public bool IsOn => value != 0;

    /// <summary> 段階の数（2 なら OFF / ON） </summary>
    public int StateCount => Mathf.Max(2, stateCount);

    /// <summary> 最初の状態 </summary>
    public int InitialValue => Mathf.Clamp(initialValue, 0, StateCount - 1);

    /// <summary> 固定されているか（答え合わせが済んだなど。スイッチから変えられない） </summary>
    public bool Locked => locked;

    /// <summary> 表示名 </summary>
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    /// <summary> 状態の表示（ON / OFF か 数字） </summary>
    public string ValueText => StateCount == 2 ? (IsOn ? "ON" : "OFF") : value.ToString();

    /// <summary> 時間で戻るまでの残り秒数（戻らない・最初の状態なら 0） </summary>
    public float RemainingTime => resetAfter > 0.0f && value != InitialValue && !locked ? Mathf.Max(0.0f, resetAfter - resetTimer) : 0.0f;

    /// <summary> シーンにある仕掛けの状態（デバッグ用） </summary>
    public static IReadOnlyList<GimmickState> All => all;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { all.Clear(); }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        saveKey = MakeSaveKey(gameObject, saveId);
        value = InitialValue;
        if(save) { value = Mathf.Clamp(Save.Get(saveKey, value), 0, StateCount - 1); }
        locked = save && Save.Get(saveKey + "/Locked", false);

        // 時間で戻る仕掛けは、入り直したら最初の状態から（固定されたもの＝答え合わせが済んだものはそのまま）
        if(resetAfter > 0.0f && !locked) { value = InitialValue; }
    }

    private void Update()
    {
        if(resetAfter <= 0.0f || locked || value == InitialValue) { return; }
        if(InputBlock.IsBlocked) { return; }

        resetTimer += Time.deltaTime;

        int tick = Mathf.FloorToInt(resetTimer);
        if(tick != lastTick) { lastTick = tick; SeManager.PlaySe(tickSe, transform.position); }

        if(resetTimer >= resetAfter)
        {
            Set(InitialValue);
            SeManager.PlaySe(resetSe, transform.position);
        }
    }

    private void OnEnable()  { if(!all.Contains(this)) { all.Add(this); } }
    private void OnDisable() { all.Remove(this); }

    private void Start()
    {
        // セーブから戻ったときは、変わったときのイベントを「実行後の見た目」にする
        if(value != InitialValue && onChanged != null) { onChanged.Restore(gameObject); }
    }

    //=====================================================================
    // 変える
    //=====================================================================

    /// <summary>
    /// 状態を変える
    /// </summary>
    /// <param name="newValue"> 新しい値（段階の数の中に収める） </param>
    /// <param name="force"> 固定されていても変える（イベントの部品・答え合わせのやり直しから） </param>
    /// <param name="playEvent"> 変わったときのイベントを実行するか </param>
    /// <returns> 変わったか </returns>
    public bool Set(int newValue, bool force = false, bool playEvent = true)
    {
        if(locked && !force) { return false; }

        newValue = Mathf.Clamp(newValue, 0, StateCount - 1);
        if(newValue == value) { return false; }

        value = newValue;
        if(save) { Save.SetPending(saveKey, value); }
        resetTimer = 0.0f;
        lastTick = 0;

        if(playEvent && onChanged != null) { onChanged.Play(gameObject); }
        Changed?.Invoke(this, value);
        return true;
    }

    /// <summary> ON にする </summary>
    public bool TurnOn(bool force = false) => Set(Mathf.Max(1, value == 0 ? 1 : value), force);

    /// <summary> OFF にする </summary>
    public bool TurnOff(bool force = false) => Set(0, force);

    /// <summary> ON / OFF を切り替える（段階が 3 以上なら次の段階へ） </summary>
    public bool Toggle(bool force = false) => StateCount == 2 ? Set(IsOn ? 0 : 1, force) : Advance(force);

    /// <summary> 次の段階へ（最後の次は、設定により 0 に戻るか止まる） </summary>
    public bool Advance(bool force = false)
    {
        int next = value + 1;
        if(next >= StateCount) { next = wrap ? 0 : StateCount - 1; }
        return Set(next, force);
    }

    /// <summary> 最初の状態に戻す </summary>
    public bool ResetValue(bool force = true) => Set(InitialValue, force);

    /// <summary> 固定する・固定を外す（セーブする） </summary>
    public void SetLocked(bool value)
    {
        locked = value;
        if(save) { Save.SetPending(saveKey + "/Locked", value); }
    }

    //=====================================================================
    // セーブ用の名前
    //=====================================================================

    public string SaveKey => saveKey ?? MakeSaveKey(gameObject, saveId);

    public static string MakeSaveKey(GameObject target, string id)
    {
        string name = string.IsNullOrEmpty(id) ? ScenePath(target.transform) : id;
        return "Gimmick/" + target.scene.name + "/" + name;
    }

    private static string ScenePath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }
}
