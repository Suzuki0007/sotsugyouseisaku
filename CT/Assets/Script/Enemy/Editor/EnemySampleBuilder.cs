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

        BuildStage3(swing);
        BuildStage4(swing);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    //=====================================================================
    // 段階3のサンプル（落下・突き出る・設置・弾幕・仲間を呼ぶ・見えない敵・ギミック用）
    //=====================================================================

    private static void BuildStage3(AttackData swing)
    {
        EffectData explosion = AssetDatabase.LoadAssetAtPath<EffectData>("Assets/Data/Effect/Sample/Fx_Sample_Explosion.asset");
        EffectData hit = AssetDatabase.LoadAssetAtPath<EffectData>("Assets/Data/Effect/Sample/Fx_Sample_Hit.asset");

        AttackData rock = Attack("Attack_Sample_落石", a =>
        {
            a.origin = AttackOrigin.PlayerAtStart;
            a.telegraphTime = 1.2f;
            a.telegraphColor = new Color(0.0f, 0.0f, 0.0f, 0.35f);
            a.telegraphFill = true;
            a.shape.type = AttackShapeType.Circle;
            a.shape.radius = 1.2f;
            a.activation = AttackActivation.Drop;
            a.activeEffect = explosion;
            a.recoveryTime = 0.2f;
            a.memo = "プレイヤーの足元に影（予告）→ 上から落ちる。落ちてくる物（プレハブ）を入れると、最後の 0.35 秒で降ってくる";
        });

        AttackData spike = Attack("Attack_Sample_とげ（下から）", a =>
        {
            a.origin = AttackOrigin.PlayerAtStart;
            a.telegraphTime = 0.9f;
            a.shape.type = AttackShapeType.Circle;
            a.shape.radius = 1.0f;
            a.activation = AttackActivation.Rise;
            a.riseLinger = 1.0f;
            a.activeEffect = hit;
            a.recoveryTime = 0.3f;
            a.memo = "プレイヤーの足元から突き出る。突き出る物（プレハブ）に当たり判定を付けると、残っている 1 秒は壁になる";
        });

        AttackData arm = Attack("Attack_Sample_腕（下から伸びる）", a =>
        {
            a.origin = AttackOrigin.PlayerAtStart;
            a.aim = AttackAim.Fixed;
            a.fixedAngle = 90.0f;
            a.forwardOffset = -1.5f;
            a.telegraphTime = 1.0f;
            a.telegraphColor = new Color(0.6f, 0.1f, 0.9f, 0.35f);
            a.shape.type = AttackShapeType.Rectangle;
            a.shape.length = 3.0f;
            a.shape.width = 0.9f;
            a.stopAtWall = false;
            a.activation = AttackActivation.Rise;
            a.riseLinger = 0.6f;
            a.riseHitWhileLingering = true;
            a.hitInterval = 0.5f;
            a.activeEffect = hit;
            a.recoveryTime = 0.4f;
            a.memo = "見えない敵用：プレイヤーの位置を縦に貫く腕。突き出る物に腕の絵のプレハブを入れる";
        });

        AttackData bomb = Attack("Attack_Sample_爆弾", a =>
        {
            a.telegraphTime = 0.4f;
            a.shape.type = AttackShapeType.Circle;
            a.shape.radius = 2.0f;
            a.activation = AttackActivation.Place;
            a.fuseTime = 3.0f;
            a.detonateOnTouch = true;
            a.detonateOnAttacked = true;
            a.targets = AttackTargets.Both;
            a.damageKind = DamageKind.Explosion;
            a.damage = 2;
            a.activeEffect = explosion;
            a.recoveryTime = 0.3f;
            a.memo = "その場に置く。3 秒・触れる・攻撃されるで爆発。敵も巻き込む（爆発で敵を倒す遊びに）";
        });

        AttackData radial = Attack("Attack_Sample_全方位弾", a =>
        {
            a.telegraphTime = 0.5f;
            a.showTelegraph = false;
            a.activation = AttackActivation.Bullet;
            a.bulletPattern = BulletPattern.Circle;
            a.bulletCount = 12;
            a.bulletVolleys = 3;
            a.bulletVolleyInterval = 0.5f;
            a.bulletSpeed = 4.0f;
            a.recoveryTime = 0.5f;
            a.memo = "まわり 12 方向に 3 回";
        });

        AttackData spiral = Attack("Attack_Sample_うず巻き弾", a =>
        {
            a.telegraphTime = 0.0f;
            a.showTelegraph = false;
            a.aim = AttackAim.Fixed;
            a.activation = AttackActivation.Bullet;
            a.bulletPattern = BulletPattern.Spiral;
            a.bulletCount = 4;
            a.bulletVolleys = 24;
            a.bulletVolleyInterval = 0.12f;
            a.bulletSpiralStep = 11.0f;
            a.bulletSpeed = 3.5f;
            a.bulletDeletable = false;
            a.recoveryTime = 0.8f;
            a.memo = "4 方向の弾を回しながら連射。弾消しでは消せない";
        });

        Attack("Attack_Sample_電気の床（敵に当たる）", a =>
        {
            a.telegraphTime = 0.5f;
            a.telegraphColor = new Color(1.0f, 0.9f, 0.1f, 0.35f);
            a.shape.type = AttackShapeType.Circle;
            a.shape.radius = 2.5f;
            a.activation = AttackActivation.Sustained;
            a.sustainTime = 3.0f;
            a.hitInterval = 0.5f;
            a.targets = AttackTargets.Enemies;
            a.damageKind = DamageKind.Gimmick;
            a.recoveryTime = 0.0f;
            a.memo = "ギミック用：イベントの部品「ダメージエリアを出す」で使う。敵だけに当たる電気の床（3 秒）";
        });

        // ---- 行動パターン ----
        EnemyPattern hidden = Pattern("Pattern_Sample_見えない敵（足元から腕）", p =>
        {
            EnemyPhase strong = p.phases[0];
            strong.name = "強い";
            strong.actions.Add(new AttackAction { attack = arm });
            strong.actions.Add(new WaitAction { minSeconds = 0.6f, maxSeconds = 0.6f });
            strong.actions.Add(new AttackAction { attack = spike });

            var weak = new EnemyPhase { name = "弱い" };
            weak.actions.Add(new AttackAction { attack = spike });
            weak.actions.Add(new WaitAction { minSeconds = 2.0f, maxSeconds = 2.0f });
            p.phases.Add(weak);
            p.memo = "攻撃の発生源（見えない敵）用。ギミックの「敵に命令：段階を変える（2）」で弱くなる。「止める」で止まる";
        });

        EnemyPattern bomber = Pattern("Pattern_Sample_爆弾を置いて逃げる", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new ApproachAction { stopDistance = 3.0f, maxSeconds = 2.5f });
            phase.actions.Add(new AttackAction { attack = bomb });
            phase.actions.Add(new RetreatAction { keepDistance = 5.0f, maxSeconds = 2.0f });
            phase.actions.Add(new WaitAction { minSeconds = 1.0f, maxSeconds = 1.5f });
            p.memo = "近づいて爆弾を置き、離れる";
        });

        EnemyPattern barrage = Pattern("Pattern_Sample_弾幕", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new AttackAction { attack = radial });
            phase.actions.Add(new WaitAction { minSeconds = 1.0f, maxSeconds = 1.0f });
            phase.actions.Add(new AttackAction { attack = spiral });
            phase.actions.Add(new WaitAction { minSeconds = 1.0f, maxSeconds = 1.0f });
            p.memo = "全方位弾 → うず巻き弾";
        });

        global::EnemyData minion = AssetDatabase.LoadAssetAtPath<global::EnemyData>(EnemyBaseBuilder.SampleDataPath);
        EnemyPattern summoner = Pattern("Pattern_Sample_仲間を呼ぶ", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.order = ActionOrder.Sequential;
            phase.actions.Add(new SummonAction { enemy = minion, count = 2, maxAlive = 3, place = SummonPlace.AroundSelf });
            phase.actions.Add(new WaitAction { minSeconds = 3.0f, maxSeconds = 3.0f });
            phase.actions.Add(new SummonAction { enemy = minion, count = 1, maxAlive = 3, place = SummonPlace.Egg, hatchTime = 3.0f, eggHealth = 2 });
            phase.actions.Add(new WaitAction { minSeconds = 3.0f, maxSeconds = 3.0f });
            p.memo = "まわりに仲間を2体 → 卵を1つ（3 秒でかえる。2 回叩くと壊れる）。同時に 3 体まで";
        });

        // ---- 敵のデータ ----
        EnemyData($"{Folder}/EnemyData_Sample_爆弾魔.asset", "爆弾を置く敵", 3, 2.5f, EnemySize.Small, bomber, null);
        EnemyData($"{Folder}/EnemyData_Sample_弾幕.asset", "弾幕の敵", 6, 0.0f, EnemySize.Medium, barrage, null);
        EnemyData($"{Folder}/EnemyData_Sample_呼ぶ.asset", "仲間を呼ぶ敵", 6, 1.0f, EnemySize.Medium, summoner, null);

        // 見えない敵の行動パターンは、敵のデータではなく攻撃の発生源（AttackEmitter）に入れて使う
        _ = hidden;
        _ = swing;
    }

    //=====================================================================
    // 段階4のサンプル（追跡）
    //=====================================================================

    private static void BuildStage4(AttackData swing)
    {
        EffectData notice = AssetDatabase.LoadAssetAtPath<EffectData>("Assets/Data/Effect/Sample/Fx_Sample_Hit.asset");

        EnemyPattern chaser = Pattern("Pattern_Sample_追いかけてくる（追跡イベント用）", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new ChaseAction { searchSeconds = 3.0f, nearSpeed = 0.9f, farSpeed = 1.4f, farDistance = 6.0f, noticeEffect = notice });
            phase.actions.Add(new WanderAction { radius = 2.0f, count = 1, speedMultiplier = 0.6f });
            p.memo = "見えている間は追いかけ、見失ったら最後に見た場所を 3 秒探す。あきらめたら少しうろつく。戦闘エリア（負け方：捕まる）と組み合わせて追跡イベントにする";
        });

        EnemyPattern guard = Pattern("Pattern_Sample_見張り（見つけたら追う）", p =>
        {
            EnemyPhase phase = p.phases[0];
            phase.actions.Add(new ChaseAction { stopDistance = 1.6f, searchSeconds = 2.0f, sightCheck = SightCheck.Seen, sightRange = 7.0f, noticeEffect = notice });
            phase.actions.Add(new AttackAction { attack = swing, distanceCheck = DistanceCheck.Closer, distance = 2.2f });
            phase.actions.Add(new PatrolAction { allPoints = false, waitAtPoint = 0.8f, sightCheck = SightCheck.NotSeen, sightRange = 7.0f });
            p.memo = "プレイヤーが見えない（壁の向こう・7 マスより遠い）ときは巡回。見つけたら追いかけ、近づいたらなぎ払う。見失ったら探して巡回に戻る（隠れてやり過ごせる）";
        });

        EnemyData($"{Folder}/EnemyData_Sample_追跡者.asset", "追いかけてくる敵", 99, 4.0f, EnemySize.Medium, chaser, so =>
        {
            so.FindProperty("contactDamage").intValue = 0;
            so.FindProperty("memo").stringValue = "追跡イベント用。倒せない前提（体力 99）。捕まったかどうかは戦闘エリアで決める";
        });
        EnemyData($"{Folder}/EnemyData_Sample_見張り.asset", "見張り", 3, 2.2f, EnemySize.Medium, guard, null);
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
