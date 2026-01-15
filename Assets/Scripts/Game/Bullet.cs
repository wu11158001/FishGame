using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 30;
    [SerializeField] float RayDistance = 50f;

    [Networked] Vector3 Direction { get; set; }

    Transform EffectPool;
    Fish TargetFish;

    readonly Vector2 MinBounds = new(-9.6f, -5.4f);
    readonly Vector2 MaxBounds = new(9.6f, 5.4f);
    
    public void SetData(Fish targetFish)
    {
        TargetFish = targetFish;
    }

    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
        Direction = transform.forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid) 
            return;

        Move();
        CheckBounds();
        CheckHit();
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        if (!Object.HasStateAuthority)
            return;

        transform.Translate(Vector3.forward * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        if (!Object.HasStateAuthority)
            return;

        Vector3 pos = transform.position;

        if (pos.x < MinBounds.x || pos.x > MaxBounds.x)
        {
            // 有鎖定魚但反彈了，解除鎖定
            TargetFish = null;
            Direction = new Vector3(-Direction.x, 0, Direction.z);
        }

        if (pos.z < MinBounds.y || pos.z > MaxBounds.y)
        {
            // 有鎖定魚但反彈了，解除鎖定
            TargetFish = null;
            Direction = new Vector3(Direction.x, 0, -Direction.z);
        }

        transform.forward = Direction;
    }

    /// <summary>
    /// 檢測擊中
    /// </summary>
    private void CheckHit()
    {
        if (!Object.HasStateAuthority)
            return;

        RaycastHit hit;
        LayerMask mask = LayerMask.GetMask("Fish");

        if (Physics.Raycast(transform.position, Vector3.down, out hit, RayDistance, mask))
        {
            Fish hitFish = hit.collider.GetComponentInParent<Fish>();

            // 判斷是否有鎖定魚
            if (TargetFish != null)
            {
                if (hitFish != null && hitFish == TargetFish)
                {
                    HitTarget(hitFish);
                }
            }
            else if (hitFish != null)
            {
                HitTarget(hitFish);
            }
        }
    }

    /// <summary>
    /// 擊中目標
    /// </summary>
    /// <param name="hit"></param>
    private void HitTarget(Fish fish)
    {
        if (fish == null)
        {
            Debug.LogError("找不到擊中魚的腳本");
            return;
        }

        // 產生擊中效果
        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                        key: NetworkPrefabEnum.HitEffect,
                        Pos: transform.position,
                        rot: Quaternion.identity,
                        parent: EffectPool,
                        player: Object.InputAuthority);

        FishData_Network data = fish.GetFishData();

        // 判斷階段(休閒/咬分/吐分)
        GamePeriod period = GamePeriod.IdlePeriod;
        GamePeriod playerPeriod = TempDataManagement.Instance.TempAccountData.GamePeriod;
        GamePeriod levelPeriod = TempDataManagement.Instance.CurrentLevelData.GamePeriod;
        if(playerPeriod == GamePeriod.IdlePeriod)
        {
            // 玩家屬於休閒期，依照關卡設置
            period = levelPeriod;
        }
        else
        {
            // 玩家屬於吐分/咬分期，依照玩家設置
            period = playerPeriod;
        }

        // 如果屬於休閒期，判斷獎池
        if(period == GamePeriod.IdlePeriod)
        {
            double payoutPeriodValue = TempDataManagement.Instance.CurrentLevelData.PayoutPeriodValue;
            double suckingPeriodValue = TempDataManagement.Instance.CurrentLevelData.SuckingPeriodValue;
            double jackpot = TempDataManagement.Instance.CurrentLevelData.Jackpot;

            if (jackpot < suckingPeriodValue)
                period = GamePeriod.SuckingPeriod;
            else if(jackpot > payoutPeriodValue)
                period = GamePeriod.PayoutPeriod;
        }

        // 各階段給予機率變化
        double probability = data.Probability;
        switch (period)
        {
            // 休閒期
            case GamePeriod.IdlePeriod:
                // 依照魚的機率
                probability = data.Probability;
                break;

            // 咬分期
            case GamePeriod.SuckingPeriod:
                // 減少倍率
                float lose = Mathf.Max(0, (float)TempDataManagement.Instance.CurrentLevelData.SuckingPeriodLose);
                probability /= lose;
                break;

            // 吐分期
            case GamePeriod.PayoutPeriod:
                // 增加倍率
                float add = Mathf.Max(0, (float)TempDataManagement.Instance.CurrentLevelData.PayoutPeriodAdd);
                probability *= add;
                break;
        }      

        double hitValue = UnityEngine.Random.value;

        if (hitValue <= probability)
        {
            // 獲得金幣
            double currDefaultCost = TempDataManagement.Instance.CurrentLevelData.DefaultCost;
            double reward = currDefaultCost * data.Magnification;

            // 判斷獎池
            if(TempDataManagement.Instance.CurrentLevelData.Jackpot < reward)
            {
                Debug.Log($"獎池不足!");
                return;
            }

            TempDataManagement.Instance.RecodJackpot -= reward;

            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward);
            fish.GetHit(player: Runner.LocalPlayer, reward: reward);
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
