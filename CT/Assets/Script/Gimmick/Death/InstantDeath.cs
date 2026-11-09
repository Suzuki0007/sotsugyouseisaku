/*********************************************************************/
// * \file   InstantDeath.cs
// * \brief  即死（その場からやり直す／ゲームオーバー）の処理
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// 即死したあと
/// </summary>
public enum DeathResult
{
    [InspectorName("やり直す（やり直す位置から。なければ再開する位置から）")]
    Retry = 0,

    [InspectorName("ゲームオーバー")]
    GameOver = 1,
}

/// <summary>
/// 即死の処理
/// </summary>
///
/// ・「やり直す」：演出のあと、暗転して同じシーンを読み込み直し、やり直す位置（RetryPoint で記録した位置）から始め直す。
///   記録した後に拾った物・立てたフラグ・開けた扉などは元に戻る（戦闘エリアの「やり直す」と同じ仕組み）。
///   やり直す位置の記録がなければ、セーブの「再開する位置」（シーンに着いた場所・セーブポイント）から
/// ・「ゲームオーバー」：今の体力を 0 にする（いつものゲームオーバーへ）
public static class InstantDeath
{
    private static bool running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { running = false; }

    /// <summary> 即死の処理中か </summary>
    public static bool Running => running;

    /// <summary>
    /// 即死させる
    /// </summary>
    /// <param name="host"> コルーチンを動かすオブジェクト（シーンに置いた物） </param>
    /// <param name="beforeDeath"> 演出（叫び声・赤い画面・会話など）。終わるまで待ってから暗転する </param>
    public static void Kill(MonoBehaviour host, DeathResult result, EventSequence beforeDeath)
    {
        if(running || host == null) { return; }
        host.StartCoroutine(Routine(host.gameObject, result, beforeDeath));
    }

    private static IEnumerator Routine(GameObject host, DeathResult result, EventSequence beforeDeath)
    {
        running = true;

        // 2人とも動けなくする
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        Stop(m != null ? m.Noah : null);
        Stop(m != null ? m.Sigle : null);

        if(beforeDeath != null)
        {
            beforeDeath.Play(host);
            yield return null;
            while(EventSequence.IsBusy) { yield return null; }
        }

        if(result == DeathResult.GameOver)
        {
            running = false;
            Health party = PlayerCharacters.PartyHealth;
            if(party != null)
            {
                bool inv = Health.PlayerInvincible;
                Health.PlayerInvincible = false;
                party.Invincible = false;
                party.Damage(DamageInfo.Create(party.CurrentHealth, DamageKind.Other, null, host.transform.position));
                Health.PlayerInvincible = inv;
            }
            yield break;
        }

        // やり直す：記録した時点に戻して、同じシーンを読み込み直す
        string scene = host.scene.name;
        Save.DiscardPending();
        if(RetryPoint.HasRecord(scene))
        {
            GameFlags.RestoreTemporary(RetryPoint.RecordedTemporaryFlags);
            PlayerSpawnPoint.PendingPosition = RetryPoint.RecordedPosition;
            PlayerSpawnPoint.PendingSpawnPointId = null;
        }
        else
        {
            // 記録がなければ、セーブの再開する位置（このシーンのものなら）
            SaveSlotInfo info = SaveSlotInfo.FromCurrent();
            if(info.SceneName == scene && info.HasPosition) { PlayerSpawnPoint.PendingPosition = info.Position; }
            else if(info.SceneName == scene)                { PlayerSpawnPoint.PendingSpawnPointId = info.SpawnPointId; }
        }
        if(InventoryManager.Instance != null) { InventoryManager.Instance.ReloadFromSave(); }

        // 読み込み直すとこのコルーチンを動かしている物も消えるので、ここで終わりにする
        // （暗転中は InputBlock で罠も止まるので、二重には起きない）
        running = false;
        SceneFader.Instance.FadeAndLoad(scene);
    }

    private static void Stop(GameObject character)
    {
        PlayerMovement movement = character != null ? character.GetComponent<PlayerMovement>() : null;
        if(movement == null) { return; }
        movement.Stop();
        movement.SetControlEnabled(false);
    }
}
