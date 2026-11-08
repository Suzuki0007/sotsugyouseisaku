/*********************************************************************/
// * \file   EnemySampleBuilder.cs
// * \brief  敵のサンプル（攻撃・行動パターン・敵のデータ）を作る
// *
// * \author
/*********************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > 敵 > サンプルを作る
/// </summary>
///
/// Assets/Data/Enemies/Sample/ に、攻撃（なぎ払い・突進・ビーム・足元の爆発・リング・狙い撃ち）、
/// 行動パターン、敵のデータを作る。同じ名前のものがあれば作らない（手で直したものを上書きしない）
public static class EnemySampleBuilder
{
    private const string Folder = "Assets/Data/Enemies/Sample";
    private const string AttackFolder = Folder + "/Attacks";
    private const string PatternFolder = Folder + "/Patterns";

    [MenuItem("Tools/敵/サンプルを作る（攻撃・行動パターン・敵のデータ）", false, 201)]
    private static void BuildFromMenu()
    {
        Build();
        EditorUtility.DisplayDialog("敵のサンプル", $"{Folder} にサンプルを作りました（すでにあるものはそのままです）。\nHierarchy 右クリック > 敵 > 敵を置く で置いて、敵のデータを差し替えて試してください。", "OK");
    }

    public static void Build()
    {
        EnemyBaseBuilder.Build(false);
        EnemyBaseBuilder.EnsurePrefabComponents();

        Directory.CreateDirectory(AttackFolder);
        Directory.CreateDirectory(PatternFolder);

        // ---- 攻撃 ----
        AttackData swing = Attack("Attack_Sample_なぎ払い", a =>
        {
            a.telegraphTime = 0.6f;
            a.shape.type = AttackShapeType.Sector;
            a.shape.radius = 2.2f;
            a.shape.angle = 120.0f;
            a.recoveryTime = 0.6f;
            a.memo = "敵の前を扇形になぎ払う。近づいてから使う";
        });

        AttackData charge = Attack("Attack_Sample_突進", a =>
        {
            a.telegraphTime = 0.9f;
            a.shape.type = AttackShapeType.Rectangle;
            a.shape.length = 7.0f;
            a.shape.width = 1.2f;
            a.motion = AttackMotion.Charge;
            a.chargeSpeed = 12.0f;
            a.chargeStunTime = 1.0f;
            a.recoveryTime = 0.4f;
            a.memo = "予告の長方形を走る。壁にぶつかると 1 秒気絶（反撃のチャンス）";
        });

        AttackData beam = Attack("Attack_Sample_ビーム", a =>
        {
            a.telegraphTime = 1.0f;
            a.trackDuringTelegraph = true;
            a.telegraphColor = new Color(1.0f, 0.3f, 0.9f, 0.3f);
            a.shape.type = AttackShapeType.Rectangle;
            a.shape.length = 10.0f;
            a.shape.width = 0.8f;
            a.activation = AttackActivation.Sustained;
            a.sustainTime = 1.5f;
            a.hitInterval = 0.4f;
            a.recoveryTime = 0.8f;
            a.memo = "予告の間はプレイヤーを狙い続け、発動で向きが決まる。壁で止まる";
        });

        AttackData blast = Attack("Attack_Sample_足元の爆発", a =>
        {
            a.origin = AttackOrigin.PlayerAtStart;
            a.telegraphTime = 1.0f;
            a.telegraphBlink = true;
            a.shape.type = AttackShapeType.Circle;
            a.shape.radius = 1.3f;
            a.damageKind = DamageKind.Explosion;
            a.recoveryTime = 0.3f;
            a.memo = "予告を出した時点のプレイヤーの足元が爆発する。動けば避けられる";
        });

        AttackData ring = Attack("Attack_Sample_リング", a =>
        {
            a.telegraphTime = 1.0f;
            a.telegraphColor = new Color(1.0f, 0.6f, 0.1f, 0.35f);
            a.shape.type = AttackShapeType.Ring;
            a.shape.radius = 3.5f;
            a.shape.innerRadius = 1.8f;
            a.recoveryTime = 0.5f;
            a.memo = "敵のまわりのドーナツ。敵の近くに入れば安全";
        });

        AttackData shot = Attack("Attack_Sample_狙い撃ち", a =>
        {
            a.telegraphTime = 0.0f;
            a.showTelegraph = false;
            a.activation = AttackActivation.Bullet;
            a.bulletCount = 1;
            a.bulletVolleys = 6;
            a.bulletVolleyInterval = 1.0f;
            a.recoveryTime = 0.0f;
            a.memo = "今までの敵と同じ：1 秒ごとにプレイヤーを狙って 6 発撃つ";
        });

        // ---- 行動パターン ----
        EnemyPattern melee = Pattern("Pattern_Sample_近づいてなぎ払う", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new ApproachAction { stopDistance = 1.8f, maxSeconds = 3.0f });
            phase.actions.Add(new AttackAction { attack = swing, distanceCheck = DistanceCheck.Closer, distance = 2.5f });
            phase.actions.Add(new WaitAction { minSeconds = 0.4f, maxSeconds = 0.8f });
            p.memo = "近づいて、近ければなぎ払う";
        });

        EnemyPattern rush = Pattern("Pattern_Sample_突進", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new WaitAction { minSeconds = 0.8f, maxSeconds = 1.5f });
            phase.actions.Add(new ApproachAction { stopDistance = 6.0f, maxSeconds = 2.0f, distanceCheck = DistanceCheck.Farther, distance = 7.0f });
            phase.actions.Add(new AttackAction { attack = charge });
            p.memo = "遠ければ少し近づき、プレイヤーに向かって突進する";
        });

        EnemyPattern turret = Pattern("Pattern_Sample_砲台（体力で変わる）", p =>
        {
            EnemyPhase first = p.phases[0];
            first.name = "通常";
            first.actions.Add(new AttackAction { attack = beam });
            first.actions.Add(new WaitAction { minSeconds = 1.0f, maxSeconds = 1.0f });
            first.exit = PhaseExit.HealthBelow;
            first.healthRatio = 0.5f;

            var angry = new EnemyPhase { name = "怒り", order = ActionOrder.Random, enterPause = 0.8f, enterAnimation = "Angry" };
            angry.actions.Add(new AttackAction { attack = beam });
            angry.actions.Add(new AttackAction { attack = ring, distanceCheck = DistanceCheck.Closer, distance = 4.0f });
            angry.actions.Add(new AttackAction { attack = blast, repeat = 3, interval = 0.2f });
            p.phases.Add(angry);
            p.memo = "体力が半分になると「怒り」：ビーム・リング（近いとき）・足元の爆発×3 をランダムに";
        });

        EnemyPattern shooter = Pattern("Pattern_Sample_撃って近づく（今までの敵）", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new AttackAction { attack = shot });
            phase.actions.Add(new ApproachAction { stopDistance = 2.0f, maxSeconds = 3.0f, speedMultiplier = 1.0f });
            p.memo = "今までの敵（Enemy.prefab）と同じ：6 発撃ってから 3 秒近づく。壁は回り込む";
        });

        EnemyPattern patrol = Pattern("Pattern_Sample_巡回して見つけたら近づく", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new ApproachAction { stopDistance = 1.8f, maxSeconds = 2.0f, distanceCheck = DistanceCheck.Closer, distance = 5.0f });
            phase.actions.Add(new AttackAction { attack = swing, distanceCheck = DistanceCheck.Closer, distance = 2.5f });
            phase.actions.Add(new PatrolAction { allPoints = false, waitAtPoint = 0.5f, distanceCheck = DistanceCheck.Farther, distance = 5.0f });
            p.memo = "プレイヤーが遠いときは巡回、5 マス以内なら近づいてなぎ払う。シーンの敵（EnemyBrain）に巡回ポイントを入れる";
        });

        // ---- 敵のデータ ----
        EnemyData(EnemyBaseBuilder.SampleDataPath, "サンプルの敵", 3, 2.0f, EnemySize.Medium, melee, null);
        EnemyData($"{Folder}/EnemyData_Sample_突進.asset", "突進する敵", 4, 2.5f, EnemySize.Medium, rush, null);
        EnemyData($"{Folder}/EnemyData_Sample_砲台.asset", "砲台（ボスの例）", 8, 0.0f, EnemySize.Large, turret, so =>
        {
            SerializedProperty rules = so.FindProperty("damageRules");
            rules.arraySize = 2;
            SerializedProperty r0 = rules.GetArrayElementAtIndex(0);
            r0.FindPropertyRelative("kind").intValue = (int)DamageKindFilter.PlayerBullet;
            r0.FindPropertyRelative("character").enumValueIndex = (int)CharacterFilter.Any;
            r0.FindPropertyRelative("multiplier").floatValue = 0.5f;
            r0.FindPropertyRelative("judge").enumValueIndex = (int)GaugeJudge.None;
            SerializedProperty r1 = rules.GetArrayElementAtIndex(1);
            r1.FindPropertyRelative("kind").intValue = (int)DamageKindFilter.GaugeAttack;
            r1.FindPropertyRelative("character").enumValueIndex = (int)CharacterFilter.Any;
            r1.FindPropertyRelative("multiplier").floatValue = 2.0f;
            r1.FindPropertyRelative("judge").enumValueIndex = (int)GaugeJudge.Perfect;
            so.FindProperty("memo").stringValue = "シグルの弾は半分、ゲージ攻撃のパーフェクトは2倍。体力が半分で怒る";
        });
        EnemyData($"{Folder}/EnemyData_Sample_撃つ.asset", "撃つ敵（今までの敵と同じ動き）", 5, 2.0f, EnemySize.Medium, shooter, so =>
        {
            so.FindProperty("contactDamage").intValue = 0;
        });
        EnemyData($"{Folder}/EnemyData_Sample_巡回.asset", "巡回する敵", 3, 2.0f, EnemySize.Medium, patrol, null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //=====================================================================
    // 作成の共通処理
    //=====================================================================

    private static AttackData Attack(string name, System.Action<AttackData> setup)
    {
        string path = $"{AttackFolder}/{name}.asset";
        AttackData existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
        if(existing != null) { return existing; }

        AttackData data = ScriptableObject.CreateInstance<AttackData>();
        setup(data);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static EnemyPattern Pattern(string name, System.Action<EnemyPattern> setup)
    {
        string path = $"{PatternFolder}/{name}.asset";
        EnemyPattern existing = AssetDatabase.LoadAssetAtPath<EnemyPattern>(path);
        if(existing != null) { return existing; }

        EnemyPattern pattern = ScriptableObject.CreateInstance<EnemyPattern>();
        setup(pattern);
        AssetDatabase.CreateAsset(pattern, path);
        return pattern;
    }

    private static void EnemyData(string path, string displayName, int hp, float speed, EnemySize size, EnemyPattern pattern, System.Action<SerializedObject> extra)
    {
        global::EnemyData data = AssetDatabase.LoadAssetAtPath<global::EnemyData>(path);
        bool created = data == null;
        if(created) { data = ScriptableObject.CreateInstance<global::EnemyData>(); }

        var so = new SerializedObject(data);

        // すでにある敵のデータは、行動パターンが空のときだけ入れる（手で直した設定は残す）
        if(created)
        {
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("maxHealth").intValue = hp;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.FindProperty("size").enumValueIndex = (int)size;
            so.FindProperty("deathEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EffectData>("Assets/Data/Effect/Sample/Fx_Sample_Explosion.asset");
            extra?.Invoke(so);
        }
        if(so.FindProperty("pattern").objectReferenceValue == null) { so.FindProperty("pattern").objectReferenceValue = pattern; }
        so.ApplyModifiedPropertiesWithoutUndo();

        if(created) { AssetDatabase.CreateAsset(data, path); }
        else        { EditorUtility.SetDirty(data); }
    }
}
