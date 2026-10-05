using UnityEngine;

public class EnemyMovement : CharaBase
{
    public void Move(float speed)
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}
