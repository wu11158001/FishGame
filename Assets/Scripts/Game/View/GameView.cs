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
    [SerializeField] TextMeshProUGUI AccountText;
    [SerializeField] Button CoinStoreBtn;
    [SerializeField] TextMeshProUGUI AccountCoinText;

    [Header("BaseSkillArea")]
    [SerializeField] Toggle LockingTog;
    [SerializeField] GameObject LockingSelectFrame;
    [SerializeField] Toggle AutoTog;
    [SerializeField] GameObject AutoSelectFrame;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    private void OnDestroy()
    {
        if(TempDataManagement.Instance != null)
        {
            TempDataManagement.Instance.TempAccountCoinChangeDelegate -= TempAccountCoinChange;
            TempDataManagement.Instance.CurrCostChangeDelegate -= CurrCostChange;
            TempDataManagement.Instance.IsSkill_AutoCloseDelegate -= Skill_AutoClose;
        }
    }

    protected override void Start()
    {
        base.Start();

        TurretBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsOpenView = true;
            _ = AddressableManagement.Instance.OpenTurretStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsOpenView = false;
            });
        });

        CoinStoreBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsOpenView = true;
            _ = AddressableManagement.Instance.OpenCoinStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsOpenView = false;
            });
        });

        ReduceCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        AddCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: false); });        
        LockingTog.onValueChanged.AddListener((isOn) => { Skill_Lock(isOn); });
        AutoTog.onValueChanged.AddListener((isOn) => { Skill_Auto(isOn); });

        if (TempDataManagement.Instance != null)
        {
            TempDataManagement.Instance.TempAccountCoinChangeDelegate += TempAccountCoinChange;
            TempDataManagement.Instance.CurrCostChangeDelegate += CurrCostChange;
            TempDataManagement.Instance.IsSkill_AutoCloseDelegate += Skill_AutoClose;
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

        AccountText.text = TempDataManagement.Instance.TempAccountData.Account;
        CurrCostText.text = StringUtility.CurrencyFormat(TempDataManagement.Instance.CurrentLevelData.DefaultCost);
        AccountCoinText.text = StringUtility.CurrencyFormat(TempDataManagement.Instance.TempAccountData.Coins);

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 暫存帳戶金幣資料變更
    /// </summary>
    private void TempAccountCoinChange(double coin)
    {
        Debug.Log($"暫存帳戶金幣資料變更: {coin}");
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
    /// 技能_鎖定
    /// </summary>
    private void Skill_Lock(bool isOn)
    {
        LockingSelectFrame.SetActive(isOn);
        TempDataManagement.Instance.IsSkill_Locking = isOn;
    }

    /// <summary>
    /// 技能_自動強制關閉事件
    /// </summary>
    private void Skill_AutoClose()
    {
        AutoTog.isOn = false;
    }

    /// <summary>
    /// 技能_自動
    /// </summary>
    private void Skill_Auto(bool isOn)
    {
        AutoSelectFrame.SetActive(isOn);
        TempDataManagement.Instance.IsSkill_Auto = isOn;
    }
}
