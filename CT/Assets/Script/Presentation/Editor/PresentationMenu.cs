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
