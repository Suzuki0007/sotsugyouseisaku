/*********************************************************************/
// * \file   AttackSwitch.cs
// * \brief  攻撃が当たると作動するスイッチ（ギミック）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 攻撃が当たると作動するスイッチ
/// </summary>
///
/// ・Collider2D と一緒に付ける（攻撃を受ける部品 DamageReceiver も自動で付く）
/// ・ノアの範囲攻撃・シグルの弾・敵の攻撃や弾など、当たった攻撃を条件で絞って、EventSequence を実行する
///   例：シグルの弾でしか作動しない的 ／ 敵の突進を当てると壊れる柱 ／ ゲージ攻撃のパーフェクトで割れる岩
/// ・「1回だけ」なら作動したことをセーブし、シーンに戻ったときは作動後の見た目にする（EventSequence の復元）
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(DamageReceiver))]
public class AttackSwitch : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("反応する攻撃")]

    [SerializeField, Label("攻撃の種類")]
    private DamageKindFilter kind = DamageKindFilter.Any;

    [SerializeField, Label("攻撃したキャラ")]
    [Tooltip("「プレイヤー以外」にすると、敵の攻撃・ギミックだけで作動する")]
    private CharacterFilter character = CharacterFilter.Any;

    [SerializeField, Label("ゲージの判定")]
    [Tooltip("ゲージ攻撃のとき、判定の結果で絞る（「なし」なら問わない）")]
    private GaugeJudge judge = GaugeJudge.None;

    [SerializeField, Label("作動までの回数", 1)]
    [Tooltip("条件に合う攻撃がこの回数当たったら作動する")]
    private int requiredHits = 1;

    [Header("作動したとき")]

    [SerializeField, Label("実行するイベント")]
    private EventSequence sequence;

    [SerializeField, Label("1回だけ")]
    [Tooltip("オン：1回作動したら、もう反応しない（セーブもする）／オフ：何度でも作動する")]
    private bool once = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する（名前や親を変えると記録がリセットされる）")]
    private string saveId = "";

    [SerializeField, Label("当たったときのエフェクト")]
    [Tooltip("条件に合う攻撃が当たるたびに出す")]
    private EffectData hitEffect;

    [SerializeField, Label("当たったときの SE")]
    private SeData hitSe;

    [SerializeField, Label("効かない攻撃のエフェクト")]
    [Tooltip("条件に合わない攻撃が当たったときに出す（「これでは駄目」と分かるように）。空なら何もしない")]
    private EffectData blockedEffect;

    private DamageReceiver receiver;
    private int hits;
    private bool done;
    private string saveKey;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public bool Done => done;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        receiver = GetComponent<DamageReceiver>();
        receiver.OnHit += OnHit;
        saveKey = MakeSaveKey(gameObject, saveId);
    }

    private void Start()
    {
        if(once && Save.Get(saveKey, false))
        {
            done = true;
            if(sequence != null) { sequence.Restore(gameObject); }
        }
    }

    private void OnDestroy()
    {
        if(receiver != null) { receiver.OnHit -= OnHit; }
    }

    private void OnHit(DamageInfo info)
    {
        if(done && once) { return; }

        var rule = new DamageRule { kind = kind, character = character, judge = judge };
        if(!rule.Matches(info))
        {
            EffectManager.Play(blockedEffect, info.point);
            return;
        }

        hits++;
        EffectManager.Play(hitEffect, info.point);
        SeManager.PlaySe(hitSe, info.point);

        if(hits < requiredHits) { return; }

        hits = 0;
        if(once)
        {
            done = true;
            Save.SetPending(saveKey, true);
        }
        if(sequence != null) { sequence.Play(gameObject); }
    }

    /// <summary>
    /// 作動したことを記録するセーブ用の名前
    /// </summary>
    public static string MakeSaveKey(GameObject target, string id)
    {
        string name = string.IsNullOrEmpty(id) ? SceneObjectPath(target.transform) : id;
        return "Switch/" + target.scene.name + "/" + name;
    }

    private static string SceneObjectPath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }
}
