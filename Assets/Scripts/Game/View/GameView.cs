using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class GameView : BasicView
{
    [Header("SeatArea")]
    [SerializeField] RectTransform SeatArea;
    [SerializeField] Button TurretBtn;
    [SerializeField] Button ReduceCostBtn;
    [SerializeField] Button AddCostBtn;
    [SerializeField] TextMeshProUGUI CurrCostText;

    [Header("AccountInfoArea")]
    [SerializeField] Button CoinStoreBtn;
    [SerializeField] TextMeshProUGUI AccountCoinText;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    private void OnDestroy()
    {
        if(GameTempDataManagement.Instance != null)
        {
            GameTempDataManagement.Instance.TempAccountCoinChangeDelegate -= TempAccountDataChange;
            GameTempDataManagement.Instance.CurrCostChangeDelegate -= CurrCostChange;
        }
    }

    protected override void Start()
    {
        base.Start();

        TurretBtn.onClick.AddListener(() => { _ = AddressableManagement.Instance.OpenTurretStoreView(); });
        ReduceCostBtn.onClick.AddListener(() => { GameTempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        AddCostBtn.onClick.AddListener(() => { GameTempDataManagement.Instance.ChangeCurrCost(isReduce: false); });
        CoinStoreBtn.onClick.AddListener(() => { _ = AddressableManagement.Instance.OpenCoinStoreView(); });

        if (GameTempDataManagement.Instance != null)
        {
            GameTempDataManagement.Instance.TempAccountCoinChangeDelegate += TempAccountDataChange;
            GameTempDataManagement.Instance.CurrCostChangeDelegate += CurrCostChange;
        }

        AddressableManagement.Instance.OpenGameFloatBtn();
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

        CurrCostText.text = StringUtility.CurrencyFormat(GameTempDataManagement.Instance.CurrentLevelData.DefaultCost);
        AccountCoinText.text = StringUtility.CurrencyFormat(GameTempDataManagement.Instance.TempAccountData.Coins);

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 暫存資料變更
    /// </summary>
    private void TempAccountDataChange(double coin)
    {
        AccountCoinText.text = StringUtility.CurrencyFormat(coin);
    }

    /// <summary>
    /// 當前子彈花費變更
    /// </summary>
    /// <param name="cost"></param>
    private void CurrCostChange(double cost)
    {
        CurrCostText.text = StringUtility.CurrencyFormat(cost);
    }
}
