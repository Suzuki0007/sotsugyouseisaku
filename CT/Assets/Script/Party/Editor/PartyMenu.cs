/*********************************************************************/
// * \file   PartyMenu.cs
// * \brief  Hierarchy 右クリック > 仕掛け > 2人 > …（狭い通路・壊せる壁・見える物・持ち上げる場所）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2人の仕掛けを置くメニュー
/// </summary>
public static class PartyMenu
{
    public const string HintSettingsPath = "Assets/Resources/PartyHints.asset";

    //=====================================================================
    // 狭い通路
    //=====================================================================

    [MenuItem("GameObject/仕掛け/2人/狭い通路（ノアだけ通れる）", false, 40)]
    private static void PlacePassage(MenuCommand command)
    {
        EnsureHintSettings();

        var go = new GameObject("狭い通路");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(1.0f, 2.0f), new Color(0.3f, 0.3f, 0.45f, 0.6f), 2);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 2.0f);
        go.AddComponent<CharacterBarrier>();

        GimmickMenu.Place(go, command, "狭い通路");
    }

    //=====================================================================
    // 壊せる壁
    //=====================================================================

    [MenuItem("GameObject/仕掛け/2人/壊せる壁（シグルだけ壊せる）", false, 41)]
    private static void PlaceBreakable(MenuCommand command)
    {
        EnsureHintSettings();

        var go = new GameObject("壊せる壁");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(1.0f, 1.0f), new Color(0.6f, 0.55f, 0.5f, 1.0f), 4);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 1.0f);
        go.AddComponent<GimmickState>();
        go.AddComponent<BreakableWall>();

        GimmickMenu.Place(go, command, "壊せる壁");
        GimmickMenu.SnapToGrid(go);
    }

    //=====================================================================
    // 条件で見える物
    //=====================================================================

    [MenuItem("GameObject/仕掛け/2人/条件で見える物（ノアにだけ見える）", false, 42)]
    private static void PlaceVisible(MenuCommand command)
    {
        var go = new GameObject("ノアにだけ見える物");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(1.0f, 1.0f), new Color(0.7f, 0.5f, 1.0f, 0.8f), 3);
        go.AddComponent<ConditionalVisible>();

        GimmickMenu.Place(go, command, "条件で見える物");
    }

    //=====================================================================
    // 持ち上げる場所
    //=====================================================================

    [MenuItem("GameObject/仕掛け/2人/持ち上げる場所（踏み台と行き先）", false, 43)]
    private static void PlaceLift(MenuCommand command)
    {
        var root = new GameObject("持ち上げる場所");

        // 踏み台（下）：2人そろって調べると、ノアが上へ
        var up = new GameObject("踏み台（上がる）");
        up.transform.SetParent(root.transform, false);
        GimmickMenu.Look(up, GimmickMenu.Knob, new Vector2(0.5f, 0.5f), new Color(0.4f, 1.0f, 0.6f, 0.9f), 3);
        var upArea = up.AddComponent<CircleCollider2D>();
        upArea.isTrigger = true;
        upArea.radius = 1.0f;
        var upLift = up.AddComponent<LiftPoint>();

        // 行き先（上）と、降りる場所
        var top = new GameObject("行き先（上）");
        top.transform.SetParent(root.transform, false);
        top.transform.localPosition = new Vector3(0.0f, 3.0f, 0.0f);

        var down = new GameObject("降りる場所");
        down.transform.SetParent(root.transform, false);
        down.transform.localPosition = new Vector3(0.0f, 3.0f, 0.0f);
        GimmickMenu.Look(down, GimmickMenu.Knob, new Vector2(0.5f, 0.5f), new Color(0.4f, 0.8f, 1.0f, 0.9f), 3);
        var downArea = down.AddComponent<CircleCollider2D>();
        downArea.isTrigger = true;
        downArea.radius = 1.0f;
        var downLift = down.AddComponent<LiftPoint>();

        var bottom = new GameObject("行き先（下）");
        bottom.transform.SetParent(root.transform, false);
        bottom.transform.localPosition = new Vector3(0.0f, -0.8f, 0.0f);

        var so = new SerializedObject(upLift);
        so.FindProperty("destination").objectReferenceValue = top.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        so = new SerializedObject(downLift);
        so.FindProperty("destination").objectReferenceValue = bottom.transform;
        so.FindProperty("requireHelper").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        GimmickMenu.Place(root, command, "持ち上げる場所");
    }

    //=====================================================================
    // 離れ離れ：カメラの範囲・合流の範囲
    //=====================================================================

    [MenuItem("GameObject/仕掛け/2人/カメラの範囲（エリアごと）", false, 50)]
    private static void PlaceCameraArea(MenuCommand command)
    {
        var go = new GameObject("カメラの範囲");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(20.0f, 12.0f);
        go.AddComponent<CameraArea>();
        GimmickMenu.Place(go, command, "カメラの範囲");
    }

    [MenuItem("GameObject/仕掛け/2人/合流の範囲", false, 51)]
    private static void PlaceRejoin(MenuCommand command)
    {
        var go = new GameObject("合流の範囲");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.0f, 3.0f);

        var child = new GameObject("合流したとき");
        child.transform.SetParent(go.transform, false);
        var sequence = child.AddComponent<EventSequence>();

        var rejoin = go.AddComponent<PartyRejoinArea>();
        var so = new SerializedObject(rejoin);
        so.FindProperty("onRejoined").objectReferenceValue = sequence;
        so.ApplyModifiedPropertiesWithoutUndo();

        GimmickMenu.Place(go, command, "合流の範囲");
    }

    //=====================================================================
    // 目印の設定
    //=====================================================================

    [MenuItem("Tools/仕掛け/相棒の目印の設定を作る", false, 310)]
    private static void CreateHintSettingsMenu()
    {
        PartyHintSettings s = EnsureHintSettings();
        Selection.activeObject = s;
        EditorGUIUtility.PingObject(s);
    }

    /// <summary>
    /// 相棒の目印の設定（Resources/PartyHints.asset）がなければ作る
    /// </summary>
    public static PartyHintSettings EnsureHintSettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<PartyHintSettings>(HintSettingsPath);
        if(s != null) { return s; }

        s = ScriptableObject.CreateInstance<PartyHintSettings>();
        s.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        s.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
        System.IO.Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.CreateAsset(s, HintSettingsPath);
        AssetDatabase.SaveAssets();
        return s;
    }
}
