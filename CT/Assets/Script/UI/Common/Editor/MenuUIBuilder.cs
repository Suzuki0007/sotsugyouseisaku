/*********************************************************************/
// * \file   MenuUIBuilder.cs
// * \brief  メニュー系の UI（ボタン・確認ダイアログ・セーブデータ選択画面）を作るエディタ用の補助
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メニュー系の UI を作るエディタ用の補助（タイトル画面・メニュー画面で同じ見た目にする）
/// </summary>
public static class MenuUIBuilder
{
    public static readonly Color ButtonNormal   = new Color(0.12f, 0.12f, 0.16f, 0.92f);
    public static readonly Color ButtonSelected = new Color(0.25f, 0.35f, 0.65f, 1f);
    public static readonly Color PanelColor     = new Color(0.04f, 0.04f, 0.07f, 0.94f);

    //=====================================================================
    // 部品
    //=====================================================================

    /// <summary>
    /// セーブデータ選択画面（全画面）を作る。確認ダイアログも中に作る。最初は非表示
    /// </summary>
    public static SaveSlotPanel CreateSlotPanel(Transform parent)
    {
        var panel = CreateImage("SaveSlotPanel", parent, PanelColor);
        Stretch(panel.rectTransform);

        var title = CreateText("Title", panel.transform, "", 44, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1400, 80));

        var list = CreateRect("Slots", panel.transform);
        SetRect(list, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1100, 620));
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;

        var slots = new List<SaveSlotButton>();
        for(int i = 1; i <= Save.SlotCount; i++) { slots.Add(CreateSlotButton(list, i)); }

        Button back = CreateButton("BackButton", panel.transform, "もどる（Esc）", 32);
        SetRect((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(360, 70));

        ConfirmDialog confirm = CreateConfirmDialog(panel.transform);

        var slotPanel = panel.gameObject.AddComponent<SaveSlotPanel>();
        var so = new SerializedObject(slotPanel);
        so.FindProperty("titleText").objectReferenceValue = title;
        SerializedProperty slotProp = so.FindProperty("slotButtons");
        slotProp.arraySize = slots.Count;
        for(int i = 0; i < slots.Count; i++) { slotProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i]; }
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("confirm").objectReferenceValue = confirm;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        return slotPanel;
    }

    /// <summary>
    /// 「はい／いいえ」の確認ダイアログ（全画面を少し暗くして中央に箱）を作る。最初は非表示
    /// </summary>
    public static ConfirmDialog CreateConfirmDialog(Transform parent)
    {
        var dim = CreateImage("ConfirmDialog", parent, new Color(0, 0, 0, 0.6f));
        Stretch(dim.rectTransform);
        var box = CreateImage("Box", dim.transform, new Color(0.1f, 0.1f, 0.14f, 0.97f));
        SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 400));
        var text = CreateText("Message", box.transform, "", 36, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1040, 200));
        Button yes = CreateButton("YesButton", box.transform, "はい", 36);
        SetRect((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-170, 80), new Vector2(280, 80));
        Button no = CreateButton("NoButton", box.transform, "いいえ", 36);
        SetRect((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(170, 80), new Vector2(280, 80));

        var dialog = dim.gameObject.AddComponent<ConfirmDialog>();
        var so = new SerializedObject(dialog);
        so.FindProperty("messageText").objectReferenceValue = text;
        so.FindProperty("yesButton").objectReferenceValue = yes;
        so.FindProperty("noButton").objectReferenceValue = no;
        so.ApplyModifiedPropertiesWithoutUndo();

        dim.gameObject.SetActive(false);
        return dialog;
    }

    private static SaveSlotButton CreateSlotButton(Transform parent, int slot)
    {
        Button button = CreateButton("Slot" + slot, parent, "", 32);
        Object.DestroyImmediate(button.GetComponentInChildren<TMP_Text>().gameObject);

        var title = CreateText("SlotTitle", button.transform, "スロット " + slot, 38, TextAlignmentOptions.TopLeft);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(40, -22), new Vector2(800, 56), new Vector2(0f, 1f));
        var detail = CreateText("Detail", button.transform, "データなし", 30, TextAlignmentOptions.TopLeft);
        SetRect(detail.rectTransform, new Vector2(0f, 1f), new Vector2(40, -82), new Vector2(1000, 90), new Vector2(0f, 1f));

        var slotButton = button.gameObject.AddComponent<SaveSlotButton>();
        var so = new SerializedObject(slotButton);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("detailText").objectReferenceValue = detail;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slotButton;
    }

    //=====================================================================
    // 基本の部品
    //=====================================================================

    public static Button CreateButton(string name, Transform parent, string label, float fontSize)
    {
        var image = CreateImage(name, parent, Color.white);
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;

        var button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormal;
        colors.highlightedColor = ButtonSelected;
        colors.selectedColor = ButtonSelected;
        colors.pressedColor = new Color(0.18f, 0.25f, 0.5f, 1f);
        colors.disabledColor = new Color(0.12f, 0.12f, 0.16f, 0.35f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var text = CreateText("Label", image.transform, label, fontSize, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return (RectTransform)go.transform;
    }

    /// <summary>
    /// 画面いっぱいの Canvas（1920×1080 基準）を持つオブジェクトを作る
    /// </summary>
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    /// <summary>
    /// 縦に並べるレイアウトを付ける
    /// </summary>
    public static void AddVerticalLayout(RectTransform rt, float spacing)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
    }

    /// <summary>
    /// 場所の表示名の表（Assets/Resources/PlaceNames.asset）がなければ作る
    /// </summary>
    public static PlaceNameTable EnsurePlaceNameTable()
    {
        const string path = "Assets/Resources/" + PlaceNameTable.ResourcePath + ".asset";
        var table = AssetDatabase.LoadAssetAtPath<PlaceNameTable>(path);
        if(table != null) { return table; }

        table = ScriptableObject.CreateInstance<PlaceNameTable>();
        table.AddForEditor("Stage1_1", "ステージ1-1");
        table.AddForEditor("Stage1_2", "ステージ1-2");
        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();
        return table;
    }
}
