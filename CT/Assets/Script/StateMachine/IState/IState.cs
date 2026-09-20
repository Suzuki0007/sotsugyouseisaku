/*********************************************************************/
// * \file   InterfaceState.cs
// * \brief  インターフェースステートクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

// interfaceとは複数のクラスで共通して使用される機能を定義する
public interface IState<TOwner>
{
    void Enter(TOwner owner);
    void Update(TOwner owner);
    void Exit(TOwner owner);
}

