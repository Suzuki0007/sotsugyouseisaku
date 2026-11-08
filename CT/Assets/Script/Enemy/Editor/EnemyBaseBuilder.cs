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
/// ・Assets/Prefab/Enemies/Enemy_Base.prefab：新しい作りの敵の共通プレハブ（見た目と当たり判定は古い Enemy.prefab から写す）
/// ・Assets/Data/Enemies/Sample/EnemyData_Sample.asset：サンプルの敵のデータ
/// すでにあるものは作り直さない（プレハブは「作り直す」を選んだときだけ上書き）
public static class EnemyBaseBuilder
{
    public const string PrefabPath = "Assets/Prefab/Enemies/Enemy_Base.prefab";
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
}
