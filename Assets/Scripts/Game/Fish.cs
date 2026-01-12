using UnityEngine;
using Fusion;
using System.Linq;

public class Fish : NetworkBehaviour
{
    // 爆金物件
    [SerializeField] GamePrefabEnum CoinTextType = GamePrefabEnum.CoinText_0;
    [SerializeField] GameObject FishModel;

    // 移動計時器
    [Networked] TickTimer MoveTimer { get; set; }
    // 總移動時間
    [Networked] float TotalDuration { get; set; }
    // 魚資料
    [Networked] FishData_Network FishData_Network { get; set; }
    // 深度
    [Networked] float Depth { get; set; }

    NetworkPrefabEnum FishType;
    Vector3[] PathPoints;

    LocalPool LocalPool;
    Transform CoinTextPool;

    public void SetData(NetworkPrefabEnum fishType, bool isMirror, float depth, WayPoint wayPoint, int skipWaypoint)
    {
        FishType = fishType;

        // 移動路徑獲取
        var query = wayPoint.Points.Select(t => t.position);
        if (isMirror) query = query.Reverse();
        PathPoints = query.Skip(skipWaypoint).ToArray();

        // --- 新增：初始化位置與面向 ---
        if (PathPoints != null && PathPoints.Length >= 2)
        {
            Vector3 startPos = PathPoints[0];
            startPos.y = depth;
            transform.position = startPos;

            Vector3 nextPos = PathPoints[1];
            nextPos.y = depth;
            Vector3 initialDir = nextPos - startPos;
            if (initialDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(initialDir);
            }
        }
        // ----------------------------

        FishData fishData = GameTempDataManagement.Instance.GetFishData(FishType);
        if (fishData != null)
            FishData_Network = fishData.ToNetworkStruct();

        Depth = depth;
    }

    public override void Spawned()
    {
        if (FishModel != null)
            FishModel.SetActive(true);

        if (Object.HasStateAuthority)
        {
            TotalDuration = FishData_Network.Duration;
            MoveTimer = TickTimer.CreateFromSeconds(Runner, FishData_Network.Duration);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        Move();
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        if (!Object.HasStateAuthority)
            return;

        if (PathPoints == null || PathPoints.Length < 2)
            return;

        float elapsed = TotalDuration - (MoveTimer.RemainingTime(Runner) ?? 0);
        float t = Mathf.Clamp01(elapsed / TotalDuration);

        Vector3 nextPos = GetCatmullRomPosition(t, PathPoints);
        nextPos.y = Depth;
        Vector3 direction = nextPos - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = nextPos;

        if (t >= 1.0f && Object.HasStateAuthority)
        {
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
        ShowCoinText(
            reward: reward);

        if (FishModel != null)
            FishModel.SetActive(false);

        RPC_GetHit(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GetHit(PlayerRef player)
    {
        Runner.Despawn(Object);
    }
}