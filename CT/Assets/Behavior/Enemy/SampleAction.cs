using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "sample", story: "hhhhこれは練習ニダ", category: "Action", id: "117c27b1744664b771c15349976721c7")]
public partial class SampleAction : Action
{
    [SerializeReference] 
    public BlackboardVariable<float> time;

    protected override Status OnStart()
    {
        //time = new BlackboardVariable<float>(3f);
        if(time.Value <= 0f)
        {
            Debug.Log("timeが0以下ですぞ");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // タイムを減らす
        time.Value -= Time.deltaTime;

        // タイムが0以下になったら失敗を返す
        if(time.Value <= 0f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

