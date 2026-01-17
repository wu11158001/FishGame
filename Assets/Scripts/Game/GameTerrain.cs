using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class GameTerrain : NetworkBehaviour
{
    [Header("Seat")]
    [SerializeField] List<GameObject> Seats;

    [Header("Fish Value")]
    // 初始生成數量
    [SerializeField] int InitCreateFishCount = 8;
    // 一般魚生成時間(秒)
    [SerializeField] float NormalFishCreatTime = 8;
    // 一般魚一次生成最小數量
    [SerializeField] int MinCreateNormalFish = 3;
    // 一般魚一次生成最大數量
    [SerializeField]  int MaxCreateNormalFish = 8;

    [Header("Water Wave")]
    // 浪潮效果持續時間
    [SerializeField] float WaterWaveDuration = 4;

    // 紀錄座位上玩家ID
    [Networked, Capacity(4), OnChangedRender(nameof(OnSpawnLocalTurret))]
    NetworkArray<int> SeatPlayerIDs { get; }

    // 產生一般魚計時器
    [Networked] TickTimer SpawnTimer { get; set; }
    // 首次產生魚
    [Networked] bool IsFirstCreate { get; set; }

    // 遊戲狀態
    [Networked] GameState CurrentState { get; set; }
    // 遊戲狀態更換時間
    [Networked] TickTimer StateChangeTimer { get; set; }

    // 當前浪潮魚產生Index
    [Networked] int CurrWaterWaveFishIndex { get; set; }

    WayPointMain WayPointMain;
    Transform FishPool;

    // 一般魚Enum
    List<NetworkPrefabEnum> NormalFishTypes = new();
    Coroutine CreateFishCoroutine;

    // 浪潮開始倒計時
    float WaterWaveTime;
    GameObject WaterWaveObj;
    WaterWaveFishData WaterWaveFishData;

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
    }

    public override void Spawned()
    {
        // 產生浪潮特效
        var task4 = AddressableManagement.Instance.CreateGamePrefab(
            prefabType: GamePrefabEnum.WaterWave, 
            callback: (obj) => 
            {
                WaterWaveObj = obj;
                obj.SetActive(false); 
            });

        WaterWaveTime = TempDataManagement.Instance.CurrentLevelData.WaterWaveTime;

        if (Object.HasStateAuthority)
        {
            // 初始化座位
            for(int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
                IsFirstCreate = true;
            }

            // 初始狀態：普通模式
            CurrentState = GameState.Normal;
            // 開始計時浪潮時間
            StateChangeTimer = TickTimer.CreateFromSeconds(Runner, WaterWaveTime);
        }

        StartCoroutine(IJoinSeat());
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!Object.HasStateAuthority)
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
        }        
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

        OnSpawnLocalTurret();
    }

    /// <summary>
    /// 產生本地玩家砲台
    /// </summary>
    private void OnSpawnLocalTurret()
    {
        if (isLocalSpawn) 
            return;

        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            int index = i;

            if (SeatPlayerIDs[index] == Runner.LocalPlayer.PlayerId)
            {
                isLocalSpawn = true;

                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.PlayerTurret,
                    Pos: Seats[index].transform.position,
                    rot: Quaternion.identity,
                    parent: Seats[index].transform,
                    player: Runner.LocalPlayer,
                    callback: (obj) =>
                    {
                        PlayerTurret playerTurret = obj.GetComponent<PlayerTurret>();
                        if(playerTurret != null)
                        {
                            playerTurret.SetData(turretIndex: TempDataManagement.Instance.TempAccountData.DefaultTurret);
                        }
                    });

                // 位置在1.3攝影機顛倒
                if(index == 1 || index == 3)
                {
                    Transform cameraTr = Camera.main.transform;
                    cameraTr.rotation = Quaternion.Euler(90, 0, 180);
                }
                TempDataManagement.Instance.IsMirror = index == 1 || index == 3;
                TempDataManagement.Instance.SeatPosition = Seats[index].transform.position;

                _ = AddressableManagement.Instance.OpenGameView(localSeat: index);

                break;
            }
        }
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    private void LeftRoom(NetworkRunner runner, PlayerRef player)
    {
        // 原房主離開後，Photon Cloud 會瞬間指派新的 Master Client
        if (Runner.IsSharedModeMasterClient)
        {
            // 請求地形權限
            if (!Object.HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }

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

            // 請求所有場上魚的權限
            if (FishPool == null)
                FishPool = GameObject.Find(FusionPoolNameEnum.FishPool.ToString()).transform;

            for (int i = 0; i < FishPool.childCount; i++)
            {
                if(FishPool.GetChild(i).TryGetComponent<NetworkObject>(out var fish))
                {
                    if (!fish.HasStateAuthority)
                    {
                        fish.RequestStateAuthority();
                    }
                }
            }

            StartCoroutine(IYieldResetSeat(player));
        }
    }

    /// <summary>
    /// 等待獲取權限重設離開玩家座位
    /// </summary>
    /// <param name="leftPlayer"></param>
    /// <returns></returns>
    private IEnumerator IYieldResetSeat(PlayerRef leftPlayer)
    {
        float timer = 0;
        while (!Object.HasStateAuthority && timer < 2.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                if (SeatPlayerIDs[i] == leftPlayer.PlayerId)
                {
                    SeatPlayerIDs.Set(i, -1);
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("取得權限超時，無法清理座位");
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

    #region 魚

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

            if (CreateFishCoroutine != null)
                StopCoroutine(CreateFishCoroutine);

            CreateFishCoroutine = StartCoroutine(ICreatNormalFish());
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
            WayPointMain = GameObject.Find($"{GamePrefabEnum.WayPointMain}(Clone)").GetComponent<WayPointMain>();

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

            int skipWaypoint = 0;

            // 首次產生魚位置在畫面中
            if (IsFirstCreate)
            {
                skipWaypoint = UnityEngine.Random.Range(1, wayPoint.Points.Count - 1);
                initPos = wayPoint.Points[skipWaypoint].position;
            }

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
                                   skipWaypoint: skipWaypoint);
                       });

            if (!IsFirstCreate)
                yield return new WaitForSeconds(0.25f);
        }

        if (IsFirstCreate)
            IsFirstCreate = false;
    }

    #endregion

    #region 浪潮

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

        WaterWaveObj.SetActive(true);

        // 場上魚移動加速
        if (FishPool == null)
            FishPool = GameObject.Find(FusionPoolNameEnum.FishPool.ToString()).transform;

        for (int i = 0; i < FishPool.childCount; i++)
        {
            if (FishPool.GetChild(i).TryGetComponent<Fish>(out var fish))
            {
                if(fish.gameObject.activeInHierarchy)
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

        WaterWaveObj.SetActive(false);
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
            if (WaterWaveFishData == null)
                WaterWaveFishData = WaterWaveFishManagement.GetWaterWaveFishData(TempDataManagement.Instance.CurrentLevelData.LevelType);

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
            WayPointMain = GameObject.Find($"{GamePrefabEnum.WayPointMain}(Clone)").GetComponent<WayPointMain>();

        if (WaterWaveFishData == null)
            WaterWaveFishData = WaterWaveFishManagement.GetWaterWaveFishData(TempDataManagement.Instance.CurrentLevelData.LevelType);

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
}
