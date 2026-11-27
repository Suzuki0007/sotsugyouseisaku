/*********************************************************************/
// * \file   GroundTint.cs
// * \brief  地面の色むら：床・砂の上に大きなもやもやを重ね、場所ごとに色と明るさを少し変える（同じタイルの格子を目立たなくする）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 地面の色むら（同じタイルの並びを目立たなくする）
/// </summary>
///
/// ・マップ全体に大きなもやもや（暗い所・明るい所）を薄く重ね、湿った所・日に焼けた所のような色の違いを作る
/// ・タイルごとではなく場所で決まる模様なので、同じタイルが並んでも格子が目立たなくなる
/// ・Hierarchy 右クリック > マップの見た目 > 地面の色むら で、マップの範囲を覆う大きさで作れる
/// ・並び順 2（床より上・壁の面より下）。壁の面・キャラには掛からない
[AddComponentMenu("マップの見た目/地面の色むら")]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class GroundTint : MonoBehaviour
{
    [SerializeField, Label("強さ", 0.0f, 1.0f)]
    [Tooltip("色むらの強さ（0＝見えない）。0.3〜0.5 くらいで、よく見るとわかる程度")]
    private float strength = 0.4f;

    private void OnValidate()
    {
        var sr = GetComponent<SpriteRenderer>();
        if(sr == null) { return; }
        Color c = sr.color;
        c.a = strength;
        sr.color = c;
    }
}
