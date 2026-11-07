/*********************************************************************/
// * \file   SpriteAnimImporter.cs
// * \brief  画像の名前から「動き」と「向き」を判断して SpriteAnimData に入れるエディタ処理
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 画像の名前から動き・向きを判断する
///
/// 例:
///   animation_noah_front2.png (4コマのスプライトシート) → 動き: Walk (名前に動きが無く、コマが複数なので) / 向き: 下
///   Noah-behind.png (1コマ)                             → 動き: Idle / 向き: 上
///   Noah_Walk_Left_00.png, Noah_Walk_Left_01.png …     → 動き: Walk / 向き: 左
///   chest_open_00.png …                                 → 動き: Open / 向きなし
///   torch_00.png …                                      → 動き: Idle / 向きなし (ループ)
///
/// 区切りは _ - スペース ( ) のどれでもよい。単語の後ろの数字 (front2 の 2) は無視する
/// </summary>
public static class SpriteAnimImporter
{
    //=====================================================================
    // 単語の辞書
    //=====================================================================

    private static readonly Dictionary<string, SpriteAnimFacing> DirectionWords = new Dictionary<string, SpriteAnimFacing>
    {
        { "front", SpriteAnimFacing.Down }, { "down", SpriteAnimFacing.Down }, { "forward", SpriteAnimFacing.Down },
        { "正面", SpriteAnimFacing.Down },  { "前", SpriteAnimFacing.Down },   { "下", SpriteAnimFacing.Down },

        { "back", SpriteAnimFacing.Up },    { "behind", SpriteAnimFacing.Up }, { "up", SpriteAnimFacing.Up },
        { "rear", SpriteAnimFacing.Up },    { "背面", SpriteAnimFacing.Up },   { "後ろ", SpriteAnimFacing.Up },
        { "上", SpriteAnimFacing.Up },

        { "left", SpriteAnimFacing.Left },  { "左", SpriteAnimFacing.Left },

        { "right", SpriteAnimFacing.Right }, { "side", SpriteAnimFacing.Right }, { "右", SpriteAnimFacing.Right },
        { "横", SpriteAnimFacing.Right },
    };

    private static readonly Dictionary<string, string> ActionWords = new Dictionary<string, string>
    {
        { "idle", "Idle" },     { "wait", "Idle" },   { "stand", "Idle" },  { "待機", "Idle" },
        { "walk", "Walk" },     { "move", "Walk" },   { "歩き", "Walk" },   { "歩く", "Walk" },
        { "run", "Run" },       { "dash", "Run" },    { "走り", "Run" },
        { "damage", "Damage" }, { "hurt", "Damage" }, { "ダメージ", "Damage" },
        { "dead", "Dead" },     { "death", "Dead" },  { "die", "Dead" },    { "死亡", "Dead" },
        { "attack", "Attack" }, { "shot", "Attack" }, { "攻撃", "Attack" },
        { "open", "Open" },     { "開く", "Open" },
        { "close", "Close" },   { "閉じる", "Close" },
    };

    // 意味を持たない単語 (動きにもキャラ名にもしない)
    private static readonly HashSet<string> IgnoreWords = new HashSet<string>
    {
        "animation", "anim", "motion", "playermotion", "sprite", "sprites", "spr", "sheet", "img", "image", "chara", "character",
    };

    //=====================================================================
    // 判断結果
    //=====================================================================

    /// <summary>
    /// 画像1まとまり (スプライトシート1枚、または連番ファイル1組) の判断結果
    /// </summary>
    public class Group
    {
        public string sourceName;          // 元の名前 (ログ用)
        public string character = "";      // キャラ名らしき単語
        public string action;              // 動きの名前
        public bool actionGuessed;         // 名前に動きが無く、推測したか
        public SpriteAnimFacing? facing;   // 向き (無ければ null)
        public List<Sprite> sprites = new List<Sprite>();
    }

    //=====================================================================
    // 選択したものから判断する
    //=====================================================================

    /// <summary>
    /// 選択した画像 (Texture2D / Sprite) を、まとまりごとに分けて判断する
    /// </summary>
    public static List<Group> Analyze(IEnumerable<Object> objects)
    {
        // まとまりの名前 → 画像
        var buckets = new Dictionary<string, List<Sprite>>();
        var order = new List<string>();

        void Add(string key, IEnumerable<Sprite> sprites)
        {
            if(!buckets.TryGetValue(key, out List<Sprite> list))
            {
                list = new List<Sprite>();
                buckets[key] = list;
                order.Add(key);
            }
            foreach(Sprite s in sprites) { if(s != null && !list.Contains(s)) { list.Add(s); } }
        }

        foreach(Object obj in objects)
        {
            if(obj is Texture2D texture)
            {
                Sprite[] sprites = LoadSprites(texture);
                if(sprites.Length == 0)
                {
                    Debug.LogWarning($"「{texture.name}」はスプライトとして読み込まれていません。Inspector で Texture Type を「Sprite (2D and UI)」にしてください", texture);
                    continue;
                }

                // 切り分けたスプライトシート → 1枚で1つのまとまり
                // 1コマだけの画像 → 名前の末尾の番号を取って、連番ファイルどうしでまとめる
                if(sprites.Length > 1) { Add(texture.name, sprites); }
                else                   { Add(StripTrailingNumber(texture.name), sprites); }
            }
            else if(obj is Sprite sprite)
            {
                Add(StripTrailingNumber(sprite.name), new[] { sprite });
            }
        }

        var result = new List<Group>();
        foreach(string key in order)
        {
            List<Sprite> sprites = buckets[key];
            sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            result.Add(Parse(key, sprites));
        }

        // キャラ名は、いちばん多く出てきた単語にそろえる
        string common = result.Select(g => g.character).Where(c => !string.IsNullOrEmpty(c))
                              .GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
        if(!string.IsNullOrEmpty(common))
        {
            foreach(Group g in result) { g.character = common; }
        }

        return result;
    }

    /// <summary>
    /// 1つのまとまりの名前を読む
    /// </summary>
    public static Group Parse(string groupName, List<Sprite> sprites)
    {
        var group = new Group { sourceName = groupName, sprites = sprites };

        foreach(string raw in Regex.Split(groupName, @"[_\-\s\.\(\)\[\]]+"))
        {
            // 単語の後ろの数字は無視する (front2 → front、192 → 空)
            string word = Regex.Replace(raw, @"\d+$", "").ToLowerInvariant();
            if(string.IsNullOrEmpty(word) || IgnoreWords.Contains(word)) { continue; }

            if(group.facing == null && DirectionWords.TryGetValue(word, out SpriteAnimFacing f)) { group.facing = f; continue; }
            if(group.action == null && ActionWords.TryGetValue(word, out string a)) { group.action = a; continue; }
            if(string.IsNullOrEmpty(group.character)) { group.character = word; }
        }

        if(group.action == null)
        {
            // 名前に動きが無いとき : 向きのあるコマ送りは歩き、それ以外は待機
            group.action = (group.facing != null && sprites.Count > 1) ? "Walk" : "Idle";
            group.actionGuessed = true;
        }

        return group;
    }

    //=====================================================================
    // SpriteAnimData に入れる
    //=====================================================================

    /// <summary>
    /// 判断結果を SpriteAnimData に入れる
    /// 同じ名前の動きがあれば画像だけ入れ替え (速さ・イベントなどは残す)、無ければ追加する
    /// </summary>
    /// <returns> 結果の説明 (ログ用) </returns>
    public static string Apply(SpriteAnimData data, List<Group> groups)
    {
        Undo.RecordObject(data, "画像から動きを追加");

        var log = new StringBuilder();
        List<SpriteAnimClip> clips = data.EditableClips;

        foreach(IGrouping<string, Group> byAction in groups.GroupBy(g => g.action))
        {
            SpriteAnimClip clip = data.Find(byAction.Key);
            bool isNew = clip == null;
            if(isNew)
            {
                clip = new SpriteAnimClip { name = byAction.Key };
                ApplyDefaults(clip);
                clips.Add(clip);
            }

            bool anyDirection = byAction.Any(g => g.facing != null);

            // 向きの無い画像で入れ替えるときは、ほかの向きの画像は消す
            if(isNew || !anyDirection) { clip.down = new Sprite[0]; clip.up = new Sprite[0]; clip.left = new Sprite[0]; clip.right = new Sprite[0]; }

            foreach(Group g in byAction)
            {
                Sprite[] frames = g.sprites.ToArray();
                switch(g.facing ?? SpriteAnimFacing.Down)
                {
                    case SpriteAnimFacing.Down:  clip.down  = frames; break;
                    case SpriteAnimFacing.Up:    clip.up    = frames; break;
                    case SpriteAnimFacing.Left:  clip.left  = frames; break;
                    case SpriteAnimFacing.Right: clip.right = frames; break;
                }

                log.Append($"  {g.sourceName} ({frames.Length}コマ) → {clip.name}");
                if(g.facing != null) { log.Append($" / {FacingLabel(g.facing.Value)}向き"); }
                if(g.actionGuessed)  { log.Append("   ※名前に動きが無いので推測"); }
                log.AppendLine();
            }

            // 入っている画像から、向きの持ち方を決める
            bool hasVertical = SpriteAnimClip.Has(clip.up) || (anyDirection && SpriteAnimClip.Has(clip.down));
            bool hasSide     = SpriteAnimClip.Has(clip.left) || SpriteAnimClip.Has(clip.right);
            if(!anyDirection)     { clip.directionMode = SpriteAnimDirectionMode.Single; }
            else if(!hasVertical) { clip.directionMode = SpriteAnimDirectionMode.Side; }
            else                  { clip.directionMode = hasSide || SpriteAnimClip.Has(clip.up) ? SpriteAnimDirectionMode.FourWay : SpriteAnimDirectionMode.Single; }
        }

        if(string.IsNullOrEmpty(data.EditableDefaultClip) || !data.Has(data.EditableDefaultClip))
        {
            data.EditableDefaultClip = data.Has("Idle") ? "Idle" : (clips.Count > 0 ? clips[0].name : "");
        }

        EditorUtility.SetDirty(data);
        return log.ToString();
    }

    /// <summary>
    /// 動きの名前に合わせて、再生のしかたの初期値を決める
    /// </summary>
    public static void ApplyDefaults(SpriteAnimClip clip)
    {
        switch(clip.name)
        {
            case "Dead":
            case "Open":
            case "Close":
            case "Damage":
            case "Attack":
                clip.playMode = SpriteAnimPlayMode.Once;
                clip.fps = 12.0f;
                break;
            default:
                clip.playMode = SpriteAnimPlayMode.Loop;
                clip.fps = 8.0f;
                break;
        }
    }

    public static string FacingLabel(SpriteAnimFacing f)
    {
        switch(f)
        {
            case SpriteAnimFacing.Up:    return "上";
            case SpriteAnimFacing.Left:  return "左";
            case SpriteAnimFacing.Right: return "右";
            default:                     return "下";
        }
    }

    //=====================================================================
    // 共通
    //=====================================================================

    public static Sprite[] LoadSprites(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        System.Array.Sort(sprites, (a, b) => EditorUtility.NaturalCompare(a.name, b.name));
        return sprites;
    }

    /// <summary>
    /// "hit_01" → "hit"
    /// </summary>
    public static string StripTrailingNumber(string name)
    {
        string s = Regex.Replace(name, @"[_\-\s]*\d+$", "");
        return string.IsNullOrEmpty(s) ? name : s;
    }

    public static string GetFolder(Object obj)
    {
        return Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj)).Replace('\\', '/');
    }
}

/// <summary>
/// Projectウィンドウで画像を選択して右クリック → Create → Animation →
///   SpriteAnimData (選択した画像から作成)
/// 作成した SpriteAnimData は、選んだ画像と同じフォルダに保存される
/// </summary>
public static class SpriteAnimDataCreator
{
    private const string Menu = "Assets/Create/Animation/SpriteAnimData (選択した画像から作成)";

    [MenuItem(Menu, false, 0)]
    private static void Create()
    {
        List<SpriteAnimImporter.Group> groups = SpriteAnimImporter.Analyze(Selection.objects);
        if(groups.Count == 0) { return; }

        Sprite first = groups[0].sprites[0];
        string character = groups[0].character;
        string baseName = string.IsNullOrEmpty(character) ? SpriteAnimImporter.StripTrailingNumber(first.name) : character;
        if(baseName.Length > 0) { baseName = char.ToUpperInvariant(baseName[0]) + baseName.Substring(1); }

        SpriteAnimData data = ScriptableObject.CreateInstance<SpriteAnimData>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{SpriteAnimImporter.GetFolder(first)}/Anim_{baseName}.asset");
        AssetDatabase.CreateAsset(data, path);

        string log = SpriteAnimImporter.Apply(data, groups);
        AssetDatabase.SaveAssets();

        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
        Debug.Log($"【アニメーション】{path} を作成しました\n{log}", data);
    }

    [MenuItem(Menu, true)]
    private static bool Validate() => Selection.objects.Any(o => o is Sprite || o is Texture2D);
}
