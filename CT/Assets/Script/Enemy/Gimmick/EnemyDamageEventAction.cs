/*********************************************************************/
// * \file   EnemyDamageEventAction.cs
// * \brief  イベントの部品「敵にダメージ」
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベントの部品「敵にダメージ」
/// </summary>
///
/// ・ギミックを作動させたら敵にダメージ（天井を落とす・電気を流す など）
/// ・攻撃の種類は既定で「ギミック」。敵のデータの効き方で「ギミックでしか倒せない」敵も作れる
/// ・攻撃を受ける部品（DamageReceiver）を指定すると、弱点などその部分に当てる（部位のダメージの倍率が効く）
public class EnemyDamageEventAction : EventAction
{
    [SerializeField]
    [Tooltip("ダメージを与える敵")]
    private List<Enemy> enemies = new List<Enemy>();

    [SerializeField]
    [Tooltip("ダメージを与える部分（敵の弱点など、攻撃を受ける部品）。敵に直接当てるなら空でよい")]
    private List<DamageReceiver> parts = new List<DamageReceiver>();

    [SerializeField, Label("ダメージ", 1)]
    private int damage = 1;

    [SerializeField, Label("攻撃の種類")]
    private DamageKind kind = DamageKind.Gimmick;

    [SerializeField, Label("エフェクト")]
    [Tooltip("当たった敵の位置に出す")]
    private EffectData effect;

    [SerializeField, Label("SE")]
    private SeData se;

    public override void Execute(GameObject gameObject)
    {
        foreach(Enemy enemy in enemies)
        {
            if(enemy == null || enemy.Health == null) { continue; }
            Hit(enemy.transform.position);
            enemy.Health.Damage(DamageInfo.Create(damage, kind, gameObject, enemy.transform.position));
        }

        foreach(DamageReceiver part in parts)
        {
            if(part == null) { continue; }
            Hit(part.transform.position);
            part.Receive(DamageInfo.Create(damage, kind, gameObject, part.transform.position));
        }
    }

    private void Hit(Vector3 position)
    {
        EffectManager.Play(effect, position);
        SeManager.PlaySe(se, position);
    }
}
