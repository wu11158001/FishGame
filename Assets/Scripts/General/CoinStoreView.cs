using UnityEngine;
using UnityEngine.UI;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

public class CoinStoreView : BasicView
{
    [SerializeField] List<Sprite> CoinSprites = new();
    [SerializeField] CoinStoreUnit CoinStoreUnit;
    [SerializeField] RectTransform ContentRect;

    Dictionary<StoreCoinEnum, CoinStoreData> CoinStoreDataDic = new();
    List<CoinStoreUnit> CoinStoreUnitDatas = new();

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountCoinDataChangeDelegate -= AccountCoinDataChange;
    }

    protected override void Start()
    {
        base.Start();

        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountCoinDataChangeDelegate += AccountCoinDataChange;
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;
        GetCoinStoreData();
    }

    /// <summary>
    /// 帳戶金幣變更
    /// </summary>
    private void AccountCoinDataChange(AccountData accountData)
    {
        if(CoinStoreUnitDatas != null)
        {
            foreach (var data in CoinStoreUnitDatas)
            {
                data.UpdateAccountData(accountData);
            }
        }
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
        for (int i = 0; i < CoinStoreUnitDatas.Count; i++)
        {
            Destroy(CoinStoreUnitDatas[i].gameObject);
        }
        CoinStoreUnitDatas.Clear();

        int index = 0;
        CoinStoreUnit.gameObject.SetActive(false);
        foreach (StoreCoinEnum coinType in Enum.GetValues(typeof(StoreCoinEnum)))
        {
            if (coinType == StoreCoinEnum.None)
                continue;

            GameObject obj = Instantiate(CoinStoreUnit.gameObject, ContentRect);
            obj.SetActive(true);
            CoinStoreUnit coinStoreUnit = obj.GetComponent<CoinStoreUnit>();

            if(coinStoreUnit != null)
            {
                CoinStoreUnitData data = new()
                {
                    AccountData = FirestoreDataManagement.Instance?.GameTempData?.TempAccountData,
                    CoinStoreData = GetCoinStoreData(coinType),
                    CoverSprite = CoinSprites[index]
                };

                coinStoreUnit.SetData(data);
                CoinStoreUnitDatas.Add(coinStoreUnit);
            }
            else
            {
                Debug.LogError("創建金幣商品錯誤");
            }

            index++;
        }
    }
}

/// <summary>
/// 金幣商店資料
/// </summary>
public class CoinStoreUnitData
{
    /// <summary> 帳戶資料 </summary>
    public AccountData AccountData;

    /// <summary> 金幣商店資料 </summary>
    public CoinStoreData CoinStoreData;

    /// <summary> 金幣圖 </summary>
    public Sprite CoverSprite;
}