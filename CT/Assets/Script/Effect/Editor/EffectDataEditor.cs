/*********************************************************************/
// * \file   EffectDataEditor.cs
// * \brief  EffectData の Inspector 表示 (プレビュー・試し再生・設定ミスの警告)
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// EffectData の Inspector
///   ・スプライト連番はその場でアニメーションのプレビューが見られる
///   ・Playモード中は「試し再生」ボタンでゲーム画面に出せる
///   ・必要な設定が足りないときは警告を出す
///   ・使わない項目は隠す
/// </summary>
[CustomEditor(typeof(EffectData))]
[CanEditMultipleObjects]
public class EffectDataEditor : Editor
{
    private SerializedProperty displayMode, frames, fps, color, material, prefab;
    private SerializedProperty lifetime, duration;
    private SerializedProperty offset, scale, scaleRandom, randomRotation, randomFlipX;
    private SerializedProperty overrideSorting, sortingLayerID, sortingOrder;
    private SerializedProperty se;
    private SerializedProperty ignoreTimeScale, minInterval, maxConcurrent, preloadCount;

    private static bool showAdvanced;
    private const float PreviewHeight = 140.0f;

    private void OnEnable()
    {
        displayMode     = serializedObject.FindProperty("displayMode");
        frames          = serializedObject.FindProperty("frames");
        fps             = serializedObject.FindProperty("fps");
        color           = serializedObject.FindProperty("color");
        material        = serializedObject.FindProperty("material");
        prefab          = serializedObject.FindProperty("prefab");
        lifetime        = serializedObject.FindProperty("lifetime");
        duration        = serializedObject.FindProperty("duration");
        offset          = serializedObject.FindProperty("offset");
        scale           = serializedObject.FindProperty("scale");
        scaleRandom     = serializedObject.FindProperty("scaleRandom");
        randomRotation  = serializedObject.FindProperty("randomRotation");
        randomFlipX     = serializedObject.FindProperty("randomFlipX");
        overrideSorting = serializedObject.FindProperty("overrideSorting");
        sortingLayerID  = serializedObject.FindProperty("sortingLayerID");
        sortingOrder    = serializedObject.FindProperty("sortingOrder");
        se              = serializedObject.FindProperty("se");
        ignoreTimeScale = serializedObject.FindProperty("ignoreTimeScale");
        minInterval     = serializedObject.FindProperty("minInterval");
        maxConcurrent   = serializedObject.FindProperty("maxConcurrent");
        preloadCount    = serializedObject.FindProperty("preloadCount");
    }

    // スプライト連番のプレビューを動かすため、常に再描画する
    public override bool RequiresConstantRepaint()
    {
        EffectData data = target as EffectData;
        return data != null && data.DisplayMode == EffectDisplayMode.SpriteFrames && data.HasFrames;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EffectData data = (EffectData)target;
        bool spriteMode = !displayMode.hasMultipleDifferentValues &&
                          displayMode.enumValueIndex == (int)EffectDisplayMode.SpriteFrames;

        //------------------------------------------------------------------
        Header("見た目");
        EditorGUILayout.PropertyField(displayMode, new GUIContent("表示方法"));

        if(spriteMode)
        {
            EditorGUILayout.PropertyField(frames, new GUIContent("画像 (上から順に再生)"), true);
            EditorGUILayout.PropertyField(fps, new GUIContent("速さ (枚/秒)"));
            EditorGUILayout.PropertyField(color, new GUIContent("色"));
            EditorGUILayout.PropertyField(material, new GUIContent("マテリアル (任意)"));
        }
        else
        {
            EditorGUILayout.PropertyField(prefab, new GUIContent("Prefab"));
        }

        if(!serializedObject.isEditingMultipleObjects) { DrawPreview(data); }

        //------------------------------------------------------------------
        Header("消え方");
        EditorGUILayout.PropertyField(lifetime, new GUIContent("いつ消えるか"));

        bool needsDuration = lifetime.enumValueIndex == (int)EffectLifetime.Duration ||
                             (!spriteMode && lifetime.enumValueIndex == (int)EffectLifetime.Auto && !HasFinishDetector(data));
        if(needsDuration)
        {
            EditorGUILayout.PropertyField(duration, new GUIContent("消えるまでの秒数"));
        }

        if(spriteMode && data.HasFrames && lifetime.enumValueIndex == (int)EffectLifetime.Auto)
        {
            EditorGUILayout.LabelField(" ", $"1回の長さ : {data.FrameCycleTime:0.00} 秒", EditorStyles.miniLabel);
        }

        //------------------------------------------------------------------
        Header("位置・大きさ・向き");
        EditorGUILayout.PropertyField(offset, new GUIContent("位置のずらし"));
        EditorGUILayout.PropertyField(scale, new GUIContent("大きさ"));
        EditorGUILayout.PropertyField(scaleRandom, new GUIContent("大きさのばらつき"));
        EditorGUILayout.PropertyField(randomRotation, new GUIContent("向きをランダムに"));
        EditorGUILayout.PropertyField(randomFlipX, new GUIContent("左右反転をランダムに"));

        //------------------------------------------------------------------
        Header("表示順");
        EditorGUILayout.PropertyField(overrideSorting, new GUIContent("表示順を指定する"));
        if(overrideSorting.boolValue)
        {
            EditorGUILayout.PropertyField(sortingLayerID, new GUIContent("Sorting Layer"));
            EditorGUILayout.PropertyField(sortingOrder, new GUIContent("Order in Layer (大きいほど手前)"));
        }

        //------------------------------------------------------------------
        Header("音");
        EditorGUILayout.PropertyField(se, new GUIContent("一緒に鳴らすSE (任意)"));

        //------------------------------------------------------------------
        EditorGUILayout.Space(6);
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "詳細設定", true);
        if(showAdvanced)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(ignoreTimeScale, new GUIContent("ポーズ中も動かす"));
            EditorGUILayout.PropertyField(minInterval, new GUIContent("連打の最短間隔 (秒)"));
            EditorGUILayout.PropertyField(maxConcurrent, new GUIContent("同時に出せる最大数"));
            EditorGUILayout.PropertyField(preloadCount, new GUIContent("事前に作っておく数"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        //------------------------------------------------------------------
        if(!serializedObject.isEditingMultipleObjects) { DrawWarnings(data, spriteMode); }
        DrawTestButtons();
    }

    //=====================================================================
    // プレビュー
    //=====================================================================

    private void DrawPreview(EffectData data)
    {
        Rect rect = SpritePreviewGUI.BeginArea(PreviewHeight);

        if(data.DisplayMode == EffectDisplayMode.SpriteFrames)
        {
            if(!data.HasFrames)
            {
                SpritePreviewGUI.CenterLabel(rect, "画像を入れるとここでプレビューできます");
                return;
            }

            // 1回で終わるエフェクトは、少し間を空けて繰り返し表示する
            double cycle = data.FrameCycleTime;
            double loopLength = data.Lifetime == EffectLifetime.Auto ? cycle + 0.4 : cycle;
            double t = EditorApplication.timeSinceStartup % System.Math.Max(0.01, loopLength);
            if(data.Lifetime == EffectLifetime.Auto && t > cycle)
            {
                return;
            }
            int index = SpriteFrameTiming.FrameAt((float)t, data.Frames.Length, data.Fps, false, out _);

            Sprite sprite = data.Frames[index];
            if(sprite == null)
            {
                SpritePreviewGUI.CenterLabel(rect, $"{index + 1} 枚目の画像が空です");
                return;
            }

            SpritePreviewGUI.DrawSprite(rect, sprite, data.Color, false);
            SpritePreviewGUI.CornerLabel(rect, $"{index + 1} / {data.Frames.Length}");
        }
        else
        {
            if(data.Prefab == null)
            {
                SpritePreviewGUI.CenterLabel(rect, "Prefab を入れてください");
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(data.Prefab);
            if(preview != null)
            {
                GUI.DrawTexture(SpritePreviewGUI.Fit(rect, preview.width, preview.height), preview, ScaleMode.ScaleToFit);
            }
            else
            {
                SpritePreviewGUI.CenterLabel(rect, "Prefab のプレビューは Playモードの「試し再生」で確認してください");
                Repaint();// サムネイルの読み込みが終わったら表示されるように
            }
        }
    }

    //=====================================================================
    // 警告
    //=====================================================================

    private static void DrawWarnings(EffectData data, bool spriteMode)
    {
        EditorGUILayout.Space(6);

        if(spriteMode)
        {
            if(!data.HasFrames)
            {
                EditorGUILayout.HelpBox("画像が入っていません。「画像」に再生したいスプライトを入れてください。\n" +
                                        "(画像をまとめて選び、右クリック → Create → Effect から作ると自動で入ります)", MessageType.Warning);
            }
            else
            {
                for(int i = 0; i < data.Frames.Length; i++)
                {
                    if(data.Frames[i] != null) { continue; }
                    EditorGUILayout.HelpBox($"{i + 1} 枚目の画像が空です。", MessageType.Warning);
                    break;
                }
            }
        }
        else
        {
            if(data.Prefab == null)
            {
                EditorGUILayout.HelpBox("Prefab が入っていません。", MessageType.Warning);
                return;
            }

            if(data.Lifetime == EffectLifetime.Auto)
            {
                foreach(ParticleSystem ps in data.Prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if(!ps.main.loop) { continue; }
                    EditorGUILayout.HelpBox($"パーティクル「{ps.name}」が Looping になっているため、「自動」では消えません。\n" +
                                            "「秒数を指定」か「ループ」にするか、パーティクルの Looping を外してください。", MessageType.Warning);
                    break;
                }

                if(!HasFinishDetector(data))
                {
                    EditorGUILayout.HelpBox("この Prefab には ParticleSystem / Animator が無いため、「消えるまでの秒数」で消えます。", MessageType.Info);
                }
            }
        }

        if(data.Lifetime == EffectLifetime.Loop)
        {
            EditorGUILayout.HelpBox("ループエフェクトは止めるまで出続けます。\n" +
                                    "・EffectTrigger : オブジェクトが消える/相手が出ていくと自動で止まる\n" +
                                    "・追従させた場合 : 追従相手が消えると自動で止まる", MessageType.Info);
        }
    }

    private static bool HasFinishDetector(EffectData data)
    {
        if(data.Prefab == null) { return true; }
        return data.Prefab.GetComponentInChildren<ParticleSystem>(true) != null ||
               data.Prefab.GetComponentInChildren<Animator>(true) != null ||
               data.Prefab.GetComponentInChildren<IEffectFinishable>(true) != null;
    }

    //=====================================================================
    // 試し再生
    //=====================================================================

    private void DrawTestButtons()
    {
        EditorGUILayout.Space(6);

        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Playモード中は、ここからゲーム画面にエフェクトを試し出しできます。", MessageType.None);
            return;
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("▶ 試し再生 (画面の中央)", GUILayout.Height(28)))
            {
                foreach(Object t in targets)
                {
                    EffectManager.Play((EffectData)t, GetTestPosition());
                }
            }

            if(GUILayout.Button("■ 止める", GUILayout.Height(28), GUILayout.Width(80)))
            {
                foreach(Object t in targets)
                {
                    EffectManager.StopAll((EffectData)t, true);
                }
            }
        }
    }

    private static Vector3 GetTestPosition()
    {
        Camera cam = Camera.main;
        if(cam == null) { return Vector3.zero; }
        Vector3 p = cam.transform.position;
        return new Vector3(p.x, p.y, 0.0f);
    }

    //=====================================================================
    // 共通
    //=====================================================================

    private static void Header(string text)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }
}

/// <summary>
/// [SortingLayer] を付けた int を Sorting Layer の選択欄で表示する
/// </summary>
[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
public class SortingLayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "[SortingLayer] は int にだけ使えます");
            return;
        }

        SortingLayer[] layers = SortingLayer.layers;
        string[] names = new string[layers.Length];
        int current = 0;
        for(int i = 0; i < layers.Length; i++)
        {
            names[i] = layers[i].name;
            if(layers[i].id == property.intValue) { current = i; }
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, label.text, current, names);
        if(EditorGUI.EndChangeCheck()) { property.intValue = layers[selected].id; }
        EditorGUI.showMixedValue = false;
        EditorGUI.EndProperty();
    }
}
