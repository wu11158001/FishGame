using UnityEngine;
using Fusion;
using System.Linq;

public class NormalFish : NetworkBehaviour
{
    [Networked] private TickTimer MoveTimer { get; set; }
    [Networked] private float TotalDuration { get; set; }

    private Vector3[] PathPoints;

    public void SetData(bool isMirror, WayPoint wayPoint)
    {
        var query = wayPoint.Points.Select(t => t.position);
        if (isMirror) query = query.Reverse();
        PathPoints = query.ToArray();

        TotalDuration = 5;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            MoveTimer = TickTimer.CreateFromSeconds(Runner, TotalDuration);
        }
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
}