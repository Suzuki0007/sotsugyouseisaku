
/*********************************************************************/
// * \file   State.cs
// * \brief  ステートクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

public abstract class State<TOwner> : IState<TOwner>
{
    public virtual void Enter(TOwner owner) { }
    public virtual void Update(TOwner owner) { }
    public virtual void Exit(TOwner owner) { }
}