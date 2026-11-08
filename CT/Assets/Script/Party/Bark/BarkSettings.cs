/*********************************************************************/
// * \file   BarkSettings.cs
// * \brief  相棒の一言の見た目・出し方の設定（Resources/BarkSettings.asset）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;

/// <summary>
/// 相棒の一言の設定（Resources/BarkSettings.asset。なければ初期値で動く）
/// </summary>
[CreateAssetMenu(menuName = "仕掛け/相棒の一言の設定", fileName = "BarkSettings")]
public class BarkSettings : ScriptableObject
{
    [Header("出し方")]

    [Label("一言を出す")]
    [Tooltip("オフにすると、一言を出さない（イベントの部品からも）")]
    public bool enabled = true;

    [Label("一言どうしの間（秒）", 0.0f)]
    [Tooltip("一言が終わってから、次の一言を自動で出せるまでの秒数（しゃべりすぎないように）。イベント・範囲からの一言は待たない")]
    public float globalCooldown = 4.0f;

    [Label("1文字あたりの秒数", 0.0f)]
    [Tooltip("行の「出している秒数」が 0 のとき：文字数 × これ（下の最短・最長の間）")]
    public float secondsPerChar = 0.09f;

    [Label("最短の秒数", 0.1f)]
    public float minSeconds = 1.6f;

    [Label("最長の秒数", 0.1f)]
    public float maxSeconds = 5.0f;

    [Label("消える時間（秒）", 0.0f)]
    public float fadeSeconds = 0.3f;

    [Header("見た目")]

    [Label("頭の上の高さ")]
    [Tooltip("キャラの位置からどれだけ上に出すか")]
    public float height = 1.1f;

    [Label("文字のフォント")]
    public TMP_FontAsset font;

    [Label("文字の大きさ", 0.1f)]
    public float fontSize = 2.4f;

    [Label("吹き出しの最大の幅", 0.5f)]
    [Tooltip("これより長い文は折り返す")]
    public float maxWidth = 4.0f;

    [Label("吹き出しの余白")]
    public Vector2 padding = new Vector2(0.25f, 0.14f);

    [Label("吹き出しの絵")]
    [Tooltip("大きさに合わせて拡大して使う（Sliced にはしない）")]
    public Sprite background;

    [Label("吹き出しの色")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);

    [Label("文字の色")]
    public Color textColor = Color.white;

    [Label("ノアの名前")]
    public string noahName = "ノア";

    [Label("ノアの色")]
    [Tooltip("吹き出しの縁（下の線）と、画面の外にいるときの名前の色")]
    public Color noahColor = new Color(1.0f, 0.55f, 0.75f, 1.0f);

    [Label("シグルの名前")]
    public string sigleName = "シグル";

    [Label("シグルの色")]
    public Color sigleColor = new Color(0.45f, 0.7f, 1.0f, 1.0f);

    [Label("描く順番")]
    public int sortingOrder = 210;

    [Header("画面の外にいるとき")]

    [Label("画面の下に出す")]
    [Tooltip("オン：話すキャラが画面の外にいるとき（離れ離れのとき）、画面の下に名前つきで出す（通信のように）\nオフ：出さない")]
    public bool showOffscreen = true;

    [Label("画面の下からの位置", 0.0f, 0.5f)]
    [Tooltip("画面の高さを 1 として、下からどこに出すか")]
    public float offscreenY = 0.14f;
}
