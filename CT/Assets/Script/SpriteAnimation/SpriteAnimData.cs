/*********************************************************************/
// * \file   SpriteAnimData.cs
// * \brief  キャラクター・オブジェクト1体分のスプライトアニメーション設定
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 向きの持ち方
/// </summary>
public enum SpriteAnimDirectionMode
{
    [InspectorName("向きなし (1種類だけ)")]
    Single,

    [InspectorName("上下左右 (左右は片方だけでも反転して使う)")]
    FourWay,

    [InspectorName("左右だけ (片方だけでも反転して使う)")]
    Side,
}

/// <summary>
/// 再生のしかた
/// </summary>
public enum SpriteAnimPlayMode
{
    [InspectorName("ループ (繰り返す)")]
    Loop,

    [InspectorName("1回 (最後のコマで止まる)")]
    Once,

    [InspectorName("1回 → 次の動きへ")]
    OnceThenNext,
}

/// <summary>
/// キャラクターの向き
/// </summary>
public enum SpriteAnimFacing
{
    [InspectorName("下 (正面)")] Down,
    [InspectorName("上 (背中)")] Up,
    [InspectorName("左")]        Left,
    [InspectorName("右")]        Right,
}

/// <summary>
/// 決まったコマで鳴らす音・出すエフェクト
/// </summary>
[System.Serializable]
public class SpriteAnimFrameEvent
{
    [Tooltip("何コマ目で (1 = 最初のコマ)")]
    [Min(1)]
    public int frame = 1;

    [Tooltip("鳴らすSE (任意)")]
    public SeData se;

    [Tooltip("出すエフェクト (任意)")]
    public EffectData effect;

    [Tooltip("エフェクトを出す位置のずらし")]
    public Vector2 effectOffset = Vector2.zero;

    [Tooltip("エフェクトをキャラクターにくっつけて動かす")]
    public bool effectFollow = false;

    [Tooltip("同じオブジェクトの EffectTrigger の行の名前 (任意)。その行のエフェクトを出す")]
    public string effectTriggerName = "";

    [Tooltip("プログラム向けの合図 (任意)。SpriteAnimator.OnMessage で受け取れる")]
    public string message = "";
}

/// <summary>
/// 動き1つ分 (Idle・Walk など)
/// </summary>
[System.Serializable]
public class SpriteAnimClip
{
    [Tooltip("動きの名前 (Idle / Walk / Damage / Dead / Open など)")]
    public string name = "Idle";

    [Tooltip("向きの持ち方")]
    public SpriteAnimDirectionMode directionMode = SpriteAnimDirectionMode.Single;

    [Tooltip("下向き (正面) の画像。「向きなし」のときはここだけ使う")]
    public Sprite[] down = new Sprite[0];

    [Tooltip("上向き (背中) の画像")]
    public Sprite[] up = new Sprite[0];

    [Tooltip("左向きの画像 (空なら右を反転して使う)")]
    public Sprite[] left = new Sprite[0];

    [Tooltip("右向きの画像 (空なら左を反転して使う)")]
    public Sprite[] right = new Sprite[0];

    [Tooltip("1秒に何コマ進めるか (大きいほど速い)")]
    [Min(0.1f)]
    public float fps = 8.0f;

    [Tooltip("再生のしかた")]
    public SpriteAnimPlayMode playMode = SpriteAnimPlayMode.Loop;

    [Tooltip("「1回 → 次の動きへ」のとき、次に流す動きの名前")]
    public string nextClip = "";

    [Tooltip("コマごとの長さの倍率 (空ならすべて 1。2 にするとそのコマだけ2倍長く表示)")]
    public List<float> frameLengths = new List<float>();

    [Tooltip("決まったコマで鳴らす音・出すエフェクト")]
    public List<SpriteAnimFrameEvent> events = new List<SpriteAnimFrameEvent>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool IsLoop => playMode == SpriteAnimPlayMode.Loop;

    public bool HasAnyFrames => Has(down) || Has(up) || Has(left) || Has(right);

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 向きに合った画像の並びを返す (無い向きは、反転やほかの向きで代用する)
    /// </summary>
    /// <param name="facing"> 今の向き </param>
    /// <param name="lastHorizontalLeft"> 最後に向いた左右が左か (「左右だけ」で上下を向いたときに使う) </param>
    /// <param name="flip"> 左右反転して表示するか </param>
    public Sprite[] GetFrames(SpriteAnimFacing facing, bool lastHorizontalLeft, out bool flip)
    {
        flip = false;

        switch(directionMode)
        {
            case SpriteAnimDirectionMode.Single:
                return FirstOf(down, up, right, left);

            case SpriteAnimDirectionMode.Side:
            {
                bool wantLeft = facing == SpriteAnimFacing.Left ||
                                (facing != SpriteAnimFacing.Right && lastHorizontalLeft);
                return Horizontal(wantLeft, out flip) ?? FirstOf(down, up, right, left);
            }

            default:// FourWay
            {
                switch(facing)
                {
                    case SpriteAnimFacing.Up:    if(Has(up))   { return up; }   break;
                    case SpriteAnimFacing.Down:  if(Has(down)) { return down; } break;
                    case SpriteAnimFacing.Left:  { Sprite[] h = Horizontal(true, out flip);  if(h != null) { return h; } break; }
                    case SpriteAnimFacing.Right: { Sprite[] h = Horizontal(false, out flip); if(h != null) { return h; } break; }
                }
                flip = false;
                return FirstOf(down, up, right, left);
            }
        }
    }

    /// <summary>
    /// 1周の秒数 (向きによってコマ数が違う場合は、その向きの長さ)
    /// </summary>
    public float CycleTime(Sprite[] frames)
    {
        return SpriteFrameTiming.CycleTime(frames != null ? frames.Length : 0, fps, frameLengths);
    }

    /// <summary>
    /// 使っている画像をすべて返す (エディタのチェック用)
    /// </summary>
    public IEnumerable<Sprite> AllSprites()
    {
        foreach(Sprite[] list in new[] { down, up, left, right })
        {
            if(list == null) { continue; }
            foreach(Sprite s in list) { if(s != null) { yield return s; } }
        }
    }

    private Sprite[] Horizontal(bool wantLeft, out bool flip)
    {
        flip = false;
        if(wantLeft)
        {
            if(Has(left))  { return left; }
            if(Has(right)) { flip = true; return right; }
        }
        else
        {
            if(Has(right)) { return right; }
            if(Has(left))  { flip = true; return left; }
        }
        return null;
    }

    private static Sprite[] FirstOf(params Sprite[][] lists)
    {
        foreach(Sprite[] l in lists) { if(Has(l)) { return l; } }
        return null;
    }

    public static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
}

/// <summary>
/// キャラクター・オブジェクト1体分のアニメーション設定 (ScriptableObject)
///
/// 作り方:
///   ・連番画像 / スプライトシートをまとめて選択して右クリック → Create → Animation → SpriteAnimData (選択した画像から作成)
///   ・または 右クリック → Create → Animation → SpriteAnimData (空) で作って、Inspector に画像をドラッグする
///
/// 使い方:
///   ・動かしたいオブジェクト (SpriteRenderer があるもの) に SpriteAnimator を付けて、このデータを指定する
///   ・キャラクターは CharacterAnimLink も付けると、移動に合わせて Idle / Walk が自動で切り替わる
/// </summary>
[CreateAssetMenu(menuName = "Animation/SpriteAnimData (空)", fileName = "Anim_New", order = 20)]
public class SpriteAnimData : ScriptableObject
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("最初に流す動き (空なら一番上の動き)")]
    [SerializeField]
    private string defaultClip = "";

    [Tooltip("動きの一覧")]
    [SerializeField]
    private List<SpriteAnimClip> clips = new List<SpriteAnimClip>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public IReadOnlyList<SpriteAnimClip> Clips => clips;

    public string DefaultClipName => defaultClip;

    /// <summary>
    /// 最初に流す動き
    /// </summary>
    public SpriteAnimClip DefaultClip
    {
        get
        {
            SpriteAnimClip c = Find(defaultClip);
            if(c != null) { return c; }
            return clips.Count > 0 ? clips[0] : null;
        }
    }

#if UNITY_EDITOR
    /// <summary> エディタ拡張から編集する用 </summary>
    public List<SpriteAnimClip> EditableClips => clips;

    /// <summary> エディタ拡張から編集する用 </summary>
    public string EditableDefaultClip { get => defaultClip; set => defaultClip = value; }
#endif

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 名前で動きを探す (大文字・小文字は区別しない)。無ければ null
    /// </summary>
    public SpriteAnimClip Find(string clipName)
    {
        if(string.IsNullOrEmpty(clipName)) { return null; }

        for(int i = 0; i < clips.Count; i++)
        {
            if(clips[i] != null && string.Equals(clips[i].name, clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                return clips[i];
            }
        }
        return null;
    }

    public bool Has(string clipName) => Find(clipName) != null;
}
