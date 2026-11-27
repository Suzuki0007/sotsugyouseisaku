/*********************************************************************/
// * \file   GesturePreset.cs
// * \brief  手触りのプリセット：手の動作の入力（ゆっくり回す・長押し・繰り返し）の型・キー・速さ・重さ・離したときの動き・見た目
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 手触りのプリセット（ゆっくり回す・長押し・繰り返しの手触り）
/// </summary>
///
/// ・部品「手の動作を待つ」で選ぶ。失敗・やり直し・時間切れはない
/// ・ゆっくり回す：押している間少しずつ進む。「重さ」で進むほど重くできる。離すと止まる（戻らない）
/// ・長押し：押している間、一定の速さで進む。離すと止まる／ゆっくり戻る
/// ・繰り返し：押すたびに 1 段進む。次を押せるまでの間を決められる（速く押しても速くならない）
/// ・「押すたびに進む」をオンにすると、回す・長押しも、押すたびに決まった量だけ進む（長押しが苦手な人向け）
/// ・置き場所は Assets/Resources/Presentation/Gesture/
[CreateAssetMenu(menuName = "CT/演出/手触りのプリセット", fileName = "Gesture_")]
public class GesturePreset : ScriptableObject
{
    public enum Kind
    {
        [InspectorName("ゆっくり回す")]
        Rotate = 0,

        [InspectorName("長押し")]
        Hold = 1,

        [InspectorName("繰り返し（1 回ずつ押す）")]
        Repeat = 2,
    }

    public enum KeyChoice
    {
        [InspectorName("E（調べると同じ）")]
        E = 0,

        [InspectorName("Space")]
        Space = 1,

        [InspectorName("E・Space・Enter のどれでも")]
        Any = 2,
    }

    public enum Release
    {
        [InspectorName("止まる（戻らない）")]
        Stop = 0,

        [InspectorName("ゆっくり戻る")]
        Return = 1,
    }

    public enum Look
    {
        [InspectorName("なし（画面の中の物で見せる）")]
        None = 0,

        [InspectorName("輪")]
        Ring = 1,
    }

    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [SerializeField, Label("型")]
    private Kind kind = Kind.Hold;

    [SerializeField, Label("使うキー")]
    [Tooltip("既定は E（調べると同じキー）。会話の送りとぶつからないように、ふつうは E")]
    private KeyChoice key = KeyChoice.E;

    [Header("進み方")]

    [SerializeField, Label("終わるまでの秒数", 0.1f)]
    [Tooltip("回す・長押し：押し続けたとき、重さがないなら何秒で終わるか")]
    private float seconds = 3.0f;

    [SerializeField]
    [Tooltip("重さ：横＝進み具合（0〜1）、縦＝進む速さの倍率（1＝ふつう、0.2＝とても重い）。最後の 10% だけ重い・近づくほど軽い、などを形で決める")]
    private AnimationCurve weight = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);

    [SerializeField, Label("離したとき")]
    private Release release = Release.Stop;

    [SerializeField, Label("戻る速さ（秒）", 0.1f)]
    [Tooltip("「ゆっくり戻る」のとき、0 まで戻るのにかかる秒数")]
    private float returnSeconds = 4.0f;

    [Header("繰り返し")]

    [SerializeField, Label("回数", 1.0f)]
    [Tooltip("繰り返し：この回数押すと終わる")]
    private int count = 5;

    [SerializeField, Label("次を押せるまでの間（秒）", 0.0f)]
    [Tooltip("繰り返し：1 回押したあと、次の 1 回を受けるまでの間（墓を掘る手応え。速く押しても速くならない）")]
    private float repeatInterval = 0.6f;

    [Header("押すたびに進む（長押しが苦手な人向け）")]

    [SerializeField, Label("押すたびに進む")]
    [Tooltip("オンにすると、回す・長押しも、押すたびに下の量だけ進む")]
    private bool tapMode = false;

    [SerializeField, Label("1 回で進む量", 0.01f, 1.0f)]
    private float tapStep = 0.1f;

    [Header("見た目・手応え")]

    [SerializeField, Label("進み具合の見た目")]
    [Tooltip("なるべく画面の中の物（時計の針・伸びる手・掘られる土）で見せ、輪は必要な場面だけ")]
    private Look look = Look.None;

    [SerializeField, Label("案内の文")]
    [Tooltip("押し方の案内（最初の数秒と、止まっているときに出す）。空なら型に合わせた文")]
    private string prompt = "";

    [SerializeField, Label("震えの強さ", 0.0f, 0.3f)]
    [Tooltip("押している間・押した瞬間に、部品の「震わせる物」を揺らす強さ（マス）。音に頼らない手応え")]
    private float shake = 0.04f;

    [SerializeField, Label("押している間、まわりを止める")]
    [Tooltip("押し始めると、まわりの動き（時間）と音を止める（最後の一音）。終わると戻る")]
    private bool freezeWorld = false;

    //=====================================================================
    // 公開
    //=====================================================================

    public string Memo => memo;
    public Kind Type => kind;
    public float Seconds => Mathf.Max(0.1f, seconds);
    public Release ReleaseMode => release;
    public float ReturnSeconds => Mathf.Max(0.1f, returnSeconds);
    public int Count => Mathf.Max(1, count);
    public float RepeatInterval => repeatInterval;
    public bool TapMode => tapMode;
    public float TapStep => tapStep;
    public Look ProgressLook => look;
    public float Shake => shake;
    public bool FreezeWorld => freezeWorld;

    /// <summary> 進み具合での速さの倍率 </summary>
    public float WeightAt(float progress) => weight != null && weight.length > 0 ? Mathf.Max(0.0f, weight.Evaluate(progress)) : 1.0f;

    /// <summary> 案内の文 </summary>
    public string PromptText
    {
        get
        {
            if(!string.IsNullOrEmpty(prompt)) { return prompt; }
            string k = key == KeyChoice.Space ? "Space" : "E";
            if(tapMode && kind != Kind.Repeat) { return "[" + k + "] 押すたびに進む"; }
            switch(kind)
            {
                case Kind.Repeat: return "[" + k + "] 1 回ずつ押す";
                case Kind.Rotate: return "[" + k + "] ゆっくり押し続ける";
                default:          return "[" + k + "] 押し続ける";
            }
        }
    }

    /// <summary> 今押されているか </summary>
    public bool IsHeld(Keyboard kb)
    {
        if(kb == null) { return false; }
        switch(key)
        {
            case KeyChoice.Space: return kb.spaceKey.isPressed;
            case KeyChoice.Any:   return kb.eKey.isPressed || kb.spaceKey.isPressed || kb.enterKey.isPressed;
            default:              return kb.eKey.isPressed;
        }
    }

    /// <summary> このフレームで押されたか </summary>
    public bool WasPressed(Keyboard kb)
    {
        if(kb == null) { return false; }
        switch(key)
        {
            case KeyChoice.Space: return kb.spaceKey.wasPressedThisFrame;
            case KeyChoice.Any:   return kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
            default:              return kb.eKey.wasPressedThisFrame;
        }
    }

#if UNITY_EDITOR
    public void EditorSet(string memoText, Kind k, float secs, AnimationCurve w, Release r, float retSecs, int cnt, float interval, Look lk, string promptText, float shk, bool freeze)
    {
        memo = memoText; kind = k; seconds = secs; weight = w; release = r; returnSeconds = retSecs; count = cnt; repeatInterval = interval;
        look = lk; prompt = promptText; shake = shk; freezeWorld = freeze;
    }
#endif
}
