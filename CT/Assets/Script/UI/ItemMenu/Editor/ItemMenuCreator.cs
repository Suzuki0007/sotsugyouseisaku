/*********************************************************************/
// * \file   ItemMenuCreator.cs
// * \brief  アイテム画面のプレハブと鍵の ItemData を作成するエディタ拡張
// *
// * \author
/*********************************************************************/

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メニューの Tools > アイテム画面 から
///   ・UIプレハブを作成 … Assets/Resources/UI/ItemMenu.prefab を作る（あれば作り直す）
///   ・鍵のItemDataを作成 … Assets/Resources/Items/Item_Key.asset を作る（あれば何もしない）
/// プレハブはゲーム開始時に自動で生成されるので、シーンに置く必要はない
/// 見た目はプレハブを開いて自由に調整してよい（作り直すと上書きされるので注意）
/// </summary>
public static class ItemMenuCreator
{
    private const string PrefabFolder = "Assets/Resources/UI";
    private const string PrefabPath   = PrefabFolder + "/ItemMenu.prefab";
    private const string ItemFolder   = "Assets/Resources/" + ItemData.ResourcesFolder;
    private const string KeyItemPath  = ItemFolder + "/Item_Key.asset";

    // 色
    private static readonly Color DimColor       = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color WindowColor    = new Color(0.08f, 0.09f, 0.13f, 0.95f);
    private static readonly Color AreaColor      = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color TextColor      = new Color(0.95f, 0.95f, 0.95f, 1f);
    private static readonly Color SubTextColor   = new Color(0.75f, 0.77f, 0.82f, 1f);
    private static readonly Color TitleColor     = new Color(1f, 0.85f, 0.4f, 1f);

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Tools/アイテム画面/UIプレハブを作成")]
    private static void CreatePrefabMenu()
    {
        if(File.Exists(PrefabPath) &&
           !EditorUtility.DisplayDialog("アイテム画面", $"{PrefabPath} を作り直します（調整した見た目は上書きされます）。よろしいですか？", "作り直す", "やめる"))
        {
            return;
        }

        GameObject prefab = CreatePrefab();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    [MenuItem("Tools/アイテム画面/鍵のItemDataを作成")]
    private static void CreateKeyItemMenu()
    {
        ItemData item = CreateKeyItemData();
        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);
    }

    /// <summary>
    /// バッチモード用（プレハブと鍵の ItemData をまとめて作る）
    /// </summary>
    public static void CreateAllForBatch()
    {
        CreatePrefab();
        CreateKeyItemData();
    }

    //=====================================================================
    // 鍵の ItemData
    //=====================================================================

    private static ItemData CreateKeyItemData()
    {
        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(KeyItemPath);
        if(existing != null)
        {
            Debug.Log($"{KeyItemPath} はすでにあります");
            return existing;
        }

        EnsureFolder(ItemFolder);

        ItemData item = ScriptableObject.CreateInstance<ItemData>();

        var serialized = new SerializedObject(item);
        serialized.FindProperty("id").stringValue = ItemData.KeyItemId;
        serialized.FindProperty("displayName").stringValue = "鍵";
        serialized.FindProperty("description").stringValue = "どこかの宝箱を開けられそうな鍵。";
        serialized.FindProperty("isKey").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, KeyItemPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"{KeyItemPath} を作成しました。名前・説明・アイコンは Inspector で変更してください");
        return item;
    }

    //=====================================================================
    // プレハブ
    //=====================================================================

    private static GameObject CreatePrefab()
    {
        EnsureFolder(PrefabFolder);

        TMP_FontAsset font = FindJapaneseFont();

        // ルート（Canvas）
        GameObject root = new GameObject("ItemMenu", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;// 他のUIより手前（暗転よりは後ろ）

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        ItemMenuUI menu = root.AddComponent<ItemMenuUI>();

        // 背景を暗くするパネル（開閉する部分）
        RectTransform panel = CreateImage("Panel", root.transform, DimColor);
        Stretch(panel);

        // ウィンドウ
        RectTransform window = CreateImage("Window", panel, WindowColor);
        SetRect(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 780f));

        // タイトル
        TMP_Text title = CreateText("Title", window, font, "アイテム", 48, TitleColor, TextAlignmentOptions.Left);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(-80f, 70f));

        // 操作説明
        TMP_Text hint = CreateText("Hint", window, font, "↑↓ / W S：選択　　Tab / Esc：閉じる", 26, SubTextColor, TextAlignmentOptions.Right);
        SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 36f), new Vector2(-80f, 40f));

        // 一覧（左）
        RectTransform listArea = CreateImage("ListArea", window, AreaColor);
        SetOffsets(listArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 80f), new Vector2(500f, -100f));

        ScrollRect scroll = listArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = CreateRect("Viewport", listArea);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        // マウスホイールのスクロールを受け取るための透明な画像
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        ItemSlotUI slotTemplate = CreateSlotTemplate(content, font);

        // 詳細（右）
        RectTransform detail = CreateImage("Detail", window, AreaColor);
        SetOffsets(detail, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(530f, 80f), new Vector2(-40f, -100f));

        Image detailIcon = CreateImage("Icon", detail, Color.white).GetComponent<Image>();
        SetRect(detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -130f), new Vector2(180f, 180f));
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;

        TMP_Text detailName = CreateText("Name", detail, font, "アイテム名", 44, TextColor, TextAlignmentOptions.Left);
        SetOffsets(detailName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(250f, -180f), new Vector2(-40f, -80f));

        TMP_Text detailDescription = CreateText("Description", detail, font, "アイテムの説明", 30, SubTextColor, TextAlignmentOptions.TopLeft);
        SetOffsets(detailDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 40f), new Vector2(-40f, -250f));
        detailDescription.textWrappingMode = TextWrappingModes.Normal;

        // 何も持っていないとき
        TMP_Text empty = CreateText("EmptyMessage", window, font, "アイテムを持っていません", 34, SubTextColor, TextAlignmentOptions.Center);
        SetOffsets(empty.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 80f), new Vector2(-40f, -100f));

        // ItemMenuUI の参照を設定する
        var serialized = new SerializedObject(menu);
        serialized.FindProperty("panel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("listRoot").objectReferenceValue = content;
        serialized.FindProperty("slotTemplate").objectReferenceValue = slotTemplate;
        serialized.FindProperty("listScroll").objectReferenceValue = scroll;
        serialized.FindProperty("detailRoot").objectReferenceValue = detail.gameObject;
        serialized.FindProperty("nameText").objectReferenceValue = detailName;
        serialized.FindProperty("descriptionText").objectReferenceValue = detailDescription;
        serialized.FindProperty("iconImage").objectReferenceValue = detailIcon;
        serialized.FindProperty("emptyMessage").objectReferenceValue = empty.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 初期状態は閉じておく
        slotTemplate.gameObject.SetActive(false);
        empty.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"{PrefabPath} を作成しました");
        return prefab;
    }

    /// <summary>
    /// 一覧の1行のひな形を作る
    /// </summary>
    private static ItemSlotUI CreateSlotTemplate(Transform parent, TMP_FontAsset font)
    {
        RectTransform slot = CreateImage("SlotTemplate", parent, new Color(1f, 1f, 1f, 0.06f));
        LayoutElement element = slot.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 72f;
        element.minHeight = 72f;

        Image icon = CreateImage("Icon", slot, Color.white).GetComponent<Image>();
        SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(56f, 56f));
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text name = CreateText("Name", slot, font, "アイテム名", 32, TextColor, TextAlignmentOptions.Left);
        SetOffsets(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(90f, 0f), new Vector2(-16f, 0f));
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;

        ItemSlotUI slotUI = slot.gameObject.AddComponent<ItemSlotUI>();

        var serialized = new SerializedObject(slotUI);
        serialized.FindProperty("background").objectReferenceValue = slot.GetComponent<Image>();
        serialized.FindProperty("icon").objectReferenceValue = icon;
        serialized.FindProperty("nameText").objectReferenceValue = name;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return slotUI;
    }

    //=====================================================================
    // 作成の補助
    //=====================================================================

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string text,
                                       float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if(font != null) { tmp.font = font; }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// 親いっぱいに広げる
    /// </summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// アンカー・位置・サイズで配置する
    /// </summary>
    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>
    /// アンカーと四隅の余白で配置する
    /// </summary>
    private static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    /// <summary>
    /// 日本語フォント（NotoSansJP）を探す。なければ TMP の標準フォントを使う
    /// </summary>
    private static TMP_FontAsset FindJapaneseFont()
    {
        foreach(string guid in AssetDatabase.FindAssets("NotoSansJP t:TMP_FontAsset"))
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if(font != null) { return font; }
        }

        Debug.LogWarning("NotoSansJP のフォントが見つからないため、TMP の標準フォントを使います（日本語が表示されない場合があります）");
        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>
    /// フォルダがなければ作る（"Assets/A/B" の形）
    /// </summary>
    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) { return; }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
