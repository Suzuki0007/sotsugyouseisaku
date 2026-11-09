/*********************************************************************/
// * \file   PartyPersistence.cs
// * \brief  2人の状態（一緒・待機・別行動）・操作しているキャラ・相棒の位置のセーブと復元
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 2人の状態のセーブ
/// </summary>
///
/// ・セーブの確定（セーブポイント・シーンに着いたとき・シーンを出るとき など）のたびに、
///   2人の状態・操作しているキャラ・相棒の位置・そのシーン・「待て／ついてこい」を使えなくしているかを記録する（Party/*）
/// ・シーンに着いたとき、記録したシーンと同じなら元に戻す（つづきから・戦闘エリアのやり直し・同じシーンの読み直し）
///   操作していたキャラは再開する位置に、相棒は記録した位置に出る
/// ・違うシーンに移ったときは「一緒」に戻る（相棒もついてくる）。離れ離れのステージは 1 つのシーンの中で作る
/// ・体力（ノア・シグルそれぞれ自分の値）もセーブし、どのシーンに着いたときも戻す
///   記録した体力が 0 以下（倒れてゲームオーバーになった）・記録がない（はじめから・古いセーブ）なら、2人とも満タン
public static class PartyPersistence
{
    private const string NoahHealthKey = "Party/NoahHP";
    private const string SigleHealthKey = "Party/SigleHP";

    private const string ModeKey = "Party/Mode";
    private const string CurrentKey = "Party/Current";
    private const string SceneKey = "Party/Scene";
    private const string PartnerXKey = "Party/PartnerX";
    private const string PartnerYKey = "Party/PartnerY";
    private const string WaitLockedKey = "Party/WaitLocked";

    // Save の ResetOnPlay（SubsystemRegistration）より後に登録する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Save.BeforeCommit -= OnBeforeCommit;
        Save.BeforeCommit += OnBeforeCommit;
    }

    private static void OnBeforeCommit()
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m == null || m.Noah == null || m.Sigle == null || m.CurrentPlayer == null) { return; }

        Vector2 partner = m.Partner.transform.position;
        Save.SetPending(ModeKey, (int)m.Mode);
        Save.SetPending(CurrentKey, m.CurrentPlayer == m.Sigle ? 1 : 0);
        Save.SetPending(SceneKey, m.gameObject.scene.name);
        Save.SetPending(PartnerXKey, partner.x);
        Save.SetPending(PartnerYKey, partner.y);
        Save.SetPending(WaitLockedKey, PlayerSwitchManager.WaitCommandLocked);

        Health noahHealth = m.HealthOf(m.Noah);
        Health sigleHealth = m.HealthOf(m.Sigle);
        if(noahHealth != null)  { Save.SetPending(NoahHealthKey, noahHealth.OwnHealth); }
        if(sigleHealth != null) { Save.SetPending(SigleHealthKey, sigleHealth.OwnHealth); }
    }

    /// <summary>
    /// 体力を戻す（どのシーンでも）。記録がない・どちらかが 0 以下なら、何もしない（満タンのまま）
    /// </summary>
    private static void RestoreHealth(PlayerSwitchManager m)
    {
        int noah = Save.Get(NoahHealthKey, -1);
        int sigle = Save.Get(SigleHealthKey, -1);
        if(noah <= 0 || sigle <= 0) { return; }

        Health noahHealth = m.HealthOf(m.Noah);
        Health sigleHealth = m.HealthOf(m.Sigle);
        if(noahHealth != null)  { noahHealth.SetHealthSilently(noah); }
        if(sigleHealth != null) { sigleHealth.SetHealthSilently(sigle); }
    }

    /// <summary>
    /// シーンに着いたときに元に戻す（PlayerPartySpawner がプレイヤーを出したあとに呼ぶ）
    /// ノアは「再開する位置」に出ている前提（操作していたのがシグルなら、シグルをそこへ移す）
    /// </summary>
    public static void Restore(PlayerSwitchManager m, string sceneName)
    {
        if(m == null || m.Noah == null || m.Sigle == null) { return; }

        RestoreHealth(m);

        var mode = (PartyMode)Save.Get(ModeKey, 0);
        string savedScene = Save.Get(SceneKey, "");
        if(mode == PartyMode.Together || savedScene != sceneName)
        {
            PlayerSwitchManager.WaitCommandLocked = savedScene == sceneName && Save.Get(WaitLockedKey, false);
            return;
        }

        bool sigleWasCurrent = Save.Get(CurrentKey, 0) == 1;
        Vector3 resume = m.Noah.transform.position;
        Vector3 partner = new Vector3(Save.Get(PartnerXKey, resume.x), Save.Get(PartnerYKey, resume.y), 0.0f);

        m.SetMode(mode);
        if(sigleWasCurrent)
        {
            m.Warp(partner, resume);
            m.SelectImmediate(m.Sigle);
        }
        else
        {
            m.Warp(resume, partner);
        }
        PlayerSwitchManager.WaitCommandLocked = Save.Get(WaitLockedKey, false);
    }
}
