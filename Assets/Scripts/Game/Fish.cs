using UnityEngine;
using Fusion;
using System.Linq;
using System.Collections;

public class Fish : NetworkBehaviour
{
    // 移動計時器
    [Networked] TickTimer MoveTimer { get; set; }
    // 總移動時間
    [Networked] float TotalDuration { get; set; }
    // 魚資料
    [Networked] FishData_Network FishData_Network { get; set; }

    // 激活物件
    [SerializeField] GameObject FishModel;

    NetworkPrefabEnum FishType;
    Vector3[] PathPoints;

    // 防止閃爍隱藏時間
    const float DelayActiveTime = 0.5f;

    public void SetData(NetworkPrefabEnum fishType, bool isMirror, WayPoint wayPoint)
    {
        FishType = fishType;

        // 移動路徑獲取
        var query = wayPoint.Points.Select(t => t.position);
        if (isMirror) query = query.Reverse();
        PathPoints = query.ToArray();

        // 魚資料獲取
        FishData fishData = TempDataManagement.Instance.GetFishData(FishType);

        if (fishData != null)
            FishData_Network = fishData.ToNetworkStruct();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            TotalDuration = FishData_Network.Duration;
            MoveTimer = TickTimer.CreateFromSeconds(Runner, FishData_Network.Duration + DelayActiveTime);
        }

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 防止閃爍延遲顯示
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldShow()
    {
        FishModel.SetActive(false);
        yield return new WaitForSeconds(DelayActiveTime);
        FishModel.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (PathPoints == null || PathPoints.Length < 2) return;

        // 計算總進度 (0 ~ 1)
        float elapsed = TotalDuration - (MoveTimer.RemainingTime(Runner) ?? 0);
        float t = Mathf.Clamp01(elapsed / TotalDuration);

        // 取得 Catmull-Rom 座標
        Vector3 nextPos = GetCatmullRomPosition(t, PathPoints);

        // 旋轉處理 (2D 朝向移動方向)
        Vector3 direction = nextPos - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        transform.position = nextPos;

        if (t >= 1.0f && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// 曲線旋轉
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
    /// 魚被擊中
    /// </summary>
    /// <param name="player"></param>
    public void GetHit(PlayerRef player)
    {
        RPC_GetHit(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GetHit(PlayerRef player)
    {
        Runner.Despawn(Object);
    }
}