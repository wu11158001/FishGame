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
    public bool IsMirror { get; set; }
    public int LocalSeatIndex { get; set; }
    public List<Vector3> SeatPositions { get; set; } = new();
    // 進入關卡資料獲取檢測
    Action<CheckJoinRoomDataEnum> GetCurrentLevelDataAction;
    // 當前子彈花費變更事件
    public delegate void CurrCostChange(double cost);
    public event CurrCostChange CurrCostChangeDelegate;
    // 紀錄變更的獎池值
    public double RecodJackpot { get; set; }
    Coroutine UpdateLevelDataJackpotCoroutine;
    // 定時更新獎池時間(秒)
    const float UpdateLevelDataJackpotTime = 30f;

    /// <summary>
    /// 魚群資料
    /// </summary>
    Dictionary<NetworkPrefabEnum, FishData> FishDataDic { get; } = new();
    Action<CheckJoinRoomDataEnum> GetAllFishDataAction;

    /// <summary>
    /// 砲台資料
    /// </summary>
    Dictionary<TurretEnum, TurretData> TurretDataDic { get; } = new();
    Action<CheckJoinRoomDataEnum> GetAllTurretDataAction;

    /// <summary>
    /// 暫存帳戶資料
    /// </summary>
    public AccountData TempAccountData { get; private set; } = new();
    Action<CheckJoinRoomDataEnum> GetTempAccountDataAction;
    // 暫存金幣變更事件
    public delegate void TempAccountCoinChange(double changeValue);
    public event TempAccountCoinChange TempAccountCoinChangeDelegate;
    // 帳戶金幣定時更新
    Coroutine UpdateAccountCoinCoroutine;
    // 前一次更新帳戶金幣金額
    double PreUpdateCoin;
    // 定時更新帳戶時間(秒)
    const float UpdateAccountDataTime = 30f;
    // 控制金幣的顯示更新(有些)

    // 是否不可射擊
    public bool IsStopShot { get; set; }
    // 技能_自動
    public bool IsSkill_Auto { get; set; }
    // 技能_鎖定強制關閉事件
    public delegate void Skill_AutoClose();
    public event Skill_AutoClose IsSkill_AutoCloseDelegate;
    // 技能_鎖定
    public bool IsSkill_Locking { get; set; }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AccountCoinDataChangeDelegate -= AccountCoinDataChange;

        SendUpdateAccountCoinData();

        StopAllCoroutines();
    }

    private void Start()
    {
        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.AccountCoinDataChangeDelegate += AccountCoinDataChange;
            FirestoreManagement.Instance.LevelDataChangeDelegate += LevelDataChange;
        }            
    }

    public void Initialize()
    {
        IsStopShot = false;
        IsSkill_Auto = false;
        IsSkill_Locking = false;
    }

    #region 當前關卡資料

    /// <summary>
    /// 獲取當前關卡資料
    /// </summary>
    public void GetCurrentLevelData(LevelEnum levelType, Action<CheckJoinRoomDataEnum> callback)
    {
        GetCurrentLevelDataAction = callback;

        if(FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetDataFromFirestore(
                path: FirestoreCollectionNameEnum.LevelData,
                docId: levelType.ToString(),
                callback: GetCurrentLevelDataCallback);
        }
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
    /// 關卡資料變更
    /// </summary>
    private void LevelDataChange(LevelData levelData)
    {
        if(levelData != null)
        {
            double currDefaultCost = CurrentLevelData.DefaultCost;

            CurrentLevelData = levelData;

            // 子彈花費本地不更新
            CurrentLevelData.DefaultCost = currDefaultCost;
        }
    }

    /// <summary>
    /// 停止計時更新Firestore關卡獎池資料
    /// </summary>
    public void StopTimingUpdateLevelDataJackpot()
    {
        if (UpdateLevelDataJackpotCoroutine != null)
            StopCoroutine(UpdateLevelDataJackpotCoroutine);

        SendUpdateLevelDataJackpot();
    }

    /// <summary>
    /// 開始計時更新Firestore關卡獎池資料
    /// </summary>
    public void StartTimingUpdateLevelDataJackpot()
    {
        if (UpdateLevelDataJackpotCoroutine != null)
            StopCoroutine(UpdateLevelDataJackpotCoroutine);

        UpdateLevelDataJackpotCoroutine = StartCoroutine(IUpdateLevelDataJackpot());
    }

    /// <summary>
    /// 計時更新Firestore關卡獎池資料
    /// </summary>
    private IEnumerator IUpdateLevelDataJackpot()
    {
        while (true)
        {
            yield return new WaitForSeconds(UpdateLevelDataJackpotTime);
            SendUpdateLevelDataJackpot();
        }        
    }

    /// <summary>
    /// 發送更新Firestore關卡獎池資料
    /// </summary>
    public void SendUpdateLevelDataJackpot()
    {
        if (RecodJackpot != 0)
        {
            double currJackpot = CurrentLevelData.Jackpot;
            currJackpot += RecodJackpot;

            var updates = new Dictionary<string, object>
            {
                { "Jackpot", currJackpot }
            };

            if (FirestoreManagement.Instance != null)
            {
                FirestoreManagement.Instance.UpdateDataToFirestore(
                path: FirestoreCollectionNameEnum.LevelData,
                docId: CurrentLevelData.LevelName,
                updates: updates,
                callback: (res) =>
                {
                    if (!res.IsSuccess) Debug.LogError("發送更新Firestore關卡獎池資料失敗");
                });
            }

            RecodJackpot = 0;
        }
    }

    #endregion

    #region 魚群資料

    /// <summary>
    /// 獲取魚群資料
    /// </summary>
    public void GetAllFishData(Action<CheckJoinRoomDataEnum> callback)
    {
        GetAllFishDataAction = callback;

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.FishData,
                callback: GetAllFishDataCallback);
    }

    /// <summary>
    /// 獲取所有魚資料Callback
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
    /// 帳戶金幣變更
    /// </summary>
    private void AccountCoinDataChange(AccountData accountData)
    {
        if (accountData != null)
        {
            TempAccountData = accountData;
            TempAccountCoinChangeDelegate?.Invoke(accountData.Coins);
        }
    }

    /// <summary>
    /// 獲取暫存帳戶資料
    /// </summary>
    public void GetTempAccountData(Action<CheckJoinRoomDataEnum> callback)
    {
        GetTempAccountDataAction = callback;

        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreManagement.Instance.CurrLoginInfo.Account,
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
                    GetTempAccountDataAction?.Invoke(CheckJoinRoomDataEnum.AccountData);
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
    public void ChangeTempAccountCoin(double changeValue, bool isInvokeChange)
    {
        if (TempAccountData == null)
            return;

        TempAccountData.Coins += changeValue;

        if (isInvokeChange)
            InvokeTempAccountCoinChangeDelegate();
    }

    /// <summary>
    /// 執行變更暫存帳戶金幣事件
    /// </summary>
    public void InvokeTempAccountCoinChangeDelegate()
    {
        TempAccountCoinChangeDelegate?.Invoke(TempAccountData.Coins);
    }

    /// <summary>
    /// 變更當前子彈花費
    /// </summary>
    public void ChangeCurrCost(bool isReduce)
    {
        double changeValue =
            isReduce ?
            -CurrentLevelData.Gradient :
            CurrentLevelData.Gradient;

        double currCost = CurrentLevelData.DefaultCost;
        currCost += changeValue;

        if (currCost <= CurrentLevelData.MinCost) currCost = CurrentLevelData.MinCost;
        if (currCost >= CurrentLevelData.MaxCost) currCost = CurrentLevelData.MaxCost;

        CurrentLevelData.DefaultCost = currCost;
        CurrCostChangeDelegate?.Invoke(currCost);
    }

    /// <summary>
    /// 停止計時更新Firestore帳戶金幣資料
    /// </summary>
    public void StopTimingUpdateAccountData()
    {
        if (UpdateAccountCoinCoroutine != null)
            StopCoroutine(UpdateAccountCoinCoroutine);

        SendUpdateAccountCoinData();
    }

    /// <summary>
    /// 開始計時更新Firestore帳戶金幣資料
    /// </summary>
    public void StartTimingUpdateAccountData()
    {
        if (UpdateAccountCoinCoroutine != null)
            StopCoroutine(UpdateAccountCoinCoroutine);

        UpdateAccountCoinCoroutine = StartCoroutine(ITimingUpdateAccountData());
    }

    /// <summary>
    /// 計時更新Firestore帳戶金幣資料
    /// </summary>
    private IEnumerator ITimingUpdateAccountData()
    {
        while (true)
        {
            yield return new WaitForSeconds(UpdateAccountDataTime);
            SendUpdateAccountCoinData();
        }
    }

    /// <summary>
    /// 發送更新Firestore帳戶金幣資料
    /// </summary>
    public void SendUpdateAccountCoinData()
    {
        if (PreUpdateCoin != TempAccountData.Coins)
        {
            PreUpdateCoin = TempAccountData.Coins;

            var updates = new Dictionary<string, object>
            {
                { "Coins", TempAccountData.Coins }
            };

            if(FirestoreManagement.Instance != null)
            {
                FirestoreManagement.Instance.UpdateDataToFirestore(
                path: FirestoreCollectionNameEnum.AccountData,
                docId: FirestoreManagement.Instance.CurrLoginInfo.Account,
                updates: updates,
                callback: (res) =>
                {
                    if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣資料失敗");
                });
            }            
        }
    }

    #endregion

    #region 砲台資料

    /// <summary>
    /// 獲取所有砲台資料
    /// </summary>
    public void GetAllTurretData(Action<CheckJoinRoomDataEnum> callback)
    {
        GetAllTurretDataAction = callback;

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.TurretData,
                callback: GetAllTurretDataCallback);
    }

    /// <summary>
    /// 獲取所有砲台資料Callback
    /// </summary>
    public void GetAllTurretDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            TurretDataDic.Clear();
            List<TurretData> turretList = JsonConvert.DeserializeObject<List<TurretData>>(response.JsonData);

            foreach (var data in turretList)
            {
                TurretDataDic.Add(data.TurretType, data);
            }

            GetAllTurretDataAction?.Invoke(CheckJoinRoomDataEnum.TurretData);
        }
        else
        {
            Debug.LogError($"獲取所有砲台資料失敗");
            AddressableManagement.Instance.ShowToast("Wiring Error");
        }
    }

    /// <summary>
    /// 獲取砲台資料
    /// </summary>
    public TurretData GetTurrethData(TurretEnum turretType)
    {
        // 嘗試從字典中獲取資料
        if (TurretDataDic.TryGetValue(turretType, out TurretData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到砲台資料: {turretType}");
        return null;
    }

    #endregion

    #region 技能

    /// <summary>
    /// 技能_自動強制關閉事件
    /// </summary>
    public void IsSkill_AutoCloseEvent()
    {
        IsSkill_Auto = false;
        IsSkill_AutoCloseDelegate?.Invoke();
    }

    #endregion
}
