/*********************************************************************/
// * \file   SceneFootstep.cs
// * \brief  足音の設定（このシーン・この範囲）：外のマップでは砂の足音にする、など
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 足音の設定（このシーン・この範囲）
/// </summary>
///
/// ・シーンに置くと、ノア・シグルの足音（プレハブの FootstepSe）をこのシーンだけ替える（外のマップ＝砂の足音、など）
/// ・Is Trigger の Collider2D を付けると「その範囲の中にいる間だけ」になる（同じシーンの中で外と建物の中を分けるとき）
/// ・範囲つきの物が、範囲なし（シーン全体）の物より先。どれもなければプレハブの足音のまま
/// ・欄が空のキャラ・ついてくるときは、プレハブの足音を使う
[AddComponentMenu("音/足音の設定（このシーン・範囲）")]
public class SceneFootstep : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("鳴らし方")]
    [Tooltip("一歩ずつ：1歩分の短い音を、決めた距離ごとに鳴らす\n歩いている間ループ：歩き続ける音（長い録音）を、歩いている間だけループで鳴らす（SeData の「ループ」をオンに）")]
    private FootstepSe.PlayMode playMode = FootstepSe.PlayMode.LoopWhileWalking;

    [SerializeField, Label("ノアの足音")]
    [Tooltip("ノアを操作しているときの足音（空ならプレハブの足音）")]
    private SeData noahSe;

    [SerializeField, Label("ノアの足音（ついてくるとき）")]
    [Tooltip("ノアがついてきているときの足音。操作中と同じ音を小さい音量で作ると良い（空ならプレハブの足音）")]
    private SeData noahFollowSe;

    [SerializeField, Label("シグルの足音")]
    [Tooltip("シグルを操作しているときの足音（空ならプレハブの足音）")]
    private SeData sigleSe;

    [SerializeField, Label("シグルの足音（ついてくるとき）")]
    [Tooltip("シグルがついてきているときの足音（空ならプレハブの足音）")]
    private SeData sigleFollowSe;

    [SerializeField, TextArea(1, 3)]
    [Tooltip("メモ（ゲームには出ない）")]
    private string memo = "";

    private Collider2D area;

    private static readonly List<SceneFootstep> active = new List<SceneFootstep>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active.Clear(); }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        area = GetComponent<Collider2D>();
        if(area != null && !area.isTrigger) { area = null; }
    }

    private void OnEnable()  { if(!active.Contains(this)) { active.Add(this); } }
    private void OnDisable() { active.Remove(this); }

    //=====================================================================
    // 外から呼ぶ関数
    //=====================================================================

    public FootstepSe.PlayMode Mode => playMode;

    /// <summary> 範囲つきか（Is Trigger の Collider2D がある） </summary>
    public bool HasArea => area != null;

    /// <summary> その位置に効く設定（範囲つきの中 → シーン全体。なければ null） </summary>
    public static SceneFootstep At(Vector2 position)
    {
        SceneFootstep whole = null;
        foreach(SceneFootstep s in active)
        {
            if(s == null) { continue; }
            if(s.area != null)
            {
                if(s.area.OverlapPoint(position)) { return s; }
            }
            else if(whole == null) { whole = s; }
        }
        return whole;
    }

    /// <summary> そのキャラの足音（空ならプレハブの足音 fallback） </summary>
    public SeData SeFor(PlayerCharacter who, bool controlled, SeData fallback)
    {
        SeData se = null;
        if(who == PlayerCharacter.Noah)       { se = controlled ? noahSe : noahFollowSe; }
        else if(who == PlayerCharacter.Sigle) { se = controlled ? sigleSe : sigleFollowSe; }
        return se != null ? se : fallback;
    }
}
