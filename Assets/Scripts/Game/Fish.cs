using UnityEngine;
using Fusion;
using System.Linq;

public class Fish : NetworkBehaviour
{
    // 爆金物件
    [SerializeField] GamePrefabEnum CoinTextType = GamePrefabEnum.CoinText_0;
    [SerializeField] GameObject FishModel;
    [SerializeField] Animator Animator;

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

    public void SetData(NetworkPrefabEnum fishType, bool isMirror, int depth, int wayPointId, int skipWaypoint, float customDuration = 0)
    {
        // 獲取魚資料
        FishData fishData = TempDataManagement.Instance.GetFishData(fishType);
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
            WayPointMain = GameObject.Find($"{GamePrefabEnum.WayPointMain}").GetComponent<WayPointMain>();

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
        if (FishModel != null)
            FishModel.SetActive(true);

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
        }

        if (Animator != null) Animator.speed = AniSpeed;
    }

    public override void Render()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2)
        {
            SetPathPoints();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        Move();
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
            var wayPointObj = GameObject.Find($"{GamePrefabEnum.WayPointMain}");
            if(wayPointObj != null)
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

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2)
            return;

        float elapsed = TotalDuration - (MoveTimer.RemainingTime(Runner) ?? 0);
        float t = Mathf.Clamp01(elapsed / TotalDuration);

        Vector3 nextPos = GetCatmullRomPosition(t, LocalPathPoints);
        nextPos.y = FishPathData.Depth;

        // 面向與位移
        Vector3 moveDir = nextPos - transform.position;
        if (moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(moveDir);

        transform.position = nextPos;

        // 只有 Server 判定銷毀
        if (t >= 1.0f && Object.HasStateAuthority)
            Runner.Despawn(Object);
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
    /// 獲取魚資料
    /// </summary>
    public FishData_Network GetFishData()
    {
        return FishData_Network;
    }

    /// <summary>
    /// 顯示爆金文字
    /// </summary>
    private void ShowCoinText(double reward)
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
                coinText.SetData(value: reward);
            });
    }

    /// <summary>
    /// 魚被擊中
    /// </summary>
    public void GetHit(PlayerRef player, double reward)
    {
        // 顯示爆金文字
        ShowCoinText(reward: reward);

        if (FishModel != null)
            FishModel.SetActive(false);

        RPC_GetHit(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GetHit(PlayerRef player)
    {
        Runner.Despawn(Object);
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