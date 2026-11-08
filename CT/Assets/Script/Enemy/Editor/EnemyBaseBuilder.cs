/*********************************************************************/
// * \file   EnemyBaseBuilder.cs
// * \brief  敵の共通プレハブ（Enemy_Base）とサンプルのデータを作る
// *
// * \author
/*********************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > 敵 > 共通プレハブとサンプルを作る
/// </summary>
///
/// ・Assets/Resources/Enemies/Enemy_Base.prefab：新しい作りの敵の共通プレハブ（見た目と当たり判定は古い Enemy.prefab から写す）
/// ・Assets/Data/Enemies/Sample/EnemyData_Sample.asset：サンプルの敵のデータ
/// すでにあるものは作り直さない（プレハブは「作り直す」を選んだときだけ上書き）
public static class EnemyBaseBuilder
{
    // 「仲間を呼ぶ」で実行中に読み込む（Enemy.Spawn）ので Resources に置く
    public const string PrefabPath = "Assets/Resources/Enemies/Enemy_Base.prefab";
    public const string SampleDataPath = "Assets/Data/Enemies/Sample/EnemyData_Sample.asset";
    private const string OldPrefabPath = "Assets/Prefab/Enemies/Enemy.prefab";

    [MenuItem("Tools/敵/共通プレハブとサンプルを作る", false, 200)]
    private static void BuildFromMenu()
    {
        bool exists = File.Exists(PrefabPath);
        if(exists && !EditorUtility.DisplayDialog("共通プレハブ", "Enemy_Base.prefab はもうあります。作り直しますか？\n（プレハブに加えた変更は消えます。シーンに置いた敵の設定は残ります）", "作り直す", "やめる"))
        {
            return;
        }

        GameObject prefab = Build(true);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    /// <summary>
    /// サンプルのデータと共通プレハブを作る
    /// </summary>
    /// <param name="overwritePrefab"> プレハブがあっても作り直すか </param>
    public static GameObject Build(bool overwritePrefab)
    {
        EnemyData sample = CreateSampleData();

        if(!overwritePrefab && File.Exists(PrefabPath)) { return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); }

        var root = new GameObject("Enemy_Base");
        try
        {
            root.tag = "Enemies";
            root.layer = LayerMask.NameToLayer("Enemies");

            // 見た目（古い Enemy.prefab の絵と描画順を使う）
            var sr = root.AddComponent<SpriteRenderer>();
            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.49f;
            col.offset = new Vector2(0.0f, -0.6f);

            GameObject old = AssetDatabase.LoadAssetAtPath<GameObject>(OldPrefabPath);
            if(old != null)
            {
                SpriteRenderer oldSr = old.GetComponent<SpriteRenderer>();
                if(oldSr != null)
                {
                    sr.sprite = oldSr.sprite;
                    sr.sharedMaterial = oldSr.sharedMaterial;
                    sr.sortingLayerID = oldSr.sortingLayerID;
                    sr.sortingOrder = oldSr.sortingOrder;
                }

                CircleCollider2D oldCol = old.GetComponent<CircleCollider2D>();
                if(oldCol != null) { col.radius = oldCol.radius; col.offset = oldCol.offset; }
            }

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0.0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var health = root.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = 3;
            hso.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<SpriteAnimator>();
            root.AddComponent<CharacterAnimLink>();

            var enemy = root.AddComponent<Enemy>();
            var eso = new SerializedObject(enemy);
            eso.FindProperty("data").objectReferenceValue = sample;
            eso.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<EnemyBrain>();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// 既存の共通プレハブに、あとから足した部品（EnemyBrain など）を付け足す（作り直さない）
    /// </summary>
    public static void EnsurePrefabComponents()
    {
        if(!File.Exists(PrefabPath)) { return; }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            bool changed = false;
            if(root.GetComponent<EnemyBrain>() == null) { root.AddComponent<EnemyBrain>(); changed = true; }
            if(changed) { PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static EnemyData CreateSampleData()
    {
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(SampleDataPath);
        if(data != null) { return data; }

        Directory.CreateDirectory(Path.GetDirectoryName(SampleDataPath));
        data = ScriptableObject.CreateInstance<EnemyData>();

        var so = new SerializedObject(data);
        so.FindProperty("displayName").stringValue = "サンプルの敵";
        so.FindProperty("memo").stringValue = "共通プレハブ用のサンプル。複製して使う";
        so.FindProperty("deathEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EffectData>("Assets/Data/Effect/Sample/Fx_Sample_Explosion.asset");
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, SampleDataPath);
        AssetDatabase.SaveAssets();
        return data;
    }

    //=====================================================================
    // Hierarchy の右クリック
    //=====================================================================

    [MenuItem("GameObject/敵/敵を置く", false, 20)]
    private static void PlaceEnemy(MenuCommand command)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(prefab == null) { prefab = Build(false); }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);

        // 選んでいたオブジェクトがなければ、シーンビューの真ん中に置く
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x, pivot.y, 0.0f);
        }

        Undo.RegisterCreatedObjectUndo(go, "敵を置く");
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/敵/敵の通行設定エリア", false, 21)]
    private static void PlaceNavArea(MenuCommand command)
    {
        var go = new GameObject("敵の通行設定エリア");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3.0f, 3.0f);
        go.AddComponent<EnemyNavArea>();
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);

        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x, pivot.y, 0.0f);
        }

        Undo.RegisterCreatedObjectUndo(go, "敵の通行設定エリア");
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/敵/攻撃の発生源（見えない敵）", false, 22)]
    private static void PlaceEmitter(MenuCommand command)
    {
        var go = new GameObject("攻撃の発生源");
        var emitter = go.AddComponent<AttackEmitter>();

        // 動く範囲（この中にプレイヤーがいる間だけ攻撃してくる）
        var area = new GameObject("動く範囲");
        area.transform.SetParent(go.transform, false);
        var box = area.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(8.0f, 6.0f);

        var so = new SerializedObject(emitter);
        so.FindProperty("condition").FindPropertyRelative("area").objectReferenceValue = box;
        so.ApplyModifiedPropertiesWithoutUndo();

        PlaceAtView(go, command, "攻撃の発生源");
    }

    [MenuItem("GameObject/敵/攻撃で作動するスイッチ", false, 23)]
    private static void PlaceAttackSwitch(MenuCommand command)
    {
        var go = new GameObject("攻撃で作動するスイッチ");
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.0f, 1.0f);
        var sw = go.AddComponent<AttackSwitch>();
        var sequence = go.AddComponent<EventSequence>();

        var so = new SerializedObject(sw);
        so.FindProperty("sequence").objectReferenceValue = sequence;
        so.ApplyModifiedPropertiesWithoutUndo();

        PlaceAtView(go, command, "攻撃で作動するスイッチ");
    }

    [MenuItem("GameObject/敵/グループ（全滅・撃破の順番）", false, 26)]
    private static void PlaceGroup(MenuCommand command)
    {
        var go = new GameObject("敵のグループ");
        var group = go.AddComponent<EnemyGroup>();

        // 全滅したときのイベント
        var child = new GameObject("倒したとき");
        child.transform.SetParent(go.transform, false);
        var sequence = child.AddComponent<EventSequence>();

        var so = new SerializedObject(group);
        so.FindProperty("onDefeated").objectReferenceValue = sequence;

        // 選んでいた敵をメンバーに入れる（複数選んでから作ると、選んだ順に入る）
        SerializedProperty members = so.FindProperty("members");
        foreach(GameObject selected in Selection.gameObjects)
        {
            Enemy enemy = selected != null ? selected.GetComponent<Enemy>() : null;
            if(enemy == null) { continue; }
            members.arraySize++;
            members.GetArrayElementAtIndex(members.arraySize - 1).objectReferenceValue = enemy;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        PlaceAtView(go, new MenuCommand(null), "敵のグループ");
    }

    [MenuItem("GameObject/敵/部位を追加（選んでいる敵の子に）", false, 25)]
    private static void AddPart(MenuCommand command)
    {
        var parent = command.context as GameObject;
        if(parent == null) { parent = Selection.activeGameObject; }
        Enemy enemy = parent != null ? parent.GetComponentInParent<Enemy>() : null;
        if(enemy == null)
        {
            EditorUtility.DisplayDialog("部位を追加", "先に Hierarchy で敵（Enemy が付いたオブジェクト）を選んでください。", "OK");
            return;
        }

        var go = new GameObject("部位");
        go.transform.SetParent(enemy.transform, false);
        go.transform.localPosition = new Vector3(0.6f, 0.0f, 0.0f);
        go.tag = "Enemies";
        go.layer = LayerMask.NameToLayer("Enemies");

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        // 見た目（丸）。差し替えてよい
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        SpriteRenderer body = enemy.GetComponent<SpriteRenderer>();
        sr.sortingOrder = body != null ? body.sortingOrder + 1 : 6;

        go.AddComponent<EnemyPart>();

        Undo.RegisterCreatedObjectUndo(go, "部位を追加");
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/敵/戦闘エリア（追跡・逃げる戦闘）", false, 24)]
    private static void PlaceBattleArea(MenuCommand command)
    {
        var go = new GameObject("戦闘エリア");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.0f, 4.0f);
        var battle = go.AddComponent<BattleArea>();

        // ゴール（入ったら勝ち）
        var goalGo = new GameObject("ゴール");
        goalGo.transform.SetParent(go.transform, false);
        goalGo.transform.localPosition = new Vector3(12.0f, 0.0f, 0.0f);
        var goal = goalGo.AddComponent<BoxCollider2D>();
        goal.isTrigger = true;
        goal.size = new Vector2(2.0f, 3.0f);

        // やり直す位置
        var restart = new GameObject("やり直す位置");
        restart.transform.SetParent(go.transform, false);

        // 始まったとき・勝ったとき・負けたとき のイベント
        EventSequence MakeSequence(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(go.transform, false);
            return child.AddComponent<EventSequence>();
        }

        var so = new SerializedObject(battle);
        so.FindProperty("goal").objectReferenceValue = goal;
        so.FindProperty("restartPoint").objectReferenceValue = restart.transform;
        so.FindProperty("onStart").objectReferenceValue = MakeSequence("始まったとき");
        so.FindProperty("onWin").objectReferenceValue = MakeSequence("勝ったとき");
        so.FindProperty("onLose").objectReferenceValue = MakeSequence("負けたとき");
        so.ApplyModifiedPropertiesWithoutUndo();

        PlaceAtView(go, command, "戦闘エリア");
    }

    private static void PlaceAtView(GameObject go, MenuCommand command, string undoName)
    {
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        if(command.context == null && SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x, pivot.y, 0.0f);
        }
        Undo.RegisterCreatedObjectUndo(go, undoName);
        Selection.activeGameObject = go;
    }
}
