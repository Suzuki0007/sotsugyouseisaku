using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// RequireComponentとは、指定されたコンポーネントを自動的に追加する属性です。
[RequireComponent(typeof(Image))]
public abstract class FadeBase : MonoBehaviour
{
    [SerializeField, Header("フェードの時間")]
    private float _fadeTime;

    private Image _image;// フェード用のImageコンポーネント

    private bool _isFading = false;// フェード中かどうか

    public bool IsFading { get { return _isFading; } }// フェード中かどうかを取得する

    protected abstract float StartAlpha { get; }// フェード開始時のアルファ値
    protected abstract float EndAlpha { get; }// フェード終了時のアルファ値

    
    protected virtual void Awake()
    {
        _image = GetComponent<Image>();
    }

    // Template Methodパターンを使用して、フェード処理の共通部分を定義する
    // Template Methodパターンとは、アルゴリズムの骨組みを定義し、具体的な処理はサブクラスに委譲するデザインパターンです。
    // PlayRoutineメソッドは、フェード処理を行うコルーチンです。
    public IEnumerator PlayRoutine()
    {
        if(_isFading)
        {
            yield break;// フェード中であれば処理を中断する
        }

        _isFading = true;
        _image.raycastTarget = true;// フェード中はUIの操作を無効化する

        float elapsedTime = 0.0f;
        SetAlpha(StartAlpha);

        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime; // ポーズ中も動く
            // Mathf.Lerpを使用して、アルファ値を線形補間する
            // Mathf.Lerpは、2つの値の間を線形補間する関数です。
            float alpha = Mathf.Lerp(StartAlpha, EndAlpha, elapsedTime / _fadeTime);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(EndAlpha);
        _image.raycastTarget = EndAlpha > 0.0f;// フェードが完了したらUIの操作を有効化する
        _isFading = false;
    }

    // フェード処理を開始する
    public void Play(Action onComplete)
    {
        if(_isFading)
        {
            return;
        }

        // Coroutineを使用して、フェード処理を非同期で実行する
        // Coroutineは、Unityのコルーチン機能を使用して、非同期処理を実行するための仕組みです。
        // StartCoroutineは、コルーチンを開始するためのメソッドです。
        StartCoroutine(PlayAndNotify(onComplete));
    }

    // フェード処理が完了したらonCompleteを呼び出す
    private IEnumerator PlayAndNotify(Action onComplete)
    {
        // PlayRoutineを実行して、フェード処理を行う
        yield return PlayRoutine();

        // フェード処理が完了したら、onCompleteを呼び出す
        if(onComplete != null)
        {
            onComplete();
        }
    }

    // Imageコンポーネントのアルファ値を設定する
    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        _image.color = new Color(color.r, color.g, color.b, alpha);
    }

}