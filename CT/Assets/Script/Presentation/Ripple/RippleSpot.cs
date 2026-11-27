/*********************************************************************/
// * \file   RippleSpot.cs
// * \brief  残る波紋（フラグで）：フラグの条件を満たしている間、この場所に波紋を出しておく（ロードしたときは広がった状態から）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 残る波紋（フラグで）：直した場所の波紋を、フラグで出しておく
/// </summary>
///
/// ・条件（フラグ）をすべて満たしている間、この物の位置に波紋を出す。シーンに入ったときに満たしていれば、広がった状態で出る
/// ・ゲームの途中で満たしたら、広がる秒数で広がる。満たさなくなったら消える
/// ・今の「フラグで見た目を切り替える」と同じ使い方（直した → フラグ → 波紋が残る）
[AddComponentMenu("演出/残る波紋（フラグで）")]
public class RippleSpot : MonoBehaviour
{
    [SerializeField]
    private List<FlagCondition> flagConditions = new List<FlagCondition>();

    [SerializeField, Label("波紋")]
    [Tooltip("波紋のプリセット（戻り方が「ずっと残る」の物）")]
    private RipplePreset preset;

    private RippleView view;
    private bool started;

    public RipplePreset Preset => preset;

    private void Start()
    {
        GameFlags.OnChanged += Refresh;
        started = true;
        Apply(true);
    }

    private void OnDestroy()
    {
        GameFlags.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if(started) { Apply(false); }
    }

    private void Apply(bool instant)
    {
        bool met = FlagCondition.AllMet(flagConditions);
        if(met && view == null && preset != null)
        {
            view = RippleSystem.Spawn(preset, transform.position, preset.Seconds, "", false, instant);
        }
        else if(!met && view != null)
        {
            view.FadeOut(1.0f);
            view = null;
        }
    }
}
