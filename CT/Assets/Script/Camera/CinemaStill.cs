/*********************************************************************/
// * \file   CinemaStill.cs
// * \brief  一枚絵（カットシーンのスチル）が出ている間、映画風の上下の帯を最初から出しておく
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 一枚絵の帯（背景が一枚絵の場面で、上下の黒い帯を最初から出しておく）
/// </summary>
///
/// ・塔の全景スチルのような、画面全体を覆う一枚絵の物に付ける。絵が表示されたとたん、帯が動かずに出ている状態になる
/// ・絵が非表示になったら、会話・イベントが終わるのを待ってから帯が引っ込む
/// ・帯の太さは会話の帯（DialogueManager の「帯の太さ」）とそろえる
[DisallowMultipleComponent]
public class CinemaStill : MonoBehaviour
{
    [SerializeField, Label("帯の太さ", 0.05f, 0.3f)]
    [Tooltip("上下それぞれの帯の太さ（マップの映る範囲の高さに対する割合）。会話の帯と同じ 0.12 にしておくと、会話が始まっても帯が動かない")]
    private float barSize = 0.12f;

    [SerializeField, Label("帯が引っ込む時間（秒）", 0.0f)]
    [Tooltip("絵が消えて、会話・イベントも終わったあとに帯が引っ込む時間")]
    private float releaseTime = 1.2f;

    private CinemaMode holding;

    private void OnEnable()
    {
        holding = CinemaMode.Get();
        if(holding != null) { holding.HoldForStill(barSize); }
    }

    private void OnDisable()
    {
        if(holding != null) { holding.ReleaseStill(releaseTime); }
        holding = null;
    }
}
