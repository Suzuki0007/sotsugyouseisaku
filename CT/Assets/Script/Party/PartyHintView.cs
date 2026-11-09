/*********************************************************************/
// * \file   PartyHintView.cs
// * \brief  相棒の目印を表示する（シーンごとに自動で作られる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 目印を表示する（シーンごとに自動で作られる）
/// </summary>
public class PartyHintView : MonoBehaviour
{
    private class Marker
    {
        public GameObject root;
        public SpriteRenderer background;
        public TextMeshPro text;
    }

    private static PartyHintView instance;

    private readonly Dictionary<Transform, CharacterRestriction> requests = new Dictionary<Transform, CharacterRestriction>();
    private readonly Dictionary<Transform, Marker> shown = new Dictionary<Transform, Marker>();
    private readonly List<Marker> pool = new List<Marker>();
    private PartyHintSettings settings;

    public static PartyHintView Get()
    {
        if(instance == null)
        {
            var go = new GameObject("相棒の目印（自動で作られる）");
            instance = go.AddComponent<PartyHintView>();
        }
        return instance;
    }

    public void Request(Transform target, CharacterRestriction allowed, PartyHintSettings s)
    {
        settings = s;
        requests[target] = allowed;
    }

    private void LateUpdate()
    {
        // 今回呼ばれなかった札は片づける
        var remove = new List<Transform>();
        foreach(var pair in shown) { if(pair.Key == null || !requests.ContainsKey(pair.Key)) { remove.Add(pair.Key); } }
        foreach(Transform t in remove) { Release(shown[t]); shown.Remove(t); }

        foreach(var pair in requests)
        {
            if(pair.Key == null) { continue; }
            if(!shown.TryGetValue(pair.Key, out Marker m))
            {
                m = Acquire();
                shown[pair.Key] = m;

                // 目印が出た（相棒の出番の仕掛けに近づいた）
                GameObject current = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;
                Bark.Fire(BarkTrigger.PartnerGimmickNear, BarkContext.Of(current, pair.Key));
            }

            bool noah = pair.Value == CharacterRestriction.NoahOnly;
            m.text.text = noah ? settings.noahText : settings.sigleText;
            m.background.color = noah ? settings.noahColor : settings.sigleColor;

            // ふわふわ上下に動かす
            float bob = Mathf.Sin(Time.time * 4.0f) * 0.05f;
            m.root.transform.position = pair.Key.position + new Vector3(0.0f, settings.height + bob, 0.0f);
        }
        requests.Clear();
    }

    private Marker Acquire()
    {
        if(pool.Count > 0)
        {
            Marker reused = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            reused.root.SetActive(true);
            return reused;
        }

        var m = new Marker();
        m.root = new GameObject("目印");
        m.root.transform.SetParent(transform, false);

        // 背景（絵を決めた大きさに拡大する。Sliced は絵によっては警告が出るので使わない）
        var bg = new GameObject("背景");
        bg.transform.SetParent(m.root.transform, false);
        m.background = bg.AddComponent<SpriteRenderer>();
        m.background.sprite = settings.background;
        if(settings.background != null)
        {
            Vector2 native = settings.background.bounds.size;
            bg.transform.localScale = new Vector3(settings.backgroundSize.x / Mathf.Max(0.01f, native.x), settings.backgroundSize.y / Mathf.Max(0.01f, native.y), 1.0f);
        }
        m.background.sortingOrder = settings.sortingOrder;

        var textGo = new GameObject("文字");
        textGo.transform.SetParent(m.root.transform, false);
        m.text = textGo.AddComponent<TextMeshPro>();
        if(settings.font != null) { m.text.font = settings.font; }
        m.text.fontSize = settings.fontSize;
        m.text.alignment = TextAlignmentOptions.Center;
        m.text.color = Color.white;
        m.text.rectTransform.sizeDelta = new Vector2(3.0f, 1.0f);
        m.text.sortingOrder = settings.sortingOrder + 1;
        return m;
    }

    private void Release(Marker m)
    {
        if(m == null || m.root == null) { return; }
        m.root.SetActive(false);
        pool.Add(m);
    }
}
