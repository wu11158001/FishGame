using UnityEngine;
using Fusion;
using System.Linq;
using System.Collections;
using System;

public class Fish : NetworkBehaviour
{
    // 爆金物件
    [SerializeField] GamePrefabEnum CoinTextType = GamePrefabEnum.CoinText_0;
    [SerializeField] GameObject FishModel;
    [SerializeField] Animator Animator;

    // 判斷是否已不存在場上
    [Networked] public NetworkBool IsDie { get; set; }
    // 移動計時器
    [Networked] TickTimer MoveTimer { get; set; }
    // 總移動時間
    [Networked] float TotalDuration { get; set; }

    // 魚資料
    [Networked] FishData_Network FishData_Network { get; set; }
    // 路線資料
    [Networked] FishPathData FishPathData { get; set; }
    // 動畫速度
    [Networked, OnChangedRender(nameof(UpdateAnimationSpeed))]
    float AniSpeed { get; set; }

    Vector3[] LocalPathPoints;

    WayPointMain WayPointMain;
    LocalPool LocalPool;
    Transform CoinTextPool;
    GameTerrain GameTerrain;
    FishManager FishManager;
    GameView GameView;
    SpecialEffectController SpecialEffectController;
    CameraShake CameraShake;
    Transform EffectPool;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        GameView = FindFirstObjectByType<GameView>();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (runner == null || !runner.IsRunning) return;

        if (Object != null && Object.HasStateAuthority)
        {
            // 更新場景中魚的數量
            if (GameTerrain != null)
                GameTerrain.UpdateCurrFishCount(-1);
        }

        if (FishManager == null)
            FishManager = UnityEngine.Object.FindFirstObjectByType<FishManager>();
        if (FishManager != null)
            FishManager.UnregisterFish(this);
    }

    public void SetData(NetworkPrefabEnum fishType, bool isMirror, int depth, int wayPointId, int skipWaypoint, float customDuration = 0)
    {
        // 獲取魚資料
        FishData fishData = FirestoreDataManagement.Instance?.GameTempData?.GetFishData(fishType);
        if (fishData != null)
        {
            FishData fishDataInstance = fishData.Clone();

            if (customDuration > 0)
                fishDataInstance.Duration = customDuration;

            FishData_Network = fishDataInstance.ToNetworkStruct();
        }

        // 設置路線資料
        FishPathData = new()
        {
            WayPointId = wayPointId,
            IsMirror = isMirror,
            Depth = depth,
            SkipWaypoint = skipWaypoint,
            SpeedMultiplier = 1
        };

        if (WayPointMain == null)
            WayPointMain = UnityEngine.Object.FindFirstObjectByType<WayPointMain>();

        WayPoint wayPoint = WayPointMain.GetWayPointById(wayPointId);
        int totalPoints = wayPoint.Points.Count;

        // 計算剩餘路徑的長度比例
        float pathRatio = (float)(totalPoints - skipWaypoint - 1) / (totalPoints - 1);
        pathRatio = Mathf.Clamp01(pathRatio);

        // 調整時間：如果是中途出生，總時間應該要縮短，否則魚會在起點發呆
        float baseDuration = FishData_Network.Duration;
        if (customDuration > 0) baseDuration = customDuration;

        // 魚跑完剩下這段路實際需要的時間
        float actualRemainingDuration = baseDuration * pathRatio;

        TotalDuration = baseDuration;
        MoveTimer = TickTimer.CreateFromSeconds(Runner, actualRemainingDuration);
        AniSpeed = 1;
    }

    public override void Spawned()
    {
        if (FishManager == null)
            FishManager = UnityEngine.Object.FindFirstObjectByType<FishManager>();
        if (FishManager != null)
            FishManager.RegisterFish(this);

        if (FishModel != null)
            FishModel.SetActive(true);

        EffectPool = GameObject.Find(FusionPoolNameEnum.EffectPool.ToString()).transform;

        SetPathPoints();

        // 初始化位置與面向
        if (LocalPathPoints != null && LocalPathPoints.Length >= 2)
        {
            Vector3 startPos = LocalPathPoints[0];
            startPos.y = FishPathData.Depth;
            transform.position = startPos;

            Vector3 nextPos = LocalPathPoints[1];
            nextPos.y = FishPathData.Depth;
            Vector3 initialDir = nextPos - startPos;
            if (initialDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(initialDir);
            }
        }

        if(Object.HasStateAuthority)
        {
            AniSpeed = 1;
            IsDie = false;
        }

        if (Animator != null) Animator.speed = AniSpeed;

        // 金龍
        if(FishData_Network.FishType == NetworkPrefabEnum.DragonFish && !Object.HasStateAuthority)
        {
            GetDragonAnimProgress();
        }

        // 更新場景中魚的數量
        if (Object != null && Object.HasStateAuthority)
        {
            if (GameTerrain == null)
                GameTerrain = FindFirstObjectByType<GameTerrain>();
            if (GameTerrain != null)
                GameTerrain.UpdateCurrFishCount(changeValue: 1);
        }            
    }

    public override void Render()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2)
        {
            SetPathPoints();
        }

        // 同步動畫速度
        if (Animator != null && Animator.speed != AniSpeed)
        {
            Animator.speed = AniSpeed;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (IsFreezeTimer())
            return;

        Move();
    }

    /// <summary>
    /// 獲取魚資料
    /// </summary>
    public FishData_Network GetFishData()
    {
        return FishData_Network;
    }

    #region 控制

    /// <summary>
    /// 是否在冰凍時間
    /// </summary>
    private bool IsFreezeTimer()
    {
        if (GameTerrain == null)
            GameTerrain = FindFirstObjectByType<GameTerrain>();

        if (GameTerrain == null)
            return false;

        bool isFreezing = GameTerrain.FreezeTimer.IsRunning && !GameTerrain.FreezeTimer.Expired(Runner);

        if (Object.HasStateAuthority)
        {
            if (isFreezing)
            {
                // 將移動時間往後推，冰凍結束才不會順移
                MoveTimer = TickTimer.CreateFromSeconds(Runner, (MoveTimer.RemainingTime(Runner) ?? 0) + Runner.DeltaTime);
                // 冰凍期間停止動畫
                if (AniSpeed != 0) AniSpeed = 0;
            }
            else
            {
                if (AniSpeed == 0) AniSpeed = 1;
            }
        }

        return isFreezing;
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2) return;

        float elapsed = TotalDuration - (MoveTimer.RemainingTime(Runner) ?? 0);
        float t = Mathf.Clamp01(elapsed / TotalDuration);

        // 1. 計算當前應有的位置
        Vector3 nextPos = GetCatmullRomPosition(t, LocalPathPoints);
        nextPos.y = FishPathData.Depth;

        // 2. 計算方向：預測未來的 t (例如當前 t + 0.01)
        float lookAheadT = Mathf.Clamp01(t + 0.01f);
        Vector3 lookTarget = GetCatmullRomPosition(lookAheadT, LocalPathPoints);

        Vector3 moveDir = lookTarget - nextPos; // 這是曲線的切線方向
        moveDir.y = 0; // 強制水平向，解決 X Z 旋轉問題

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            // 使用 SmoothDamp 或 Slerp 可以讓轉向更平滑，不會瞬間「跳」過去
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 10f);
        }

        // 最後更新位置
        transform.position = nextPos;

        if (t >= 1.0f && Object.HasStateAuthority)
        {
            IsDie = true;
            Runner.Despawn(Object);
        }            
    }

    /// <summary>
    /// 取得 Catmull-Rom 座標 (此座標應包含 3D 的 X, Y, Z)
    /// </summary>
    private Vector3 GetCatmullRomPosition(float t, Vector3[] points)
    {
        int count = points.Length;
        // 將 t (0~1) 映射到路徑段數
        float totalSteps = t * (count - 1);
        int i = Mathf.FloorToInt(totalSteps);
        float weight = totalSteps - i;

        if (i >= count - 1) return points[count - 1];

        // 取得四個控制點 (Clamp 確保不越界)
        Vector3 p0 = points[Mathf.Max(i - 1, 0)];
        Vector3 p1 = points[i];
        Vector3 p2 = points[Mathf.Min(i + 1, count - 1)];
        Vector3 p3 = points[Mathf.Min(i + 2, count - 1)];

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * weight +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * weight * weight +
            (-p0 + 3f * p1 - 3f * p2 + p3) * weight * weight * weight
        );
    }

    /// <summary>
    /// 設置剩餘路徑移動時間
    /// </summary>
    public void SetFishDuration(float finishTime)
    {
        RPC_SetFishDuration(finishTime);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetFishDuration(float finishTime)
    {
        if (!Object.HasStateAuthority)
            return;

        float remaining = MoveTimer.RemainingTime(Runner) ?? 0;
        float elapsed = TotalDuration - remaining;
        float currentT = Mathf.Clamp01(elapsed / TotalDuration);

        AniSpeed = 3;

        // 如果已經快跑完了，就不處理
        if (currentT >= 0.99f) return;

        float targetRemainingTime = finishTime;
        float newTotalDuration = targetRemainingTime / (1f - currentT);

        // 更新同步變數
        TotalDuration = newTotalDuration;
        // 重設 Timer
        MoveTimer = TickTimer.CreateFromSeconds(Runner, targetRemainingTime);
    }

    /// <summary>
    /// 更新動畫速度
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (Animator != null) Animator.speed = AniSpeed;
    }

    /// <summary>
    /// 設置路線
    /// </summary>
    private void SetPathPoints()
    {
        if (WayPointMain == null)
        {
            var wayPointObj = UnityEngine.Object.FindFirstObjectByType<WayPointMain>();
            if (wayPointObj != null)
                WayPointMain = wayPointObj.GetComponent<WayPointMain>();
        }

        if (WayPointMain != null)
        {
            WayPoint wayPoint = WayPointMain.GetWayPointById(FishPathData.WayPointId);

            // 移動路徑獲取
            var query = wayPoint.Points.Select(t => t.position);
            if (FishPathData.IsMirror) query = query.Reverse();
            LocalPathPoints = query.Skip(FishPathData.SkipWaypoint).ToArray();
        }
    }

    #endregion

    #region 擊中判斷

    /// <summary>
    /// 擊中判斷
    /// </summary>
    public void GetHit(double initCost, double addOdds, NetworkPrefabEnum hitEffect, Vector3 hitEffectPos)
    {
        if (IsDie)
            return;

        FishData_Network fishData = FishData_Network;

        // 產生擊中效果
        hitEffectPos.y = 0;
        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                key: hitEffect,
                Pos: hitEffectPos,
                rot: Quaternion.identity,
                parent: EffectPool,
                player: Object.InputAuthority);

        // 判斷階段(休閒/咬分/吐分)
        GamePeriod period = GamePeriod.IdlePeriod;
        GamePeriod playerPeriod = FirestoreDataManagement.Instance.GameTempData.TempAccountData.GamePeriod;
        GamePeriod levelPeriod = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.GamePeriod;
        if (playerPeriod == GamePeriod.IdlePeriod)
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
        if (period == GamePeriod.IdlePeriod)
        {
            double payoutPeriodValue = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.PayoutPeriodValue;
            double suckingPeriodValue = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.SuckingPeriodValue;
            double jackpot = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.Jackpot;

            if (jackpot < suckingPeriodValue)
                period = GamePeriod.SuckingPeriod;
            else if (jackpot > payoutPeriodValue)
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
                // 減少機率
                float lose = Mathf.Max(0, (float)FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.SuckingPeriodLose);
                probability /= lose;
                break;

            // 吐分期
            case GamePeriod.PayoutPeriod:
                // 增加機率
                float add = Mathf.Max(0, (float)FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.PayoutPeriodAdd);
                probability *= add;
                break;
        }

        double hitValue = UnityEngine.Random.value;

        if (hitValue <= probability)
        {
            // 獲得金幣
            double reward = initCost * (fishData.Magnification + addOdds);

            // 特殊使用_轉盤Index
            int spinIndex = -1;
            // 是否及時更新金幣
            bool isUpdateCoin = true;
            // 爆金文字
            string eruptionCoinString = StringUtility.CurrencyFormat(reward);
            // 座位
            int seatIndex = FirestoreDataManagement.Instance.GameTempData.LocalSeatIndex;
            // 是否只有本地顯示
            bool isLocalShow = true;

            switch (fishData.FishType)
            {
                // 特殊魚_魟魚
                case NetworkPrefabEnum.StingrayFish:
                    int specailMagnification = UnityEngine.Random.Range((int)fishData.MinMagnification, (int)fishData.MaxMagnification + 1);
                    reward = initCost * (specailMagnification + addOdds);

                    eruptionCoinString = $"{StringUtility.CurrencyFormat(specailMagnification + addOdds)}X";
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
                    reward = initCost * (values[spinIndex] + addOdds);

                    eruptionCoinString = "Big Win !";
                    isLocalShow = false;
                    break;

                // 特殊魚_金龍
                case NetworkPrefabEnum.DragonFish:
                    eruptionCoinString = $"{StringUtility.CurrencyFormat(fishData.Magnification + addOdds)}X";
                    isLocalShow = false;
                    break;
            }

            // 判斷獎池
            if (FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.Jackpot < reward)
                return;

            // 當前不可射擊
            if (FirestoreDataManagement.Instance.GameTempData.IsStopShot)
                return;

            // 產生魚捕獲效果
            BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();
            foreach (var collider in colliders)
            {
                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                            key: NetworkPrefabEnum.FishCatchEffect,
                            Pos: collider.transform.position,
                            rot: Quaternion.identity,
                            parent: EffectPool,
                            player: Object.InputAuthority);
            }

            switch (fishData.FishType)
            {
                // 特殊魚_魟魚
                case NetworkPrefabEnum.StingrayFish:
                    // 攝影機震動
                    if (CameraShake == null)
                        CameraShake = FindFirstObjectByType<CameraShake>();
                    if (CameraShake != null)
                        CameraShake.DoShake();
                    break;

                // 特殊魚_鯊魚
                case NetworkPrefabEnum.SharkFish:
                    // 不及時更新金幣
                    isUpdateCoin = false;
                    // 不可射擊
                    FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;
                    // 開啟遮罩
                    GameView.MaskEnable(true);

                    // 攝影機震動
                    if (CameraShake == null)
                        CameraShake = FindFirstObjectByType<CameraShake>();
                    if (CameraShake != null)
                        CameraShake.DoShake();
                    break;

                // 特殊魚_金龍
                case NetworkPrefabEnum.DragonFish:
                    // 不可射擊
                    FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;

                    // 金龍全屏捕獲魚
                    if (SpecialEffectController == null)
                        SpecialEffectController = UnityEngine.Object.FindFirstObjectByType<SpecialEffectController>();
                    if (SpecialEffectController != null)
                    {
                        WaterFullHitData waterFullHitData = new()
                        {
                            PlayerRef = Runner.LocalPlayer,
                            DefaultCost = initCost,
                            SeatIndex = seatIndex,
                            Odds = UnityEngine.Random.Range(1, 4),
                            DragonReward = reward,
                        };
                        SpecialEffectController.DragonFullHit(data: waterFullHitData);
                    }

                    // 攝影機震動
                    if (CameraShake == null)
                        CameraShake = FindFirstObjectByType<CameraShake>();
                    if (CameraShake != null)
                        CameraShake.DoShake();
                    break;

                // 流水魚_0
                case NetworkPrefabEnum.TurnoverFish_0:
                    // 攝影機震動
                    if (CameraShake == null)
                        CameraShake = FindFirstObjectByType<CameraShake>();
                    if (CameraShake != null)
                        CameraShake.DoShake();

                    // 更新玩家免費子彈
                    FirestoreDataManagement.Instance.GameTempData.ChangeTempAccountFreeBullet(changeValue: fishData.FreeBullet);
                    break;
            }

            // 魚被捕獲
            FishHitData fishHitData = new()
            {
                Player = Runner.LocalPlayer,
                EruptionCoinString = eruptionCoinString,
                Reward = reward,
                SeatIndex = seatIndex,
                IsLocalShow = isLocalShow,
                SpinWheelIndex = spinIndex,
            };
            GetCatch(fishHitData);

            // 更新獎池與玩家金幣
            FirestoreDataManagement.Instance.GameTempData.RecodJackpot -= reward;
            FirestoreDataManagement.Instance.GameTempData.ChangeTempAccountCoin(changeValue: reward, isInvokeChange: isUpdateCoin);
        }
    }

    #endregion

    #region 捕獲判斷與效果

    /// <summary>
    /// 顯示爆金文字
    /// </summary>
    private void ShowCoinText(string str, int seatIndex)
    {
        if (LocalPool == null)
            LocalPool = GameObject.FindFirstObjectByType<LocalPool>();

        if (CoinTextPool == null)
            CoinTextPool = GameObject.Find(LocalPoolNamEnum.CoinTextPool.ToString()).transform;

        if (LocalPool == null || CoinTextPool == null)
            return;

        Vector3 createPos = transform.position;
        createPos.y = 1;

        LocalPool.AcquirePrefabInstance<CoinText>(
            prefabType: CoinTextType,
            parent: CoinTextPool,
            pos: createPos,
            callback: (coinText) =>
            {
                coinText.SetData(str: str, recycleSeatIndex: seatIndex);
            });
    }

    /// <summary>
    /// 擊中效果
    /// </summary>
    private void HitEffect(FishHitData fishHitData)
    {
        // 顯示爆金文字
        ShowCoinText(str: fishHitData.EruptionCoinString.ToString(), seatIndex: fishHitData.SeatIndex);

        // 特殊魚，顯示捕獲介面
        if (FishData_Network.FishType == NetworkPrefabEnum.StingrayFish ||
            FishData_Network.FishType == NetworkPrefabEnum.SharkFish ||
            FishData_Network.FishType == NetworkPrefabEnum.DragonFish)
        {
            string rewardStr = StringUtility.CurrencyFormat(fishHitData.Reward);

            switch (FishData_Network.FishType)
            {
                case NetworkPrefabEnum.SharkFish:
                    rewardStr = "Spin !";
                    break;
            }

            AddressableManagement.Instance.OpenSpecialFishCatchView(
                    seatIndex: fishHitData.SeatIndex,
                    sprite: TextureManagement.Instance.GetFishTexture(FishData_Network.FishType),
                    rewardStr: rewardStr);
        }
    }

    /// <summary>
    /// 顯示輪盤
    /// </summary>
    private void ShowSpinWheel(FishHitData fishHitData)
    {
        if (AddressableManagement.Instance == null || FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
            return;

        _ = AddressableManagement.Instance.CreateGamePrefab(
                        prefabType: GamePrefabEnum.SpinWheel,
                        callback: (obj) =>
                        {
                            Vector3 pos = transform.position;
                            pos.y = 2;
                            obj.transform.position = pos;

                            obj.transform.rotation =
                                FirestoreDataManagement.Instance.GameTempData.IsMirror?
                                Quaternion.Euler(90, 180, 0) :
                                Quaternion.Euler(90, 0, 0);

                            SpinWheel spinWheel = obj.GetComponent<SpinWheel>();
                            if (spinWheel != null)
                            {
                                SpinWhellData whellData = new()
                                {
                                    RewardStr = StringUtility.CurrencyFormat(fishHitData.Reward),
                                    MinValu = FishData_Network.MinMagnification,
                                    MaxValu = FishData_Network.MaxMagnification,
                                    TargetIndex = fishHitData.SpinWheelIndex,
                                    SeatIndex = fishHitData.SeatIndex,
                                };

                                spinWheel.SetData(whellData);
                            }
                        });
    }

    /// <summary>
    /// 關閉魚顯示
    /// </summary>
    private void DisableModel()
    {
        if (FishModel != null)
            FishModel.SetActive(false);
    }

    /// <summary>
    /// 魚被捕獲
    /// </summary>
    public void GetCatch(FishHitData fishHitData)
    {
        if(fishHitData.IsLocalShow)
        {
            // 擊中效果
            HitEffect(fishHitData);

            if(fishHitData.SpinWheelIndex >= 0)
            {
                // 輪盤
                ShowSpinWheel(fishHitData);
            }
        }

        DisableModel();

        RPC_GetCatch(fishHitData);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_GetCatch(FishHitData fishHitData)
    {
        // 全域產生效果
        if(!fishHitData.IsLocalShow)
        {
            HitEffect(fishHitData);

            if (fishHitData.SpinWheelIndex >= 0)
            {
                // 輪盤
                ShowSpinWheel(fishHitData);
            }
        }

        DisableModel();

        if (Object.HasStateAuthority)
        {
            IsDie = true;
        }

        StartCoroutine(IYieldDespawn());
    }

    /// <summary>
    /// 延遲移除物件，等待RPC傳遞
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldDespawn()
    {
        yield return new WaitForSeconds(2);

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    #endregion

    #region 特殊判斷

    /// <summary>
    /// 獲取金龍動畫進度
    /// </summary>
    private void GetDragonAnimProgress()
    {
        Debug.Log("獲取動畫進度");
        RPC_GetDragonAnimProgress();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GetDragonAnimProgress()
    {
        AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0); 
        float progress = stateInfo.normalizedTime;
        string stateName = stateInfo.IsName("LtoR") ? "LtoR" : "RtoL";

        Debug.Log($"動畫進度: {stateName} = {progress}");

        RPC_SendDragonAnimProgress(stateName, progress);
    }

    /// <summary>
    /// 發送給所有人金龍動畫進度
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendDragonAnimProgress(string stateName, float progress)
    {
        if(!Object.HasStateAuthority)
        {
            Animator.Play(stateName, 0, progress % 1f);
        }
    }

    #endregion
}

/// <summary>
/// 路線資料
/// </summary>
public struct FishPathData : INetworkStruct
{
    /// <summary> 路線ID </summary>
    public int WayPointId;

    /// <summary> 是否反向移動 </summary>
    public NetworkBool IsMirror;

    /// <summary> 深度 </summary>
    public int Depth;

    /// <summary> 排除的路線點數量 </summary>
    public int SkipWaypoint;

    /// <summary> 中途加速用的倍率 </summary>
    public float SpeedMultiplier;
}

/// <summary>
/// 魚被捕獲資料
/// </summary>
public struct FishHitData : INetworkStruct
{
    /// <summary> 玩家 </summary>
    public PlayerRef Player;

    /// <summary> 爆金文字 </summary>
    public NetworkString<_32> EruptionCoinString;

    /// <summary> 最終獎勵 </summary>
    public double Reward;

    /// <summary> 移動座位 </summary>
    public int SeatIndex;

    /// <summary> 是否只在本地顯示 </summary>
    public NetworkBool IsLocalShow;

    /// <summary> 輪盤目標Index(-1 = 不顯示) </summary>
    public int SpinWheelIndex;
}