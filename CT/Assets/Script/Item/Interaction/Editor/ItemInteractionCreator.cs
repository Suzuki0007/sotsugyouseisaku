/*********************************************************************/
// * \file   ItemInteractionCreator.cs
// * \brief  アイテム連動オブジェクトのひな形を作るエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hierarchy で右クリック → アイテム連動 から、ひな形を作る
///   ・調べるオブジェクト … ItemInteraction ＋「成功時」「それ以外」の EventSequence
///   ・持っている間だけ切り替え … ItemPossessionSwitch
/// メニューバーの GameObject > アイテム連動 からも作れる
/// </summary>
public static class ItemInteractionCreator
{
    private const string MenuRoot = "GameObject/アイテム連動/";

    //=====================================================================
    // 調べるオブジェクト
    //=====================================================================

    [MenuItem(MenuRoot + "調べるオブジェクト", false, 10)]
    private static void CreateInteraction(MenuCommand command)
    {
        GameObject root = new GameObject("ItemInteraction");

        // 調べられる範囲
        BoxCollider2D area = root.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(1.5f, 1.5f);

        ItemInteraction interaction = root.AddComponent<ItemInteraction>();

        // 成功時：見た目を変える＋会話
        EventSequence success = CreateSequence(root, "成功時");
        success.AddAction(success.gameObject.AddComponent<SetActiveEventAction>());
        success.AddAction(CreateDialogueAction(success.gameObject));

        // それ以外：会話だけ
        EventSequence otherwise = CreateSequence(root, "それ以外");
        otherwise.AddAction(CreateDialogueAction(otherwise.gameObject));

        // ルール1：鍵を持っていたら消費して成功時を実行
        var rule = new ItemInteraction.Rule
        {
            label = "鍵を持っている",
            conditions = new List<ItemCondition> { new ItemCondition { item = FindKeyItem(), count = 1 } },
            consumeItems = true,
            sequence = success,
            markCompleted = true,
        };
        interaction.AddRuleForEditor(rule);
        interaction.SetOtherwiseForEditor(otherwise);

        Finish(root, command);
    }

    //=====================================================================
    // 持っている間だけ切り替え
    //=====================================================================

    [MenuItem(MenuRoot + "持っている間だけ切り替え", false, 11)]
    private static void CreatePossessionSwitch(MenuCommand command)
    {
        GameObject root = new GameObject("ItemPossessionSwitch");
        root.AddComponent<ItemPossessionSwitch>();

        Finish(root, command);
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static EventSequence CreateSequence(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child.AddComponent<EventSequence>();
    }

    /// <summary>
    /// 会話のアクションを作る（Dialogue も同じオブジェクトに付ける）
    /// </summary>
    private static EventAction CreateDialogueAction(GameObject target)
    {
        Dialogue dialogue = target.AddComponent<Dialogue>();
        DialogueEventAction action = target.AddComponent<DialogueEventAction>();

        var serialized = new SerializedObject(action);
        serialized.FindProperty("dialogue").objectReferenceValue = dialogue;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return action;
    }

    /// <summary>
    /// Resources/Items から鍵の ItemData を探す（なければ null）
    /// </summary>
    private static ItemData FindKeyItem()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if(item != null && item.Id == ItemData.KeyItemId) { return item; }
        }
        return null;
    }

    /// <summary>
    /// 右クリックしたオブジェクトの子にする（なければシーンビューの中央に置く）、Undo 登録して選択する
    /// </summary>
    private static void Finish(GameObject root, MenuCommand command)
    {
        GameObject parent = command.context as GameObject;
        if(parent != null)
        {
            GameObjectUtility.SetParentAndAlign(root, parent);
        }
        else if(SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            root.transform.position = new Vector3(pivot.x, pivot.y, 0f);
        }

        Undo.RegisterCreatedObjectUndo(root, $"Create {root.name}");
        Selection.activeObject = root;
    }
}
