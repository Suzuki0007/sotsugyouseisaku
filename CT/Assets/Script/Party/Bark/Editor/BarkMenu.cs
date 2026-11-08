/*********************************************************************/
// * \file   BarkMenu.cs
// * \brief  相棒の一言のメニュー（一言の範囲を置く・設定を作る・データを作る）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 相棒の一言のメニュー
/// </summary>
public static class BarkMenu
{
    public const string SettingsPath = "Assets/Resources/BarkSettings.asset";
    public const string DataFolder = "Assets/Resources/Barks";

    [MenuItem("GameObject/仕掛け/2人/一言の範囲（入ると相棒がしゃべる）", false, 52)]
    private static void PlaceArea(MenuCommand command)
    {
        if(!GimmickMenu.FirstCallThisClick()) { return; }
        EnsureSettings();

        var go = new GameObject("一言の範囲");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.0f, 3.0f);
        go.AddComponent<BarkArea>();
        GimmickMenu.Place(go, command, "一言の範囲");
    }

    [MenuItem("Tools/仕掛け/相棒の一言/新しい一言を作る", false, 320)]
    private static void CreateDataMenu()
    {
        EnsureSettings();
        BarkData d = CreateData("Bark_新しい一言", BarkTrigger.Manual);
        Selection.activeObject = d;
        EditorGUIUtility.PingObject(d);
    }

    [MenuItem("Tools/仕掛け/相棒の一言/一言の設定を作る", false, 321)]
    private static void CreateSettingsMenu()
    {
        BarkSettings s = EnsureSettings();
        Selection.activeObject = s;
        EditorGUIUtility.PingObject(s);
    }

    /// <summary>
    /// 一言のデータを Resources/Barks/ に作る（同じ名前があれば番号をつける）
    /// </summary>
    public static BarkData CreateData(string name, BarkTrigger trigger)
    {
        System.IO.Directory.CreateDirectory(DataFolder);
        AssetDatabase.Refresh();

        var d = ScriptableObject.CreateInstance<BarkData>();
        d.trigger = trigger;
        var v = new BarkVariant();
        v.lines.Add(new BarkLine { speaker = BarkSpeaker.Partner, text = "" });
        d.variants.Add(v);

        string path = AssetDatabase.GenerateUniqueAssetPath(DataFolder + "/" + name + ".asset");
        AssetDatabase.CreateAsset(d, path);
        AssetDatabase.SaveAssets();
        return d;
    }

    /// <summary>
    /// 一言の設定（Resources/BarkSettings.asset）がなければ作る
    /// </summary>
    public static BarkSettings EnsureSettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<BarkSettings>(SettingsPath);
        if(s != null) { return s; }

        s = ScriptableObject.CreateInstance<BarkSettings>();
        s.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        s.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
        System.IO.Directory.CreateDirectory("Assets/Resources");
        System.IO.Directory.CreateDirectory(DataFolder);
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return s;
    }
}
