using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WayPointMain : MonoBehaviour
{
    private List<WayPoint> AllWayPoints = new();
    private List<WayPoint> NormalWayPoints = new();
    private List<WayPoint> WaterWaveWayPoints = new();

    /// <summary>
    /// 獲取一般移動路徑表
    /// </summary>
    public List<WayPoint> GetNormalWayPoints()
    {
        if (NormalWayPoints == null || NormalWayPoints.Count == 0)
        {
            List<WayPoint> points = GetComponentsInChildren<WayPoint>().ToList();

            foreach (var wayPoint in points)
            {
                if(wayPoint.WayPointType == WayPointEnum.NormalWay)
                {
                    wayPoint.SetPoint();
                    NormalWayPoints.Add(wayPoint);
                }                
            }
        }

        return NormalWayPoints;
    }

    /// <summary>
    /// 獲取浪潮魚群移動路徑表
    /// </summary>
    public List<WayPoint> GetWaterWaveWayPoints()
    {
        if (WaterWaveWayPoints == null || WaterWaveWayPoints.Count == 0)
        {
            List<WayPoint> points = GetComponentsInChildren<WayPoint>().ToList();

            foreach (var wayPoint in points)
            {
                if (wayPoint.WayPointType == WayPointEnum.WaterWaveWay)
                {
                    wayPoint.SetPoint();
                    WaterWaveWayPoints.Add(wayPoint);
                }
            }
        }

        return WaterWaveWayPoints;
    }

    /// <summary>
    /// 以Id獲取移動路徑
    /// </summary>
    /// <param name="id"></param>
    public WayPoint GetWayPointById(int id)
    {
        if (AllWayPoints == null || AllWayPoints.Count == 0)
        {
            List<WayPoint> points = GetComponentsInChildren<WayPoint>().ToList();

            foreach (var wayPoint in points)
            {
                AllWayPoints.Add(wayPoint);
            }
        }

        return AllWayPoints.Find(x => x.WayPointId == id);

    }
}
