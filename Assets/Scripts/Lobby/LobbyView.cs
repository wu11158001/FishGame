using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

public class LobbyView : BasicView
{
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Button StartBtn;

    private void OnDestroy()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AsccountDataChangeDelete -= AccountDataChange;
    }

    private void Start()
    {
        StartBtn.onClick.AddListener(() => { NetworkRunnerManagement.Instance.JoInRoom(); });

        FirestoreManagement.Instance.AsccountDataChangeDelete += AccountDataChange;
        FirestoreManagement.Instance.StartListenAccountData();
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    /// <param name="response"></param>
    private void AccountDataChange(FirestoreResponse response)
    {
        if(response != null)
        {
            AccountData data = JsonConvert.DeserializeObject<AccountData>(response.JsonData);
            CoinText.text = data.Coins.ToString();
        }
    }

}
