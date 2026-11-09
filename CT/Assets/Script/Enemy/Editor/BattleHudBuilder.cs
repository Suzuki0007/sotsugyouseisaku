/*********************************************************************/
// * \file   BattleHudBuilder.cs
// * \brief  戦闘の画面表示のプレハブ（Resources/UI/BattleHud.prefab）を作る
// *
// * \author
/*********************************************************************/

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > 敵 > 戦闘の画面表示のプレハブを作る
/// </summary>
public static class BattleHudBuilder
{
    public const string PrefabPath = "Assets/Resources/UI/BattleHud.prefab";

    [MenuItem("Tools/敵/戦闘の画面表示のプレハブを作る", false, 202)]
    private static void BuildFromMenu()
    {
        if(File.Exists(PrefabPath) &&
           !EditorUtility.DisplayDialog("戦闘の画面表示", $"{PrefabPath} を作り直します（調整した見た目は上書きされます）。よろしいですか？", "作り直す", "やめる"))
        {
            return;
        }

        GameObject prefab = Build();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    public static GameObject Build()
    {
        TMP_FontAsset font = FindJapaneseFont();

        var root = new GameObject("BattleHud");
        try
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // ゲームの UI より手前、メニュー・暗転より奥

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            scaler.matchWidthOrHeight = 0.5f;

            // 上の帯（目的・残り時間）
            var panel = new GameObject("上の帯", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1.0f);
            panelRect.pivot = new Vector2(0.5f, 1.0f);
            panelRect.anchoredPosition = new Vector2(0.0f, -24.0f);
            panelRect.sizeDelta = new Vector2(960.0f, 72.0f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.0f, 0.0f, 0.0f, 0.55f);
            panelImage.raycastTarget = false;

            TMP_Text info = CreateText("文字", panel.transform, font, "ゴールまで逃げろ　残り 30 秒", 38.0f);
            var infoRect = info.rectTransform;
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(16.0f, 0.0f);
            infoRect.offsetMax = new Vector2(-16.0f, 0.0f);

            // 真ん中のメッセージ
            TMP_Text message = CreateText("メッセージ", root.transform, font, "逃げろ！", 84.0f);
            message.fontStyle = FontStyles.Bold;
            message.outlineWidth = 0.2f;
            message.outlineColor = new Color32(0, 0, 0, 220);
            var messageRect = message.rectTransform;
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = new Vector2(0.0f, 140.0f);
            messageRect.sizeDelta = new Vector2(1600.0f, 200.0f);

            var hud = root.AddComponent<BattleHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("infoPanel").objectReferenceValue = panel;
            so.FindProperty("infoText").objectReferenceValue = info;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if(font != null) { tmp.font = font; }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_FontAsset FindJapaneseFont()
    {
        foreach(string guid in AssetDatabase.FindAssets("NotoSansJP t:TMP_FontAsset"))
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if(font != null) { return font; }
        }
        return TMP_Settings.defaultFontAsset;
    }
}
