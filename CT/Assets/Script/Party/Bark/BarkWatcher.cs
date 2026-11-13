/*********************************************************************/
// * \file   BarkWatcher.cs
// * \brief  相棒の一言のきっかけを見張る（シーンごとに自動で作られる）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一言のきっかけを見張る
/// </summary>
///
/// ・2人の状態の変化（待たせた・呼び戻した・離れ離れ・合流）、切り替え、ダメージ・体力、敵を倒した、
///   待たせている時間・止まっている時間、シーンに着いた、を Bark.Fire に渡す
/// ・調べて断られた・目印が出た・呼んでも来られない・答え合わせは、それぞれの部品から直接 Bark.Fire を呼ぶ
/// ・シーンを読み込むたびに作られる（シーンに置く物はない）
public class BarkWatcher : MonoBehaviour
{
    private PlayerSwitchManager manager;
    private Health noahHealth;
    private Health sigleHealth;
    private PartyMode lastMode;
    private float lastRatio = 1.0f;
    private float waitTime;
    private float idleTime;
    private Vector2 lastPosition;
    private GameObject lastPlayer;
    private float readyTime = -1.0f;
    private bool sceneEnterDone;
    private int damageSignalFrame = -1;

    /// <summary> 合図「体力が少ない」を出す割合（最大を 1） </summary>
    private const float LowHealthRatio = 0.34f;

    /// <summary> シーンに着いてから、2人の状態の変化で一言を出さない秒数（セーブからの復元で出さないように） </summary>
    private const float StartupQuiet = 1.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Create();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(mode == LoadSceneMode.Single) { Bark.NotifySceneLoaded(); Create(); }
    }

    private static void Create()
    {
        if(Object.FindAnyObjectByType<BarkWatcher>() != null) { return; }
        BarkView.Get().gameObject.AddComponent<BarkWatcher>();
    }

    private void OnEnable()
    {
        Enemy.Defeated += OnEnemyDefeated;
    }

    private void OnDisable()
    {
        Enemy.Defeated -= OnEnemyDefeated;
        Unhook();
    }

    //=====================================================================
    // 毎フレーム
    //=====================================================================

    private void Update()
    {
        if(manager == null) { Hook(); }
        if(manager == null || manager.CurrentPlayer == null) { return; }

        if(readyTime < 0.0f) { readyTime = Time.time + StartupQuiet; }

        // シーンに着いた（少し落ち着いてから）
        if(!sceneEnterDone && Time.time >= readyTime && !InputBlock.IsBlocked)
        {
            sceneEnterDone = true;
            Bark.Fire(BarkTrigger.SceneEnter, BarkContext.Of(manager.CurrentPlayer));
        }

        Health party = manager.PartyHealth;
        if(party != null && party.MaxHealth > 0) { lastRatio = (float)party.CurrentHealth / party.MaxHealth; }

        bool blocked = InputBlock.IsBlocked;

        // 待たせている時間
        if(manager.Mode == PartyMode.Waiting)
        {
            if(!blocked)
            {
                float prev = waitTime;
                waitTime += Time.deltaTime;
                Bark.Fire(BarkTrigger.LongWait, new BarkContext { subject = manager.Partner, previous = prev, value = waitTime });
            }
        }
        else { waitTime = 0.0f; }

        // 止まっている時間（切り替えたら数え直す）
        GameObject player = manager.CurrentPlayer;
        Vector2 pos = player.transform.position;
        if(player != lastPlayer || (pos - lastPosition).sqrMagnitude > 0.0004f) { idleTime = 0.0f; }
        else if(!blocked)
        {
            float prev = idleTime;
            idleTime += Time.deltaTime;
            Bark.Fire(BarkTrigger.Idle, new BarkContext { subject = player, previous = prev, value = idleTime });
        }
        lastPlayer = player;
        lastPosition = pos;
    }

    //=====================================================================
    // つなぐ
    //=====================================================================

    private void Hook()
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m == null || m.Noah == null || m.Sigle == null) { return; }

        manager = m;
        lastMode = m.Mode;
        manager.ModeChanged += OnModeChanged;
        manager.Switched += OnSwitched;

        noahHealth = m.HealthOf(m.Noah);
        sigleHealth = m.HealthOf(m.Sigle);
        if(noahHealth != null) { noahHealth.OnDamagedInfo += OnNoahDamaged; }
        if(sigleHealth != null) { sigleHealth.OnDamagedInfo += OnSigleDamaged; }
    }

    private void Unhook()
    {
        if(manager != null)
        {
            manager.ModeChanged -= OnModeChanged;
            manager.Switched -= OnSwitched;
        }
        if(noahHealth != null) { noahHealth.OnDamagedInfo -= OnNoahDamaged; }
        if(sigleHealth != null) { sigleHealth.OnDamagedInfo -= OnSigleDamaged; }
        manager = null;
        noahHealth = null;
        sigleHealth = null;
    }

    private bool Quiet => readyTime < 0.0f || Time.time < readyTime;

    private void OnModeChanged(PartyMode mode)
    {
        PartyMode before = lastMode;
        lastMode = mode;
        if(Quiet || manager == null) { return; }

        BarkContext c = BarkContext.Of(manager.Partner);
        if(mode == PartyMode.Waiting)        { Bark.Fire(BarkTrigger.Wait, c); }
        else if(mode == PartyMode.Separated) { Bark.Fire(BarkTrigger.Separated, c); }
        else if(mode == PartyMode.Together)
        {
            Bark.Fire(before == PartyMode.Separated ? BarkTrigger.Reunited : BarkTrigger.CalledBack, c);
        }
    }

    private void OnSwitched(GameObject current)
    {
        if(Quiet) { return; }
        Bark.Fire(BarkTrigger.Switched, BarkContext.Of(current));
    }

    private void OnNoahDamaged(DamageInfo info)  { OnDamaged(manager != null ? manager.Noah : null); }
    private void OnSigleDamaged(DamageInfo info) { OnDamaged(manager != null ? manager.Sigle : null); }

    private void OnDamaged(GameObject who)
    {
        if(manager == null || who == null) { return; }

        // 一緒のときはノアのダメージがシグルにも回るので、同じフレームの2回目は数えない
        Health party = manager.PartyHealth;
        if(party == null || party.MaxHealth <= 0 || party.IsDead) { return; }

        float ratio = (float)party.CurrentHealth / party.MaxHealth;

        // 標準の合図（一緒のときの2回目は出さない）。「体力が少ない」を先に出す（優先度の高い一言が先に決まるように）
        if(damageSignalFrame != Time.frameCount)
        {
            damageSignalFrame = Time.frameCount;
            if(ratio <= LowHealthRatio) { StandardSignals.Send(StandardSignals.LowHealth, who, party.CurrentHealth); }
            StandardSignals.Send(StandardSignals.Damaged, who, party.CurrentHealth);
        }

        if(!Bark.Fire(BarkTrigger.LowHealth, new BarkContext { subject = who, previous = lastRatio, value = ratio }))
        {
            Bark.Fire(BarkTrigger.Damaged, BarkContext.Of(who));
        }
        lastRatio = ratio;
    }

    private void OnEnemyDefeated(Enemy enemy)
    {
        if(enemy == null || manager == null) { return; }
        Bark.Fire(BarkTrigger.EnemyDefeated, new BarkContext { subject = manager.CurrentPlayer, source = enemy.transform, enemy = enemy.Data });
    }
}
