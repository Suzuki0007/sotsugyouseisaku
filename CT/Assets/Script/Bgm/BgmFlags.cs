/*********************************************************************/
// * \file   BgmFlags.cs
// * \brief  BGM切り替え用のフラグ
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;

/// <summary>
/// BGMを切り替えるための名前付きフラグ
/// どこからでも BgmFlags.Set("Boss", true) のように設定できる
/// </summary>
public static class BgmFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void Set(string name, bool value)
    {
        if(string.IsNullOrEmpty(name)) { return; }

        if(value) { flags.Add(name); }
        else      { flags.Remove(name); }
    }

    public static bool Get(string name)
    {
        return !string.IsNullOrEmpty(name) && flags.Contains(name);
    }

    public static void ClearAll()
    {
        flags.Clear();
    }

    // 再生開始時にフラグを空にする
    // エディタでドメインリロードを無効にしていると、前回の再生のフラグが残るため
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        flags.Clear();
    }
}
