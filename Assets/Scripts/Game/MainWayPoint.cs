using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MainWayPoint : MonoBehaviour
{
    public List<WayPoint> WayPoints { get; private set; } = new();

    /// <summary>
    /// 獲取移動路徑表
    /// </summary>
    public List<WayPoint> GetWayPoints()
    {
        if(WayPoints == null || WayPoints.Count == 0)
        {
            WayPoints = GetComponentsInChildren<WayPoint>().ToList();

            foreach (var wayPoint in WayPoints)
            {
                wayPoint.SetPoint();
            }
        }

        return WayPoints;
    }
}
