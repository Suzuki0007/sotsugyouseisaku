/*********************************************************************/
// * \file   AutoShade.cs
// * \brief  壁ぎわ・角の床に、薄い影を自動で塗る（Tilemap の部品。塗るのはエディタのボタン）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 隅の影を自動で塗る（壁ぎわ・角を暗く）
/// </summary>
///
/// ・影用の Tilemap に付け、「床の Tilemap」「壁の Tilemap」と影のタイルを入れて、Inspector の「影を塗り直す」を押す
/// ・床のマスのうち、上に壁がある所・左右に壁がある所・壁と同じマス（縁が乗っている所）を薄い影に、角（上と横の両方）を濃い影にする
/// ・マップを塗り直したら、もう一度押す（前の影は消してから塗る。この Tilemap に手で塗った物も消える）
/// ・光が届きにくい隅が暗いと、部屋に奥行きが出る（Docs/マップチップの置き方.md）
[AddComponentMenu("マップの見た目/隅の影")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public class AutoShade : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("影を付ける床（部屋の床・石畳など）の Tilemap。複数入れてよい")]
    public Tilemap[] floors = new Tilemap[0];

    [Tooltip("壁の Tilemap（壁の面・壁の縁など）。複数入れてよい")]
    public Tilemap[] walls = new Tilemap[0];

    [Label("薄い影")]
    [Tooltip("壁ぎわに塗るタイル（パレット Mapchip の「影_薄い」）")]
    public TileBase lightShade;

    [Label("濃い影")]
    [Tooltip("角（上と横の両方が壁）に塗るタイル（パレット Mapchip の「影_濃い」）")]
    public TileBase darkShade;

    [Label("左右の壁ぎわも暗くする")]
    [Tooltip("オフ：上の壁の下だけ暗くする（光が上から来るように見える）。オン：左右の壁ぎわ・縁の乗ったマスも暗くする")]
    public bool shadeSides = true;
}
