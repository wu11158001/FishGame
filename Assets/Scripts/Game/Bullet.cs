using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 10;
    [SerializeField] float RayDistance = 50f;

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
        if (!Object.HasStateAuthority)
            return;

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
        if(fish == null)
            fish = hit.GetComponentInParent<Fish>();

        if(fish == null)
        {
            Debug.LogError("找不到擊中魚的腳本");
            return;
        }

        FishData_Network data = fish.GetFishData();

        // 判斷階段(休閒/咬分/吐分)
        GamePeriod period = GamePeriod.IdlePeriod;
        GamePeriod playerPeriod = GameTempDataManagement.Instance.TempAccountData.GamePeriod;
        GamePeriod levelPeriod = GameTempDataManagement.Instance.CurrentLevelData.GamePeriod;
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
            double payoutPeriodValue = GameTempDataManagement.Instance.CurrentLevelData.PayoutPeriodValue;
            double suckingPeriodValue = GameTempDataManagement.Instance.CurrentLevelData.SuckingPeriodValue;
            double jackpot = GameTempDataManagement.Instance.CurrentLevelData.Jackpot;

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
                float lose = Mathf.Max(0, (float)GameTempDataManagement.Instance.CurrentLevelData.SuckingPeriodLose);
                probability /= lose;
                break;

            // 吐分期
            case GamePeriod.PayoutPeriod:
                // 增加倍率
                float add = Mathf.Max(0, (float)GameTempDataManagement.Instance.CurrentLevelData.PayoutPeriodAdd);
                probability *= add;
                break;
        }      

        double hitValue = UnityEngine.Random.value;
        if (hitValue <= probability)
        {
            // 獲得金幣
            double currDefaultCost = GameTempDataManagement.Instance.CurrentLevelData.DefaultCost;
            double reward = currDefaultCost * data.Magnification;

            // 判斷獎池
            if(GameTempDataManagement.Instance.CurrentLevelData.Jackpot < reward)
            {
                Debug.Log($"獎池不足!");
                return;
            }

            GameTempDataManagement.Instance.RecodJackpot -= reward;

            GameTempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward);
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
