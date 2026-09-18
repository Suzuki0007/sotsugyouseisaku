
using UnityEngine;

// abstractとは、クラスがインスタンス化できない
public abstract class ComponentBase<TOwner> : MonoBehaviour where TOwner : ObjectBase
{
    protected TOwner Owner { get; private set; }
}