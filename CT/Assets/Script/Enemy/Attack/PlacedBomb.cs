/*********************************************************************/
// * \file   PlacedBomb.cs
// * \brief  攻撃「設置して起爆」で置かれる物（爆弾・罠）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 置かれた爆弾・罠
/// </summary>
///
/// ・攻撃のデータ（AttackData）の「設置して起爆」で自動で作られる。シーンに置かなくてよい
/// ・起爆：時間（起爆までの時間）／プレイヤーが触れる／攻撃される（ノア・シグルの攻撃が当たる）
/// ・爆発は攻撃のデータの形・ダメージ・当たる相手を使う（「当たる相手：両方」にすると敵も巻き込む）
/// ・置いた敵が倒れても残る。シーンを出ると消える
public class PlacedBomb : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    private AttackData data;
    private Vector2 direction;
    private float length;
    private GameObject attacker;

    private float elapsed;
    private bool exploded;
    private TelegraphView rangeView;
    private TelegraphView bodyView;

    private static readonly Color BodyColor = new Color(0.15f, 0.12f, 0.12f, 0.9f);

    //=====================================================================
    // 作成
    //=====================================================================

    /// <summary>
    /// 置く
    /// </summary>
    public static PlacedBomb Place(AttackData data, Vector2 position, Vector2 direction, float length, GameObject attacker)
    {
        GameObject go = data.placeObject != null
            ? Instantiate(data.placeObject, position, Quaternion.identity)
            : new GameObject("置かれた爆弾");
        go.transform.position = new Vector3(position.x, position.y, 0.0f);

        var bomb = go.AddComponent<PlacedBomb>();
        bomb.data = data;
        bomb.direction = direction;
        bomb.length = length;
        bomb.attacker = attacker;
        bomb.Setup();
        return bomb;
    }

    private void Setup()
    {
        // 触れる・攻撃されるを知るための当たり判定（トリガー）
        var trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f;

        if(data.detonateOnAttacked)
        {
            // 攻撃を受ける部品（体力はないので、当たったことだけ分かる）
            var receiver = gameObject.AddComponent<DamageReceiver>();
            receiver.OnHit += OnAttacked;
        }

        // 見た目がないときは小さな丸
        if(data.placeObject == null)
        {
            bodyView = TelegraphView.ShowCircle(transform.position, 0.3f, BodyColor, false, data.telegraphSortingOrder + 2);
        }

        if(data.showFuseRange)
        {
            rangeView = TelegraphView.Show(data, transform.position, direction, length);
        }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(exploded) { return; }

        if(!EnemyBlock.IsBlocked) { elapsed += Time.deltaTime; }

        if(data.fuseTime > 0.0f)
        {
            if(rangeView != null) { rangeView.SetProgress(elapsed / data.fuseTime); }
            if(elapsed >= data.fuseTime) { Explode(); }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(exploded || !data.detonateOnTouch) { return; }
        if(!other.CompareTag("Player")) { return; }
        if(data.Ignores(PlayerCharacters.Of(other.gameObject))) { return; }
        Explode();
    }

    private void OnAttacked(DamageInfo info)
    {
        // プレイヤーの攻撃（と、ほかの爆発など）で爆発する。置いた敵自身の攻撃では爆発しない
        if(info.attacker != null && info.attacker == attacker) { return; }
        Explode();
    }

    /// <summary>
    /// 爆発する
    /// </summary>
    public void Explode()
    {
        if(exploded) { return; }
        exploded = true;

        Vector2 position = transform.position;
        SeManager.PlaySe(data.activeSe, position);
        EffectManager.Play(data.activeEffect, data.shape.CenterOf(position, direction, length), Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        // 自分の当たり判定は消してから当てる（連鎖で自分に当たらないように）
        foreach(Collider2D col in GetComponents<Collider2D>()) { col.enabled = false; }
        AttackHits.Hit(data, position, direction, length, attacker, null);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if(rangeView != null) { rangeView.Hide(); }
        if(bodyView != null) { bodyView.Hide(); }
        rangeView = null;
        bodyView = null;
    }
}
