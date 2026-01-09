using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class GameView : BasicView
{
    [SerializeField] Button ShutdownBtn;

    [Header("SeatArea")]
    [SerializeField] RectTransform SeatArea;
    [SerializeField] Button TurretBtn;
    [SerializeField] Button ReduceCostBtn;
    [SerializeField] Button AddCostBtn;
    [SerializeField] TextMeshProUGUI CurrCostText;

    [Header("AccountInfoArea")]
    [SerializeField] TextMeshProUGUI AccountCoinText;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    protected override void Start()
    {
        base.Start();

        ShutdownBtn.onClick.AddListener(Shutdown);
        TurretBtn.onClick.AddListener(() => { _ = AddressableManagement.Instance.OpenTurretStoreView(); });
        ReduceCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        AddCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: false); });

        TempDataManagement.Instance.TempAccountCoinChangeDelegate += TempAccountDataChange;
        TempDataManagement.Instance.CurrCostChangeDelegate += CurrCostChange;

        // 產生爆金物件池

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

        CurrCostText.text = StringUtility.CurrencyFormat(TempDataManagement.Instance.CurrentLevelData.DefaultCost);
        AccountCoinText.text = StringUtility.CurrencyFormat(TempDataManagement.Instance.TempAccountData.Coins);

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

    /// <summary>
    /// 斷開連接離開
    /// </summary>
    private void Shutdown()
    {
        Canvas_Global.Instance.ShowLoading();
        NetworkRunnerManagement.Instance.Shutdown(); 
    }
}
