/*********************************************************************/
// * \file   CloudShadow.cs
// * \brief  流れる雲の影：地面の上を、薄い大きな影がゆっくり流れる（絵をくり返し並べた SpriteRenderer を動かす）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 流れる雲の影（外の場所の空気が動いて見える）
/// </summary>
///
/// ・薄い大きな影が、決まった向き・速さで地面の上を流れていく（止まった絵でも空気が動いて見える）
/// ・Hierarchy 右クリック > マップの見た目 > 流れる雲の影 で、マップの範囲を覆う大きさで作れる
/// ・濃さ・向き・速さはこの部品で変える。屋根のある室内には置かない
/// ・再生中だけ流れる（エディタでは止まったまま）。時間が止まる場面（手の動作の「まわりを止める」など）では止まる
[AddComponentMenu("マップの見た目/流れる雲の影")]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class CloudShadow : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("濃さ", 0.0f, 1.0f)]
    [Tooltip("影の濃さ（0＝見えない）。0.15〜0.25 くらいが自然。強くすると曇りの日のよう")]
    private float strength = 0.2f;

    [SerializeField, Label("流れる向き")]
    [Tooltip("影が流れる向き（(1, -0.3) で右へ、少し下へ）。長さは使わない")]
    private Vector2 direction = new Vector2(1.0f, -0.3f);

    [SerializeField, Label("速さ（マス／秒）", 0.0f, 5.0f)]
    [Tooltip("1 秒に何マス流れるか。0.3〜0.6 くらいがゆっくりで自然")]
    private float speed = 0.4f;

    [SerializeField, Label("くり返しの幅（マス）", 1.0f, 512.0f)]
    [Tooltip("雲の絵 1 枚の大きさ（マス）。絵を差し替えたときだけ合わせる（雲の絵は 32 マス）")]
    private float period = 32.0f;

    [SerializeField, Label("画素にそろえる")]
    [Tooltip("動く量をドット絵の 1 画素（1/64 マス）ごとにする（ドット絵のちらつきを防ぐ）")]
    private bool pixelSnap = true;

    private const float PixelSize = 1.0f / 64.0f;

    private Vector3 basePosition;
    private Vector2 offset;
    private SpriteRenderer spriteRenderer;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        basePosition = transform.position;
        ApplyStrength();
    }

    private void OnValidate()
    {
        if(spriteRenderer == null) { spriteRenderer = GetComponent<SpriteRenderer>(); }
        ApplyStrength();
    }

    private void ApplyStrength()
    {
        if(spriteRenderer == null) { return; }
        Color c = spriteRenderer.color;
        c.a = strength;
        spriteRenderer.color = c;
    }

    private void OnDisable()
    {
        if(spriteRenderer != null) { transform.position = basePosition; }
    }

    private void LateUpdate()
    {
        if(speed <= 0.0f || direction.sqrMagnitude < 0.0001f) { return; }

        // くり返しの幅 1 つ分動くと元と同じ見た目になるので、その範囲で回す（絵はその分大きく並べてある）
        offset += direction.normalized * (speed * Time.deltaTime);
        offset.x = Mathf.Repeat(offset.x + period * 0.5f, period) - period * 0.5f;
        offset.y = Mathf.Repeat(offset.y + period * 0.5f, period) - period * 0.5f;

        Vector2 o = offset;
        if(pixelSnap)
        {
            o.x = Mathf.Round(o.x / PixelSize) * PixelSize;
            o.y = Mathf.Round(o.y / PixelSize) * PixelSize;
        }
        transform.position = new Vector3(basePosition.x + o.x, basePosition.y + o.y, basePosition.z);
    }
}
