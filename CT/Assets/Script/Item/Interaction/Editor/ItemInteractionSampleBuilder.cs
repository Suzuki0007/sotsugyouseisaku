/*********************************************************************/
// * \file   ItemInteractionSampleBuilder.cs
// * \brief  アイテム連動のサンプルを開いているシーンに配置するエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > アイテム連動 > サンプルを配置 で、開いているシーンのシーンビュー中央にサンプル一式を置く
///
/// 左から
///   鍵・宝石・ランタン（拾える）
///   扉        … 鍵を持って調べると、鍵を消費して扉が消える（完了・保存）
///   像        … 宝石を持って調べると、宝石を消費して像の形が変わり、隠し通路が出る（完了・保存）
///   明かり    … ランタンを持っている間だけ明るくなる
///
/// すべて「ItemInteractionSample」の下にまとめるので、不要になったらそれを消せばよい
/// 会話は DialogueManager があるシーン（Stage1_2 など）でだけ表示される
/// </summary>
public static class ItemInteractionSampleBuilder
{
    private const string RootName     = "ItemInteractionSample";
    private const string SampleFolder = "Assets/Resources/" + ItemData.ResourcesFolder + "/Sample";
    private const int    SortingOrder = 5;

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("Tools/アイテム連動/サンプルを配置（開いているシーン）")]
    private static void Build()
    {
        if(EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("アイテム連動", "再生中は配置できません。再生を止めてから実行してください。", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if(existing != null)
        {
            if(!EditorUtility.DisplayDialog("アイテム連動", "サンプルがすでにあります。置き直しますか？", "置き直す", "やめる")) { return; }
            Undo.DestroyObjectImmediate(existing);
        }

        squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // アイテム
        ItemData key     = FindOrCreateKey();
        ItemData gem     = FindOrCreateItem("Item_SampleGem", "sample_gem", "青い宝石", "像のくぼみにぴったりはまりそうな宝石。", circleSprite);
        ItemData lantern = FindOrCreateItem("Item_SampleLantern", "sample_lantern", "ランタン", "暗い場所を照らせる。持っているだけで周りが明るくなる。", squareSprite);

        // 配置
        GameObject root = new GameObject(RootName);
        Vector3 center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        root.transform.position = new Vector3(center.x, center.y, 0f);

        CreatePickup(root.transform, "拾える_鍵",       new Vector2(-6.0f, 0f), key,     new Color(1f, 0.85f, 0.2f), "Sample_Key");
        CreatePickup(root.transform, "拾える_宝石",     new Vector2(-4.5f, 0f), gem,     new Color(0.3f, 0.7f, 1f),  "Sample_Gem");
        CreatePickup(root.transform, "拾える_ランタン", new Vector2(-3.0f, 0f), lantern, new Color(1f, 0.55f, 0.15f), "Sample_Lantern");

        CreateDoor(root.transform, new Vector2(0f, 0f), key);
        CreateStatue(root.transform, new Vector2(3.5f, 0f), gem);
        CreateLight(root.transform, new Vector2(7.5f, 0f), lantern);

        Undo.RegisterCreatedObjectUndo(root, "Create Item Interaction Sample");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeObject = root;
        if(SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.FrameSelected(); }

        Debug.Log("アイテム連動のサンプルを配置しました。鍵・宝石・ランタンを拾って、扉・像を調べてみてください（シーンの保存を忘れずに）");
    }

    //=====================================================================
    // サンプル
    //=====================================================================

    /// <summary>
    /// 拾えるアイテム
    /// </summary>
    private static void CreatePickup(Transform parent, string name, Vector2 position, ItemData item, Color color, string pickupId)
    {
        GameObject go = CreateSprite(name, parent, position, new Vector2(0.6f, 0.6f), color, item == null || item.Icon == null ? squareSprite : item.Icon);

        BoxCollider2D area = go.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size *= 2f;

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        var serialized = new SerializedObject(pickup);
        serialized.FindProperty("item").objectReferenceValue = item;
        serialized.FindProperty("pickupId").stringValue = pickupId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 扉：鍵を持って調べると、鍵を消費して扉が消える
    /// </summary>
    private static void CreateDoor(Transform parent, Vector2 position, ItemData key)
    {
        GameObject area = CreateInteractionArea("扉（鍵で開く）", parent, position, new Vector2(2.2f, 2.6f));

        // 扉本体（通れない）
        GameObject door = CreateSprite("扉", area.transform, Vector2.zero, new Vector2(1f, 2f), new Color(0.45f, 0.28f, 0.15f), squareSprite);
        door.AddComponent<BoxCollider2D>();

        EventSequence success = CreateSequence(area, "成功時");
        success.AddAction(CreateSetActive(success.gameObject, new GameObject[0], new[] { door }));
        success.AddAction(CreateDialogue(success.gameObject, "鍵を使って扉を開けた。"));

        EventSequence otherwise = CreateSequence(area, "それ以外");
        otherwise.AddAction(CreateDialogue(otherwise.gameObject, "鍵がかかっている。"));

        ItemInteraction interaction = area.GetComponent<ItemInteraction>();
        interaction.AddRuleForEditor(new ItemInteraction.Rule
        {
            label = "鍵を持っている",
            conditions = new List<ItemCondition> { new ItemCondition { item = key, count = 1 } },
            consumeItems = true,
            sequence = success,
            markCompleted = true,
        });
        interaction.SetOtherwiseForEditor(otherwise);
        interaction.SetAfterCompletedForEditor(null, "Sample_Door");
    }

    /// <summary>
    /// 像：宝石を持って調べると、宝石を消費して像の形が変わり、隠し通路が出る
    /// </summary>
    private static void CreateStatue(Transform parent, Vector2 position, ItemData gem)
    {
        GameObject area = CreateInteractionArea("像（宝石をはめる）", parent, position, new Vector2(2f, 2f));

        GameObject statue = CreateSprite("像", area.transform, Vector2.zero, new Vector2(1f, 1.4f), new Color(0.6f, 0.6f, 0.65f), squareSprite);
        statue.AddComponent<BoxCollider2D>();

        GameObject passage = CreateSprite("隠し通路", area.transform, new Vector2(0f, -1.8f), new Vector2(1.2f, 1f), new Color(0.1f, 0.1f, 0.15f), squareSprite);
        passage.SetActive(false);

        EventSequence success = CreateSequence(area, "成功時");
        success.AddAction(CreateSpriteChange(success.gameObject, statue.GetComponent<SpriteRenderer>(), circleSprite));
        success.AddAction(CreateSetActive(success.gameObject, new[] { passage }, new GameObject[0]));
        success.AddAction(CreateDialogue(success.gameObject, "宝石をはめると、像が光り、足元に通路が現れた。"));

        EventSequence otherwise = CreateSequence(area, "それ以外");
        otherwise.AddAction(CreateDialogue(otherwise.gameObject, "像の胸に、何かをはめ込めそうなくぼみがある。"));

        EventSequence after = CreateSequence(area, "完了後");
        after.AddAction(CreateDialogue(after.gameObject, "像は静かに光っている。"));

        ItemInteraction interaction = area.GetComponent<ItemInteraction>();
        interaction.AddRuleForEditor(new ItemInteraction.Rule
        {
            label = "宝石を持っている",
            conditions = new List<ItemCondition> { new ItemCondition { item = gem, count = 1 } },
            consumeItems = true,
            sequence = success,
            markCompleted = true,
        });
        interaction.SetOtherwiseForEditor(otherwise);
        interaction.SetAfterCompletedForEditor(after, "Sample_Statue");
    }

    /// <summary>
    /// 明かり：ランタンを持っている間だけ明るくなる
    /// </summary>
    private static void CreateLight(Transform parent, Vector2 position, ItemData lantern)
    {
        GameObject switchObject = new GameObject("明かり（ランタンを持っている間）");
        switchObject.transform.SetParent(parent, false);
        switchObject.transform.localPosition = position;

        GameObject bright = CreateSprite("明るい", switchObject.transform, Vector2.zero, new Vector2(3f, 3f), new Color(1f, 0.9f, 0.4f, 0.45f), circleSprite);
        GameObject dark   = CreateSprite("暗い",   switchObject.transform, Vector2.zero, new Vector2(3f, 3f), new Color(0f, 0f, 0.05f, 0.6f), circleSprite);

        ItemPossessionSwitch possession = switchObject.AddComponent<ItemPossessionSwitch>();
        var serialized = new SerializedObject(possession);

        SerializedProperty conditions = serialized.FindProperty("conditions");
        conditions.arraySize = 1;
        SerializedProperty condition = conditions.GetArrayElementAtIndex(0);
        condition.FindPropertyRelative("item").objectReferenceValue = lantern;
        condition.FindPropertyRelative("count").intValue = 1;
        condition.FindPropertyRelative("notHave").boolValue = false;

        SetObjectList(serialized.FindProperty("activeWhenMet"), new Object[] { bright });
        SetObjectList(serialized.FindProperty("activeWhenNotMet"), new Object[] { dark });
        serialized.ApplyModifiedPropertiesWithoutUndo();

        bright.SetActive(false);
    }

    //=====================================================================
    // 部品の作成
    //=====================================================================

    /// <summary>
    /// 調べる範囲（BoxCollider2D のトリガー）と ItemInteraction を持つオブジェクト
    /// </summary>
    private static GameObject CreateInteractionArea(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        BoxCollider2D area = go.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = size;

        go.AddComponent<ItemInteraction>();
        return go;
    }

    private static EventSequence CreateSequence(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child.AddComponent<EventSequence>();
    }

    private static EventAction CreateDialogue(GameObject target, string text)
    {
        Dialogue dialogue = target.AddComponent<Dialogue>();
        var dialogueSerialized = new SerializedObject(dialogue);
        dialogueSerialized.FindProperty("dialogueText").stringValue = text;
        dialogueSerialized.ApplyModifiedPropertiesWithoutUndo();

        DialogueEventAction action = target.AddComponent<DialogueEventAction>();
        var actionSerialized = new SerializedObject(action);
        actionSerialized.FindProperty("dialogue").objectReferenceValue = dialogue;
        actionSerialized.ApplyModifiedPropertiesWithoutUndo();

        return action;
    }

    private static EventAction CreateSetActive(GameObject target, GameObject[] show, GameObject[] hide)
    {
        SetActiveEventAction action = target.AddComponent<SetActiveEventAction>();
        var serialized = new SerializedObject(action);
        SetObjectList(serialized.FindProperty("show"), show);
        SetObjectList(serialized.FindProperty("hide"), hide);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return action;
    }

    private static EventAction CreateSpriteChange(GameObject target, SpriteRenderer renderer, Sprite sprite)
    {
        SpriteChangeEventAction action = target.AddComponent<SpriteChangeEventAction>();
        var serialized = new SerializedObject(action);
        serialized.FindProperty("target").objectReferenceValue = renderer;
        serialized.FindProperty("sprite").objectReferenceValue = sprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return action;
    }

    /// <summary>
    /// 指定サイズ（ワールド単位）の色付きスプライトを作る
    /// </summary>
    private static GameObject CreateSprite(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = SortingOrder;

        if(sprite != null)
        {
            Vector2 spriteSize = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        }

        return go;
    }

    private static void SetObjectList(SerializedProperty list, Object[] values)
    {
        list.arraySize = values.Length;
        for(int i = 0; i < values.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    //=====================================================================
    // アイテム
    //=====================================================================

    /// <summary>
    /// 鍵の ItemData を探す（なければ Item_Key を作る）
    /// </summary>
    private static ItemData FindOrCreateKey()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if(item != null && item.Id == ItemData.KeyItemId) { return item; }
        }

        return FindOrCreateItem("Item_Key", ItemData.KeyItemId, "鍵", "どこかの扉や宝箱を開けられそうな鍵。", null, true);
    }

    /// <summary>
    /// サンプル用の ItemData を探す（なければ作る）
    /// </summary>
    private static ItemData FindOrCreateItem(string assetName, string id, string displayName, string description, Sprite icon, bool isKey = false)
    {
        string path = SampleFolder + "/" + assetName + ".asset";
        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if(existing != null) { return existing; }

        EnsureFolder(SampleFolder);

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        var serialized = new SerializedObject(item);
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
        serialized.FindProperty("icon").objectReferenceValue = icon;
        serialized.FindProperty("isKey").boolValue = isKey;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, path);
        AssetDatabase.SaveAssets();
        return item;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) { return; }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
