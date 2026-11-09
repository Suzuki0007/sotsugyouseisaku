/*********************************************************************/
// * \file   EnemyEgg.cs
// * \brief  行動「仲間を呼ぶ」の卵（時間でかえる。かえる前に壊せる）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の卵
/// </summary>
///
/// ・「仲間を呼ぶ」で「卵を置く」を選んだときに自動で作られる。シーンに置かなくてよい
/// ・かえるまでの時間が経つと敵が出る。それまでに攻撃して体力を 0 にすると壊れる
/// ・敵（タグ・レイヤー Enemies）と同じように、ノアの範囲攻撃・シグルの弾が当たる
public class EnemyEgg : MonoBehaviour
{
    private EnemyData enemy;
    private float hatchTime;
    private float elapsed;
    private PatternRunner owner;
    private EffectData effect;
    private SeData se;
    private Health health;
    private SpriteRenderer sr;
    private TelegraphView circle;

    /// <summary>
    /// 卵を置く
    /// </summary>
    public static EnemyEgg Create(EnemyData enemy, Vector2 position, float hatchTime, int hp, Sprite sprite, PatternRunner owner, EffectData effect, SeData se)
    {
        var go = new GameObject($"卵（{enemy.DisplayName}）");
        go.transform.position = new Vector3(position.x, position.y, 0.0f);
        go.tag = "Enemies";
        go.layer = LayerMask.NameToLayer("Enemies");

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        var egg = go.AddComponent<EnemyEgg>();
        egg.enemy = enemy;
        egg.hatchTime = hatchTime;
        egg.owner = owner;
        egg.effect = effect;
        egg.se = se;

        egg.health = go.AddComponent<Health>();
        egg.health.SetMaxHealth(hp, true);
        egg.health.OnDied += egg.OnBroken;
        egg.health.OnDamaged += _ => egg.Flash();

        if(sprite != null)
        {
            egg.sr = go.AddComponent<SpriteRenderer>();
            egg.sr.sprite = sprite;
            egg.sr.sortingOrder = 5;
        }
        else
        {
            egg.circle = TelegraphView.ShowCircle(position, 0.4f, new Color(0.85f, 0.8f, 0.6f, 0.95f), false, 5);
        }
        return egg;
    }

    private void Update()
    {
        if(EnemyBlock.IsBlocked) { return; }

        elapsed += Time.deltaTime;

        // かえる直前はふくらんだり縮んだりする
        float t = elapsed / Mathf.Max(0.1f, hatchTime);
        float pulse = t > 0.7f ? 1.0f + Mathf.Sin(Time.time * 30.0f) * 0.08f : 1.0f;
        if(sr != null) { transform.localScale = new Vector3(pulse, pulse, 1.0f); }

        if(elapsed >= hatchTime) { Hatch(); }
    }

    private void Hatch()
    {
        Vector2 position = transform.position;
        EffectManager.Play(effect, position);
        SeManager.PlaySe(se, position);

        Enemy spawned = Enemy.Spawn(enemy, position);
        if(owner != null) { owner.AddSummoned(spawned); }

        Destroy(gameObject);
    }

    private void OnBroken()
    {
        EffectManager.Play(enemy != null ? enemy.DeathEffect : null, transform.position);
        Destroy(gameObject);
    }

    private void Flash()
    {
        if(sr != null) { sr.color = new Color(1.0f, 0.6f, 0.6f); Invoke(nameof(ResetColor), 0.1f); }
    }

    private void ResetColor()
    {
        if(sr != null) { sr.color = Color.white; }
    }

    private void OnDestroy()
    {
        if(circle != null) { circle.Hide(); }
        circle = null;
    }
}
