using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [Networked] Vector3 Direction { get; set; }

    [SerializeField] float HitRadius;

    Vector2 MinBounds = new(-9.6f, -5.4f);
    Vector2 MaxBounds = new(9.6f, 5.4f);
    [SerializeField]float Speed;

    public override void Spawned()
    {
        Direction = transform.forward;

        Speed = 10;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            Move();
            CheckBounds();
            CheckHit();
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        //transform.Translate(Vector3.right * Speed * Runner.DeltaTime);
        transform.position += Direction * Speed * Runner.DeltaTime;
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        /*Vector2 nextPos = (Vector2)transform.position + Direction * Speed * Runner.DeltaTime;

        // 檢查左右邊界
        if (nextPos.x < MinBounds.x || nextPos.x > MaxBounds.x)
        {
            Direction = new Vector2(-Direction.x, Direction.y);
        }

        // 檢查上下邊界
        if (nextPos.y < MinBounds.y || nextPos.y > MaxBounds.y)
        {
            Direction = new(Direction.x, -Direction.y);
        }

        transform.position += (Vector3)Direction * Speed * Runner.DeltaTime;

        // 讓子彈朝向移動方向 (2D 旋轉)
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);*/

        Vector3 pos = transform.position;

        // 檢查 X 軸邊界
        if (pos.x < MinBounds.x || pos.x > MaxBounds.x)
        {
            Direction = new Vector3(-Direction.x, 0, Direction.z);
        }
        // 檢查 Z 軸邊界 (3D 的前後等於 2D 的上下)
        if (pos.z < MinBounds.y || pos.z > MaxBounds.y)
        {
            Direction = new Vector3(Direction.x, 0, -Direction.z);
        }

        // 讓子彈的 Sprite 轉向 (如果你的子彈圖片是有方向性的)
        // 在 3D 中，如果是水平飛，通常是旋轉 Y 軸
        transform.forward = Direction;
    }

    /// <summary>
    /// 檢測擊中
    /// </summary>
    private void CheckHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            point: transform.position,
            radius: HitRadius,
            layerMask: LayerMask.GetMask("Fish"));

        if (hit != null)
        {
            HitTarget(hit);
        }
    }

    /// <summary>
    /// 擊中目標
    /// </summary>
    /// <param name="hit"></param>
    private void HitTarget(Collider2D hit)
    {
        var fish = hit.GetComponent<Fish>();
        FishData_Network data = fish.GetFishData();

        int hitValue = UnityEngine.Random.Range(0, 101);
        if (hitValue <= data.Rate)
        {
            fish.GetHit(Runner.LocalPlayer);
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: data.Reward);
        }

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, HitRadius);
    }
}
