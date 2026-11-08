/*********************************************************************/
// * \file   CharacterRestriction.cs
// * \brief  仕掛けを使えるキャラ（両方・ノアだけ・シグルだけ）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 仕掛けを使えるキャラ
/// </summary>
public enum CharacterRestriction
{
    [InspectorName("両方")]
    Both = 0,

    [InspectorName("ノアだけ")]
    NoahOnly = 1,

    [InspectorName("シグルだけ")]
    SigleOnly = 2,
}

/// <summary>
/// 使えるキャラの判定
/// </summary>
public static class CharacterRestrictions
{
    /// <summary> そのキャラが使えるか </summary>
    public static bool Allows(this CharacterRestriction restriction, PlayerCharacter character)
    {
        switch(restriction)
        {
            case CharacterRestriction.NoahOnly:  return character == PlayerCharacter.Noah;
            case CharacterRestriction.SigleOnly: return character == PlayerCharacter.Sigle;
            default:                             return true;
        }
    }

    /// <summary> そのオブジェクト（ノア・シグル）が使えるか </summary>
    public static bool Allows(this CharacterRestriction restriction, GameObject character)
    {
        return restriction.Allows(PlayerCharacters.Of(character));
    }

    /// <summary> 使えるキャラの名前（「シグル」など。両方なら空） </summary>
    public static string AllowedName(this CharacterRestriction restriction)
    {
        switch(restriction)
        {
            case CharacterRestriction.NoahOnly:  return "ノア";
            case CharacterRestriction.SigleOnly: return "シグル";
            default:                             return "";
        }
    }
}
