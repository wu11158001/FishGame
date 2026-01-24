using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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
    [SerializeField] Image FreezeMask;

    [Header("Props Btns")]
    [SerializeField] Button Props_FreezeBtn;

    GameFloatBtn GameFloatBtn;
    bool IsLocalMirror;
    Coroutine FreezeCoroutine;

    GameTerrain GameTerrain;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    private void OnDestroy()
    {
        StopAllCoroutines();

        if(TempDataManagement.Instance != null)
        {
            TempDataManagement.Instance.TempAccountCoinChangeDelegate -= TempAccountCoinChange;
            TempDataManagement.Instance.IsSkill_AutoCloseDelegate -= Skill_AutoClose;
        }
    }

    protected override void Start()
    {
        base.Start();

        // 更換砲台
        TurretBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsStopShot = true;
            _ = AddressableManagement.Instance.OpenTurretStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsStopShot = false;
            });
        });

        // 金幣商店
        CoinStoreBtn.onClick.AddListener(() => 
        {
            TempDataManagement.Instance.IsStopShot = true;
            _ = AddressableManagement.Instance.OpenCoinStoreView(closeAction: () =>
            {
                TempDataManagement.Instance.IsStopShot = false;
            });
        });

        // 減少子彈花費
        ReduceCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: true); });
        // 增加子彈花費
        AddCostBtn.onClick.AddListener(() => { TempDataManagement.Instance.ChangeCurrCost(isReduce: false); });

        // 基本技能_鎖定
        LockingTog.onValueChanged.AddListener((isOn) => { Skill_Lock(isOn); });
        // 基本技能_自動射擊
        AutoTog.onValueChanged.AddListener((isOn) => { Skill_Auto(isOn); });

        // 道具_冰凍
        Props_FreezeBtn.onClick.AddListener(PropsFreezeBtnClick);

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

    private void Initialize()
    {
        MainCanvasGroup.alpha = 0;
        MaskEnable(false);
        FreezeMask.gameObject.SetActive(false);
    }

    public void SetData(int localSeat, bool isMirror, Action closeAction)
    {
        IsLocalMirror = isMirror;
        CloseAction = closeAction;

        Initialize();

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
        CoinStoreBtn.interactable = !isShow;
    }

    /// <summary>
    /// 點擊冰凍道具
    /// </summary>
    public void PropsFreezeBtnClick()
    {
        if (FreezeMask.gameObject.activeInHierarchy)
        {
            // 冰凍道具使用中!
            AddressableManagement.Instance.ShowToast("Freezing item in use");
            return;
        }

        if (GameTerrain == null)
            GameTerrain = FindFirstObjectByType<GameTerrain>();
        if (GameTerrain != null)
            GameTerrain.DoFreeze();
    }

    /// <summary>
    /// 顯示冰凍效果
    /// </summary>
    public void ShowFreezeEffect()
    {        
        // 顯示冰凍效果
        if (FreezeCoroutine != null)
            StopCoroutine(FreezeCoroutine);

        FreezeCoroutine =StartCoroutine(IShowFreezeEffect());
    }

    private IEnumerator IShowFreezeEffect()
    {
        FreezeMask.gameObject.SetActive(true);

        float duration = 0.5f;
        float currentTime = 0f;
        Color startColor = FreezeMask.color;

        // 淡入
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, currentTime / duration);
            FreezeMask.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        FreezeMask.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        yield return new WaitForSeconds(LocalData.FreezeTime - duration);

        // 淡出
        currentTime = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
            FreezeMask.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        FreezeMask.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        FreezeMask.gameObject.SetActive(false);
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
