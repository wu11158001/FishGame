using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

public class DataManagement : SingletonMonoBehaviour<DataManagement>
{
    Dictionary<NetworkPrefabEnum, FishData> FishDataDic { get; } = new();

    Action GetAllFishDataAction;

    /// <summary>
    /// 獲取魚群資料
    /// </summary>
    public void GetAllFishData(Action callback)
    {
        GetAllFishDataAction = callback;

        List<NetworkPrefabEnum> fishTypes = Enum.GetValues(typeof(NetworkPrefabEnum))
            .Cast<NetworkPrefabEnum>()
            .Where(e => e.ToString().StartsWith("NormalFish"))
            .ToList();

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.FishData,
                callback: GetAllFishDataCallback);
    }

    /// <summary>
    /// 獲取魚群資料Callback
    /// </summary>
    private void GetAllFishDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            FishDataDic.Clear();
            List<FishData> fishList = JsonConvert.DeserializeObject<List<FishData>>(response.JsonData);

            foreach (var data in fishList)
            {
                FishDataDic.Add(data.FishType, data);
            }

            GetAllFishDataAction?.Invoke();
        }
        else
        {
            Debug.LogError($"獲取魚群資料失敗");
            AddressableManagement.Instance.ShowToast("Wiring Error");
        }        
    }

    /// <summary>
    /// 獲取魚資料
    /// </summary>
    public FishData GetFishData(NetworkPrefabEnum fishType)
    {
        // 嘗試從字典中獲取資料
        if (FishDataDic.TryGetValue(fishType, out FishData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到魚種資料: {fishType}");
        return null;
    }
}
