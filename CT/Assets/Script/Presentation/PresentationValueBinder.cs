/*********************************************************************/
// * \file   PresentationValueBinder.cs
// * \brief  演出の数値で動かす：数値（手の動作の進み具合・回数など）で、物を回す・伸ばす・動かす・絵を替える・出す
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出の数値で動かす（時計の針を回す・手を伸ばす・掘られた土の絵を替える・墓標を 1 つずつ立てる）
/// </summary>
///
/// ・物に付けて「数値の名前」を入れると、その数値に合わせて動く（手の動作の進み具合を、画面の中の物で見せる）
/// ・数値は「値の範囲」で 0〜1 に直してから使う（回数 0〜5 なら 範囲 0〜5）
/// ・回す（Z の角度）／大きさ／位置のずれ／絵を段ごとに替える／子を順に出す、から選ぶ
[AddComponentMenu("演出/演出の数値で動かす")]
public class PresentationValueBinder : MonoBehaviour
{
    public enum Mode
    {
        [InspectorName("回す（角度）")]
        Rotate = 0,

        [InspectorName("大きさ")]
        Scale = 1,

        [InspectorName("位置をずらす")]
        Move = 2,

        [InspectorName("絵を段ごとに替える")]
        Sprites = 3,

        [InspectorName("子を順に出す（墓標を 1 つずつ など）")]
        ShowChildren = 4,
    }

    [SerializeField, Label("数値の名前")]
    [Tooltip("部品「手の動作を待つ」の「進み具合を入れる数値」「回数を入れる数値」、部品「演出の数値を変える」で使う名前")]
    private string valueName = "";

    [SerializeField, Label("値の範囲（から）")]
    private float valueFrom = 0.0f;

    [SerializeField, Label("値の範囲（まで）")]
    [Tooltip("この値で動きが終わりになる（割合なら 1、回数 5 回なら 5）")]
    private float valueTo = 1.0f;

    [SerializeField, Label("動かし方")]
    private Mode mode = Mode.Rotate;

    [SerializeField, Label("角度（始め）")]
    private float angleFrom = 0.0f;

    [SerializeField, Label("角度（終わり）")]
    [Tooltip("時計の針なら -360（右回りに 1 周）")]
    private float angleTo = -360.0f;

    [SerializeField, Label("大きさ（始め）")]
    private Vector3 scaleFrom = Vector3.one;

    [SerializeField, Label("大きさ（終わり）")]
    private Vector3 scaleTo = new Vector3(2.0f, 1.0f, 1.0f);

    [SerializeField, Label("ずらす量（終わり）")]
    [Tooltip("置いた位置から、終わりにどれだけずらすか（マス）")]
    private Vector3 moveTo = new Vector3(0.0f, 1.0f, 0.0f);

    [SerializeField]
    [Tooltip("絵を段ごとに替える：値に合わせて上から順に使う絵")]
    private List<Sprite> sprites = new List<Sprite>();

    [SerializeField, Label("なめらかに追う秒数", 0.0f)]
    [Tooltip("値が飛んだときに、この秒数で追いつく（0 ならすぐ）")]
    private float smooth = 0.1f;

    private Vector3 basePosition;
    private float shown = -1.0f;
    private float target;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        basePosition = transform.localPosition;
        TryGetComponent(out spriteRenderer);
    }

    private void OnEnable()
    {
        PresentationValues.Changed += OnChanged;
        target = Normalize(PresentationValues.Get(valueName));
        shown = target;
        Apply(shown);
    }

    private void OnDisable()
    {
        PresentationValues.Changed -= OnChanged;
    }

    private void OnChanged(string name, float value)
    {
        if(name == valueName) { target = Normalize(value); }
    }

    private float Normalize(float v)
    {
        float span = valueTo - valueFrom;
        return Mathf.Abs(span) < 0.0001f ? 0.0f : Mathf.Clamp01((v - valueFrom) / span);
    }

    private void LateUpdate()
    {
        if(Mathf.Approximately(shown, target)) { return; }
        shown = smooth <= 0.0f ? target : Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / smooth);
        Apply(shown);
    }

    private void Apply(float t)
    {
        switch(mode)
        {
            case Mode.Rotate:
                transform.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Lerp(angleFrom, angleTo, t));
                break;
            case Mode.Scale:
                transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, t);
                break;
            case Mode.Move:
                transform.localPosition = basePosition + moveTo * t;
                break;
            case Mode.Sprites:
                if(spriteRenderer != null && sprites.Count > 0)
                {
                    int i = Mathf.Clamp(Mathf.FloorToInt(t * sprites.Count), 0, sprites.Count - 1);
                    if(t >= 1.0f) { i = sprites.Count - 1; }
                    spriteRenderer.sprite = sprites[i];
                }
                break;
            case Mode.ShowChildren:
            {
                int n = transform.childCount;
                int show = Mathf.RoundToInt(t * n);
                for(int i = 0; i < n; i++)
                {
                    GameObject c = transform.GetChild(i).gameObject;
                    bool on = i < show;
                    if(c.activeSelf != on) { c.SetActive(on); }
                }
                break;
            }
        }
    }
}
