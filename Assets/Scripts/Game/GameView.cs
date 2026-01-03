using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Newtonsoft.Json;

public class GameView : BasicView
{
    [SerializeField] Button ShutdownBtn;

    [Header("SeatArea")]
    [SerializeField] RectTransform SeatArea;
    [SerializeField] Button ReduceCostBtn;
    [SerializeField] Button AddCostBtn;
    [SerializeField] TextMeshProUGUI CurrCostText;

    [Header("AccountInfoArea")]
    [SerializeField] TextMeshProUGUI AccountCoinText;

    Vector2 LeftSeatPosision = new(-594, -495);
    Vector2 RightSeatPosision = new(594, -495);

    private void Start()
    {
        ShutdownBtn.onClick.AddListener(Shutdown);

        ReduceCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        AddCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: false); });

        TempDataManagement.Instance.TempAccountCoinChangeDelegate += TempAccountDataChange;
        TempDataManagement.Instance.CurrCostChangeDelegate += CurrCostChange;
    }

    public void SetData(int localSeat, Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        // 座位區域
        SeatArea.anchoredPosition =
            localSeat % 2 == 0 ?
            LeftSeatPosision :
            RightSeatPosision;

        CurrCostText.text = $"{StringUtility.CurrencyFormat(TempDataManagement.Instance.CurrentLevelData.DefaultCost)}";
        AccountCoinText.text = $"{StringUtility.CurrencyFormat(TempDataManagement.Instance.TempAccountData.Coins)}";

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 暫存資料變更
    /// </summary>
    private void TempAccountDataChange(int coin)
    {
        AccountCoinText.text = $"{StringUtility.CurrencyFormat(coin)}";
    }

    /// <summary>
    /// 當前子彈花費變更
    /// </summary>
    /// <param name="cost"></param>
    private void CurrCostChange(int cost)
    {
        CurrCostText.text = $"{cost}";
    }

    /// <summary>
    /// 斷開連接離開
    /// </summary>
    private void Shutdown()
    {
        AddressableManagement.Instance.ShowLoading();
        NetworkRunnerManagement.Instance.Shutdown(); 
    }
}
