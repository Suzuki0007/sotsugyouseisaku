/*********************************************************************/
// * \file   BattleArea.cs
// * \brief  戦闘エリア（追跡イベント・逃げるだけの戦闘・生き残る戦闘など。勝ち方と負け方を決める）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘の始まり方
/// </summary>
public enum BattleStart
{
    [InspectorName("エリアに入ったら")]
    EnterArea = 0,

    [InspectorName("イベントの部品から（会話のあとなど）")]
    ByEvent = 1,

    [InspectorName("シーンが始まったら")]
    OnSceneStart = 2,
}

/// <summary>
/// 勝ち方
/// </summary>
public enum BattleWin
{
    [InspectorName("なし（イベントの部品で終える）")]
    None = 0,

    [InspectorName("敵を全部倒す")]
    DefeatAll = 1,

    [InspectorName("○秒生き残る")]
    Survive = 2,

    [InspectorName("ゴールに着く")]
    ReachGoal = 3,

    [InspectorName("フラグが立つ（ギミックを解くなど）")]
    Flag = 4,
}

/// <summary>
/// 負け方
/// </summary>
public enum BattleLose
{
    [InspectorName("なし（体力がなくなったらいつものゲームオーバー）")]
    None = 0,

    [InspectorName("捕まる（敵に近づかれる）")]
    Caught = 1,

    [InspectorName("時間切れ")]
    TimeUp = 2,
}

/// <summary>
/// 負けたとき
/// </summary>
public enum BattleLoseResult
{
    [InspectorName("やり直す（始まりの位置に戻る）")]
    Restart = 0,

    [InspectorName("ダメージを受けて続ける")]
    Damage = 1,

    [InspectorName("ゲームオーバー")]
    GameOver = 2,

    [InspectorName("戦闘を終えて「負けたとき」のイベントだけ")]
    EventOnly = 3,
}

/// <summary>
/// 勝ったあとの敵
/// </summary>
public enum BattleEndEnemies
{
    [InspectorName("そのまま")]
    Keep = 0,

    [InspectorName("止める")]
    Stop = 1,

    [InspectorName("消す")]
    Remove = 2,
}

/// <summary>
/// ウェーブ（1組分）
/// </summary>
[System.Serializable]
public class BattleWave
{
    [Tooltip("このウェーブで出てくる、シーンに置いた敵（始まるまで隠れている）")]
    public List<Enemy> enemies = new List<Enemy>();

    [Tooltip("このウェーブで新しく出す敵（敵のデータ・出る場所・数）")]
    public List<WaveSpawn> spawns = new List<WaveSpawn>();

    [Label("出てくるまでの秒数", 0.0f)]
    [Tooltip("前のウェーブを倒してから（最初のウェーブは戦闘が始まってから）出てくるまで")]
    public float delay = 1.0f;

    [Label("メッセージ")]
    [Tooltip("出てくるときに画面の真ん中に出す（例：第2波！）。空なら出さない")]
    public string message = "";
}

/// <summary>
/// ウェーブで新しく出す敵
/// </summary>
[System.Serializable]
public class WaveSpawn
{
    [Label("敵")]
    public EnemyData enemy;

    [Label("出る場所")]
    [Tooltip("空のオブジェクトを置いて入れる")]
    public Transform point;

    [Label("数", 1)]
    public int count = 1;
}

/// <summary>
/// 戦闘エリア
/// </summary>
///
/// ・追いかけられるイベント・逃げるだけの戦闘・○秒生き残る戦闘・全滅させる戦闘を、勝ち方と負け方の組み合わせで作る
///   例：追跡イベント ＝ 勝ち方「ゴールに着く」×負け方「捕まる」×負けたとき「やり直す」。参加する敵の行動パターンは「追いかける」
/// ・始まるまで、参加する敵・攻撃の発生源は止めておける（「始まるまで敵を止めておく」）
/// ・戦闘中は、セーブポイントでセーブできない。「戦闘中にオンにするフラグ」で BGM を変えられる（BgmConditionFlag）
/// ・時間は、会話中などは進まない
/// ・「1回だけ」なら勝ったことをセーブし、シーンに戻ったときは勝ったあとの状態にする
[RequireComponent(typeof(Collider2D))]
public class BattleArea : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("始まり")]

    [SerializeField, Label("始まり方")]
    [Tooltip("エリアに入ったら：このオブジェクトの Collider2D（Is Trigger）に入ったら始まる")]
    private BattleStart start = BattleStart.EnterArea;

    [SerializeField]
    [Tooltip("参加する敵。始まるまで止めておける。「敵を全部倒す」「捕まる」で見る")]
    private List<Enemy> enemies = new List<Enemy>();

    [SerializeField]
    [Tooltip("参加する攻撃の発生源（見えない敵）。始まるまで止め、終わったら止める")]
    private List<AttackEmitter> emitters = new List<AttackEmitter>();

    [SerializeField, Label("始まるまで敵を止めておく")]
    private bool holdUntilStart = true;

    [SerializeField]
    [Tooltip("ウェーブ（順に出てくる敵の組）。前のウェーブを全部倒すと次が出る。ウェーブの敵は始まるまで隠れている。勝ち方「敵を全部倒す」は、全部のウェーブを倒したら勝ち")]
    private List<BattleWave> waves = new List<BattleWave>();

    [Header("勝ち方")]

    [SerializeField, Label("勝ち方")]
    private BattleWin win = BattleWin.ReachGoal;

    [SerializeField, Label("生き残る秒数", 1.0f)]
    private float surviveSeconds = 30.0f;

    [SerializeField, Label("ゴール")]
    [Tooltip("「ゴールに着く」のとき。操作中のキャラがこの Collider2D（Is Trigger）の中に入ったら勝ち")]
    private Collider2D goal;

    [SerializeField, Label("フラグ")]
    [Tooltip("「フラグが立つ」のとき。このフラグが ON になったら勝ち（ギミックを解いたら ON にする、など）")]
    private GameFlag winFlag;

    [Header("負け方")]

    [SerializeField, Label("負け方")]
    private BattleLose lose = BattleLose.Caught;

    [SerializeField, Label("捕まる距離", 0.1f)]
    [Tooltip("参加する敵が、操作中のキャラにこの距離まで近づいたら「捕まった」")]
    private float catchDistance = 1.0f;

    [SerializeField, Label("制限時間（秒）", 1.0f)]
    [Tooltip("「時間切れ」のとき")]
    private float timeLimit = 60.0f;

    [SerializeField, Label("負けたとき")]
    private BattleLoseResult loseResult = BattleLoseResult.Restart;

    [SerializeField, Label("負けたときのダメージ", 1)]
    [Tooltip("「ダメージを受けて続ける」のとき")]
    private int loseDamage = 1;

    [SerializeField, Label("やり直す位置")]
    [Tooltip("「やり直す」で戻る位置（空のオブジェクトを置いて入れる）。空なら、始まったときにプレイヤーがいた位置")]
    private Transform restartPoint;

    [SerializeField, Label("やり直したあと敵が止まる時間", 0.0f)]
    [Tooltip("やり直し・ダメージのあと、敵が動き出すまでの秒数（すぐまた捕まらないように）。やり直しは、暗転してシーンを読み込み直し、戦闘が始まったときの状態（拾った物・フラグ・所持品・扉など）に戻す")]
    private float restartGrace = 1.5f;

    [Header("演出")]

    [SerializeField, Label("始まったとき")]
    [Tooltip("始まったときに実行するイベント（扉を閉める・会話・SE など）")]
    private EventSequence onStart;

    [SerializeField, Label("勝ったとき")]
    private EventSequence onWin;

    [SerializeField, Label("負けたとき")]
    [Tooltip("負けたときに実行するイベント（「捕まった！」の会話など。やり直しの前に実行）")]
    private EventSequence onLose;

    [SerializeField, Label("戦闘中にオンにするフラグ")]
    [Tooltip("戦闘中だけ ON（セーブしない一時的なフラグがおすすめ）。BgmConditionFlag で追跡中の曲に変えられる")]
    private GameFlag battleFlag;

    [Header("画面の表示")]

    [SerializeField, Label("目的と残り時間を画面に出す")]
    [Tooltip("戦闘中、画面の上に目的・残り時間・残りの敵を出す")]
    private bool showHud = true;

    [SerializeField, Label("目的の文")]
    [Tooltip("画面の上に出す目的。空なら勝ち方から自動で決める（ゴールまで逃げろ／生き残れ／敵を全部倒せ）")]
    private string objective = "";

    [SerializeField, Label("始まったときのメッセージ")]
    [Tooltip("画面の真ん中に少しの間出す（例：逃げろ！）。空なら出さない")]
    private string startMessage = "";

    [SerializeField, Label("勝ったときのメッセージ")]
    [Tooltip("例：逃げ切った！。空なら出さない")]
    private string winMessage = "";

    [SerializeField, Label("負けたときのメッセージ")]
    [Tooltip("例：捕まった！。やり直すときは、これを出してから暗転する。空なら出さない")]
    private string loseMessage = "";

    [SerializeField, Label("メッセージを出す時間（秒）", 0.3f)]
    private float messageSeconds = 1.5f;

    [Header("終わったら")]

    [SerializeField, Label("勝ったあとの敵")]
    private BattleEndEnemies endEnemies = BattleEndEnemies.Remove;

    [SerializeField, Label("1回だけ")]
    [Tooltip("オン：勝ったら二度と始まらない（セーブもする）")]
    private bool once = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する")]
    private string saveId = "";

    //=====================================================================
    // 状態
    //=====================================================================

    private static readonly List<BattleArea> active = new List<BattleArea>();

    /// <summary> 今戦闘中のエリアがあるか（セーブポイントはこの間セーブできない） </summary>
    public static bool AnyActive => active.Count > 0;

    /// <summary> 今戦闘中のエリア（デバッグ用） </summary>
    public static IReadOnlyList<BattleArea> Active => active;

    // やり直しで読み込み直したあと、どの戦闘エリアを、どの位置から始め直すか
    private static string pendingRestartKey;
    private static Vector2 pendingRestartPosition;

    // 始まったときのセーブしないフラグ（やり直しで戻す）
    private static HashSet<string> temporaryFlagsAtStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        active.Clear();
        pendingRestartKey = null;
        temporaryFlagsAtStart = null;
    }

    private bool running;
    private bool won;
    private float elapsed;
    private Vector2 restartPosition;
    private string saveKey;

    public bool Running => running;
    public bool Won => won;
    public float Elapsed => elapsed;

    /// <summary> デバッグ表示用 </summary>
    public string StatusText
    {
        get
        {
            if(won) { return "勝った"; }
            if(!running) { return "始まっていない"; }

            string text = $"戦闘中 {elapsed:0.0} 秒";
            if(win == BattleWin.Survive) { text += $"（あと {Mathf.Max(0.0f, surviveSeconds - elapsed):0.0} 秒で勝ち）"; }
            if(lose == BattleLose.TimeUp) { text += $"（あと {Mathf.Max(0.0f, timeLimit - elapsed):0.0} 秒で時間切れ）"; }
            if(win == BattleWin.DefeatAll) { text += $"（残り {AliveEnemyCount} 体）"; }
            if(HasWaves) { text += $"（ウェーブ {Mathf.Max(1, CurrentWave)}/{WaveCount}）"; }
            return text;
        }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    // 参加する敵（最初から＋出てきたウェーブの敵）
    private readonly List<Enemy> participants = new List<Enemy>();
    private readonly List<Enemy> waveMembers = new List<Enemy>();
    private int waveIndex = -1;
    private bool wavesDone;
    private bool waveStarting;

    private bool HasWaves => waves != null && waves.Count > 0;

    private void Start()
    {
        saveKey = MakeSaveKey(gameObject, saveId);
        participants.AddRange(enemies);

        // ウェーブの敵は、出番まで隠しておく
        foreach(BattleWave wave in waves)
        {
            if(wave == null) { continue; }
            foreach(Enemy e in wave.enemies) { if(e != null) { e.gameObject.SetActive(false); } }
        }

        // 勝ったことがあれば、勝ったあとの状態にする（イベントは見た目だけ戻す）
        if(once && Save.Get(saveKey, false))
        {
            won = true;
            ApplyEndEnemies(true);
            if(onWin != null) { onWin.Restore(gameObject); }
            return;
        }

        if(holdUntilStart) { SetParticipants(false); }

        // やり直しで読み込み直したとき：プレイヤーが出てから始め直す
        if(pendingRestartKey == saveKey)
        {
            pendingRestartKey = null;
            StartCoroutine(BeginAfterRestart());
            return;
        }

        if(start == BattleStart.OnSceneStart) { Begin(); }
    }

    private System.Collections.IEnumerator BeginAfterRestart()
    {
        while(PlayerCharacters.Get(PlayerCharacters.Current) == null) { yield return null; }

        Begin(pendingRestartPosition);
        PauseEnemies(restartGrace);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(start != BattleStart.EnterArea || running || won) { return; }
        if(!other.CompareTag("Player")) { return; }
        Begin();
    }

    //=====================================================================
    // 始める・終える
    //=====================================================================

    /// <summary>
    /// 戦闘を始める（イベントの部品・エリアに入ったとき）
    /// </summary>
    public void Begin()
    {
        Begin(null);
    }

    private void Begin(Vector2? restartFrom)
    {
        if(running || won || restarting) { return; }

        // やり直しのために、始まったときの状態を残す
        // ・セーブの仮置き（拾った物・調べた物・セーブするフラグ・所持品）は、ここで確定しておき、やり直しでは確定後の変化を捨てる
        // ・セーブしないフラグは写し取っておく（戦闘中フラグを ON にする前に）
        if(loseResult == BattleLoseResult.Restart)
        {
            Save.Commit();
            temporaryFlagsAtStart = GameFlags.CaptureTemporary();
        }

        running = true;
        elapsed = 0.0f;
        if(!active.Contains(this)) { active.Add(this); }

        GameObject player = PlayerCharacters.Get(PlayerCharacters.Current);
        if(restartFrom.HasValue)          { restartPosition = restartFrom.Value; }
        else if(restartPoint != null)     { restartPosition = restartPoint.position; }
        else if(player != null)           { restartPosition = player.transform.position; }
        else                              { restartPosition = transform.position; }


        SetParticipants(true);
        if(battleFlag != null) { battleFlag.Value = true; }
        if(onStart != null) { onStart.Play(gameObject); }
        StandardSignals.Send(StandardSignals.BattleStarted, gameObject);

        if(HasWaves && waveIndex < 0) { StartCoroutine(StartWave(0)); }

        BattleHud.ShowMessage(startMessage, messageSeconds);
        UpdateHud();
    }

    /// <summary>
    /// 勝ったことにする（イベントの部品・デバッグ）
    /// </summary>
    public void ForceWin()
    {
        if(!running) { Begin(); }
        Win();
    }

    /// <summary>
    /// 勝ち負けをつけずにやめる（イベントの部品）
    /// </summary>
    public void Cancel()
    {
        if(!running) { return; }
        End();
        SetParticipants(false);
    }

    private void End()
    {
        running = false;
        active.Remove(this);
        if(battleFlag != null) { battleFlag.Value = false; }
        BattleHud.HideInfo(this);
    }

    private void OnDestroy()
    {
        BattleHud.HideInfo(this);
    }

    //=====================================================================
    // 画面の表示
    //=====================================================================

    /// <summary>
    /// 画面の上の帯（目的・残り時間・残りの敵）を更新する
    /// </summary>
    private void UpdateHud()
    {
        if(!showHud || !running) { return; }

        var parts = new List<string>();

        string goalText = !string.IsNullOrEmpty(objective) ? objective : DefaultObjective;
        if(!string.IsNullOrEmpty(goalText)) { parts.Add(goalText); }

        bool warning = false;
        if(win == BattleWin.Survive)
        {
            parts.Add($"あと {Mathf.CeilToInt(Mathf.Max(0.0f, surviveSeconds - elapsed))} 秒");
        }
        if(lose == BattleLose.TimeUp)
        {
            float remain = Mathf.Max(0.0f, timeLimit - elapsed);
            parts.Add($"残り {Mathf.CeilToInt(remain)} 秒");
            warning = remain <= BattleHud.WarningSeconds;
        }
        if(win == BattleWin.DefeatAll)
        {
            if(HasWaves) { parts.Add($"ウェーブ {Mathf.Max(1, CurrentWave)}/{WaveCount}"); }
            parts.Add($"残り {AliveEnemyCount} 体");
        }

        if(parts.Count == 0) { BattleHud.HideInfo(this); return; }
        BattleHud.SetInfo(this, string.Join("　", parts), warning);
    }

    private string DefaultObjective
    {
        get
        {
            switch(win)
            {
                case BattleWin.ReachGoal: return "ゴールまで逃げろ";
                case BattleWin.Survive:   return "生き残れ";
                case BattleWin.DefeatAll: return "敵を全部倒せ";
            }
            return "";
        }
    }

    private void Win()
    {
        if(!running) { return; }
        End();
        won = true;

        ApplyEndEnemies(false);
        if(once) { Save.SetPending(saveKey, true); }
        if(onWin != null) { onWin.Play(gameObject); }
        StandardSignals.Send(StandardSignals.BattleWon, gameObject);
        BattleHud.ShowMessage(winMessage, messageSeconds);
    }

    private void Lose()
    {
        if(onLose != null) { onLose.Play(gameObject); }
        StandardSignals.Send(StandardSignals.BattleLost, gameObject);
        BattleHud.ShowMessage(loseMessage, messageSeconds);

        switch(loseResult)
        {
            case BattleLoseResult.Restart:
                Restart();
                break;

            case BattleLoseResult.Damage:
                Health party = PlayerCharacters.PartyHealth;
                if(party != null) { party.Damage(DamageInfo.Create(loseDamage, DamageKind.EnemyAttack, null, transform.position)); }
                PauseEnemies(restartGrace);
                break;

            case BattleLoseResult.GameOver:
                End();
                Health health = PlayerCharacters.PartyHealth;
                if(health != null) { health.Damage(DamageInfo.Create(health.CurrentHealth, DamageKind.EnemyAttack, null, transform.position)); }
                break;

            case BattleLoseResult.EventOnly:
                End();
                SetParticipants(false);
                break;
        }
    }

    private bool restarting;

    /// <summary>
    /// やり直す：戦闘が始まったときの状態に戻して、始め直す
    /// </summary>
    /// ・戦闘中に拾った物・調べた物・立てたフラグ・所持品の変化はすべて捨てる（始まったときに確定したセーブに戻す）
    /// ・シーンを暗転して読み込み直すので、扉・宝箱・置かれた爆弾・敵・体力などの見た目も、始まったときのセーブの状態に戻る
    /// ・プレイヤーはやり直す位置に出て、戦闘はすぐ始まり直す（敵は少しの間止まっている）
    private void Restart()
    {
        if(restarting) { return; }
        restarting = true;
        End();
        StartCoroutine(RestartRoutine());
    }

    private System.Collections.IEnumerator RestartRoutine()
    {
        // 「負けたとき」のイベント（「捕まった！」など）と、負けたときのメッセージが終わるのを待つ
        float waitMessage = string.IsNullOrEmpty(loseMessage) ? 0.0f : messageSeconds;
        float waited = 0.0f;
        while((onLose != null && onLose.IsPlaying) || waited < waitMessage)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        // 始まったときの状態に戻す
        Save.DiscardPending();
        GameFlags.RestoreTemporary(temporaryFlagsAtStart);
        if(InventoryManager.Instance != null) { InventoryManager.Instance.ReloadFromSave(); }

        // 同じシーンを読み込み直し、やり直す位置から始め直す
        pendingRestartKey = saveKey;
        pendingRestartPosition = restartPosition;
        PlayerSpawnPoint.PendingPosition = restartPosition;
        SceneFader.Instance.FadeAndLoad(gameObject.scene.name);
    }

    private void PauseEnemies(float seconds)
    {
        if(seconds <= 0.0f) { return; }
        foreach(Enemy enemy in participants) { if(enemy != null && !enemy.IsDead) { enemy.PauseFor(seconds); } }
        foreach(AttackEmitter emitter in emitters) { if(emitter != null) { emitter.StopFor(seconds); } }
    }

    /// <summary>
    /// 参加する敵・攻撃の発生源を動かす／止める
    /// </summary>
    private void SetParticipants(bool move)
    {
        foreach(Enemy enemy in participants) { if(enemy != null) { enemy.SetPaused(!move); } }
        foreach(AttackEmitter emitter in emitters) { if(emitter != null) { emitter.SetStopped(!move); } }
    }

    private void ApplyEndEnemies(bool restoring)
    {
        foreach(AttackEmitter emitter in emitters) { if(emitter != null) { emitter.SetStopped(true); } }

        foreach(Enemy enemy in participants)
        {
            if(enemy == null || enemy.IsDead) { continue; }
            switch(endEnemies)
            {
                case BattleEndEnemies.Stop:   enemy.SetPaused(true); break;
                case BattleEndEnemies.Remove: enemy.Remove(); break;
                default:                      if(restoring) { enemy.SetPaused(false); } break;
            }
        }
    }

    //=====================================================================
    // 勝ち負けの判定
    //=====================================================================

    private void Update()
    {
        if(!running || restarting) { return; }

        // 会話中などは時間を進めない（勝ち負けも決めない）
        if(EnemyBlock.IsBlocked) { return; }
        elapsed += Time.deltaTime;

        UpdateWaves();

        if(IsWon()) { Win(); return; }
        if(IsLost()) { Lose(); return; }

        UpdateHud();
    }

    private bool IsWon()
    {
        switch(win)
        {
            case BattleWin.DefeatAll:
                if(HasWaves) { return wavesDone && AliveEnemyCount == 0; }
                return participants.Count > 0 && AliveEnemyCount == 0;
            case BattleWin.Survive:   return elapsed >= surviveSeconds;
            case BattleWin.Flag:      return winFlag != null && winFlag.Value;

            case BattleWin.ReachGoal:
                GameObject player = PlayerCharacters.Get(PlayerCharacters.Current);
                return goal != null && player != null && goal.OverlapPoint(player.transform.position);
        }
        return false;
    }

    private bool IsLost()
    {
        switch(lose)
        {
            case BattleLose.TimeUp:
                return elapsed >= timeLimit;

            case BattleLose.Caught:
                GameObject player = PlayerCharacters.Get(PlayerCharacters.Current);
                if(player == null) { return false; }
                foreach(Enemy enemy in participants)
                {
                    if(enemy == null || enemy.IsDead || enemy.Stopped) { continue; }
                    if(Vector2.Distance(enemy.transform.position, player.transform.position) <= catchDistance) { return true; }
                }
                return false;
        }
        return false;
    }

    private int AliveEnemyCount
    {
        get
        {
            int count = 0;
            foreach(Enemy enemy in participants) { if(enemy != null && !enemy.IsDead) { count++; } }
            return count;
        }
    }

    //=====================================================================
    // ウェーブ
    //=====================================================================

    /// <summary>
    /// 今のウェーブを全部倒したら、次のウェーブを出す
    /// </summary>
    private void UpdateWaves()
    {
        if(!HasWaves || wavesDone || waveStarting || waveIndex < 0) { return; }

        foreach(Enemy e in waveMembers) { if(e != null && !e.IsDead) { return; } }

        if(waveIndex + 1 < waves.Count) { StartCoroutine(StartWave(waveIndex + 1)); }
        else                            { wavesDone = true; }
    }

    private IEnumerator StartWave(int index)
    {
        waveStarting = true;
        waveIndex = index;
        BattleWave wave = waves[index];

        // 出てくるまで待つ（会話中などは数えない）
        float elapsed = 0.0f;
        float delay = wave != null ? wave.delay : 0.0f;
        while(elapsed < delay)
        {
            if(!EnemyBlock.IsBlocked) { elapsed += Time.deltaTime; }
            yield return null;
        }

        waveMembers.Clear();
        if(wave != null)
        {
            BattleHud.ShowMessage(wave.message, messageSeconds);

            foreach(Enemy e in wave.enemies)
            {
                if(e == null) { continue; }
                e.gameObject.SetActive(true);
                AddWaveMember(e);
            }

            foreach(WaveSpawn spawn in wave.spawns)
            {
                if(spawn == null || spawn.enemy == null) { continue; }
                Vector2 center = spawn.point != null ? (Vector2)spawn.point.position : (Vector2)transform.position;
                for(int i = 0; i < Mathf.Max(1, spawn.count); i++)
                {
                    Vector2 pos = center + (i == 0 ? Vector2.zero : UnityEngine.Random.insideUnitCircle * 1.2f);
                    NavMesh2D nav = NavMesh2D.Instance;
                    if(nav != null) { nav.SamplePosition(pos, 1.5f, spawn.enemy.Size, out pos); }
                    AddWaveMember(Enemy.Spawn(spawn.enemy, pos));
                }
            }
        }

        waveStarting = false;
    }

    private void AddWaveMember(Enemy enemy)
    {
        if(enemy == null) { return; }
        waveMembers.Add(enemy);
        if(!participants.Contains(enemy)) { participants.Add(enemy); }
    }

    /// <summary> 今のウェーブ（1 から。ウェーブがなければ 0） </summary>
    public int CurrentWave => HasWaves ? waveIndex + 1 : 0;

    public int WaveCount => HasWaves ? waves.Count : 0;

    //=====================================================================
    // セーブ
    //=====================================================================

    public static string MakeSaveKey(GameObject target, string id)
    {
        string name = id;
        if(string.IsNullOrEmpty(name))
        {
            name = target.name;
            for(Transform parent = target.transform.parent; parent != null; parent = parent.parent) { name = parent.name + "/" + name; }
        }
        return "Battle/" + target.scene.name + "/" + name;
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmos()
    {
        Collider2D area = GetComponent<Collider2D>();
        if(area != null && start == BattleStart.EnterArea)
        {
            Gizmos.color = new Color(1.0f, 0.4f, 0.2f, 0.15f);
            Gizmos.DrawCube(area.bounds.center, area.bounds.size);
        }

        if(goal != null && win == BattleWin.ReachGoal)
        {
            Gizmos.color = new Color(0.2f, 1.0f, 0.4f, 0.3f);
            Gizmos.DrawCube(goal.bounds.center, goal.bounds.size);
        }

        if(restartPoint != null)
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1.0f, 0.9f);
            Gizmos.DrawWireSphere(restartPoint.position, 0.4f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(lose != BattleLose.Caught) { return; }
        Gizmos.color = new Color(1.0f, 0.2f, 0.2f, 0.8f);
        foreach(Enemy enemy in enemies) { if(enemy != null) { Gizmos.DrawWireSphere(enemy.transform.position, catchDistance); } }
    }
}
