using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    public float Speed = 20f;
    public float lifeTime = 2f;

    [Networked] 
    private Vector2 Direction { get; set; }

    private Vector2 MinBounds;
    private Vector2 MaxBounds;

    [Networked]
    private int Count { get; set; }

    public override void Spawned()
    {
        // 取得螢幕邊界的計算 (世界座標)
        Camera cam = Camera.main;
        MinBounds = cam.ViewportToWorldPoint(new Vector2(0, 0));
        MaxBounds = cam.ViewportToWorldPoint(new Vector2(1, 1));

        Direction = transform.rotation * Vector2.right;
        Count = 0;
    }

    public override void FixedUpdateNetwork()
    {
        // 只有狀態權限者計算位移與反彈
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
