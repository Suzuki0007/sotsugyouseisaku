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

    [SerializeField, InspectorName("体力を共有する相手")]
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

    public void Damage(int damage)
    {
        if(IsDead) { return; }
        if(Invincible) { return; }
        if(PlayerInvincible && CompareTag("Player")) { return; }

        // 共有している場合は、共有先の体力を減らす
        // 自分のイベントも出す（シグルのダメージのアニメ・エフェクトなどは、シグルの Health を見ているため）
        if(shareWith != null)
        {
            int before = shareWith.CurrentHealth;
            shareWith.Damage(damage);
            if(shareWith.CurrentHealth >= before) { return; }    // 共有先が無敵などで減らなかった

            OnDamaged?.Invoke(damage);
            if(IsDead) { OnDied?.Invoke(); }
            return;
        }

        CurrentHealth -= damage;
        if(CurrentHealth < 0) { CurrentHealth = 0; }

        // 被ダメージイベントをだす
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
