using UnityEngine;
using System;
using System.Collections.Generic;

public class LobbyPopupController : MonoBehaviour
{
    AccountData AccountData;

    Queue<LobbyPopupEnum> PopupQueue = new();

    private void OnDestroy()
    {
        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AsccountDataChangeDelegate -= AccountDataChange;
    }

    private void Start()
    {
        if (FirestoreDataManagement.Instance != null)
        {
            //FirestoreDataManagement.Instance.GameTempData = null;
            FirestoreDataManagement.Instance.AsccountDataChangeDelegate += AccountDataChange;
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
            Debug.Log("進入大廳，開始彈窗流程");
            Canvas_Global.Instance.CloseLoading();
            Canvas_Global.Instance.CloseSceneLoadingView();
            AccountData = accountData;
            PopupQueue.Clear();
            foreach (LobbyPopupEnum popup in Enum.GetValues(typeof(LobbyPopupEnum)))
            {
                PopupQueue.Enqueue(popup);
            }
                
            PopupProcess();
        }
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
}
