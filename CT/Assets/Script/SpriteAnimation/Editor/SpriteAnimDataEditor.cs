/*********************************************************************/
// * \file   SpriteAnimDataEditor.cs
// * \brief  SpriteAnimData の Inspector 表示 (動きの切り替え・プレビュー・画像のドロップ・警告)
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SpriteAnimData の Inspector
///   ・上のボタンで動き (Idle / Walk …) を切り替えて編集する
///   ・その場でアニメーションのプレビューが見られる (↓↑←→ で向きを切り替え)
///   ・画像をドラッグ&ドロップすると、名前から動き・向きを判断して追加する
///   ・設定ミスがあれば警告を出す
/// </summary>
[CustomEditor(typeof(SpriteAnimData))]
public class SpriteAnimDataEditor : Editor
{
    private SerializedProperty defaultClip, clips;

    private int selected;
    private SpriteAnimFacing previewFacing = SpriteAnimFacing.Down;
    private double previewStart;
    private static bool showLengths;

    private const float PreviewHeight = 160.0f;
    private static readonly string[] FacingButtons = { "↓ 下", "↑ 上", "← 左", "→ 右" };

    private void OnEnable()
    {
        defaultClip = serializedObject.FindProperty("defaultClip");
        clips       = serializedObject.FindProperty("clips");
        previewStart = EditorApplication.timeSinceStartup;
    }

    public override bool RequiresConstantRepaint() => true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var data = (SpriteAnimData)target;

        DrawDropArea(data);

        //------------------------------------------------------------------
        Header("動き");
        if(clips.arraySize == 0)
        {
            EditorGUILayout.HelpBox("動きがありません。上の枠に画像をドラッグするか、「＋ 追加」を押してください。", MessageType.Info);
        }

        string[] names = ClipNames(data);
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, clips.arraySize - 1));
        if(names.Length > 0)
        {
            int newSel = GUILayout.SelectionGrid(selected, names, Mathf.Min(4, names.Length));
            if(newSel != selected) { selected = newSel; previewStart = EditorApplication.timeSinceStartup; }
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("＋ 追加"))
            {
                clips.arraySize++;
                SerializedProperty c = clips.GetArrayElementAtIndex(clips.arraySize - 1);
                c.FindPropertyRelative("name").stringValue = UniqueName(data, "New");
                selected = clips.arraySize - 1;
            }

            using(new EditorGUI.DisabledScope(clips.arraySize == 0))
            {
                if(GUILayout.Button("複製") && clips.arraySize > 0)
                {
                    clips.InsertArrayElementAtIndex(selected);
                    selected++;
                    SerializedProperty c = clips.GetArrayElementAtIndex(selected);
                    c.FindPropertyRelative("name").stringValue = UniqueName(data, c.FindPropertyRelative("name").stringValue);
                }

                if(GUILayout.Button("削除") && clips.arraySize > 0 &&
                   EditorUtility.DisplayDialog("動きの削除", $"「{names[selected]}」を削除しますか？", "削除", "やめる"))
                {
                    clips.DeleteArrayElementAtIndex(selected);
                    selected = Mathf.Max(0, selected - 1);
                }

                if(GUILayout.Button("▲", GUILayout.Width(26)) && selected > 0)
                {
                    clips.MoveArrayElement(selected, selected - 1);
                    selected--;
                }
                if(GUILayout.Button("▼", GUILayout.Width(26)) && selected < clips.arraySize - 1)
                {
                    clips.MoveArrayElement(selected, selected + 1);
                    selected++;
                }
            }
        }

        // 最初に流す動き
        names = ClipNamesFromProperty();
        if(names.Length > 0)
        {
            int current = System.Array.FindIndex(names, n => string.Equals(n, defaultClip.stringValue, System.StringComparison.OrdinalIgnoreCase));
            int picked = EditorGUILayout.Popup(new GUIContent("最初に流す動き", "表示されたときに最初に流す動き"), Mathf.Max(0, current), names);
            if(picked != current) { defaultClip.stringValue = names[picked]; }
        }

        //------------------------------------------------------------------
        if(clips.arraySize > 0)
        {
            SerializedProperty clip = clips.GetArrayElementAtIndex(selected);
            DrawClip(clip, names);

            serializedObject.ApplyModifiedProperties();

            SpriteAnimClip runtimeClip = selected < data.Clips.Count ? data.Clips[selected] : null;
            if(runtimeClip != null)
            {
                DrawPreview(runtimeClip);
                DrawClipWarnings(data, runtimeClip);
            }
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }

        DrawDataWarnings(data);
    }

    //=====================================================================
    // 動き1つ分
    //=====================================================================

    private void DrawClip(SerializedProperty clip, string[] names)
    {
        Header("設定");

        SerializedProperty mode = clip.FindPropertyRelative("directionMode");
        SerializedProperty playMode = clip.FindPropertyRelative("playMode");

        EditorGUILayout.PropertyField(clip.FindPropertyRelative("name"), new GUIContent("名前", "Idle / Walk / Damage / Dead / Open など。CharacterAnimLink などはこの名前で探す"));
        EditorGUILayout.PropertyField(mode, new GUIContent("向き"));

        switch((SpriteAnimDirectionMode)mode.enumValueIndex)
        {
            case SpriteAnimDirectionMode.Single:
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("down"), new GUIContent("画像 (上から順に再生)"), true);
                break;

            case SpriteAnimDirectionMode.Side:
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("right"), new GUIContent("右向き"), true);
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("left"), new GUIContent("左向き (空なら右を反転)"), true);
                break;

            default:
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("down"), new GUIContent("下向き (正面)"), true);
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("up"), new GUIContent("上向き (背中)"), true);
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("left"), new GUIContent("左向き (空なら右を反転)"), true);
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("right"), new GUIContent("右向き (空なら左を反転)"), true);
                break;
        }

        EditorGUILayout.PropertyField(clip.FindPropertyRelative("fps"), new GUIContent("速さ (コマ/秒)", "1秒に何コマ進めるか。大きいほど速い"));
        EditorGUILayout.PropertyField(playMode, new GUIContent("再生のしかた"));

        if(playMode.enumValueIndex == (int)SpriteAnimPlayMode.OnceThenNext)
        {
            SerializedProperty next = clip.FindPropertyRelative("nextClip");
            int cur = System.Array.IndexOf(names, next.stringValue);
            int picked = EditorGUILayout.Popup("次に流す動き", cur, names);
            if(picked != cur && picked >= 0) { next.stringValue = names[picked]; }
        }

        showLengths = EditorGUILayout.Foldout(showLengths, "コマごとの長さ (溜め・決めポーズ用)", true);
        if(showLengths)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("1 = ふつう、2 = そのコマだけ2倍長く表示。空ならすべて同じ長さです。\n上から 1コマ目, 2コマ目 … の順です。", MessageType.None);
            EditorGUILayout.PropertyField(clip.FindPropertyRelative("frameLengths"), new GUIContent("長さの倍率"), true);
            EditorGUI.indentLevel--;
        }

        Header("コマごとのイベント (音・エフェクト)");
        EditorGUILayout.PropertyField(clip.FindPropertyRelative("events"), new GUIContent("イベント"), true);
    }

    //=====================================================================
    // プレビュー
    //=====================================================================

    private void DrawPreview(SpriteAnimClip clip)
    {
        Header("プレビュー");

        if(clip.directionMode != SpriteAnimDirectionMode.Single)
        {
            int f = GUILayout.Toolbar((int)previewFacing, FacingButtons);
            if(f != (int)previewFacing) { previewFacing = (SpriteAnimFacing)f; previewStart = EditorApplication.timeSinceStartup; }
        }

        Rect rect = SpritePreviewGUI.BeginArea(PreviewHeight);

        Sprite[] frames = clip.GetFrames(previewFacing, previewFacing == SpriteAnimFacing.Left, out bool flip);
        if(!SpriteAnimClip.Has(frames))
        {
            SpritePreviewGUI.CenterLabel(rect, "画像を入れるとここでプレビューできます");
            return;
        }

        // 1回で終わる動きは、最後のコマで少し止めてから繰り返す
        float cycle = clip.CycleTime(frames);
        float loopLength = clip.IsLoop ? cycle : cycle + 0.6f;
        float t = (float)((EditorApplication.timeSinceStartup - previewStart) % System.Math.Max(0.01, loopLength));
        int index = SpriteFrameTiming.FrameAt(t, frames.Length, clip.fps, clip.IsLoop, out _, clip.frameLengths);

        Sprite sprite = frames[index];
        if(sprite == null)
        {
            SpritePreviewGUI.CenterLabel(rect, $"{index + 1} コマ目の画像が空です");
            return;
        }

        SpritePreviewGUI.DrawSprite(rect, sprite, Color.white, flip);

        string info = $"{index + 1} / {frames.Length}   1回 {cycle:0.00} 秒";
        if(flip) { info += "   (反転して表示)"; }
        SpritePreviewGUI.CornerLabel(rect, info);
    }

    //=====================================================================
    // 画像のドロップ
    //=====================================================================

    private void DrawDropArea(SpriteAnimData data)
    {
        Rect rect = GUILayoutUtility.GetRect(10.0f, 44.0f, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "ここに画像をドラッグ&ドロップ\n(名前から動き・向きを判断して、追加・入れ替えします)", EditorStyles.helpBox);

        Event e = Event.current;
        if(!rect.Contains(e.mousePosition)) { return; }
        if(e.type != EventType.DragUpdated && e.type != EventType.DragPerform) { return; }

        bool hasImage = DragAndDrop.objectReferences.Any(o => o is Sprite || o is Texture2D);
        DragAndDrop.visualMode = hasImage ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

        if(e.type == EventType.DragPerform && hasImage)
        {
            DragAndDrop.AcceptDrag();
            List<SpriteAnimImporter.Group> groups = SpriteAnimImporter.Analyze(DragAndDrop.objectReferences);
            if(groups.Count > 0)
            {
                string log = SpriteAnimImporter.Apply(data, groups);
                AssetDatabase.SaveAssets();
                serializedObject.Update();// 直接書き換えた内容を Inspector に反映する
                Debug.Log($"【アニメーション】{data.name} に画像を入れました\n{log}", data);
            }
        }
        e.Use();
    }

    //=====================================================================
    // 警告
    //=====================================================================

    private static void DrawClipWarnings(SpriteAnimData data, SpriteAnimClip clip)
    {
        EditorGUILayout.Space(4);

        if(string.IsNullOrWhiteSpace(clip.name))
        {
            EditorGUILayout.HelpBox("名前が空です。", MessageType.Warning);
        }
        else if(data.Clips.Count(c => c != null && string.Equals(c.name, clip.name, System.StringComparison.OrdinalIgnoreCase)) > 1)
        {
            EditorGUILayout.HelpBox($"「{clip.name}」という名前の動きが2つ以上あります。上にあるほうだけが使われます。", MessageType.Warning);
        }

        if(!clip.HasAnyFrames)
        {
            EditorGUILayout.HelpBox("画像が入っていません。", MessageType.Warning);
            return;
        }

        if(clip.AllSprites().Count() < new[] { clip.down, clip.up, clip.left, clip.right }.Where(SpriteAnimClip.Has).Sum(a => a.Length))
        {
            EditorGUILayout.HelpBox("空の欄 (None) のコマがあります。", MessageType.Warning);
        }

        if(clip.directionMode == SpriteAnimDirectionMode.FourWay)
        {
            var missing = new List<string>();
            if(!SpriteAnimClip.Has(clip.down)) { missing.Add("下"); }
            if(!SpriteAnimClip.Has(clip.up))   { missing.Add("上"); }
            if(!SpriteAnimClip.Has(clip.left) && !SpriteAnimClip.Has(clip.right)) { missing.Add("左右"); }
            if(missing.Count > 0)
            {
                EditorGUILayout.HelpBox($"{string.Join("・", missing)}向きの画像がありません。ほかの向きの画像で代用します。", MessageType.Info);
            }
        }

        if(clip.playMode == SpriteAnimPlayMode.OnceThenNext && !data.Has(clip.nextClip))
        {
            EditorGUILayout.HelpBox("「次に流す動き」が見つかりません。最後のコマで止まります。", MessageType.Warning);
        }

        int maxFrames = new[] { clip.down, clip.up, clip.left, clip.right }.Where(SpriteAnimClip.Has).Max(a => a.Length);
        foreach(SpriteAnimFrameEvent ev in clip.events)
        {
            if(ev != null && ev.frame > maxFrames)
            {
                EditorGUILayout.HelpBox($"イベントの「{ev.frame} コマ目」は、コマ数 ({maxFrames}) より大きいため出ません。", MessageType.Warning);
                break;
            }
        }

        if((clip.name == "Damage" || clip.name == "Attack") && clip.IsLoop)
        {
            EditorGUILayout.HelpBox("ダメージ・攻撃は、ふつう「1回」にします (ループだと1周流したところで元の動きに戻ります)。", MessageType.Info);
        }
    }

    /// <summary>
    /// 画像の読み込み設定 (Pixels Per Unit・Filter Mode) がそろっているか
    /// そろっていないと、コマが変わるたびに大きさやボケ方が変わってしまう
    /// </summary>
    private static void DrawDataWarnings(SpriteAnimData data)
    {
        List<TextureImporter> importers = data.Clips.Where(c => c != null)
            .SelectMany(c => c.AllSprites())
            .Select(s => AssetDatabase.GetAssetPath(s.texture))
            .Distinct()
            .Select(p => AssetImporter.GetAtPath(p) as TextureImporter)
            .Where(i => i != null)
            .ToList();
        if(importers.Count < 2) { return; }

        TextureImporter first = importers[0];
        bool ppuDiff    = importers.Any(i => !Mathf.Approximately(i.spritePixelsPerUnit, first.spritePixelsPerUnit));
        bool filterDiff = importers.Any(i => i.filterMode != first.filterMode);
        if(!ppuDiff && !filterDiff) { return; }

        EditorGUILayout.Space(4);
        string what = string.Join("・", new[] { ppuDiff ? "Pixels Per Unit" : null, filterDiff ? "Filter Mode" : null }.Where(s => s != null));
        EditorGUILayout.HelpBox($"画像によって {what} がバラバラです。コマが変わるたびに大きさや見え方が変わってしまいます。", MessageType.Warning);

        if(GUILayout.Button($"すべて「{System.IO.Path.GetFileName(first.assetPath)}」の設定 (PPU {first.spritePixelsPerUnit} / {first.filterMode}) にそろえる"))
        {
            foreach(TextureImporter i in importers.Skip(1))
            {
                i.spritePixelsPerUnit = first.spritePixelsPerUnit;
                i.filterMode = first.filterMode;
                i.SaveAndReimport();
            }
        }
    }

    //=====================================================================
    // 共通
    //=====================================================================

    private static string[] ClipNames(SpriteAnimData data)
    {
        return data.Clips.Select(c => c == null || string.IsNullOrEmpty(c.name) ? "(名前なし)" : c.name).ToArray();
    }

    private string[] ClipNamesFromProperty()
    {
        var result = new string[clips.arraySize];
        for(int i = 0; i < clips.arraySize; i++)
        {
            string n = clips.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            result[i] = string.IsNullOrEmpty(n) ? "(名前なし)" : n;
        }
        return result;
    }

    private static string UniqueName(SpriteAnimData data, string baseName)
    {
        string n = baseName;
        int i = 2;
        while(data.Has(n)) { n = baseName + i++; }
        return n;
    }

    private static void Header(string text)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }
}

/// <summary>
/// コマごとのイベント1行分の表示 (日本語の項目名にする)
/// </summary>
[CustomPropertyDrawer(typeof(SpriteAnimFrameEvent))]
public class SpriteAnimFrameEventDrawer : PropertyDrawer
{
    private static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if(!property.isExpanded) { return Line; }
        bool hasEffect = property.FindPropertyRelative("effect").objectReferenceValue != null;
        return Line * (hasEffect ? 8 : 6);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int frame = property.FindPropertyRelative("frame").intValue;
        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, $"{frame} コマ目  {Summary(property)}", true);

        if(property.isExpanded)
        {
            EditorGUI.indentLevel++;
            Field(ref r, property, "frame", "何コマ目で (1 = 最初)");
            Field(ref r, property, "se", "鳴らすSE");
            Field(ref r, property, "effect", "出すエフェクト");
            if(property.FindPropertyRelative("effect").objectReferenceValue != null)
            {
                Field(ref r, property, "effectOffset", "エフェクトの位置のずらし");
                Field(ref r, property, "effectFollow", "くっついて動く");
            }
            Field(ref r, property, "effectTriggerName", "EffectTrigger の行の名前");
            Field(ref r, property, "message", "合図 (プログラム用)");
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private static void Field(ref Rect r, SerializedProperty parent, string name, string label)
    {
        r.y += Line;
        SerializedProperty p = parent.FindPropertyRelative(name);
        EditorGUI.PropertyField(r, p, new GUIContent(label, p.tooltip));
    }

    private static string Summary(SerializedProperty p)
    {
        var parts = new List<string>();
        Object se = p.FindPropertyRelative("se").objectReferenceValue;
        Object fx = p.FindPropertyRelative("effect").objectReferenceValue;
        string trig = p.FindPropertyRelative("effectTriggerName").stringValue;
        string msg = p.FindPropertyRelative("message").stringValue;
        if(se != null) { parts.Add("SE: " + se.name); }
        if(fx != null) { parts.Add("エフェクト: " + fx.name); }
        if(!string.IsNullOrEmpty(trig)) { parts.Add("Trigger: " + trig); }
        if(!string.IsNullOrEmpty(msg)) { parts.Add("合図: " + msg); }
        return parts.Count > 0 ? "(" + string.Join(" / ", parts) + ")" : "(未設定)";
    }
}
