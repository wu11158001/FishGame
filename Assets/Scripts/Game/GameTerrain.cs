using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class GameTerrain : NetworkBehaviour
{
    [Header("Seat")]
    [SerializeField] public List<Transform> Seats;

    [Header("Fish Value")]
    // 最大生成數量
    [SerializeField] int MaxCreateFishCount = 20;
    // 初始生成數量
    [SerializeField] int InitCreateFishCount = 12;
    // 一般魚生成時間(秒)
    [SerializeField] float NormalFishCreatTime = 2;
    // 一般魚一次生成最小數量
    [SerializeField] int MinCreateNormalFish = 1;
    // 一般魚一次生成最大數量
    [SerializeField]  int MaxCreateNormalFish = 2;
    // 最大魚深度
    [SerializeField] int MaxFishDepth = -40;
    // 最小魚深度
    [SerializeField] int MinFishDepth = -5;

    [Header("Water Wave")]
    // 浪潮效果持續時間
    [SerializeField] float WaterWaveDuration = 4;

    // 紀錄座位上玩家ID
    [Networked, Capacity(4), OnChangedRender(nameof(OnSeatPlayerIsChange))]
    NetworkArray<int> SeatPlayerIDs { get; }

    // 產生一般魚計時器
    [Networked] TickTimer SpawnTimer { get; set; }
    // 首次產生魚
    [Networked] bool IsFirstCreate { get; set; }
    // 場景中魚的數量
    [Networked] int CurrFishCount { get; set; }

    // 產生特殊魚計時器
    [Networked] TickTimer SpecialSpawnTimer { get; set; }

    // 遊戲狀態
    [Networked] GameState CurrentState { get; set; }
    // 遊戲狀態更換時間
    [Networked] public TickTimer StateChangeTimer { get; set; }

    // 當前浪潮魚產生Index
    [Networked] int CurrWaterWaveFishIndex { get; set; }

    // 冰凍時間
    [Networked] public TickTimer FreezeTimer { get; set; }

    [Networked, OnChangedRender(nameof(UpdateShowWaterWave))]
    NetworkBool IsShowWaterWave { get; set; }

    WayPointMain WayPointMain;
    Transform FishPool;

    // 一般魚Enum
    List<NetworkPrefabEnum> NormalFishTypes = new();
    Coroutine CreateFishCoroutine;

    // 浪潮
    float WaterWaveTime;
    GameObject WaterWaveObj;
    WaterWaveFishData WaterWaveFishData;

    // 特殊魚
    float SpecialSpawnTime;
    Coroutine SpecialFishCoroutine;

    GameView GameView;

    // 本地玩家是否已生成
    bool isLocalSpawn;

    private void OnDestroy()
    {
        if (NetworkRunnerManagement.Instance != null)
            NetworkRunnerManagement.Instance.PlayerLeftEvent -= LeftRoom;

        StopAllCoroutines();
    }

    private void Start()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent += LeftRoom;

        // 初始化座位座標
        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            foreach (var seat in Seats)
            {
                FirestoreDataManagement.Instance.GameTempData.SeatPositions.Add(seat.position);
            }
        }        
    }

    public override void Spawned()
    {
        // 產生浪潮特效
        _ = AddressableManagement.Instance.CreateGamePrefab(
            prefabType: GamePrefabEnum.WaterWave, 
            callback: (obj) => 
            {
                WaterWaveObj = obj;
                obj.SetActive(false); 
            });

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            WaterWaveTime = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.WaterWaveTime;
            SpecialSpawnTime = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.SpecialFishTime;
        }        

        if (Object.HasStateAuthority)
        {
            IsShowWaterWave = false;

            // 初始化座位
            for (int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
                IsFirstCreate = true;
            }

            // 初始狀態：普通模式
            CurrentState = GameState.Normal;
            // 開始計時浪潮時間
            StateChangeTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveTime);
            // 開始計時特殊魚產生時間
            SpecialSpawnTimer = TickTimer.CreateFromSeconds(Runner, SpecialSpawnTime);
        }

        StartCoroutine(IJoinSeat());
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (IsFreezeTimer())
            return;

        switch (CurrentState)
        {
            // 一般狀態
            case GameState.Normal:
                NormalModeUpdate();
                break;

            // 浪潮狀態
            case GameState.WaterWave:
                WaterWaveModeUpdate();
                break;

            // 浪潮魚群狀態
            case GameState.WaterWaveFishs:
                WaterWaveFishModeUpdate();
                break;

            // 特殊魚狀態
            case GameState.SpecialFish:
                SpecialFishUpdate();
                break;
        }        
    }

    /// <summary>
    /// 是否處於冰凍時間
    /// </summary>
    private bool IsFreezeTimer()
    {
        // 如果冰凍正在計時
        if (FreezeTimer.IsRunning && !FreezeTimer.Expired(Runner))
        {
            // 延後一般魚產生時間
            if (SpawnTimer.IsRunning)
            {
                // 將目標 Tick 往後推 1 個 Tick
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, (SpawnTimer.RemainingTime(Runner) ?? 0) + Runner.DeltaTime);
            }

            // 延後特殊魚產生時間
            if (SpecialSpawnTimer.IsRunning)
            {
                SpecialSpawnTimer = TickTimer.CreateFromSeconds(Runner, (SpecialSpawnTimer.RemainingTime(Runner) ?? 0) + Runner.DeltaTime);
            }

            // 延後遊戲狀態切換時間
            if (StateChangeTimer.IsRunning)
            {
                StateChangeTimer = TickTimer.CreateFromSeconds(Runner, (StateChangeTimer.RemainingTime(Runner) ?? 0) + Runner.DeltaTime);
            }
        }

        return FreezeTimer.IsRunning && !FreezeTimer.Expired(Runner);
    }

    #region 玩家

    /// <summary>
    /// 加入座位
    /// </summary>
    /// <returns></returns>
    private IEnumerator IJoinSeat()
    {
        yield return null;

        if (Object != null && Object.IsValid)
        {
            JoinSeat();
        }

        yield return null;

        OnSeatPlayerIsChange();
    }

    /// <summary>
    /// 座位玩家變化
    /// </summary>
    private async void OnSeatPlayerIsChange()
    {
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            int index = i;

            // 產生本地玩家砲台
            if (!isLocalSpawn && SeatPlayerIDs[index] == Runner.LocalPlayer.PlayerId)
            {
                if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
                    return;

                isLocalSpawn = true;
                bool isMirror = index == 1 || index == 3;

                await AddressableManagement.Instance.OpenGameView(localSeat: index, isMirror: isMirror);

                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.PlayerTurret,
                    Pos: Seats[index].position,
                    rot: Quaternion.identity,
                    parent: Seats[index],
                    player: Runner.LocalPlayer,
                    callback: (obj) =>
                    {
                        PlayerTurret playerTurret = obj.GetComponent<PlayerTurret>();
                        if(playerTurret != null)
                        {
                            playerTurret.SetData(turretIndex: FirestoreDataManagement.Instance.GameTempData.TempAccountData.DefaultTurret, seatIndex: index);
                        }
                    });

                // 位置在1.3攝影機顛倒
                if(isMirror)
                {
                    Transform cameraTr = Camera.main.transform;
                    cameraTr.rotation = Quaternion.Euler(90, 0, 180);
                }

                FirestoreDataManagement.Instance.GameTempData.IsMirror = index == 1 || index == 3;
                FirestoreDataManagement.Instance.GameTempData.LocalSeatIndex = index;
            }

            // 座位玩家已離開
            if(SeatPlayerIDs[index] == -1)
            {
                // UI清理
                if (GameView == null)
                    GameView = FindFirstObjectByType<GameView>();
                if (GameView != null)
                    GameView.PlayerCostChange(hasStateAuthority: false, seatIndex: index, cost: -1);
            }
        }
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    private void LeftRoom(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer) 
            return;

        // 原房主離開後，Photon Cloud 會瞬間指派新的 Master Client
        if (Runner.IsSharedModeMasterClient)
        {
            // 請求地形權限
            if (Object != null && Object.IsValid && !Object.HasStateAuthority)
                Object.RequestStateAuthority();

            // 請求座位權限
            foreach (var seatGo in Seats)
            {
                if (seatGo != null && seatGo.TryGetComponent<NetworkObject>(out var seatNO))
                {
                    if (!seatNO.HasStateAuthority)
                    {
                        seatNO.RequestStateAuthority();
                    }
                }
            }

            // 請求權限
            foreach (var netObj in Runner.GetAllNetworkObjects())
            {
                if (netObj != null && netObj.IsValid && !netObj.HasStateAuthority)
                {
                    // 請求所有場上魚的權限
                    if (netObj.GetComponent<Fish>() != null)
                    {
                        netObj.RequestStateAuthority();
                    }

                    // 請求特殊效果控制權限
                    if (netObj.GetComponent<SpecialEffectController>() != null)
                    {
                        netObj.RequestStateAuthority();
                    }

                    // 請求Fusion物件池權限
                    if (netObj.gameObject.name == "FusionPool")
                    {
                        netObj.RequestStateAuthority();
                    }
                }
            }

            StartCoroutine(IYieldResetSeat(player));
        }
    }

    /// <summary>
    /// 等待獲取權限重設離開玩家座位
    /// </summary>
    private IEnumerator IYieldResetSeat(PlayerRef leftPlayer)
    {
        // 增加一點點緩衝時間，確保 Fusion 已經處理完 Master Client 變更
        yield return new WaitForSecondsRealtime(0.2f);

        float timer = 0;
        // 使用 Object.IsValid 確保物件還在，並等待權限
        while (Object != null && Object.IsValid && !Object.HasStateAuthority && timer < 3.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            // 成功取得權限，執行清理
            ClearSeatLogic(leftPlayer);
        }
        else
        {
            // 如果還是沒權限，可以嘗試最後一次「主動請求」
            // 有時候自動移交還沒完成，主動 Request 可以強制觸發
            Object.RequestStateAuthority();
            yield return new WaitForSeconds(0.1f);

            if (Object.HasStateAuthority)
                ClearSeatLogic(leftPlayer);
            else
                Debug.LogError($"取得權限超時，目前 Master 是: {Runner.LocalPlayer}");
        }
    }

    /// <summary>
    /// 清理座位
    /// </summary>
    private void ClearSeatLogic(PlayerRef leftPlayer)
    {
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            int index = i;

            if (SeatPlayerIDs[i] == leftPlayer.PlayerId)
            {
                // 清理座位
                SeatPlayerIDs.Set(i, -1);
                Debug.Log($"[Master] 已清理玩家 {leftPlayer.PlayerId} 的座位 {i}");
                break;
            }
        }
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    private void JoinSeat()
    {
        RPC_JoinSeat(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_JoinSeat(PlayerRef player)
    {
        // 已經有位置
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == player.PlayerId) 
                return; 
        }

        // 設置座位
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == -1)
            {
                SeatPlayerIDs.Set(i, player.PlayerId);
                return;
            }
        }
    }

    #endregion

    #region 特殊魚

    /// <summary>
    /// 特殊魚狀態Update
    /// </summary>
    private void SpecialFishUpdate()
    {
        // 檢查是否回到一般狀態
        if (StateChangeTimer.Expired(Runner))
        {
            CurrentState = GameState.Normal;
            // 重製浪潮倒計時
            StateChangeTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveTime);
            return;
        }

        // 產生一般魚
        if (SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, NormalFishCreatTime);

            // 控制場景中魚的數量
            if (CurrFishCount < MaxCreateFishCount)
            {
                if (CreateFishCoroutine != null)
                    StopCoroutine(CreateFishCoroutine);

                CreateFishCoroutine = StartCoroutine(ICreatNormalFish());
            }
        }
    }

    /// <summary>
    /// 開始特殊魚狀態
    /// </summary>
    private void StartSpecialFish()
    {
        CurrentState = GameState.SpecialFish;

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            switch (FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.LevelType)
            {
                // 經典關卡
                case LevelEnum.ClassicLevel:
                    if (SpecialFishCoroutine != null)
                        StopCoroutine(SpecialFishCoroutine);

                    SpecialFishCoroutine = StartCoroutine(ISpawnStingrayFish());
                    break;

                // 鯊魚關卡
                case LevelEnum.SharkLevel:
                    SpawnSharkFish();
                    break;

                // 金龍關卡
                case LevelEnum.DragonLevel:
                    SpawnDragonFish();
                    break;
            }
        }
    }

    /// <summary>
    /// 產生魟魚
    /// </summary>
    /// <returns></returns>
    private IEnumerator ISpawnStingrayFish()
    {
        // 路線不與前一隻一樣
        int preWaypointIndex = -1;
        // 下一隻等待時間
        float yieldTime = 5;
        // 魚類型
        NetworkPrefabEnum fishType = NetworkPrefabEnum.StingrayFish;

        for (int i = 0; i < 2; i++)
        {
            // 隨機選擇路線
            List<WayPoint> wayPoints = WayPointMain.GetNormalWayPoints();
            int wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);
            // 路線不與前一隻一樣
            while (preWaypointIndex == wayPointIndex)
            {
                wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);
            }
            WayPoint wayPoint = wayPoints[wayPointIndex];

            // 面向左或右
            bool isMirror = i % 2 == 0;

            // 初始位置
            Vector3 initPos =
                isMirror ?
                wayPoint.Points[wayPoint.Points.Count - 1].position :
                wayPoint.Points[0].position;

            // 跳過路線點
            int skipWaypoint = 0;

            // 深度            
            int depth = UnityEngine.Random.Range(MaxFishDepth, MinFishDepth);

            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                key: fishType,
                Pos: initPos,
                rot: Quaternion.identity,
                parent: FishPool,
                player: Runner.LocalPlayer,
                callback: (fish) =>
                {
                    Fish normalFish = fish.GetComponent<Fish>();
                    if (normalFish != null)
                        normalFish.SetData(
                            fishType: fishType,
                            isMirror: isMirror,
                            depth: depth,
                            wayPointId: wayPoint.WayPointId,
                            skipWaypoint: skipWaypoint);
                });

            yield return new WaitForSeconds(yieldTime);
        }

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FishData fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fishType);
            if (fishData != null)
            {
                ResetTimmer(fishDuration: fishData.Duration, yieldTime: yieldTime);
            }
        }
    }

    /// <summary>
    /// 產生鯊魚
    /// </summary>
    private void SpawnSharkFish()
    {
        // 隨機選擇路線
        List<WayPoint> wayPoints = WayPointMain.GetNormalWayPoints();
        int wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);
        WayPoint wayPoint = wayPoints[wayPointIndex];

        // 面向左或右
        bool isMirror = UnityEngine.Random.value > 0.5f;

        // 初始位置
        Vector3 initPos =
            isMirror ?
            wayPoint.Points[wayPoint.Points.Count - 1].position :
            wayPoint.Points[0].position;

        // 跳過路線點
        int skipWaypoint = 0;

        // 深度            
        int depth = UnityEngine.Random.Range(MaxFishDepth, MinFishDepth);

        // 魚類型
        NetworkPrefabEnum fishType = NetworkPrefabEnum.SharkFish;

        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
            key: fishType,
            Pos: initPos,
            rot: Quaternion.identity,
            parent: FishPool,
            player: Runner.LocalPlayer,
            callback: (fish) =>
            {
                Fish normalFish = fish.GetComponent<Fish>();
                if (normalFish != null)
                    normalFish.SetData(
                        fishType: fishType,
                        isMirror: isMirror,
                        depth: depth,
                        wayPointId: wayPoint.WayPointId,
                        skipWaypoint: skipWaypoint);
            });

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FishData fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fishType);
            if (fishData != null)
            {
                ResetTimmer(fishDuration: fishData.Duration);
            }
        }        
    }

    /// <summary>
    /// 產生金龍
    /// </summary>
    private void SpawnDragonFish()
    {
        // 面向左或右
        bool isMirror = UnityEngine.Random.value > 0.5f;

        // 初始位置
        Vector3 initPos = Vector3.zero;

        // 跳過路線點
        int skipWaypoint = 0;

        // 深度            
        int depth = UnityEngine.Random.Range(MaxFishDepth, MinFishDepth);

        // 魚類型
        NetworkPrefabEnum fishType = NetworkPrefabEnum.DragonFish;

        NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
            key: fishType,
            Pos: initPos,
            rot: Quaternion.identity,
            parent: FishPool,
            player: Runner.LocalPlayer,
            callback: (fish) =>
            {
                Fish normalFish = fish.GetComponent<Fish>();
                if (normalFish != null)
                    normalFish.SetData(
                        fishType: fishType,
                        isMirror: isMirror,
                        depth: depth,
                        wayPointId: 201,
                        skipWaypoint: skipWaypoint);
            });

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FishData fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fishType);
            if (fishData != null)
            {
                ResetTimmer(fishDuration: fishData.Duration);
            }
        }        
    }

    /// <summary>
    /// 重新設置倒計時
    /// </summary>
    private void ResetTimmer(float fishDuration, float yieldTime = 0)
    {
        // 重製特殊魚倒計時
        SpecialSpawnTimer = TickTimer.CreateFromSeconds(Runner, SpecialSpawnTime + fishDuration + yieldTime);
        // 重製狀態更換倒計時
        StateChangeTimer = TickTimer.CreateFromSeconds(Runner, fishDuration);
    }

    #endregion

    #region 一般魚

    /// <summary>
    /// 一般狀態Update
    /// </summary>
    private void NormalModeUpdate()
    {
        // 檢查是否該進入浪潮
        if (StateChangeTimer.Expired(Runner))
        {
            StartWaterWave();
            return;
        }

        // 產生一般魚
        if (SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, NormalFishCreatTime);

            // 控制場景中魚的數量
            if(CurrFishCount < MaxCreateFishCount)
            {
                if (CreateFishCoroutine != null)
                    StopCoroutine(CreateFishCoroutine);

                CreateFishCoroutine = StartCoroutine(ICreatNormalFish());
            }
        }

        // 產生特殊魚
        if(SpecialSpawnTimer.ExpiredOrNotRunning(Runner))
        {
            // 特殊魚期間狀態不計時
            StateChangeTimer = TickTimer.None;
            StartSpecialFish();
        }
    }

    /// <summary>
    /// 產生一般魚
    /// </summary>
    private IEnumerator ICreatNormalFish()
    {
        if (!Object.HasStateAuthority)
            yield break;

        if(FishPool == null)
            FishPool = GameObject.Find(FusionPoolNameEnum.FishPool.ToString()).transform;

        if(WayPointMain == null)
            WayPointMain = UnityEngine.Object.FindFirstObjectByType<WayPointMain>();

        if(NormalFishTypes == null || NormalFishTypes.Count == 0)
        {
            NormalFishTypes = Enum.GetValues(typeof(NetworkPrefabEnum))
                .Cast<NetworkPrefabEnum>()
                .Where(e => e.ToString().StartsWith("NormalFish"))
                .ToList();
        }

        if(FishPool == null || WayPointMain == null || NormalFishTypes == null || NormalFishTypes.Count == 0)
        {
            Debug.LogError("產生一般魚錯誤!");
            yield break;
        }

        int preWaypointIndex = -1;

        // 總生成數量
        int totalCount = 
            IsFirstCreate ?
            InitCreateFishCount :
            UnityEngine.Random.Range(MinCreateNormalFish, MaxCreateNormalFish + 1);

        for (int i = 0; i < totalCount; i++)
        {
            // 隨機魚種類
            int fishTypeIndex = UnityEngine.Random.Range(0, NormalFishTypes.Count);
            NetworkPrefabEnum fishType = NormalFishTypes[fishTypeIndex];

            // 隨機選擇路線
            List<WayPoint> wayPoints = WayPointMain.GetNormalWayPoints();
            int wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);
            // 路線不與前一隻一樣
            while (preWaypointIndex == wayPointIndex)
            {
                wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);
            }
            WayPoint wayPoint = wayPoints[wayPointIndex];

            // 面向左或右
            bool isMirror = UnityEngine.Random.value > 0.5f;

            // 初始位置
            Vector3 initPos =
                isMirror ?
                wayPoint.Points[wayPoint.Points.Count - 1].position :
                wayPoint.Points[0].position;

            // 跳過路線點
            int skipWaypoint = 0;

            // 深度            
            int depth = UnityEngine.Random.Range(MaxFishDepth, MinFishDepth);

            // 首次產生魚位置在畫面中
            if (IsFirstCreate)
            {
                skipWaypoint = UnityEngine.Random.Range(1, wayPoint.Points.Count - 1);
                initPos = wayPoint.Points[skipWaypoint].position;
            }

            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                       key: fishType,
                       Pos: initPos,
                       rot: Quaternion.identity,
                       parent: FishPool,
                       player: Runner.LocalPlayer,
                       callback: (fish) =>
                       {
                           Fish normalFish = fish.GetComponent<Fish>();
                           if (normalFish != null)
                               normalFish.SetData(
                                   fishType: fishType,
                                   isMirror: isMirror,
                                   depth: depth,
                                   wayPointId: wayPoint.WayPointId,
                                   skipWaypoint: skipWaypoint);
                       });

            if (!IsFirstCreate)
                yield return new WaitForSeconds(0.25f);
        }

        if (IsFirstCreate)
            IsFirstCreate = false;
    }

    /// <summary>
    /// 更新場景中魚的數量
    /// </summary>
    public void UpdateCurrFishCount(int changeValue)
    {
        if (Object!= null && Object.IsValid && Object.HasStateAuthority)
        {
            CurrFishCount += changeValue;
        }
    }

    #endregion

    #region 浪潮

    /// <summary>
    /// 更新顯示浪潮特效
    /// </summary>
    private void UpdateShowWaterWave()
    {
        if (WaterWaveObj != null)
            WaterWaveObj.SetActive(IsShowWaterWave);
    }

    /// <summary>
    /// 浪潮開始
    /// </summary>
    private void StartWaterWave()
    {
        CurrentState = GameState.WaterWave;

        // 停止目前的生魚協程
        if (CreateFishCoroutine != null)
            StopCoroutine(CreateFishCoroutine);

        // 設定浪潮結束的倒數計時
        StateChangeTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveDuration);

        IsShowWaterWave = true;

        // 重製冰凍時間
        FreezeTimer = default;

        // 場上魚移動加速
        foreach (var netObj in Runner.GetAllNetworkObjects())
        {
            if (netObj != null && netObj.IsValid && netObj.HasStateAuthority && netObj.gameObject.activeInHierarchy)
            {
                Fish fish = netObj.GetComponent<Fish>();
                if (fish != null)
                {
                    fish.SetFishDuration(finishTime: WaterWaveDuration - 1f);
                }
            }
        }
    }

    /// <summary>
    /// 浪潮狀態Update
    /// </summary>
    private void WaterWaveModeUpdate()
    {
        // 檢查是否該進入浪潮魚群
        if (StateChangeTimer.Expired(Runner))
        {
            StartWaterWaveFish();
            return;
        }
    }

    #endregion

    #region 浪潮魚群

    /// <summary>
    /// 浪潮魚群開始
    /// </summary>
    private void StartWaterWaveFish()
    {
        CurrentState = GameState.WaterWaveFishs;

        // 浪潮魚群期間狀態不計時
        StateChangeTimer = TickTimer.None;

        IsShowWaterWave = false;
        CurrWaterWaveFishIndex = 0;
    }

    /// <summary>
    /// 浪潮魚群狀態Update
    /// </summary>
    private void WaterWaveFishModeUpdate()
    {
        // 產生浪潮魚群
        if (SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
            {
                if (WaterWaveFishData == null)
                    WaterWaveFishData = WaterWaveFishManagement.GetWaterWaveFishData(FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.LevelType);
            }            

            SpawnTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveFishData.SpawnBetweenTime);

            CreateWaterWaveFish();

            // 浪潮魚群結束
            if (CurrWaterWaveFishIndex >= WaterWaveFishData.FishsType.Count)
            {
                EndWaterWave();
            }
        }
    }

    /// <summary>
    /// 浪潮魚群結束
    /// </summary>
    private void EndWaterWave()
    {
        CurrentState = GameState.Normal;

        // 開始計時下次浪潮時間
        StateChangeTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveTime + WaterWaveFishData.MoveDuration);

        // 延遲一小段時間重新開始產生一般魚
        SpawnTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveFishData.MoveDuration);
    }

    /// <summary>
    /// 產生浪潮魚
    /// </summary>
    private void CreateWaterWaveFish()
    {
        if (!Object.HasStateAuthority)
            return;

        if (FishPool == null)
            FishPool = GameObject.Find(FusionPoolNameEnum.FishPool.ToString()).transform;

        if (WayPointMain == null)
            WayPointMain = UnityEngine.Object.FindFirstObjectByType<WayPointMain>();

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            if (WaterWaveFishData == null)
                WaterWaveFishData = WaterWaveFishManagement.GetWaterWaveFishData(FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.LevelType);
        }        

        if (FishPool == null || WayPointMain == null || WaterWaveFishData == null)
        {
            Debug.LogError("產生浪潮魚錯誤!");
            return;
        }

        // 上下路線
        for (int i = 0; i < 2; i++)
        {
            int index = i;

            // 魚種類
            NetworkPrefabEnum fishType = WaterWaveFishData.FishsType[CurrWaterWaveFishIndex];

            // 路線
            WayPoint wayPoint = WayPointMain.GetWaterWaveWayPoints()[index];

            // 面向左或右
            bool isMirror = index == 0;

            // 初始位置
            Vector3 initPos =
                isMirror ?
                wayPoint.Points[wayPoint.Points.Count - 1].position :
                wayPoint.Points[0].position;

            // 深度            
            int depth = UnityEngine.Random.Range(-40, -5);

            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                key: fishType,
                Pos: initPos,
                rot: Quaternion.identity,
                parent: FishPool,
                player: Runner.LocalPlayer,
                callback: (fish) =>
                {
                    Fish normalFish = fish.GetComponent<Fish>();
                    if (normalFish != null)
                        normalFish.SetData(
                            fishType: fishType,
                            isMirror: isMirror,
                            depth: depth,
                            wayPointId: wayPoint.WayPointId,
                            skipWaypoint: 0,
                            customDuration: WaterWaveFishData.MoveDuration);
           });
        }

        CurrWaterWaveFishIndex++;
    }

    #endregion

    #region 特殊效果


    /// <summary>
    /// 增加冰凍的時間
    /// </summary>
    public void AddFreezeTime()
    {
        RPC_AddFreezeTime();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_AddFreezeTime()
    {
        // 所有玩家顯示冰凍效果
        if (GameView == null)
            GameView = FindFirstObjectByType<GameView>();
        if (GameView != null)
            GameView.ShowFreezeEffect();

        // 增加冰凍時間
        if (Object.HasStateAuthority)
        {
            float currentRemaining = FreezeTimer.RemainingTime(Runner) ?? 0f;
            // 剩餘時間 + 新增時間
            FreezeTimer = TickTimer.CreateFromSeconds(Runner, currentRemaining + LocalData.FreezeTime);
        }        
    }

    #endregion
}
