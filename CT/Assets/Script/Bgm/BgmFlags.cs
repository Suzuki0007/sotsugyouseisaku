/*********************************************************************/
// * \file   BgmFlags.cs
// * \brief  BGM切り替え用のフラグ
// *
// * \author
/*********************************************************************/

/// <summary>
/// BGMを切り替えるための名前付きフラグ
/// どこからでも BgmFlags.Set("Boss", true) のように設定できる
/// </summary>
///
/// 中身は GameFlags の「セーブしないフラグ」と同じ。
/// 「セーブしない」GameFlag の ID を同じ名前にすると、アセットからも同じフラグとして使える
public static class BgmFlags
{
    public static void Set(string name, bool value)
    {
        GameFlags.SetTemporary(name, value);
    }

    public static bool Get(string name)
    {
        return GameFlags.GetTemporary(name);
    }

    public static void ClearAll()
    {
        GameFlags.ClearTemporary();
    }
}
