using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 10;
    [SerializeField] float RayDistance = 30f;

    [Networked] Vector3 Direction { get; set; }

    LocalPool LocalPool;
    Transform EffectPool;
    Transform CoinTextPool;

    readonly Vector2 MinBounds = new(-9.6f, -5.4f);
    readonly Vector2 MaxBounds = new(9.6f, 5.4f);
    
    public override void Spawned()
    {
        LocalPool = GameObject.FindFirstObjectByType<LocalPool>();
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
        CoinTextPool = GameObject.Find(LocalPoolNamEnum.CoinTextPool.ToString()).transform;

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

        double hitValue = UnityEngine.Random.value;
        if (hitValue <= data.Probability)
        {
            fish.GetHit(Runner.LocalPlayer);

            // 獲得金幣
            double currDefaultCost = TempDataManagement.Instance.CurrentLevelData.DefaultCost;
            double reward = currDefaultCost * data.Magnification;
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward);

            // 顯示爆金文字
            ShowCoinText(
                data.Magnification, 
                reward: reward);
        }

        Runner.Despawn(Object);
    }

    /// <summary>
    /// 顯示爆金文字
    /// </summary>
    private void ShowCoinText(double fishMagnification, double reward)
    {
        GamePrefabEnum coinTextType = GamePrefabEnum.CoinText_0;

        // 依照魚的倍率判斷顯示的爆金文字
        if (fishMagnification >= 0.1f)
            coinTextType = GamePrefabEnum.CoinText_0;
        else if(fishMagnification >= 0.05f)
            coinTextType = GamePrefabEnum.CoinText_1;
        else
            coinTextType = GamePrefabEnum.CoinText_0;

        Vector3 createPos = transform.position;
        createPos.y = 1;

        LocalPool.AcquirePrefabInstance<CoinText>(
            prefabType: coinTextType,
            parent: CoinTextPool,
            pos: createPos,
            callback: (coinText) =>
            {
                coinText.SetData(value: reward);
            });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 startPos = transform.position;
        Vector3 direction = Vector3.down * RayDistance;
        Gizmos.DrawRay(startPos, direction);
    }
}
