/*********************************************************************/
// * \file   InteractPrompt.cs
// * \brief  調べられる・拾えるときに、その物の上にキー（[E] 調べる など）を出す
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 調べる物が「キーの案内」をどう出すかを決める（決めない Intaractable は、種類ごとの決まった文で出す）
/// </summary>
public interface IInteractPrompt
{
    /// <summary> 今キーの案内を出すか（操作中のキャラが範囲にいて、E で何か起きるとき） </summary>
    bool PromptVisible { get; }

    /// <summary> キーのあとに出す文（調べる・拾う・締める など） </summary>
    string PromptLabel { get; }
}

/// <summary>
/// キーの案内の表示（自動で作られる。シーンをまたいで残る）
/// </summary>
///
/// ・操作中のキャラが調べる範囲に入っている物のうち、いちばん近い物の上に「[E] 調べる」などを出す
/// ・IInteractPrompt を持つ物（ItemInteraction・仕掛け・ペグなど）は、その判断に従う
///   （エリアに入るだけで動く物・使えないキャラ・もう何も起きない物には出さない）
/// ・持たない物（拾える物・セーブポイント・会話・宝箱など）は、範囲に操作中のキャラがいれば出す
/// ・会話・イベント・メニューの間は出さない。暗い場所でも見える（光の影響を受けない文字）
public class InteractPromptView : MonoBehaviour
{
    //=====================================================================
    // 設定
    //=====================================================================

    private const string KeyText = "E";
    private const float RefreshInterval = 0.5f;
    private const float AboveMargin = 0.35f;

    // IInteractPrompt を持たない種類の文
    private static readonly Dictionary<string, string> labels = new Dictionary<string, string>
    {
        { "ItemPickup", "拾う" },
        { "KeyItem", "拾う" },
        { "Treasure", "開ける" },
        { "SavePoint", "セーブ" },
        { "Dialogue", "話す" },
        { "DefeatedTalk", "話す" },
    };

    private static InteractPromptView instance;
    private static readonly FieldInfo playerField = typeof(Intaractable).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<Intaractable> candidates = new List<Intaractable>();
    private float refreshTimer;

    private TextMeshPro text;
    private Transform currentTarget;

    //=====================================================================
    // 自動で作る
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if(instance != null) { return; }

        var go = new GameObject("キーの案内（自動で作られる）");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<InteractPromptView>();
        instance.Build();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(instance != null) { instance.refreshTimer = 0.0f; instance.candidates.Clear(); }
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }

    private void Build()
    {
        var go = new GameObject("文字");
        go.transform.SetParent(transform, false);
        text = go.AddComponent<TextMeshPro>();
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(6.0f, 1.0f);
        text.sortingOrder = 300;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 220);
        text.gameObject.SetActive(false);
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if(refreshTimer <= 0.0f)
        {
            refreshTimer = RefreshInterval;
            candidates.Clear();
            candidates.AddRange(FindObjectsByType<Intaractable>());
        }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        GameObject current = manager != null ? manager.CurrentPlayer : null;

        Intaractable best = null;
        string bestLabel = null;
        if(current != null && !InputBlock.IsBlocked)
        {
            float bestDistance = float.MaxValue;
            foreach(Intaractable target in candidates)
            {
                if(target == null || !target.isActiveAndEnabled) { continue; }
                if(!TryGetLabel(target, current, out string label)) { continue; }

                float d = ((Vector2)target.transform.position - (Vector2)current.transform.position).sqrMagnitude;
                if(d < bestDistance) { bestDistance = d; best = target; bestLabel = label; }
            }
        }

        if(best == null)
        {
            if(text.gameObject.activeSelf) { text.gameObject.SetActive(false); }
            currentTarget = null;
            return;
        }

        if(!text.gameObject.activeSelf) { text.gameObject.SetActive(true); }
        if(currentTarget != best.transform || text.text.Length == 0)
        {
            text.text = $"<color=#FFE070>[{KeyText}]</color> {bestLabel}";
            currentTarget = best.transform;
        }
        text.transform.position = AbovePosition(best);
    }

    /// <summary>
    /// その物に今キーの案内を出すか、出すなら文
    /// </summary>
    private static bool TryGetLabel(Intaractable target, GameObject current, out string label)
    {
        label = null;

        if(target is IInteractPrompt prompt)
        {
            if(!prompt.PromptVisible) { return false; }
            label = prompt.PromptLabel;
            return true;
        }

        // 決まった文で出す種類：範囲の中に操作中のキャラがいるか
        if(!labels.TryGetValue(target.GetType().Name, out label)) { return false; }
        if(playerField == null) { return false; }
        return playerField.GetValue(target) as GameObject == current;
    }

    /// <summary>
    /// 物の上（見た目か当たり判定のいちばん上）
    /// </summary>
    private static Vector3 AbovePosition(Intaractable target)
    {
        Bounds? bounds = null;
        foreach(SpriteRenderer r in target.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!r.enabled || r.sprite == null) { continue; }
            if(bounds.HasValue) { Bounds b = bounds.Value; b.Encapsulate(r.bounds); bounds = b; } else { bounds = r.bounds; }
        }
        if(!bounds.HasValue)
        {
            Collider2D col = target.GetComponent<Collider2D>();
            if(col != null) { bounds = col.bounds; }
        }

        Vector3 pos = bounds.HasValue ? new Vector3(bounds.Value.center.x, bounds.Value.max.y, 0.0f) : target.transform.position;
        pos.y += AboveMargin;
        pos.z = 0.0f;
        return pos;
    }
}
