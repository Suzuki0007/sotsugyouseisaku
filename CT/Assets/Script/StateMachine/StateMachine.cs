/*********************************************************************/
// * \file   StateMachine.cs
// * \brief  ステートマシンクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

public class StateMachine<TOwner>
{
    // readonlyは、初回のみ代入可能で、以降は変更不可の変数を定義するための修飾子です。
    readonly TOwner owner;
    IState<TOwner> pending; // 次の状態を保持する変数

    public IState<TOwner> Current { get; private set; } // 現在の状態を保持する変数
    public IState<TOwner> Previous { get; private set; } // 前の状態を保持する変数

    // コンストラクタ
    public StateMachine(TOwner owner) 
    {
        this.owner = owner;
    }

    // 状態を変更
    public void ChangeState(IState<TOwner> next)
    {
        pending = next;
    }

    public void Update()
    {
        if(pending != null)
        {
            Apply(pending);// 状態を適用する
            // Current?.Uptate(owner); // nullチェック
            if(Current != null)
            {
                Current.Update(owner);
            }
        }
    }

    // 状態を適用する
    void Apply(IState<TOwner> next)
    {
        pending = null;

        // 現在の状態がnullでない場合、Exitを呼び出す
        if(Current != null)
        {
            Current.Exit(owner);
        }

        Previous = Current;     // 前の状態を更新
        Current = next;         // 現在の状態を更新
        Current.Enter(owner);   // 新しい状態に入る
    }

}