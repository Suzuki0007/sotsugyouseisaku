/*********************************************************************/
// * \file   MapchipSeamFix.cs
// * \brief  マップチップの継ぎ目（タイルの間の細い線）の対策：取り込み設定とスプライトアトラスをまとめて設定する
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// マップチップの継ぎ目の対策（Tools > Mapchip > Fix Seams）
/// </summary>
///
/// ・タイルの間に細い線がチラつくのは、隣の絵の色がはみ出す（にじむ）ため。絵は直さず、設定だけで防ぐ
/// ・Assets/Images/Mapchip の画像：Sprite・Point・圧縮なし・ミップなし・Clamp・Full Rect（PPU・分け方・ピボットは変えない）
/// ・スプライトアトラス Assets/Images/Atlas/Mapchip.spriteatlasv2 にフォルダごと入れる（回転なし・詰めない・余白 4・ふちの色で余白を埋める・Point・圧縮なし）
/// ・何度押してもよい（同じ設定にそろえ直す）。新しいチップは Mapchip のフォルダに置いてから押す
public static class MapchipSeamFix
{
    //=====================================================================
    // 定数
    //=====================================================================

    public const string ChipFolder = "Assets/Images/Mapchip";
    public const string AtlasFolder = "Assets/Images/Atlas";
    public const string AtlasPath = AtlasFolder + "/Mapchip.spriteatlasv2";

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Tools/Mapchip/Fix Seams")]
    private static void FixFromMenu()
    {
        string report = Fix();
        Debug.Log(report);
        EditorUtility.DisplayDialog("マップチップの継ぎ目の対策", report, "OK");
    }

    /// <summary>
    /// 画像の取り込み設定とアトラスをまとめて設定し、変えた所の一覧を返す
    /// </summary>
    public static string Fix()
    {
        var sb = new StringBuilder();
        sb.AppendLine("【画像の取り込み設定】");
        FixTextures(sb);
        sb.AppendLine("【Sprite Packer Mode】");
        FixPackerMode(sb);
        sb.AppendLine("【スプライトアトラス】");
        FixAtlas(sb);
        return sb.ToString().TrimEnd();
    }

    //=====================================================================
    // 画像
    //=====================================================================

    private static void FixTextures(StringBuilder sb)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ChipFolder });
        if(guids.Length == 0) { sb.AppendLine("  画像がありません：" + ChipFolder); return; }

        foreach(string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if(imp == null) { continue; }

            var changes = new List<string>();

            if(imp.textureType != TextureImporterType.Sprite) { changes.Add("Texture Type " + imp.textureType + " → Sprite"); imp.textureType = TextureImporterType.Sprite; }
            if(imp.filterMode != FilterMode.Point) { changes.Add("Filter " + imp.filterMode + " → Point"); imp.filterMode = FilterMode.Point; }
            if(imp.textureCompression != TextureImporterCompression.Uncompressed) { changes.Add("圧縮 " + imp.textureCompression + " → なし"); imp.textureCompression = TextureImporterCompression.Uncompressed; }
            if(imp.mipmapEnabled) { changes.Add("Mip Maps オン → オフ"); imp.mipmapEnabled = false; }
            if(imp.wrapMode != TextureWrapMode.Clamp) { changes.Add("Wrap " + imp.wrapMode + " → Clamp"); imp.wrapMode = TextureWrapMode.Clamp; }

            // Mesh Type は TextureImporterSettings にしかない（PPU・分け方・ピボットは触らない）
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            if(s.spriteMeshType != SpriteMeshType.FullRect)
            {
                changes.Add("Mesh Type " + s.spriteMeshType + " → Full Rect");
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
            }

            string name = Path.GetFileNameWithoutExtension(path);
            if(changes.Count == 0) { sb.AppendLine("  " + name + "：変更なし"); continue; }

            imp.SaveAndReimport();
            sb.AppendLine("  " + name + "：" + string.Join("、", changes));
        }
    }

    //=====================================================================
    // Sprite Packer Mode
    //=====================================================================

    private static void FixPackerMode(StringBuilder sb)
    {
        SpritePackerMode mode = EditorSettings.spritePackerMode;
        if(mode == SpritePackerMode.SpriteAtlasV2) { sb.AppendLine("  SpriteAtlasV2（変更なし）"); return; }
        if(mode == SpritePackerMode.Disabled)
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            sb.AppendLine("  Disabled → SpriteAtlasV2");
            return;
        }
        // ビルドのときだけ・古い形式などは、人が選んだ設定なので変えない
        sb.AppendLine("  " + mode + "（無効ではないので変えていません）");
    }

    //=====================================================================
    // アトラス
    //=====================================================================

    private static void FixAtlas(StringBuilder sb)
    {
        if(!AssetDatabase.IsValidFolder(AtlasFolder))
        {
            AssetDatabase.CreateFolder("Assets/Images", "Atlas");
            sb.AppendLine("  フォルダを作成：" + AtlasFolder);
        }

        bool existed = File.Exists(AtlasPath);

        // 中身はフォルダ1つだけ。作り直しても .meta（GUID）は残る
        var atlas = new SpriteAtlasAsset();
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(ChipFolder);
        atlas.Add(new[] { folder });
        SpriteAtlasAsset.Save(atlas, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        sb.AppendLine("  " + (existed ? "作り直し：" : "作成：") + AtlasPath + "（中身：" + ChipFolder + "）");

        var imp = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
        if(imp == null) { sb.AppendLine("  ✖ アトラスの設定を読めませんでした"); return; }

        SpriteAtlasPackingSettings packing = imp.packingSettings;
        packing.enableRotation = false;
        packing.enableTightPacking = false;
        packing.padding = 4;
        packing.enableAlphaDilation = true;   // 余白をふちの色で埋める（にじんでも同じ色になる）
        imp.packingSettings = packing;

        SpriteAtlasTextureSettings tex = imp.textureSettings;
        tex.filterMode = FilterMode.Point;
        tex.generateMipMaps = false;
        imp.textureSettings = tex;

        TextureImporterPlatformSettings platform = imp.GetPlatformSettings("DefaultTexturePlatform");
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SetPlatformSettings(platform);

        imp.SaveAndReimport();
        sb.AppendLine("  回転なし・詰めない・余白 4・余白をふちの色で埋める・Point・ミップなし・圧縮なし");

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        sb.AppendLine("  パック：" + EditorUserBuildSettings.activeBuildTarget);
    }

    //=====================================================================
    // 確かめ（eval・テスト用）
    //=====================================================================

    /// <summary>
    /// 今の設定を1行ずつ返す（画像・アトラス）
    /// </summary>
    public static string Describe()
    {
        var sb = new StringBuilder();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ChipFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if(imp == null) { continue; }
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            sb.AppendLine(Path.GetFileNameWithoutExtension(path) + " type=" + imp.textureType + " ppu=" + imp.spritePixelsPerUnit + " mode=" + imp.spriteImportMode
                + " filter=" + imp.filterMode + " comp=" + imp.textureCompression + " mip=" + imp.mipmapEnabled + " wrap=" + imp.wrapMode + " mesh=" + s.spriteMeshType);
        }
        sb.AppendLine("packer=" + EditorSettings.spritePackerMode);
        var atlasImp = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
        if(atlasImp != null)
        {
            SpriteAtlasPackingSettings p = atlasImp.packingSettings;
            SpriteAtlasTextureSettings t = atlasImp.textureSettings;
            sb.AppendLine("atlas rot=" + p.enableRotation + " tight=" + p.enableTightPacking + " pad=" + p.padding + " dilate=" + p.enableAlphaDilation
                + " filter=" + t.filterMode + " mip=" + t.generateMipMaps + " comp=" + atlasImp.GetPlatformSettings("DefaultTexturePlatform").textureCompression);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if(atlas != null) { sb.AppendLine("atlas sprites=" + atlas.spriteCount); }
        }
        return sb.ToString().TrimEnd();
    }
}
