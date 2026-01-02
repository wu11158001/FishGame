using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [Networked] Vector2 Direction { get; set; }

    Vector2 MinBounds = new(-9.6f, -5.4f);
    Vector2 MaxBounds = new(9.6f, 5.4f);
    float Speed = 10f;

    [Networked]
    private int Count { get; set; }

    public override void Spawned()
    {
        Direction = transform.rotation * Vector2.right;
        Count = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            Move();
            CheckBounds();
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        transform.Translate(Vector3.right * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        Vector2 nextPos = (Vector2)transform.position + Direction * Speed * Runner.DeltaTime;

        // 檢查左右邊界
        if (nextPos.x < MinBounds.x || nextPos.x > MaxBounds.x)
        {
            Direction = new Vector2(-Direction.x, Direction.y);

            Count++;
        }
        // 檢查上下邊界
        if (nextPos.y < MinBounds.y || nextPos.y > MaxBounds.y)
        {
            Direction = new(Direction.x, -Direction.y);

            Count++;
        }

        transform.position += (Vector3)Direction * Speed * Runner.DeltaTime;

        // 讓子彈朝向移動方向 (2D 旋轉)
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if(Count >= 3)
            Runner.Despawn(Object);
    }
}
