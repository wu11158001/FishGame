using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;

public class GameView : BasicView
{
    [Header("SeatArea")]
    [SerializeField] RectTransform SeatArea;
    [SerializeField] Button TurretBtn;
    [SerializeField] Button ReduceCostBtn;
    [SerializeField] Button AddCostBtn;

    [Header("CostArea")]
    [SerializeField] List<TextMeshProUGUI> PlayerCostTexts = new();
    [SerializeField] List<GameObject> PlayerCostPanels = new();

    [Header("AccountInfoArea")]
    [SerializeField] TextMeshProUGUI AccountText;
    [SerializeField] Button CoinStoreBtn;
    [SerializeField] TextMeshProUGUI AccountCoinText;

    [Header("BaseSkillArea")]
    [SerializeField] Toggle LockingTog;
    [SerializeField] GameObject LockingSelectFrame;
    [SerializeField] Toggle AutoTog;
    [SerializeField] GameObject AutoSelectFrame;

    [Header("Mask")]
    [SerializeField] GameObject MaskObj;

    GameFloatBtn GameFloatBtn;
    bool IsLocalMirror;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    private void OnDestroy()
    {
        if(TempDataManagement.Instance != null)
        {
            TempDataManagement.Instance.TempAccountCoinChangeDelegate -= TempAccountCoinChange;
            TempDataManagement.Instance.IsSkill_AutoCloseDelegate -= Skill_AutoClose;
        }
    }

    protected override void Start()
    {
        base.Start();

        TurretBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsStopShot = true;
            _ = AddressableManagement.Instance.OpenTurretStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsStopShot = false;
            });
        });

        CoinStoreBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsStopShot = true;
            _ = AddressableManagement.Instance.OpenCoinStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsStopShot = false;
            });
        });

        ReduceCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        AddCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: false); });        
        LockingTog.onValueChanged.AddListener((isOn) => { Skill_Lock(isOn); });
        AutoTog.onValueChanged.AddListener((isOn) => { Skill_Auto(isOn); });

        if (TempDataManagement.Instance != null)
        {
            TempDataManagement.Instance.TempAccountCoinChangeDelegate += TempAccountCoinChange;
            TempDataManagement.Instance.IsSkill_AutoCloseDelegate += Skill_AutoClose;
        }

        AddressableManagement.Instance.OpenGameFloatBtn(
            callback: (gameFloatBtn) =>
            {
                GameFloatBtn = gameFloatBtn;
            });
    }

    public void SetData(int localSeat, bool isMirror, Action closeAction)
    {
        IsLocalMirror = isMirror;
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;
        MaskEnable(false);

        // 座位區域
        SeatArea.anchoredPosition =
            localSeat % 2 == 0 ?
            LeftSeatPosision :
            RightSeatPosision;

        AccountText.text = TempDataManagement.Instance.TempAccountData.Account;
        AccountCoinText.text = StringUtility.CurrencyFormat(TempDataManagement.Instance.TempAccountData.Coins);

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 遮罩顯示控制
    /// </summary>
    public void MaskEnable(bool isShow)
    {
        MaskObj.SetActive(isShow);
        GameFloatBtn?.SetEnable(!isShow);
    }

    /// <summary>
    /// 暫存帳戶金幣資料變更
    /// </summary>
    private void TempAccountCoinChange(double newCoin)
    {
        AccountCoinText.text = StringUtility.CurrencyFormat(newCoin);
    }

    /// <summary>
    /// 玩家子彈花費變更
    /// </summary>
    public void PlayerCostChange(int seatIndex, double cost)
    {
        int seat = seatIndex;

        // 反向
        if (IsLocalMirror)
        {
            seat = 3 - seatIndex;
        }

        if(seat >= PlayerCostTexts.Count || seat < 0)
        {
            Debug.LogError($"玩家子彈花費變更錯誤: index = {seat}");
            return;
        }

        if(cost < 0)
        {
            PlayerCostPanels[seat].SetActive(false);
            return;
        }

        if(!PlayerCostPanels[seat].activeSelf)
            PlayerCostPanels[seat].SetActive(true);

        PlayerCostTexts[seat].text = StringUtility.CurrencyFormat(cost);
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
