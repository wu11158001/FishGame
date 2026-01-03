using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using Newtonsoft.Json;

public class TempDataManagement : SingletonMonoBehaviour<TempDataManagement>
{
    /// <summary>
    /// 當前關卡資料
    /// </summary>
    public LevelData CurrentLevelData { get; private set; } = new();
    Action<CheckJoinRoomDataEnum> GetCurrentLevelDataAction;
    public delegate void CueeCostChange(int cost);
    public event CueeCostChange CurrCostChangeDelegate;

    /// <summary>
    /// 紀錄魚群資料
    /// </summary>
    Dictionary<NetworkPrefabEnum, FishData> FishDataDic { get; } = new();
    Action<CheckJoinRoomDataEnum> GetAllFishDataAction;

    /// <summary>
    /// 暫存帳戶資料
    /// </summary>
    public AccountData TempAccountData { get; private set; } = new();
    Action<CheckJoinRoomDataEnum> GetTempAccountDataAction;
    public delegate void TempAccountCoinChange(int changeValue);
    public event TempAccountCoinChange TempAccountCoinChangeDelegate;
    Coroutine UpdateAccountCoroutine;

    #region 當前關卡資料

    /// <summary>
    /// 獲取當前關卡資料
    /// </summary>
    public void GetCurrentLevelData(LevelEnum levelType, Action<CheckJoinRoomDataEnum> callback)
    {
        GetCurrentLevelDataAction = callback;

        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionNameEnum.LevelData,
            docId: levelType.ToString(),
            callback: GetCurrentLevelDataCallback);
    }

    /// <summary>
    /// 獲取當前關卡資料Callback
    /// </summary>
    private void GetCurrentLevelDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                LevelData data = JsonConvert.DeserializeObject<LevelData>(response.JsonData);
                if (data != null)
                {
                    CurrentLevelData = data;
                    GetCurrentLevelDataAction?.Invoke(CheckJoinRoomDataEnum.LevelData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取當前關卡資料錯誤: {e}");
            }
        }
    }

    /// <summary>
    /// 變更當前子彈花費
    /// </summary>
    public void ChangeCurrCost(bool isReduce)
    {
        int changeValue =
            isReduce ?
            -CurrentLevelData.Gradient :
            CurrentLevelData.Gradient;

        int currCost = CurrentLevelData.DefaultCost;
        currCost += changeValue;

        if (currCost <= CurrentLevelData.MinCost) currCost = CurrentLevelData.MinCost;
        if (currCost >= CurrentLevelData.MaxCost) currCost = CurrentLevelData.MaxCost;

        CurrentLevelData.DefaultCost = currCost;

        CurrCostChangeDelegate?.Invoke(currCost);
    }

    /// <summary>
    /// 停止計時更新Firestore帳戶資料
    /// </summary>
    public void StopTimingUpdateAccountData()
    {
        if (UpdateAccountCoroutine != null)
            StopCoroutine(UpdateAccountCoroutine);

        SendUpdateAccountData();
    }

    /// <summary>
    /// 開始計時更新Firestore帳戶資料
    /// </summary>
    public void StartTimingUpdateAccountData()
    {
        if (UpdateAccountCoroutine != null)
            StopCoroutine(UpdateAccountCoroutine);

        UpdateAccountCoroutine = StartCoroutine(ITimingUpdateAccountData());
    }

    /// <summary>
    /// 計時更新Firestore帳戶資料
    /// </summary>
    private IEnumerator ITimingUpdateAccountData()
    {
        while (true)
        {
            yield return new WaitForSeconds(60);
            SendUpdateAccountData();
        }        
    }

    /// <summary>
    /// 發送更新Firestore帳戶資料
    /// </summary>
    public void SendUpdateAccountData()
    {
        LoginInfo loginInfo = PlayerPrefsManagement.GetLoginInfo();

        var updates = new Dictionary<string, object>
        {
            { "Coins", TempAccountData.Coins }
        };

        FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: loginInfo.Account,
            updates: updates,
            callback: (res) => {
                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶資料失敗");
            });
    }

    #endregion

    #region 魚群資料

    /// <summary>
    /// 獲取魚群資料
    /// </summary>
    public void GetAllFishData(Action<CheckJoinRoomDataEnum> callback)
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

            GetAllFishDataAction?.Invoke(CheckJoinRoomDataEnum.FishData);
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

    #endregion

    #region 遊戲中帳戶資料

    /// <summary>
    /// 獲取暫存帳戶資料
    /// </summary>
    public void GetTempAccountData(Action<CheckJoinRoomDataEnum> callback)
    {
        GetTempAccountDataAction = callback;

        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: PlayerPrefsManagement.GetLoginInfo().Account,
            callback: GetTempAccountDataCallback);
    }

    /// <summary>
    /// 獲取暫存帳戶資料Callback
    /// </summary>
    private void GetTempAccountDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                AccountData data = JsonConvert.DeserializeObject<AccountData>(response.JsonData);
                if(data != null)
                {
                    TempAccountData = data;
                    GetTempAccountDataAction?.Invoke(CheckJoinRoomDataEnum.Account);
                }
                else
                {
                    Debug.LogError($"獲取帳戶資料null!");
                }
            }
            catch (Exception e)
            {
                AddressableManagement.Instance.ShowToast("Wiring Error");
                Debug.LogError($"JSON 解析異常: {e.Message}");
            }
        }
        else
        {
            AddressableManagement.Instance.ShowToast("Wiring Error");
            Debug.LogError($"獲取帳戶資料錯誤!");
        }
    }

    /// <summary>
    /// 變更暫存帳戶金幣
    /// </summary>
    public void ChangeTempAccountCoin(int changeValue)
    {
        if (TempAccountData == null)
            return;

        TempAccountData.Coins += changeValue;

        TempAccountCoinChangeDelegate?.Invoke(TempAccountData.Coins);
    }

    #endregion
}
