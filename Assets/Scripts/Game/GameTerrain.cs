using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class GameTerrain : NetworkBehaviour
{
    [SerializeField] List<GameObject> Seats;

    /// <summary> 紀錄座位上玩家ID </summary>
    [Networked, Capacity(4)]
    [OnChangedRender(nameof(OnSpawnLocalObject))]
    NetworkArray<int> SeatPlayerIDs { get; }

    /// <summary>
    /// 產生一般魚計時器
    /// </summary>
    [Networked] 
    TickTimer SpawnTimer { get; set; }

    MainWayPoint MainWayPoint;
    Transform FishPool;

    // 一般魚Enum
    List<NetworkPrefabEnum> NormalFishTypes = new();

    // 本地玩家是否已生成
    bool isLocalSpawn;
    // 一般魚生成時間
    float NormalFishCreatTime = 5;
    // 一般魚一次生成最小數量
    int MinCreateNormalFish = 2;
    // 一般魚一次生成最大數量
    int MaxCreateNormalFish = 5;

    private void OnDestroy()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent -= LeftRoom;
    }

    private void Start()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent += LeftRoom;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // 初始化座位
            for(int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
            }
        }

        StartCoroutine(IJoinSeat());
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            CreatNormalFish();
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, NormalFishCreatTime);
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

        OnSpawnLocalObject();
    }

    /// <summary>
    /// 產生本地玩家物件
    /// </summary>
    private void OnSpawnLocalObject()
    {
        if (isLocalSpawn) 
            return;

        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == Runner.LocalPlayer.PlayerId)
            {
                isLocalSpawn = true;

                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.Player,
                    Pos: Seats[i].transform.position,
                    rot: Quaternion.identity,
                    parent: Seats[i].transform,
                    player: Runner.LocalPlayer);

                break;
            }
        }

        AddressableManagement.Instance.CloseLoading();
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
            // 既然我是權限者了，直接改 Networked Array
            for (int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                if (SeatPlayerIDs[i] == leftPlayer.PlayerId)
                {
                    Debug.Log($"[新房主] 成功取得權限，清理座位 Index: {i}");
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
    /// 產生一般魚
    /// </summary>
    private void CreatNormalFish()
    {
        if (!Object.HasStateAuthority)
            return;

        if(FishPool == null)
            FishPool = GameObject.Find(PoolNameEnum.FishPool.ToString()).transform;

        if(MainWayPoint == null)
            MainWayPoint = GameObject.Find($"{GamePrefabEnum.MainWayPoint}(Clone)").GetComponent<MainWayPoint>();

        if(NormalFishTypes == null || NormalFishTypes.Count == 0)
        {
            NormalFishTypes = Enum.GetValues(typeof(NetworkPrefabEnum))
                .Cast<NetworkPrefabEnum>()
                .Where(e => e.ToString().StartsWith("NormalFish"))
                .ToList();
        }

        if(FishPool == null || MainWayPoint == null || NormalFishTypes == null || NormalFishTypes.Count == 0)
        {
            Debug.LogError("產生一般魚錯誤!");
            return;
        }

        // 總生成數量
        int totalCount = UnityEngine.Random.Range(MinCreateNormalFish, MaxCreateNormalFish + 1);
        for (int i = 0; i < totalCount; i++)
        {
            // 隨機魚種類
            int fishTypeIndex = UnityEngine.Random.Range(0, NormalFishTypes.Count);
            NetworkPrefabEnum fishType = NormalFishTypes[fishTypeIndex];

            // 隨機選擇路線
            List<WayPoint> wayPoints = MainWayPoint.GetWayPoints();
            int wayPointIndex = UnityEngine.Random.Range(0, wayPoints.Count);

            WayPoint wayPoint = wayPoints[wayPointIndex];

            // 面向左或右
            bool isMirror = UnityEngine.Random.value > 0.5f;
            Vector3 initPos =
                isMirror ?
                wayPoint.Points[0].position :
                wayPoint.Points[wayPoint.Points.Count - 1].position;

            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                       key: fishType,
                       Pos: initPos,
                       rot: Quaternion.identity,
                       parent: FishPool,
                       player: Runner.LocalPlayer,
                       callback: (fish) =>
                       {
                           NormalFish normalFish = fish.GetComponent<NormalFish>();
                           if (normalFish != null)
                               normalFish.SetData(
                                   fishType: fishType,
                                   isMirror: isMirror, 
                                   wayPoint: wayPoint);
                       });
        }
    }

    #endregion
}
