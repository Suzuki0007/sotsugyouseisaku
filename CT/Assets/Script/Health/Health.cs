/*********************************************************************/
// * \file   Health.cs
// * \brief  体力クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 体力クラス
/// </summary>
public class Health : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private int maxHealth;

    [SerializeField, Label("体力を共有する相手")]
    [Tooltip("ここに指定した Health と体力を共有する（受けたダメージ・回復はそちらに回る）。空なら自分の体力を使う。\nシグル → ノア は、プレイヤーを出すときに自動で設定される")]
    private Health shareWith;

    private int currentHealth;

    //=====================================================================
    // イベント
    //=====================================================================

    public event System.Action OnDied;

    /// <summary> ダメージを受けたとき (引数: 受けたダメージ量) </summary>
    public event System.Action<int> OnDamaged;

    /// <summary> 回復したとき (引数: 実際に回復した量) </summary>
    public event System.Action<int> OnHealed;

    /// <summary> ダメージを受けたとき (引数: 攻撃の情報。OnDamaged の直前に呼ばれる) </summary>
    public event System.Action<DamageInfo> OnDamagedInfo;

    /// <summary> 最後に受けた攻撃の情報（倒されたときに、何で倒されたかを見るのに使う） </summary>
    public DamageInfo LastDamage { get; private set; }

    /// <summary> 攻撃が当たったが、効き方の設定でダメージが 0 になったとき（効かない攻撃。「カキン」と弾く演出などに使う） </summary>
    public event System.Action<DamageInfo> OnBlocked;

    /// <summary>
    /// ダメージを受ける前に量を変える処理（敵の「攻撃の種類・キャラごとの効き方」など）。null なら変えない
    /// </summary>
    public System.Func<DamageInfo, DamageInfo> DamageFilter { get; set; }

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 今の体力（共有している場合は共有先の体力） </summary>
    public int CurrentHealth
    {
        get => shareWith != null ? shareWith.CurrentHealth : currentHealth;
        private set => currentHealth = value;
    }

    /// <summary> 最大の体力（共有している場合は共有先の最大値） </summary>
    public int MaxHealth => shareWith != null ? shareWith.MaxHealth : maxHealth;

    /// <summary> 体力を共有している相手（なければ null） </summary>
    public Health SharedWith => shareWith;

    public bool IsDead => CurrentHealth <= 0;

    /// <summary>
    /// 無敵（ダメージを受けない）。このオブジェクトだけに効く
    /// </summary>
    public bool Invincible { get; set; }

    /// <summary>
    /// プレイヤー（Player タグ）全員を無敵にする（デバッグウィンドウ用）
    /// シーンが変わってプレイヤーが作り直されても続く。再生を止めると元に戻る
    /// </summary>
    public static bool PlayerInvincible { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        PlayerInvincible = false;
    }

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        CurrentHealth = maxHealth;

        if(shareWith != null && !CanShareWith(shareWith))
        {
            Debug.LogError($"{name}: 体力の共有先が自分自身か、共有がつながって輪になっています。共有をやめます", this);
            shareWith = null;
        }
    }

    /// <summary>
    /// 最大の体力を変える（敵のデータから設定するときなどに使う）
    /// </summary>
    /// <param name="value"> 新しい最大値（1 以上） </param>
    /// <param name="refill"> 体力を最大まで戻すか </param>
    public void SetMaxHealth(int value, bool refill = true)
    {
        maxHealth = Mathf.Max(1, value);
        if(refill || currentHealth > maxHealth) { currentHealth = maxHealth; }
    }

    /// <summary>
    /// イベントを出さずに体力を 0 にする（セーブから「倒された状態」を戻すときなど）
    /// </summary>
    public void SetDeadSilently()
    {
        currentHealth = 0;
    }

    /// <summary>
    /// 自分の体力（共有していても、共有先ではなく自分の値）。セーブ用
    /// </summary>
    public int OwnHealth => currentHealth;

    /// <summary>
    /// イベントを出さずに自分の体力を決める（セーブから戻すとき。1〜最大に収める）
    /// </summary>
    public void SetHealthSilently(int value)
    {
        currentHealth = Mathf.Clamp(value, 1, Mathf.Max(1, maxHealth));
    }

    /// <summary>
    /// 体力を共有する相手を設定する（null で共有をやめる）
    /// </summary>
    /// 例：シグルの Health → ノアの Health。シグルが受けた攻撃はノアの体力から減る
    public void ShareWith(Health target)
    {
        if(target != null && !CanShareWith(target))
        {
            Debug.LogError($"{name}: 体力の共有先が自分自身か、共有がつながって輪になるので設定できません", this);
            return;
        }
        shareWith = target;
    }

    /// <summary>
    /// 共有先をたどって自分に戻ってこないか（輪になると無限に呼び合うので防ぐ）
    /// </summary>
    private bool CanShareWith(Health target)
    {
        for(Health h = target; h != null; h = h.shareWith)
        {
            if(h == this) { return false; }
        }
        return true;
    }

    /// <summary>
    /// ダメージを受ける（攻撃の種類は「その他」）
    /// </summary>
    public void Damage(int damage)
    {
        Damage(DamageInfo.Simple(damage));
    }

    /// <summary>
    /// 攻撃の情報つきでダメージを受ける
    /// </summary>
    public void Damage(DamageInfo info)
    {
        if(IsDead) { return; }
        if(Invincible) { return; }
        if(PlayerInvincible && CompareTag("Player")) { return; }

        // 効き方の設定でダメージを変える（0 になったら「効かなかった」）
        if(DamageFilter != null) { info = DamageFilter(info); }
        if(info.amount <= 0)
        {
            OnBlocked?.Invoke(info);
            return;
        }

        int damage = info.amount;

        // 共有している場合は、共有先の体力を減らす
        // 自分のイベントも出す（シグルのダメージのアニメ・エフェクトなどは、シグルの Health を見ているため）
        if(shareWith != null)
        {
            int before = shareWith.CurrentHealth;
            shareWith.Damage(info);
            if(shareWith.CurrentHealth >= before) { return; }    // 共有先が無敵などで減らなかった

            LastDamage = info;
            OnDamagedInfo?.Invoke(info);
            OnDamaged?.Invoke(damage);
            if(IsDead) { OnDied?.Invoke(); }
            return;
        }

        CurrentHealth -= damage;
        if(CurrentHealth < 0) { CurrentHealth = 0; }

        // 被ダメージイベントをだす
        LastDamage = info;
        OnDamagedInfo?.Invoke(info);
        OnDamaged?.Invoke(damage);

        if(CurrentHealth <= 0)
        {
            // 死亡イベントをだす
            OnDied?.Invoke();
        }
    }

    public void Heal(int healAmount)
    {
        // 共有している場合は、共有先を回復する
        if(shareWith != null)
        {
            int sharedBefore = shareWith.CurrentHealth;
            shareWith.Heal(healAmount);
            int sharedHealed = shareWith.CurrentHealth - sharedBefore;
            if(sharedHealed > 0) { OnHealed?.Invoke(sharedHealed); }
            return;
        }

        int before = CurrentHealth;

        CurrentHealth += healAmount;

        if(CurrentHealth > maxHealth) { CurrentHealth = maxHealth; }

        int healed = CurrentHealth - before;
        if(healed > 0) { OnHealed?.Invoke(healed); }
    }
}
