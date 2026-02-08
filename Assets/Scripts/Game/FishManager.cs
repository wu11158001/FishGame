using System.Collections.Generic;
using UnityEngine;

public class FishManager : MonoBehaviour
{
    List<Fish> ActiveFishes = new();

    // 畫面邊界
    const float LockingSidePosX = 9.5f;
    const float LockingSidePosY = 5.5f;

    /// <summary>
    /// 註冊產生的魚
    /// </summary>
    public void RegisterFish(Fish fish)
    {
        if (!ActiveFishes.Contains(fish))
        {
            ActiveFishes.Add(fish);
        }
    }

    /// <summary>
    /// 魚死亡註銷
    /// </summary>
    public void UnregisterFish(Fish fish)
    {
        if (ActiveFishes.Contains(fish))
        {
            ActiveFishes.Remove(fish);
        }
    }

    /// <summary>
    /// 獲取場上所有的魚
    /// </summary>
    public ActiveFishData GetActiveFishes()
    {
        // 過濾掉已經無效的物件
        ActiveFishes.RemoveAll(f => f == null);

        ActiveFishData activeFishData = new();
        activeFishData.FishList = new();
        activeFishData.TargetObjList = new();
        foreach (Fish fish in ActiveFishes)
        {
            BoxCollider[] colliders = fish.GetComponentsInChildren<BoxCollider>();

            foreach (var box in colliders)
            {
                bool isInSceneX = box.gameObject.transform.position.x >= -LockingSidePosX && box.gameObject.transform.position.x <= LockingSidePosX;
                bool isInSceneZ = box.gameObject.transform.position.z >= -LockingSidePosY && box.gameObject.transform.position.z <= LockingSidePosY;

                if (isInSceneX && isInSceneZ && fish != null && !fish.IsDie)
                {
                    activeFishData.FishList.Add(fish);
                    activeFishData.TargetObjList.Add(box.gameObject.transform);
                    break;
                }
            }
        }

        return activeFishData;
    }
}

/// <summary>
/// 場景存活魚資料
/// </summary>
public struct ActiveFishData
{
    /// <summary> 魚 </summary>
    public List<Fish> FishList;

    /// <summary> 魚碰撞框物件 </summary>
    public List<Transform> TargetObjList;
}