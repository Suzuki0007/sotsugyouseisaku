/*********************************************************************/
// * \file   WalkAreaExclude.cs
// * \brief  歩ける範囲（WalkArea）の子に付けると、その形を「歩けない形」にする印
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 歩けない形（WalkArea の子の Collider2D に付ける印）
/// </summary>
///
/// ・柱・穴など、歩ける範囲の中で歩けない所に使う。形は同じオブジェクトの Circle / Box / Polygon Collider2D
/// ・プレイヤー・敵・相棒の道のどれも、ここには入らない
/// ・WalkArea の ⋮ メニュー「歩けない形を追加（円）」「歩けない形を追加（四角）」で作れる
[AddComponentMenu("仕掛け/歩けない形（歩ける範囲の子）")]
public class WalkAreaExclude : MonoBehaviour
{
}
