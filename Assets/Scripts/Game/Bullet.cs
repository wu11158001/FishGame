using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 10;
    [SerializeField] float RayDistance = 30f;

    [Networked] Vector3 Direction { get; set; }

    Transform EffectPool;

    readonly Vector2 MinBounds = new(-9.6f, -5.4f);
    readonly Vector2 MaxBounds = new(9.6f, 5.4f);
    
    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;

        Direction = transform.forward;
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
        transform.Translate(Vector3.forward * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        Vector3 pos = transform.position;

        if (pos.x < MinBounds.x || pos.x > MaxBounds.x)
        {
            Direction = new Vector3(-Direction.x, 0, Direction.z);
        }

        if (pos.z < MinBounds.y || pos.z > MaxBounds.y)
        {
            Direction = new Vector3(Direction.x, 0, -Direction.z);
        }

        transform.forward = Direction;
    }

    /// <summary>
    /// 檢測擊中
    /// </summary>
    private void CheckHit()
    {
        RaycastHit hit;
        LayerMask mask = LayerMask.GetMask("Fish");

        if (Physics.Raycast(transform.position, Vector3.down, out hit, RayDistance, mask))
        {
            HitTarget(hit.collider);
        }
    }

    /// <summary>
    /// 擊中目標
    /// </summary>
    /// <param name="hit"></param>
    private void HitTarget(Collider hit)
    {
        // 產生擊中效果
        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                        key: NetworkPrefabEnum.HitEffect,
                        Pos: transform.position,
                        rot: Quaternion.identity,
                        parent: EffectPool,
                        player: Object.InputAuthority);

        // 判斷是否擊中
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
        Gizmos.color = Color.red;
        Vector3 startPos = transform.position;
        Vector3 direction = Vector3.down * RayDistance;
        Gizmos.DrawRay(startPos, direction);
    }
}
