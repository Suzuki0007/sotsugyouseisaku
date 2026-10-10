using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EnemyShot", story: "敵の弾発射アクション", category: "Action", id: "48e3bc1300d3e6dc39a43a969b7c71a8")]
public partial class EnemyShotAction : Action
{
    //====================================================================================
    // 変数
    //====================================================================================

    [SerializeReference] 
    public BlackboardVariable<GameObject> Self;

    [SerializeReference]
    public BlackboardVariable<float> shootInterval;

    [SerializeReference]
    public BlackboardVariable<float> waitDuration;

    private float shotTimer;
    private float elapsedTimer;

    EnemyShooter shooter;

    //====================================================================================
    // 関数
    //====================================================================================

    protected override Status OnStart()
    {
        if(shootInterval.Value <= 0f) { Debug.Log("shootIntervalが0以下ですぞ"); }

        shooter = Self.Value.GetComponent<EnemyShooter>();
        if(shooter == null) { Debug.Log("EnemyShooterがnullですぞ"); }

        shotTimer = 0f;
        elapsedTimer = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        shotTimer += Time.deltaTime;
        elapsedTimer += Time.deltaTime;

        if(shotTimer >= shootInterval.Value)
        {
            Debug.Log("弾発射");
            shotTimer = 0f;
            shooter.Shoot();
        }

        if(elapsedTimer >= waitDuration.Value)
        {
            Debug.Log("waitDurationが経過したので成功を返す");
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

