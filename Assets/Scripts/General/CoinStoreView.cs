using UnityEngine;
using UnityEngine.UI;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CoinStoreView : BasicView
{
    Dictionary<StoreCoinEnum, CoinStoreData> CoinStoreDataDic = new();
    CoinStoreData CoinStoreData;

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        GetCoinStoreData();
    }

    /// <summary>
    /// 獲取金幣商店資料
    /// </summary>
    private void GetCoinStoreData()
    {
        if(FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.CoinStoreData,
                callback: GetCoinStoreDataCallback);
        }
    }

    /// <summary>
    /// 獲取金幣商店資料Callback
    /// </summary>
    private void GetCoinStoreDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                List<CoinStoreData> coinList = JsonConvert.DeserializeObject<List<CoinStoreData>>(response.JsonData);

                foreach (var data in coinList)
                {
                    CoinStoreDataDic.Add(data.CoinType, data);
                }

                ReciveAllDataComplete();
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取金幣商店資料錯誤: {e}");
            }
        }
    }

    /// <summary>
    /// 獲取商店金幣資料
    /// </summary>
    private CoinStoreData GetCoinStoreData(StoreCoinEnum coinType)
    {
        // 嘗試從字典中獲取資料
        if (CoinStoreDataDic.TryGetValue(coinType, out CoinStoreData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到商店金幣資料: {coinType}");
        return null;
    }

    /// <summary>
    /// 接收資料完成
    /// </summary>
    private void ReciveAllDataComplete()
    {
        CreateCoinStoreUnit();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建金幣商品
    /// </summary>
    private void CreateCoinStoreUnit()
    {

    }
}
