/*********************************************************************/
// * \file   SceneChecker.cs
// * \brief  シーンの設定ミスを探すチェック処理
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンの設定ミスを探すチェック処理（SceneCheckWindow から使う）
/// </summary>
///
/// チェックを増やすときは CheckScene / CheckProject に足す
public static class SceneChecker
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum Level
    {
        Error,      // 動かない・セーブが壊れる
        Warning,    // 意図どおりに動かない可能性が高い
        Info,       // 問題ないこともある
    }

    public class Issue
    {
        public Level level;
        public string sceneName;    // プロジェクト全体の問題なら空
        public string scenePath;
        public string objectPath;   // ヒエラルキー上のパス
        public Object target;       // クリックで選択するもの（シーンを閉じると消える）
        public string message;
    }

    //=====================================================================
    // シーンのチェック
    //=====================================================================

    public static void CheckScene(Scene scene, List<Issue> issues)
    {
        var ctx = new Context(scene, issues);

        var allObjects = new List<GameObject>();
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) { allObjects.Add(t.gameObject); }
        }

        // スクリプトが外れている
        foreach(GameObject go in allObjects)
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if(missing > 0) { ctx.Add(Level.Error, go, $"スクリプトが外れたコンポーネントが {missing} 個あります（Missing Script）"); }
        }

        CheckSceneChanges(ctx);
        CheckSpawnPoints(ctx);
        CheckInteractables(ctx);
        CheckTreasures(ctx);
        CheckPickups(ctx);
        CheckItemInteractions(ctx);
        CheckEventZones(ctx);
        CheckEventSequences(ctx);
        CheckActions(ctx);
        CheckSwitches(ctx);
        CheckDialogues(ctx);
    }

    private static void CheckSceneChanges(Context ctx)
    {
        foreach(SceneChange change in ctx.Find<SceneChange>())
        {
            var so = new SerializedObject(change);
            string sceneName = so.FindProperty("sceneName").stringValue;
            string spawnId = so.FindProperty("spawnPointId").stringValue;

            if(string.IsNullOrEmpty(sceneName))
            {
                ctx.Add(Level.Info, change, "SceneChange の遷移先シーンが空です（ボタンなどから ChangeScene を呼ぶ場合は問題ありません）");
                continue;
            }

            if(!SceneLookup.IsInBuild(sceneName))
            {
                ctx.Add(Level.Error, change, $"遷移先シーン「{sceneName}」が Build Settings に入っていません（遷移できません）");
            }

            if(!string.IsNullOrEmpty(spawnId))
            {
                string path = SceneLookup.FindScenePath(sceneName);
                if(path != null && !SceneLookup.SpawnPointIds(path).Contains(spawnId))
                {
                    ctx.Add(Level.Error, change, $"出現位置「{spawnId}」が「{sceneName}」にありません（デフォルトの位置に出ます）");
                }
            }

            if(change.GetComponent<Collider2D>() == null)
            {
                ctx.Add(Level.Info, change, "SceneChange に Collider2D がありません（ぶつかっても遷移しません。ボタンなどから呼ぶ場合は問題ありません）");
            }
        }
    }

    private static void CheckSpawnPoints(Context ctx)
    {
        var seen = new Dictionary<string, PlayerSpawnPoint>();
        foreach(PlayerSpawnPoint point in ctx.Find<PlayerSpawnPoint>())
        {
            string id = point.SpawnPointId;
            if(string.IsNullOrEmpty(id))
            {
                ctx.Add(Level.Warning, point, "PlayerSpawnPoint の ID が空です（どこからも使えません）");
                continue;
            }

            if(seen.ContainsKey(id)) { ctx.Add(Level.Error, point, $"出現位置 ID「{id}」が重なっています（どちらに出るか分かりません）"); }
            else                     { seen.Add(id, point); }
        }
    }

    private static void CheckInteractables(Context ctx)
    {
        foreach(Intaractable target in ctx.Find<Intaractable>())
        {
            // DialogueEventAction から呼ぶための Dialogue は、調べる範囲がなくてよい
            if(target is Dialogue && target.GetComponent<EventAction>() != null) { continue; }

            if(!HasTrigger(target.gameObject))
            {
                ctx.Add(Level.Error, target, $"{target.GetType().Name} に Is Trigger をオンにした Collider2D がありません（調べられません）");
            }
        }
    }

    private static void CheckTreasures(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(Treasure treasure in ctx.Find<Treasure>())
        {
            var so = new SerializedObject(treasure);
            string id = so.FindProperty("id").stringValue;

            if(string.IsNullOrEmpty(id))      { ctx.Add(Level.Error, treasure, "宝箱の Id が空です（他の宝箱と開いた記録が混ざります）"); }
            else if(!seen.Add(id))            { ctx.Add(Level.Error, treasure, $"宝箱の Id「{id}」が重なっています（片方を開けると両方開きます）"); }

            if(so.FindProperty("beforeRenderer").objectReferenceValue == null &&
               so.FindProperty("openAnimator").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, treasure, "宝箱の Before Renderer が空です（開けたときにエラーになります）");
            }
        }
    }

    private static void CheckPickups(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(ItemPickup pickup in ctx.Find<ItemPickup>())
        {
            var so = new SerializedObject(pickup);
            if(so.FindProperty("item").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, pickup, "ItemPickup の Item が空です");
            }

            string key = ItemPickup.MakeSaveKey(pickup.gameObject, so.FindProperty("pickupId").stringValue);
            if(!seen.Add(key))
            {
                ctx.Add(Level.Error, pickup, "同じ名前の ItemPickup があります（片方を拾うと両方消えます）。Pickup Id に別々の名前を入れてください");
            }
        }
    }

    private static void CheckItemInteractions(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(ItemInteraction interaction in ctx.Find<ItemInteraction>())
        {
            var so = new SerializedObject(interaction);

            if(so.FindProperty("saveCompleted").boolValue)
            {
                string id = so.FindProperty("saveId").stringValue;
                string key = string.IsNullOrEmpty(id) ? HierarchyPath(interaction.transform) : id;
                if(!seen.Add(key))
                {
                    ctx.Add(Level.Error, interaction, "同じ名前の ItemInteraction があります（完了状態が混ざります）。Save Id に別々の名前を入れてください");
                }
            }

            SerializedProperty rules = so.FindProperty("rules");
            if(rules.arraySize == 0 && so.FindProperty("otherwise").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, interaction, "ItemInteraction にルールも Otherwise もありません（何も起きません）");
            }

            for(int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                string label = rule.FindPropertyRelative("label").stringValue;
                string name = $"ルール{i + 1}" + (string.IsNullOrEmpty(label) ? "" : $"「{label}」");

                if(rule.FindPropertyRelative("sequence").objectReferenceValue == null)
                {
                    ctx.Add(Level.Warning, interaction, $"{name} の Sequence が空です（条件を満たしても何も起きません）");
                }

                if(HasNullRef(rule.FindPropertyRelative("conditions"), "item"))
                {
                    ctx.Add(Level.Warning, interaction, $"{name} に Item が空の条件があります（常に満たす扱いになります）");
                }

                if(HasNullRef(rule.FindPropertyRelative("flagConditions"), "flag"))
                {
                    ctx.Add(Level.Warning, interaction, $"{name} に Flag が空の条件があります（常に満たす扱いになります）");
                }
            }
        }
    }

    private static void CheckEventZones(Context ctx)
    {
        foreach(EventZone zone in ctx.Find<EventZone>())
        {
            var so = new SerializedObject(zone);
            if(so.FindProperty("eventSequence").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, zone, "EventZone の Event Sequence が空です（入っても何も起きません）");
            }

            if(!HasTrigger(zone.gameObject))
            {
                ctx.Add(Level.Error, zone, "EventZone に Is Trigger をオンにした Collider2D がありません");
            }
        }
    }

    private static void CheckEventSequences(Context ctx)
    {
        foreach(EventSequence sequence in ctx.Find<EventSequence>())
        {
            var so = new SerializedObject(sequence);
            SerializedProperty actions = so.FindProperty("actions");
            bool sequential = so.FindProperty("playMode").enumValueIndex == (int)EventSequence.PlayMode.Sequential;

            var registered = new List<EventAction>();
            bool hasNull = false;
            for(int i = 0; i < actions.arraySize; i++)
            {
                var action = actions.GetArrayElementAtIndex(i).objectReferenceValue as EventAction;
                if(action == null) { hasNull = true; } else { registered.Add(action); }
            }

            EventAction[] own = sequence.GetComponents<EventAction>();

            if(actions.arraySize == 0)
            {
                if(own.Length == 0) { ctx.Add(Level.Warning, sequence, "EventSequence に実行する部品がありません"); }
                else                { registered.AddRange(own); }
            }
            else
            {
                foreach(EventAction action in own)
                {
                    if(!registered.Contains(action))
                    {
                        ctx.Add(Level.Warning, sequence, $"{action.GetType().Name} が付いているのに Actions に入っていません（実行されません）");
                    }
                }
            }

            if(hasNull) { ctx.Add(Level.Warning, sequence, "EventSequence の Actions に空の欄があります"); }

            // 順番に実行中に、自分（か親）を非表示にすると途中で止まる
            if(!sequential) { continue; }

            for(int i = 0; i < registered.Count; i++)
            {
                if(!(registered[i] is SetActiveEventAction setActive)) { continue; }
                if(i == registered.Count - 1) { continue; }  // 最後なら後ろに止まるものがない

                var aso = new SerializedObject(setActive);
                SerializedProperty hide = aso.FindProperty("hide");
                for(int j = 0; j < hide.arraySize; j++)
                {
                    var go = hide.GetArrayElementAtIndex(j).objectReferenceValue as GameObject;
                    if(go != null && sequence.transform.IsChildOf(go.transform))
                    {
                        ctx.Add(Level.Error, sequence, $"SetActiveEventAction が EventSequence 自身（または親）の「{go.name}」を非表示にするため、後ろの部品が実行されません。最後に移すか、別のオブジェクトに分けてください");
                    }
                }
            }
        }
    }

    private static void CheckActions(Context ctx)
    {
        foreach(DialogueEventAction action in ctx.Find<DialogueEventAction>())
        {
            if(new SerializedObject(action).FindProperty("dialogue").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, action, "DialogueEventAction の Dialogue が空です（会話が出ません）");
            }
        }

        foreach(SetFlagEventAction action in ctx.Find<SetFlagEventAction>())
        {
            SerializedProperty flags = new SerializedObject(action).FindProperty("flags");
            if(flags.arraySize == 0)               { ctx.Add(Level.Warning, action, "SetFlagEventAction にフラグがありません"); }
            else if(HasNullRef(flags, "flag"))     { ctx.Add(Level.Warning, action, "SetFlagEventAction に Flag が空の欄があります"); }
        }
    }

    private static void CheckSwitches(Context ctx)
    {
        foreach(FlagSwitch sw in ctx.Find<FlagSwitch>())
        {
            var so = new SerializedObject(sw);
            CheckSwitchTargets(ctx, sw, so);
            if(HasNullRef(so.FindProperty("flagConditions"), "flag")) { ctx.Add(Level.Warning, sw, "FlagSwitch に Flag が空の条件があります"); }
            if(HasNullRef(so.FindProperty("itemConditions"), "item")) { ctx.Add(Level.Warning, sw, "FlagSwitch に Item が空の条件があります"); }
        }

        foreach(ItemPossessionSwitch sw in ctx.Find<ItemPossessionSwitch>())
        {
            var so = new SerializedObject(sw);
            CheckSwitchTargets(ctx, sw, so);
            if(HasNullRef(so.FindProperty("conditions"), "item")) { ctx.Add(Level.Warning, sw, "ItemPossessionSwitch に Item が空の条件があります"); }
        }
    }

    private static void CheckSwitchTargets(Context ctx, Component sw, SerializedObject so)
    {
        SerializedProperty met = so.FindProperty("activeWhenMet");
        SerializedProperty notMet = so.FindProperty("activeWhenNotMet");

        if(met.arraySize == 0 && notMet.arraySize == 0)
        {
            ctx.Add(Level.Warning, sw, $"{sw.GetType().Name} に切り替えるオブジェクトがありません");
        }

        if(ContainsRef(met, sw.gameObject) || ContainsRef(notMet, sw.gameObject))
        {
            ctx.Add(Level.Error, sw, $"{sw.GetType().Name} の切り替え対象に自分自身が入っています（非表示になると切り替えが止まります）");
        }
    }

    private static void CheckDialogues(Context ctx)
    {
        List<Dialogue> dialogues = ctx.Find<Dialogue>();
        foreach(Dialogue dialogue in dialogues)
        {
            var so = new SerializedObject(dialogue);
            if(so.FindProperty("dialogueData").objectReferenceValue == null &&
               string.IsNullOrEmpty(so.FindProperty("dialogueText").stringValue))
            {
                ctx.Add(Level.Warning, dialogue, "Dialogue に会話データも Dialogue Text もありません（何も表示されません）");
            }
        }

        if(dialogues.Count > 0 && ctx.Find<DialogueManager>().Count == 0)
        {
            ctx.Add(Level.Warning, null, "シーンに DialogueManager がないため、会話が旧方式（1文だけ・立ち絵なし・閉じるまで待てない）になります。Assets/Prefab/UI/DialogueSystem.prefab を置いてください");
        }
    }

    //=====================================================================
    // プロジェクト全体のチェック
    //=====================================================================

    public static void CheckProject(List<Issue> issues)
    {
        // Build Settings
        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if(scene.enabled && !File.Exists(scene.path))
            {
                issues.Add(new Issue { level = Level.Error, message = $"Build Settings のシーン「{scene.path}」のファイルがありません" });
            }
        }

        // アイテム
        string folder = "Assets/Resources/" + ItemData.ResourcesFolder + "/";
        var itemIds = new Dictionary<string, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if(item == null) { continue; }

            if(!path.StartsWith(folder))
            {
                issues.Add(new Issue { level = Level.Error, target = item, message = $"ItemData「{item.name}」が {folder} の外にあります（セーブから復元できません）" });
            }

            if(itemIds.TryGetValue(item.Id, out string other))
            {
                issues.Add(new Issue { level = Level.Error, target = item, message = $"アイテム ID「{item.Id}」が {other} と重なっています" });
            }
            else { itemIds.Add(item.Id, path); }
        }

        // フラグ
        var flagIds = new Dictionary<string, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:GameFlag"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(path);
            if(flag == null) { continue; }

            if(flagIds.TryGetValue(flag.Id, out string other))
            {
                issues.Add(new Issue { level = Level.Error, target = flag, message = $"フラグ ID「{flag.Id}」が {other} と重なっています（同じフラグとして扱われます）" });
            }
            else { flagIds.Add(flag.Id, path); }
        }

        // 会話
        foreach(string guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
            if(data != null && (data.Lines == null || data.Lines.Length == 0))
            {
                issues.Add(new Issue { level = Level.Warning, target = data, message = $"会話データ「{data.name}」に文がありません" });
            }
        }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private class Context
    {
        private readonly Scene scene;
        private readonly List<Issue> issues;
        private readonly Dictionary<System.Type, object> cache = new Dictionary<System.Type, object>();

        public Context(Scene scene, List<Issue> issues)
        {
            this.scene = scene;
            this.issues = issues;
        }

        public List<T> Find<T>() where T : Component
        {
            if(cache.TryGetValue(typeof(T), out object found)) { return (List<T>)found; }

            var list = new List<T>();
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<T>(true));
            }
            cache[typeof(T)] = list;
            return list;
        }

        public void Add(Level level, Object target, string message)
        {
            GameObject go = target as GameObject;
            if(go == null && target is Component c) { go = c.gameObject; }

            issues.Add(new Issue
            {
                level = level,
                sceneName = scene.name,
                scenePath = scene.path,
                objectPath = go != null ? HierarchyPath(go.transform) : "",
                target = go,
                message = message,
            });
        }
    }

    private static bool HasTrigger(GameObject go)
    {
        foreach(Collider2D col in go.GetComponents<Collider2D>())
        {
            if(col.isTrigger) { return true; }
        }
        return false;
    }

    /// <summary>
    /// 配列の要素の relative の参照が空のものがあるか
    /// </summary>
    private static bool HasNullRef(SerializedProperty array, string relative)
    {
        if(array == null || !array.isArray) { return false; }

        for(int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty p = array.GetArrayElementAtIndex(i).FindPropertyRelative(relative);
            if(p != null && p.objectReferenceValue == null) { return true; }
        }
        return false;
    }

    private static bool ContainsRef(SerializedProperty array, Object target)
    {
        for(int i = 0; i < array.arraySize; i++)
        {
            if(array.GetArrayElementAtIndex(i).objectReferenceValue == target) { return true; }
        }
        return false;
    }

    public static string HierarchyPath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }
}
