using System;
using System.Collections.Generic;

public static class WaterWaveFishManagement
{
    /// <summary>
    /// 浪潮魚群資料(關卡類型, 依次產生魚的類型)
    /// </summary>
    private static Dictionary<LevelEnum, WaterWaveFishData> WaterWaveFishDic = new()
    {
        // 經典關卡
        {
            LevelEnum.ClassicLevel,
            new()
            {
                SpawnBetweenTime = 2f,
                MoveDuration = 20,
                FishsType = new()
                {
                    NetworkPrefabEnum.NormalFish_8,
                    NetworkPrefabEnum.NormalFish_8,
                    NetworkPrefabEnum.NormalFish_8,
                    NetworkPrefabEnum.NormalFish_8,
                    NetworkPrefabEnum.NormalFish_6,
                    NetworkPrefabEnum.NormalFish_6,
                    NetworkPrefabEnum.NormalFish_0,
                    NetworkPrefabEnum.NormalFish_9,
                    NetworkPrefabEnum.NormalFish_0,
                    NetworkPrefabEnum.NormalFish_9,
                    NetworkPrefabEnum.NormalFish_0,
                    NetworkPrefabEnum.NormalFish_9,
                }
            }
        },

        // 鯊魚關卡
        {
            LevelEnum.SharkLevel,
            new()
            {
                SpawnBetweenTime = 4f,
                MoveDuration = 30,
                FishsType = new()
                {
                    NetworkPrefabEnum.NormalFish_7,
                    NetworkPrefabEnum.NormalFish_5,
                    NetworkPrefabEnum.NormalFish_7,
                    NetworkPrefabEnum.NormalFish_5,
                    NetworkPrefabEnum.NormalFish_7,
                    NetworkPrefabEnum.NormalFish_5,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_2,
                    NetworkPrefabEnum.NormalFish_2,
                }
            }
        },

        // 金龍關卡
        {
            LevelEnum.DragonLevel,
            new()
            {
                SpawnBetweenTime = 4.5f,
                MoveDuration = 33,
                FishsType = new()
                {
                    NetworkPrefabEnum.NormalFish_3,
                    NetworkPrefabEnum.NormalFish_4,
                    NetworkPrefabEnum.NormalFish_3,
                    NetworkPrefabEnum.NormalFish_4,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_0,
                    NetworkPrefabEnum.NormalFish_1,
                    NetworkPrefabEnum.NormalFish_0,
                    NetworkPrefabEnum.NormalFish_9,
                    NetworkPrefabEnum.NormalFish_9,
                    NetworkPrefabEnum.NormalFish_9,
                    NetworkPrefabEnum.NormalFish_9,
                }
            }
        },
    };

    /// <summary>
    /// 獲取浪潮魚群資料
    /// </summary>
    /// <param name="levelType"></param>
    public static WaterWaveFishData GetWaterWaveFishData(LevelEnum levelType)
    {
        foreach (var data in WaterWaveFishDic)
        {
            if (data.Key == levelType)
                return data.Value;
        }

        return null;
    }
}


/// <summary>
/// 浪潮魚群資料
/// </summary>
public class WaterWaveFishData
{
    /// <summary> 產生魚的間隔時間 </summary>
    public float SpawnBetweenTime;

    /// <summary> 魚移動時間 </summary>
    public float MoveDuration;

    /// <summary> 依次產生魚的類型 </summary>
    public List<NetworkPrefabEnum> FishsType;
}
