using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "abcdefuck", story: "ghijklmniger", category: "Action", id: "41c461d8c96988060f8e258cd53cf1f1")]
public partial class AbcdefuckAction : Action
{
    /// <summary>
    /// BlackboardVariableを使ってBehaviorTreeのInspector上で設定する
    /// </summary>
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    EnemyMovement movement;

    protected override Status OnStart()
    {
        movement = Self.Value.GetComponent<EnemyMovement>();
        if(movement == null) 
        {
            Debug.Log("EnemyMoveComponentがnullですぞ");
            return Status.Failure; 
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        movement.Move(0.5f);

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

