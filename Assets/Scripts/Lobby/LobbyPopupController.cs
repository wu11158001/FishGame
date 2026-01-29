using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class LobbyPopupController : MonoBehaviour
{
    Queue<LobbyPopupEnum> PopupQueue = new();

    AccountData AccountData;

    // 檢測固定資料獲取狀態
    bool IsCheckData = false;
    Dictionary<CheckFixedDataEnum, bool> CheckFixedDataDic = new();

    // 用於判斷是否過隔日
    int DiffDays;

    private void OnDestroy()
    {
        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountDataChangeDelegate -= AccountDataChange;
    }

    private void Start()
    {
        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.GameTempData = null;
            FirestoreDataManagement.Instance.AccountDataChangeDelegate += AccountDataChange;
            FirestoreDataManagement.Instance.StartListenAccountData();
        }            
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        if (accountData != null)
        {
            AccountData = accountData;

            if(!IsCheckData)
            {
                IsCheckData = true;
                Debug.Log("進入大廳，開始獲取Firestore固定資料");

                CheckFixedDataDic.Clear();
                foreach (CheckFixedDataEnum item in Enum.GetValues(typeof(CheckFixedDataEnum)))
                {
                    CheckFixedDataDic.Add(item, false);
                }

                if (FirestoreDataManagement.Instance != null)
                {
                    // 登入與註冊獎勵資料
                    FirestoreDataManagement.Instance.GetLoginAndRegisterData(callback: CheckFixedData);

                    // 所有砲台資料
                    FirestoreDataManagement.Instance.GetAllTurretData(callback: CheckFixedData);

                    // 所有關卡資料
                    FirestoreDataManagement.Instance.GetAllLevelData(callback: CheckFixedData);

                    // 金幣商店資料
                    FirestoreDataManagement.Instance.GetCoinStoreData(callback: CheckFixedData);

                    // 所有道具商店資料
                    FirestoreDataManagement.Instance.GetAllPropsStoreData(callback: CheckFixedData);
                }
            }
            else
            {
                int diffDays = FirestoreDataManagement.Instance.GetDifferenceDays();
                if (DiffDays != diffDays)
                    StartPopupProcess();
            }
        }
    }

    /// <summary>
    /// 檢查固定資料獲取狀態
    /// </summary>
    private void CheckFixedData(CheckFixedDataEnum dataType, bool isSuccess)
    {
        if (!CheckFixedDataDic.ContainsKey(dataType) || !isSuccess)
        {
            Debug.LogError($"檢查固定資料獲取狀態錯誤: {dataType}");
            return;
        }

        CheckFixedDataDic[dataType] = isSuccess;

        if (CheckFixedDataDic.All(x => x.Value == true))
        {
            Debug.Log("固定資料獲取完成，開始彈窗流程");
            Canvas_Global.Instance.CloseLoading();
            Canvas_Global.Instance.CloseSceneLoadingView();

            StartPopupProcess();
        }
    }

    /// <summary>
    /// 開始彈窗流程
    /// </summary>
    private void StartPopupProcess()
    {
        // 當前時間與註冊時間相差天數
        int diffDays = FirestoreDataManagement.Instance.GetDifferenceDays();
        DiffDays = diffDays;

        PopupQueue.Clear();
        foreach (LobbyPopupEnum popup in Enum.GetValues(typeof(LobbyPopupEnum)))
        {
            PopupQueue.Enqueue(popup);
        }

        PopupProcess();
    }

    /// <summary>
    /// 彈窗流程
    /// </summary>
    private void PopupProcess()
    {
        if(AccountData == null)
        {
            Debug.LogError("彈窗流程錯誤: 帳戶資料null");
            return;
        }

        if (PopupQueue.Count == 0) return;

        LobbyPopupEnum popup = PopupQueue.Dequeue();

        switch (popup)
        {
            // 註冊獎勵
            case LobbyPopupEnum.RegisterReward:
                CheckRegisterReward();
                break;

            // 登入獎勵
            case LobbyPopupEnum.LoginReward:
                CheckLoginReward();
                break;

            // 7日簽到
            case LobbyPopupEnum.SevenDays:
                CheckSevenDay();
                break;

            default:
                CheckLoginReward();
                break;
        }
    }

    /// <summary>
    /// 檢測註冊獎勵
    /// </summary>
    private void CheckRegisterReward()
    {
        if(string.IsNullOrEmpty(AccountData.RegisterTime))
        {
            // 沒有註冊資料代表新註冊
            if (AddressableManagement.Instance != null)
            {
                AddressableManagement.Instance.OpenRegisterRewardView(closeAction: PopupProcess);
            }
        }
        else
        {
            PopupProcess();
        }
    }

    /// <summary>
    /// 檢測登入獎勵
    /// </summary>
    private void CheckLoginReward()
    {
        if(string.IsNullOrEmpty(AccountData.LastLoginTime))
        {
            // 沒有登入資料代表新註冊
            if (AddressableManagement.Instance != null)
            {
                AddressableManagement.Instance.OpenLoginRewardView(closeAction: PopupProcess);
            }
        }
        else
        {
            DateTime lastTime = DateTime.Parse(AccountData.LastLoginTime);
            DateTime now = DateTime.UtcNow.AddHours(8);

            // 已過隔日
            if (now.Date > lastTime.Date)
            {
                if (AddressableManagement.Instance != null)
                {
                    AddressableManagement.Instance.OpenLoginRewardView(closeAction: PopupProcess);
                }
            }
            else
            {
                PopupProcess();
            }
        }        
    }

    /// <summary>
    /// 檢測7日簽到
    /// </summary>
    private void CheckSevenDay()
    {
        // 獲取已簽到天
        SortedSet<int> sigInDays = FirestoreDataManagement.Instance.GetSignInDays();

        // 當前時間與註冊時間相差天數
        int diffDays = FirestoreDataManagement.Instance.GetDifferenceDays();

        if (diffDays < 7 && !sigInDays.Contains(diffDays))
        {
            if (AddressableManagement.Instance != null)
            {
                AddressableManagement.Instance.OpenSevenDayView(closeAction: PopupProcess);
            }
        }
        else
        {
            PopupProcess();
        }
    }
}
