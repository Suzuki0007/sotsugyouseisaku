/*********************************************************************/
// * \file   GameCondition.cs
// * \brief  ゲームの条件（フラグ・アイテム・操作中のキャラ・体力・エリア）をまとめたもの
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームの条件
/// </summary>
///
/// ・中の項目は「すべて満たす（AND）」。空の項目・「問わない」は判定しない
/// ・「どれか1つ（OR）」にしたいときは、GameCondition をリストにして AnyMet を使う
/// ・フラグ・アイテムは今までの FlagCondition / ItemCondition をそのまま使う（作り直さない。シーンの既存の設定を壊さないため）
/// 敵の段階の切り替え・見た目の変化・弱点の表示・攻撃の発生源のオン/オフなどで使う
[Serializable]
public class GameCondition
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum CharacterCheck
    {
        [InspectorName("問わない")]
        Any,

        [InspectorName("ノアを操作しているとき")]
        Noah,

        [InspectorName("シグルを操作しているとき")]
        Sigle,
    }

    public enum PartyCheck
    {
        [InspectorName("問わない")]
        Any,

        [InspectorName("2人が一緒のとき")]
        Together,

        [InspectorName("2人が離れているとき（待機・別行動）")]
        Apart,
    }

    public enum HealthCheck
    {
        [InspectorName("問わない")]
        Any,

        [InspectorName("この割合以下のとき")]
        AtOrBelow,

        [InspectorName("この割合以上のとき")]
        AtOrAbove,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("フラグの条件。すべて満たすとき（空なら判定しない）")]
    public List<FlagCondition> flags = new List<FlagCondition>();

    [Tooltip("アイテムの条件。すべて満たすとき（空なら判定しない）")]
    public List<ItemCondition> items = new List<ItemCondition>();

    [Label("操作中のキャラ")]
    [Tooltip("今操作しているキャラで判定する")]
    public CharacterCheck character = CharacterCheck.Any;

    [Label("プレイヤーの体力")]
    [Tooltip("2人で共有している体力の割合（最大を 1 とする）で判定する")]
    public HealthCheck partyHealth = HealthCheck.Any;

    [Label("体力の割合", 0.0f, 1.0f)]
    [Tooltip("0.3 なら最大の 30%")]
    public float partyHealthRatio = 0.5f;

    [Label("プレイヤーがいる範囲")]
    [Tooltip("操作中のキャラがこの Collider2D の範囲の中にいるとき（空なら判定しない。シーンに置いた物だけ指定できる）")]
    public Collider2D area;

    [Tooltip("仕掛けの状態の条件。すべて満たすとき（空なら判定しない。シーンに置いた物だけ指定できる）。\n例：「スイッチA が ON」「ダイヤルが 3」")]
    public List<GimmickCondition> gimmicks = new List<GimmickCondition>();

    [Label("2人の状態")]
    [Tooltip("ノアとシグルが一緒にいるか、離れているかで判定する")]
    public PartyCheck party = PartyCheck.Any;

    [Label("2人がそろっている距離", 0.0f)]
    [Tooltip("ノアとシグルがこの距離（マス）以内にいるとき（2人で押す扉・2人で回すレバーなど）。0 なら判定しない")]
    public float partnerWithin = 0.0f;

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 条件を満たしているか
    /// </summary>
    public bool IsMet()
    {
        if(!FlagCondition.AllMet(flags)) { return false; }
        if(!ItemCondition.AllMet(items)) { return false; }

        if(character != CharacterCheck.Any)
        {
            PlayerCharacter current = PlayerCharacters.Current;
            if(character == CharacterCheck.Noah  && current != PlayerCharacter.Noah)  { return false; }
            if(character == CharacterCheck.Sigle && current != PlayerCharacter.Sigle) { return false; }
        }

        if(partyHealth != HealthCheck.Any)
        {
            Health health = PlayerCharacters.PartyHealth;
            if(health == null || health.MaxHealth <= 0) { return false; }

            float ratio = (float)health.CurrentHealth / health.MaxHealth;
            if(partyHealth == HealthCheck.AtOrBelow && ratio > partyHealthRatio) { return false; }
            if(partyHealth == HealthCheck.AtOrAbove && ratio < partyHealthRatio) { return false; }
        }

        if(area != null)
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            GameObject player = manager != null ? manager.CurrentPlayer : null;
            if(player == null || !area.OverlapPoint(player.transform.position)) { return false; }
        }

        if(gimmicks != null)
        {
            foreach(GimmickCondition g in gimmicks)
            {
                if(g != null && !g.IsMet()) { return false; }
            }
        }

        if(party != PartyCheck.Any)
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            bool together = manager == null || manager.IsTogether;
            if(party == PartyCheck.Together && !together) { return false; }
            if(party == PartyCheck.Apart && together)     { return false; }
        }

        if(partnerWithin > 0.0f)
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            if(manager == null || manager.Noah == null || manager.Sigle == null) { return false; }
            if(Vector2.Distance(manager.Noah.transform.position, manager.Sigle.transform.position) > partnerWithin) { return false; }
        }

        return true;
    }

    /// <summary>
    /// 何も設定されていないか（常に満たす）
    /// </summary>
    public bool IsEmpty =>
        (flags == null || flags.Count == 0) &&
        (items == null || items.Count == 0) &&
        character == CharacterCheck.Any &&
        partyHealth == HealthCheck.Any &&
        area == null &&
        (gimmicks == null || gimmicks.Count == 0) &&
        party == PartyCheck.Any &&
        partnerWithin <= 0.0f;

    /// <summary>
    /// すべての条件を満たしているか（空なら常に満たす）
    /// </summary>
    public static bool AllMet(List<GameCondition> conditions)
    {
        if(conditions == null) { return true; }

        foreach(GameCondition condition in conditions)
        {
            if(condition != null && !condition.IsMet()) { return false; }
        }
        return true;
    }

    /// <summary>
    /// どれか1つでも満たしているか（空なら常に満たす）
    /// </summary>
    public static bool AnyMet(List<GameCondition> conditions)
    {
        if(conditions == null || conditions.Count == 0) { return true; }

        foreach(GameCondition condition in conditions)
        {
            if(condition == null || condition.IsMet()) { return true; }
        }
        return false;
    }
}
