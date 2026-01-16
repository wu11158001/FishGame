using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 20;
    [SerializeField] float RayDistance = 100;
    [SerializeField] float BulletRadius = 0.4f;

    [Networked] Vector3 Direction { get; set; }

    Transform EffectPool;
    Fish TargetFish;

    readonly Vector2 MinBounds = new(-10f, -6f);
    readonly Vector2 MaxBounds = new(10f, 6f);
    
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

        // 有鎖定目標
        if (TargetFish != null && TargetFish.Object != null && TargetFish.Object.IsValid)
        {
            Vector3 targetPos = TargetFish.transform.position;
            targetPos.y = transform.position.y;

            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
                transform.forward = direction;
        }

        transform.Translate(Vector3.forward * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        if (!Object.HasStateAuthority)
            return;

        // 有鎖定魚
        if (TargetFish)
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

        LayerMask mask = LayerMask.GetMask("Fish");

        // 改用 SphereCastAll，它會回傳所有被這根「柱子」穿過的魚
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, BulletRadius, Vector3.down, RayDistance, mask);

        if (hits.Length > 0)
        {
            // 1. 先檢查有沒有撞到「鎖定目標」
            if (TargetFish != null)
            {
                foreach (var hit in hits)
                {
                    Fish hitFish = hit.collider.GetComponentInParent<Fish>();
                    if (hitFish != null && hitFish == TargetFish)
                    {
                        HitTarget(hitFish);
                        return; // 打中鎖定目標，直接結束
                    }
                }

                // 如果執行到這裡，代表雖然撞到了魚，但裡面沒有我們要的 TargetFish
                // 這時候子彈會繼續飛行，穿過這些雜魚。
            }
            else
            {
                // 2. 如果沒鎖定目標，就打中「第一條」碰到的魚（按距離排序）
                // SphereCastAll 的回傳順序不一定是按距離，保險起見可以排序或直接取第一筆
                Fish hitFish = hits[0].collider.GetComponentInParent<Fish>();
                if (hitFish != null)
                {
                    HitTarget(hitFish);
                }
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

            // 產生魚擊中效果
            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                            key: NetworkPrefabEnum.FishHitEffect,
                            Pos: fish.transform.position,
                            rot: Quaternion.identity,
                            parent: EffectPool,
                            player: Object.InputAuthority);

            TempDataManagement.Instance.RecodJackpot -= reward;

            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward);
            fish.GetHit(player: Runner.LocalPlayer, reward: reward);
        }

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        // 設定 Gizmos 顏色
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;

        // 1. 畫出起點球體
        Gizmos.DrawWireSphere(startPos, BulletRadius);

        // 2. 計算終點（假設向下射）
        Vector3 direction = Vector3.down;
        Vector3 endPos = startPos + direction * RayDistance;

        // 3. 畫出路徑線（畫四條線讓它看起來像圓柱體掃過）
        Gizmos.DrawLine(startPos + Vector3.left * BulletRadius, endPos + Vector3.left * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.right * BulletRadius, endPos + Vector3.right * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.forward * BulletRadius, endPos + Vector3.forward * BulletRadius);
        Gizmos.DrawLine(startPos + Vector3.back * BulletRadius, endPos + Vector3.back * BulletRadius);

        // 4. 畫出終點球體
        Gizmos.DrawWireSphere(endPos, BulletRadius);
    }
}
