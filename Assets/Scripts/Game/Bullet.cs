using UnityEngine;
using Fusion;
using System;

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

            if (LocalTargetFish != null && LocalTargetFish.IsDie)
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
            if (LocalTargetFish.Object == null || !LocalTargetFish.Object.IsValid || LocalTargetFish.IsDie)
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
            Runner.Despawn(Object);
            return;
        }

        // 產生擊中效果
        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                        key: NetworkPrefabEnum.HitEffect,
                        Pos: transform.position,
                        rot: Quaternion.identity,
                        parent: EffectPool,
                        player: Object.InputAuthority);

        FishData_Network fishData = fish.GetFishData();

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
        double probability = fishData.Probability;
        switch (period)
        {
            // 休閒期
            case GamePeriod.IdlePeriod:
                // 依照魚的機率
                probability = fishData.Probability;
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
            double reward = currDefaultCost * fishData.Magnification;

            // 特殊使用_轉盤Index
            int spinIndex = -1;
            // 是否及時更新金幣
            bool isUpdateCoin = true;
            // 爆金文字
            string eruptionCoinString = StringUtility.CurrencyFormat(reward);
            // 座位
            int seatIndex = TempDataManagement.Instance.LocalSeatIndex;
            // 是否只有本地顯示
            bool isLocalShow = true;

            int specailMagnification = 0;
            switch (fishData.FishType)
            {
                // 特殊魚_魟魚
                case NetworkPrefabEnum.StingrayFish:
                    specailMagnification = UnityEngine.Random.Range((int)fishData.MinMagnification, (int)fishData.MaxMagnification + 1);
                    reward = currDefaultCost * specailMagnification;

                    eruptionCoinString = $"{StringUtility.CurrencyFormat(specailMagnification)}X";
                    isLocalShow = false;
                    break;

                // 特殊魚_鯊魚
                case NetworkPrefabEnum.SharkFish:
                    int segmentCount = 8;
                    // 原始 step
                    double rawStep = (fishData.MaxMagnification - fishData.MinMagnification) / (double)(segmentCount - 1);
                    // 四捨五入到最近的「漂亮數字」(例如 5 或 10)
                    int step = (int)(Math.Round(rawStep / 5.0) * 5);
                    int[] values = new int[segmentCount];
                    for (int i = 0; i < segmentCount; i++)
                    {
                        values[i] = (int)(fishData.MinMagnification + step * i);
                        if (values[i] >= fishData.MaxMagnification) values[i] = (int)fishData.MaxMagnification;
                    }
                    spinIndex = UnityEngine.Random.Range(0, values.Length);
                    reward = currDefaultCost * values[spinIndex];

                    eruptionCoinString = "Big Win !";
                    isLocalShow = false;
                    break;
            }

            // 產生魚擊中效果
            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                            key: NetworkPrefabEnum.FishHitEffect,
                            Pos: fish.transform.position,
                            rot: Quaternion.identity,
                            parent: EffectPool,
                            player: Object.InputAuthority);

            // 判斷獎池
            if (TempDataManagement.Instance.CurrentLevelData.Jackpot < reward)
            {
                Debug.LogError($"獎池不足!");
                Runner.Despawn(Object);
                return;
            }

            // 當前不可射擊
            if (TempDataManagement.Instance.IsStopShot)
            {
                Debug.LogError($"當前不可射擊不判斷獎勵!");
                Runner.Despawn(Object);
                return;
            }

            switch(fishData.FishType)
            {
                // 特殊魚_鯊魚
                case NetworkPrefabEnum.SharkFish:
                    // 不及時更新金幣
                    isUpdateCoin = false;
                    // 不可射擊
                    TempDataManagement.Instance.IsStopShot = true;
                    // 開啟遮罩
                    GameView.MaskEnable(true);
                    break;
            }

            // 魚被擊中
            FishHitData fishHitData = new()
            {
                Player = Runner.LocalPlayer,
                FishType = fishData.FishType,
                EruptionCoinString = eruptionCoinString,
                Reward = reward,
                SeatIndex = seatIndex,
                IsLocalShow = isLocalShow,
                SpinWheelIndex = spinIndex,
            };
            fish.GetHit(fishHitData);

            // 更新獎池與玩家金幣
            TempDataManagement.Instance.RecodJackpot -= reward;
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: reward, isInvokeChange: isUpdateCoin);
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
