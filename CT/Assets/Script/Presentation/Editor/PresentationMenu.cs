/*********************************************************************/
// * \file   PresentationMenu.cs
// * \brief  Hierarchy 右クリック > 演出 > シーンの演出の既定・場面の光・天候の範囲・フラグでなめらかに出す
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 演出の物を置くメニュー（Hierarchy 右クリック > 演出）
/// </summary>
public static class PresentationMenu
{
    [MenuItem("GameObject/演出/シーンの演出の既定（色調・光・天候）", false, 30)]
    private static void CreateScenePresentation(MenuCommand command)
    {
        if(Object.FindAnyObjectByType<ScenePresentation>() != null)
        {
            EditorUtility.DisplayDialog("シーンの演出の既定", "このシーンにはもうあります（1つだけ置く）。", "OK");
            Selection.activeObject = Object.FindAnyObjectByType<ScenePresentation>();
            return;
        }
        GameObject go = Place(command, "シーンの演出の既定");
        go.AddComponent<ScenePresentation>();
    }

    [MenuItem("GameObject/演出/太陽（レンズフレア）", false, 33)]
    private static void CreateLensSun(MenuCommand command)
    {
        GameObject go = Place(command, "太陽（レンズフレア）");
        go.AddComponent<LensSun>();
    }

    [MenuItem("GameObject/演出/場面の光（点の光）", false, 31)]
    private static void CreatePointLight(MenuCommand command)
    {
        CreateSceneLight(command, "場面の光（点）", Light2D.LightType.Point);
    }

    [MenuItem("GameObject/演出/場面の光（窓から差す筋・夕日など、四角い光）", false, 32)]
    private static void CreateShapeLight(MenuCommand command)
    {
        CreateSceneLight(command, "場面の光（四角）", Light2D.LightType.Freeform);
    }

    //=====================================================================
    // 青と黄色の光（11-26 依頼書 CT_mood_blue_yellow）：塗り替えの色調と組み合わせる強い光。最初は消してある（光のプリセットで点ける）
    //=====================================================================

    public enum BlueYellowLight { Window, Lamp, Moon, TowerTop, Watch }

    [MenuItem("GameObject/演出/青と黄色の光/窓の光（床に落ちる台形・硬い境目・影あり）", false, 40)]
    private static void MenuWindow(MenuCommand c) { PlaceBlueYellow(c, BlueYellowLight.Window); }

    [MenuItem("GameObject/演出/青と黄色の光/ランプ・ランタン（硬い円＋縁の光・影あり）", false, 41)]
    private static void MenuLamp(MenuCommand c) { PlaceBlueYellow(c, BlueYellowLight.Lamp); }

    [MenuItem("GameObject/演出/青と黄色の光/月明かり（柔らかい）", false, 42)]
    private static void MenuMoon(MenuCommand c) { PlaceBlueYellow(c, BlueYellowLight.Moon); }

    [MenuItem("GameObject/演出/青と黄色の光/塔の頂上の光（小さな点）", false, 43)]
    private static void MenuTop(MenuCommand c) { PlaceBlueYellow(c, BlueYellowLight.TowerTop); }

    [MenuItem("GameObject/演出/青と黄色の光/懐中時計の光（手元の小さな円）", false, 44)]
    private static void MenuWatch(MenuCommand c) { PlaceBlueYellow(c, BlueYellowLight.Watch); }

    private static void PlaceBlueYellow(MenuCommand command, BlueYellowLight kind)
    {
        Vector3 pos = Vector3.zero;
        if(SceneView.lastActiveSceneView != null) { Vector3 p = SceneView.lastActiveSceneView.pivot; pos = new Vector3(p.x, p.y, 0.0f); }
        GameObject go = BuildBlueYellowLight(kind, pos, (command.context as GameObject)?.transform);
        Undo.RegisterCreatedObjectUndo(go, go.name);
        Selection.activeGameObject = go;
    }

    /// <summary>
    /// 青と黄色の光を作る（レシピからも使う）。グループ名は 窓・ランプ・月・頂上・時計。最初は消してある（光のプリセット「青い夜」などで点く）
    /// </summary>
    public static GameObject BuildBlueYellowLight(BlueYellowLight kind, Vector3 position, Transform parent)
    {
        string name; string group;
        switch(kind)
        {
            case BlueYellowLight.Window:   name = "窓の光";       group = "窓";   break;
            case BlueYellowLight.Lamp:     name = "ランプの光";   group = "ランプ"; break;
            case BlueYellowLight.Moon:     name = "月明かり";     group = "月";   break;
            case BlueYellowLight.TowerTop: name = "塔の頂上の光"; group = "頂上"; break;
            default:                       name = "懐中時計の光"; group = "時計"; break;
        }
        var root = new GameObject(name);
        if(parent != null) { root.transform.SetParent(parent, false); }
        root.transform.position = position;

        switch(kind)
        {
            case BlueYellowLight.Window:
                // 窓から床に落ちる台形（上が窓・下が床）。硬い境目＋少し大きい弱い縁の光
                Shape(root, "光", group, 1.4f, 0.05f, true, new[] { new Vector3(-0.7f, 1.4f), new Vector3(0.7f, 1.4f), new Vector3(1.6f, -1.8f), new Vector3(-1.6f, -1.8f) });
                Shape(root, "縁", group, 0.35f, 0.05f, false, new[] { new Vector3(-0.9f, 1.6f), new Vector3(0.9f, 1.6f), new Vector3(1.95f, -2.15f), new Vector3(-1.95f, -2.15f) });
                break;
            case BlueYellowLight.Lamp:
                Point(root, "光", group, 1.4f, 1.7f, 1.6f, 1.0f, true);
                Point(root, "縁", group, 0.35f, 2.2f, 2.05f, 1.0f, false);
                break;
            case BlueYellowLight.Moon:
                Point(root, "月", group, 1.6f, 0.7f, 0.5f, 0.6f, false);
                Point(root, "地面の淡い光", group, 0.35f, 6.0f, 0.6f, 0.6f, false);
                break;
            case BlueYellowLight.TowerTop:
                Point(root, "光", group, 2.2f, 0.6f, 0.1f, 0.5f, false);
                break;
            default:
                Point(root, "光", group, 1.8f, 0.9f, 0.15f, 0.6f, false);
                break;
        }
        return root;
    }

    private static Light2D NewLight(GameObject root, string childName, string group, float intensity)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(root.transform, false);
        Light2D l = go.AddComponent<Light2D>();
        l.color = Color.white;   // 色は塗り替えで決まるので、光は明るさだけ
        l.intensity = intensity;
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for(int i = 0; i < layers.Length; i++) { ids[i] = layers[i].id; }
        var so = new SerializedObject(l);
        SerializedProperty sl = so.FindProperty("m_ApplyToSortingLayers");
        if(sl != null) { sl.arraySize = ids.Length; for(int i = 0; i < ids.Length; i++) { sl.GetArrayElementAtIndex(i).intValue = ids[i]; } so.ApplyModifiedPropertiesWithoutUndo(); }
        SceneLight sc = go.AddComponent<SceneLight>();
        var sso = new SerializedObject(sc);
        sso.FindProperty("group").stringValue = group;
        sso.ApplyModifiedPropertiesWithoutUndo();
        l.enabled = false;   // ふだんは消えている（光のプリセットに書いたときだけ点く）
        return l;
    }

    private static void Point(GameObject root, string childName, string group, float intensity, float outer, float inner, float falloff, bool shadows)
    {
        Light2D l = NewLight(root, childName, group, intensity);
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = outer;
        l.pointLightInnerRadius = inner;
        l.falloffIntensity = falloff;
        SetShadows(l, shadows);
    }

    private static void Shape(GameObject root, string childName, string group, float intensity, float falloffSize, bool shadows, Vector3[] path)
    {
        Light2D l = NewLight(root, childName, group, intensity);
        l.lightType = Light2D.LightType.Freeform;
        l.SetShapePath(path);
        l.shapeLightFalloffSize = falloffSize;
        SetShadows(l, shadows);
    }

    private static void SetShadows(Light2D l, bool on)
    {
        var so = new SerializedObject(l);
        SerializedProperty e = so.FindProperty("m_ShadowsEnabled");
        if(e != null) { e.boolValue = on; }
        SerializedProperty s = so.FindProperty("m_ShadowIntensity");
        if(s != null) { s.floatValue = 0.9f; }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("GameObject/演出/天候の範囲（入ると天候が変わる・止む）", false, 33)]
    private static void CreateWeatherArea(MenuCommand command)
    {
        GameObject go = Place(command, "天候の範囲");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(8.0f, 6.0f);
        go.AddComponent<WeatherArea>();
    }

    [MenuItem("GameObject/演出/フラグでなめらかに出す（回想中だけの物など）", false, 34)]
    private static void CreateFlagFade(MenuCommand command)
    {
        GameObject go = Place(command, "フラグでなめらかに出す");
        FlagFade fade = go.AddComponent<FlagFade>();

        // 回想中フラグを最初から入れておく
        GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(PresentationPresetBuilder.FlagPath);
        if(flag != null)
        {
            var so = new SerializedObject(fade);
            SerializedProperty list = so.FindProperty("flagConditions");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("flag").objectReferenceValue = flag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateSceneLight(MenuCommand command, string name, Light2D.LightType type)
    {
        GameObject go = Place(command, name);
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = type;
        light.intensity = 1.0f;
        light.color = new Color(0.75f, 0.82f, 1.0f, 1.0f);
        if(type == Light2D.LightType.Point)
        {
            light.pointLightOuterRadius = 5.0f;
            light.pointLightInnerRadius = 0.6f;
            light.falloffIntensity = 0.6f;
        }
        else
        {
            light.SetShapePath(new[] { new Vector3(-1.5f, -3.0f), new Vector3(1.5f, -3.0f), new Vector3(0.8f, 3.0f), new Vector3(-0.8f, 3.0f) });
            light.shapeLightFalloffSize = 1.0f;
            light.intensity = 0.6f;
        }

        // すべての並びのレイヤーを照らす
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for(int i = 0; i < layers.Length; i++) { ids[i] = layers[i].id; }
        var so = new SerializedObject(light);
        SerializedProperty sl = so.FindProperty("m_ApplyToSortingLayers");
        if(sl != null)
        {
            sl.arraySize = ids.Length;
            for(int i = 0; i < ids.Length; i++) { sl.GetArrayElementAtIndex(i).intValue = ids[i]; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        go.AddComponent<SceneLight>();
    }

    private static GameObject Place(MenuCommand command, string name)
    {
        var go = new GameObject(name);
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 p = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(p.x, p.y, 0.0f);
        }
        Undo.RegisterCreatedObjectUndo(go, name);
        Selection.activeGameObject = go;
        return go;
    }
}
