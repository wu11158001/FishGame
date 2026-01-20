using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float Speed = 20;
    [SerializeField] float RayDistance = 100;
    [SerializeField] float BulletRadius = 0.4f;

    [Networked] Vector3 Direction { get; set; }
    [Networked] NetworkId TargetFishId { get; set; }

    Transform EffectPool;
    Fish LocalTargetFish;
    GameView GameView;

    readonly Vector2 MinBounds = new(-10f, -6f);
    readonly Vector2 MaxBounds = new(10f, 6f);

    private void Start()
    {
        GameView = FindFirstObjectByType<GameView>();
    }

    public void SetData(Fish targetFish)
    {
        if (Object.HasStateAuthority)
        {
            TargetFishId = (targetFish != null) ? targetFish.Object.Id : default;
        }
    }

    public override void Spawned()
    {
        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;
        LocalTargetFish = null;

        if (Object.HasStateAuthority)
        {
            Direction = transform.forward;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        Move();

        if (Object.HasStateAuthority)
        {
            CheckBounds();
            CheckHit();
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        // 目標獲取與合法性檢查
        if (TargetFishId.IsValid)
        {
            // 如果目前沒有緩存，或是緩存的物件已經被銷毀
            if (LocalTargetFish == null || LocalTargetFish.Object == null || !LocalTargetFish.Object.IsValid)
            {
                if (Runner.TryFindObject(TargetFishId, out var netObj))
                {
                    LocalTargetFish = netObj.GetComponent<Fish>();
                }
            }

            if (LocalTargetFish != null && !LocalTargetFish.gameObject.activeInHierarchy)
            {
                LocalTargetFish = null;
            }
        }
        else
        {
            LocalTargetFish = null;
        }

        // 有鎖定
        if (LocalTargetFish != null && LocalTargetFish.Object != null && LocalTargetFish.Object.IsValid)
        {
            Vector3 targetPos = LocalTargetFish.transform.position;
            targetPos.y = transform.position.y;
            Vector3 followDir = (targetPos - transform.position).normalized;

            if (followDir != Vector3.zero)
                transform.forward = followDir;
        }
        else
        {
            transform.forward = Direction;
        }

        transform.Translate(Vector3.forward * Speed * Runner.DeltaTime);
    }

    /// <summary>
    /// 邊界判斷反彈
    /// </summary>
    private void CheckBounds()
    {
        Vector3 pos = transform.position;
        Vector3 currentDir = Direction;
        bool hasBounced = false;

        if (pos.x <= MinBounds.x || pos.x >= MaxBounds.x)
        {
            currentDir.x = -currentDir.x;
            hasBounced = true;
        }

        if (pos.z <= MinBounds.y || pos.z >= MaxBounds.y)
        {
            currentDir.z = -currentDir.z;
            hasBounced = true;
        }

        if (hasBounced)
        {
            Direction = currentDir;
        }
    }

    /// <summary>
    /// 檢測擊中
    /// </summary>
    private void CheckHit()
    {
        if (!Object.HasStateAuthority)
            return;

        // 鎖定目標不存在
        if (LocalTargetFish != null)
        {
            if (LocalTargetFish.Object == null || !LocalTargetFish.Object.IsValid || !LocalTargetFish.gameObject.activeInHierarchy)
            {
                LocalTargetFish = null;
                TargetFishId = default; 
            }
        }

        LayerMask mask = LayerMask.GetMask("Fish");
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, BulletRadius, Vector3.down, RayDistance, mask);

        if (hits.Length > 0)
        {
            if (LocalTargetFish != null)
            {
                foreach (var hit in hits)
                {
                    Fish hitFish = hit.collider.GetComponentInParent<Fish>();
                    if (hitFish != null && hitFish == LocalTargetFish)
                    {
                        HitTarget(hitFish);
                        return;
                    }
                }
            }
            else
            {
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

            int specailMagnification = 0;
            switch (data.FishType)
            {
                // 特殊魚_魟魚
                case NetworkPrefabEnum.StingrayFish:
                    specailMagnification = UnityEngine.Random.Range((int)data.MinMagnification, (int)data.MaxMagnification + 1);
                    reward = currDefaultCost * specailMagnification;
                    break;
            }

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

            // 更新獎池與玩家金幣
            TempDataManagement.Instance.RecodJackpot -= reward;
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward);

            // 魚被擊中
            string eruptionCoinString = StringUtility.CurrencyFormat(reward);
            int seatIndex = TempDataManagement.Instance.LocalSeatIndex;
            bool isLocalShow = true;
            string rewardStr = "";

            switch (data.FishType)
            {
                // 特殊魚_魟魚
                case NetworkPrefabEnum.StingrayFish:
                    eruptionCoinString = $"{StringUtility.CurrencyFormat(specailMagnification)}X";
                    isLocalShow = false;
                    rewardStr = StringUtility.CurrencyFormat(reward);
                    break;

                // 特殊魚_鯊魚
                case NetworkPrefabEnum.SharkFish:
                    eruptionCoinString = "Spin !";
                    isLocalShow = false;
                    rewardStr = "Spin !";
                    break;
            }

            fish.GetHit(
                player: Runner.LocalPlayer, 
                fishType: data.FishType,
                eruptionCoinString: eruptionCoinString,
                rewardStr: rewardStr,
                seatIndex: seatIndex,
                isLocalShow: isLocalShow);
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
