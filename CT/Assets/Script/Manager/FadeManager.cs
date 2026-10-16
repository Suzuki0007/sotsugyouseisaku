/*********************************************************************/
// * \file   FadeManager.cs
// * \brief  フェードを管理するクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using System;
using System.Collections;
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    // フェードインクラス
    [SerializeField]
    private FadeIn _fadeIn;

    // フェードアウトクラス
    [SerializeField]
    private FadeOut _fadeOut;

    public bool IsFading { get { return _fadeIn.IsFading || _fadeOut.IsFading; } }

    // フェードアウトを開始する
    // IEnumeratorとは、C#のコルーチンを表すインターフェースです。
    // コルーチンとは、Unityで非同期処理を行うための仕組みです。
    public IEnumerator Out()
    {
        return _fadeOut.PlayRoutine();
    }

    // フェードインを開始する
    public IEnumerator In()
    {
        return _fadeIn.PlayRoutine();
    }

    // フェードアウト→フェードインの処理を行う
    public void Transition(Action onColor)
    {
        if(IsFading)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(onColor));
    }

    // フェードアウト→フェードインの処理を行う
    private IEnumerator TransitionRoutine(Action onColor)
    {
        // yieldとは、非同期処理を一時停止するために使用されます。
        yield return Out();

        if(onColor != null)
        {
            onColor();
        }

        yield return In();
    }  
}
