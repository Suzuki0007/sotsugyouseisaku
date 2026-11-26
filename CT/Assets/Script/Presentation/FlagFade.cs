/*********************************************************************/
// * \file   FlagFade.cs
// * \brief  フラグでなめらかに出す：フラグの条件で、物を透明度でなめらかに出し入れする（回想中だけの家具・人影など）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// フラグでなめらかに出す（回想中だけ出す物など）
/// </summary>
///
/// ・条件をすべて満たしている間「満たしているとき」の物を出し、満たしていない間は「満たしていないとき」の物を出す
/// ・パッと消えずに、透明度でなめらかに出る・消える（絵・タイルマップ・UI）。消えきったら非表示にする（当たり判定も消える）
/// ・「回想の秒数に合わせる」をオンにすると、回想に入る・戻るときの秒数でなめらかに変わる
/// ・シーンを読み込み直したときは、すぐにその状態になる。FlagSwitch と同じ使い方（このコンポーネント自身の物は対象にしない）
[AddComponentMenu("演出/フラグでなめらかに出す")]
public class FlagFade : MonoBehaviour
{
    [Header("条件（すべて満たしたとき）")]

    [SerializeField]
    private List<FlagCondition> flagConditions = new List<FlagCondition>();

    [Header("出し入れする物")]

    [SerializeField, Tooltip("条件を満たしている間だけ出す（回想中だけの家具・人影など）")]
    private List<GameObject> activeWhenMet = new List<GameObject>();

    [SerializeField, Tooltip("条件を満たしていない間だけ出す（今の時代の物など）")]
    private List<GameObject> activeWhenNotMet = new List<GameObject>();

    [Header("なめらかさ")]

    [SerializeField, Label("回想の秒数に合わせる")]
    [Tooltip("オンで、回想に入る・戻る部品の秒数で変わる。オフで下の秒数")]
    private bool followFlashback = true;

    [SerializeField, Label("かける時間（秒）", 0.0f)]
    [Tooltip("出る・消えるまでの時間（回想の秒数に合わせないとき）")]
    private float seconds = 1.5f;

    private class Target
    {
        public GameObject go;
        public SpriteRenderer[] sprites;
        public Color[] spriteBase;
        public Tilemap[] tilemaps;
        public Color[] tilemapBase;
        public CanvasGroup[] groups;
        public float alpha;
        public Coroutine routine;
    }

    private readonly List<Target> met = new List<Target>();
    private readonly List<Target> notMet = new List<Target>();
    private bool initialized;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        if(activeWhenMet.Contains(gameObject) || activeWhenNotMet.Contains(gameObject))
        {
            Debug.LogWarning($"{name}: 自分自身が出し入れの対象に入っています。消えると切り替えが止まるため、別の物に付けてください", this);
        }

        Collect(activeWhenMet, met);
        Collect(activeWhenNotMet, notMet);
        initialized = true;

        GameFlags.OnChanged += Refresh;
        Apply(0.0f);
    }

    private void OnDestroy()
    {
        GameFlags.OnChanged -= Refresh;
    }

    private static void Collect(List<GameObject> source, List<Target> into)
    {
        for(int i = 0; i < source.Count; i++)
        {
            GameObject go = source[i];
            if(go == null) { continue; }
            var t = new Target { go = go };
            t.sprites = go.GetComponentsInChildren<SpriteRenderer>(true);
            t.spriteBase = new Color[t.sprites.Length];
            for(int k = 0; k < t.sprites.Length; k++) { t.spriteBase[k] = t.sprites[k].color; }
            t.tilemaps = go.GetComponentsInChildren<Tilemap>(true);
            t.tilemapBase = new Color[t.tilemaps.Length];
            for(int k = 0; k < t.tilemaps.Length; k++) { t.tilemapBase[k] = t.tilemaps[k].color; }
            t.groups = go.GetComponentsInChildren<CanvasGroup>(true);
            t.alpha = go.activeSelf ? 1.0f : 0.0f;
            into.Add(t);
        }
    }

    //=====================================================================
    // 切り替え
    //=====================================================================

    /// <summary> 条件に合わせて出し入れする </summary>
    public void Refresh()
    {
        if(!initialized) { return; }
        Apply(followFlashback ? PresentationDirector.LastFlashbackSeconds : seconds);
    }

    private void Apply(float duration)
    {
        bool isMet = FlagCondition.AllMet(flagConditions);
        for(int i = 0; i < met.Count; i++) { FadeTo(met[i], isMet ? 1.0f : 0.0f, duration); }
        for(int i = 0; i < notMet.Count; i++) { FadeTo(notMet[i], isMet ? 0.0f : 1.0f, duration); }
    }

    private void FadeTo(Target t, float to, float duration)
    {
        if(t.go == null) { return; }
        if(t.routine != null) { StopCoroutine(t.routine); t.routine = null; }

        if(duration <= 0.0f || !isActiveAndEnabled)
        {
            SetAlpha(t, to);
            return;
        }
        if(Mathf.Approximately(t.alpha, to) && t.go.activeSelf == (to > 0.0f)) { return; }
        t.routine = StartCoroutine(Fade(t, to, duration));
    }

    private IEnumerator Fade(Target t, float to, float duration)
    {
        if(to > 0.0f && !t.go.activeSelf) { SetAlpha(t, 0.0f); t.go.SetActive(true); }
        float from = t.alpha;
        float time = 0.0f;
        while(time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(t, Mathf.Lerp(from, to, Mathf.Clamp01(time / duration)));
            yield return null;
        }
        SetAlpha(t, to);
        t.routine = null;
    }

    private static void SetAlpha(Target t, float a)
    {
        t.alpha = a;
        for(int k = 0; k < t.sprites.Length; k++)
        {
            if(t.sprites[k] == null) { continue; }
            Color c = t.spriteBase[k]; c.a *= a; t.sprites[k].color = c;
        }
        for(int k = 0; k < t.tilemaps.Length; k++)
        {
            if(t.tilemaps[k] == null) { continue; }
            Color c = t.tilemapBase[k]; c.a *= a; t.tilemaps[k].color = c;
        }
        for(int k = 0; k < t.groups.Length; k++)
        {
            if(t.groups[k] != null) { t.groups[k].alpha = a; }
        }
        bool active = a > 0.0f;
        if(t.go.activeSelf != active) { t.go.SetActive(active); }
    }
}
