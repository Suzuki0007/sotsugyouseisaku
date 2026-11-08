/*********************************************************************/
// * \file   PartyHint.cs
// * \brief  「ここは相棒の出番」の目印（使えないキャラで近づいたときに、使えるキャラの名前を出す）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 相棒の目印
/// </summary>
///
/// ・使えるキャラが決まっている仕掛け（ノアだけのスイッチ・シグルだけの箱・狭い通路・壊せる壁など）に、
///   使えないキャラで近づくと、仕掛けの上に「シグル」「ノア」の札を出して、切り替えを促す
/// ・仕掛けは毎フレーム Request を呼ぶ。呼ばれなかった札は消える
public static class PartyHint
{
    private static PartyHintSettings settings;
    private static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { settings = null; loaded = false; }

    public static PartyHintSettings Settings
    {
        get
        {
            if(!loaded) { settings = Resources.Load<PartyHintSettings>("PartyHints"); loaded = true; }
            return settings;
        }
    }

    /// <summary>
    /// 目印を出す（毎フレーム呼ぶ）。使えるキャラが「両方」なら何もしない
    /// </summary>
    /// <param name="target"> 仕掛け </param>
    /// <param name="allowed"> 使えるキャラ </param>
    /// <param name="playerPosition"> 操作しているキャラの位置（近いかを見る） </param>
    public static void Request(Transform target, CharacterRestriction allowed, Vector2 playerPosition)
    {
        if(target == null || allowed == CharacterRestriction.Both) { return; }

        PartyHintSettings s = Settings;
        if(s == null || !s.enabled) { return; }
        if(Vector2.Distance(target.position, playerPosition) > s.showDistance + 1.0f) { return; }

        PartyHintView.Get().Request(target, allowed, s);
    }
}
