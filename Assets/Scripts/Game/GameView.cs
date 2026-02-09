using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using MPUIKIT;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class GameView : BasicView
{
    [Header("SeatArea")]
    [SerializeField] RectTransform SeatArea;
    [SerializeField] Button TurretBtn;
    [SerializeField] Button ReduceCostBtn;
    [SerializeField] Button AddCostBtn;

    [Header("SeatArea")]
    [SerializeField] List<TextMeshProUGUI> PlayerCostTexts = new();
    [SerializeField] List<GameObject> PlayerCostPanels = new();
    [SerializeField] List<ImageGradients> PlayerCostBgImageGradients = new();
    [SerializeField] List<MPImage> PlayerCostFrameImageGradients = new();
    [SerializeField] AvatatUnit AvatatUnit;
    [SerializeField] GameObject FreeBulletBlock;
    [SerializeField] TextMeshProUGUI FreeBulletText;

    [Header("AccountInfoArea")]
    [SerializeField] TextMeshProUGUI AccountText;
    [SerializeField] Button CoinStoreBtn;
    [SerializeField] TextMeshProUGUI AccountCoinText;

    [Header("Base Skill")]
    [SerializeField] Toggle LockingTog;
    [SerializeField] GameObject LockingSelectFrame;
    [SerializeField] Toggle AutoTog;
    [SerializeField] GameObject AutoSelectFrame;

    [Header("Energy Skill 0")]
    [SerializeField] Button Skill_0Btn;
    [SerializeField] Image Skill_0Mask;
    [SerializeField] TextMeshProUGUI Skill_0Progress;
    [SerializeField] RectTransform Skill_0DescribeArea;
    [SerializeField] TextMeshProUGUI Skill_0DescribeText;
    [SerializeField] EventSystemsHandler Skill_0EventHandler;

    [Header("Energy Skill 1")]
    [SerializeField] Button Skill_1Btn;
    [SerializeField] Image Skill_1Mask;
    [SerializeField] TextMeshProUGUI Skill_1Progress;
    [SerializeField] RectTransform Skill_1DescribeArea;
    [SerializeField] TextMeshProUGUI Skill_1DescribeText;
    [SerializeField] EventSystemsHandler Skill_1EventHandler;

    [Header("Mask")]
    [SerializeField] GameObject MaskObj;
    [SerializeField] Image FreezeMask;

    [Header("Props Btns")]
    [SerializeField] Button PropsBtnUnit;
    [SerializeField] RectTransform PropsContent;

    [Header("CurrLevelInfoArea")]
    [SerializeField] Image CurrLevelIconImage;
    [SerializeField] TextMeshProUGUI CurrLevelNameText;

    GameFloatBtn GameFloatBtn;
    bool IsLocalMirror;
    Coroutine FreezeCoroutine;
    GameTerrain GameTerrain;
    SpecialEffectController SpecialEffectController;
    int TempFreeBullet = 0;
    bool IsEnergySkillCd;

    readonly Vector2 LeftSeatPosision = new(-600, -500);
    readonly Vector2 RightSeatPosision = new(600, -500);

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FirestoreDataManagement.Instance.GameTempData.TempAccountCoinChangeDelegate -= TempAccountCoinChange;
            FirestoreDataManagement.Instance.GameTempData.IsSkill_AutoCloseDelegate -= Skill_AutoClose;
            FirestoreDataManagement.Instance.GameTempData.TempAccountFreeBulletChangeDelegate -= TempAccountFreeBulletDataChange;
        }

        Skill_0EventHandler.PointerEnterHandleDelegate -= ShowSkill_0Discrible;
        Skill_1EventHandler.PointerEnterHandleDelegate -= ShowSkill_1Discrible;
    }

    protected override void Start()
    {
        base.Start();

        // 更換砲台
        TurretBtn.onClick.AddListener(() => 
        {
            if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
                return;

            FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;

            _ = AddressableManagement.Instance.OpenTurretStoreView(closeAction: () =>
            {
                FirestoreDataManagement.Instance.GameTempData.IsStopShot = false;
            });
        });

        // 金幣商店
        CoinStoreBtn.onClick.AddListener(() => 
        {
            if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
                return;

            FirestoreDataManagement.Instance.GameTempData.IsStopShot = true;

            _ = AddressableManagement.Instance.OpenCoinStoreView(closeAction: () =>
            {
                FirestoreDataManagement.Instance.GameTempData.IsStopShot = false;
            });
        });

        // 減少子彈花費
        ReduceCostBtn.onClick.AddListener(() => 
        {
            if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
                return;

            FirestoreDataManagement.Instance.GameTempData.ChangeCurrCost(isReduce: true); }
        );

        // 增加子彈花費
        AddCostBtn.onClick.AddListener(() => 
        {
            if (FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
                return;

            FirestoreDataManagement.Instance.GameTempData.ChangeCurrCost(isReduce: false); 
        });

        // 基本技能_鎖定
        LockingTog.onValueChanged.AddListener((isOn) => { Skill_Lock(isOn); });
        // 基本技能_自動射擊
        AutoTog.onValueChanged.AddListener((isOn) => { Skill_Auto(isOn); });

        // 能量技能0_流星雨
        Skill_0Btn.onClick.AddListener(CkickSkill_0);
        // 能量技能1_冰隻爆裂
        Skill_1Btn.onClick.AddListener(CkickSkill_1);

        Skill_0EventHandler.PointerEnterHandleDelegate += ShowSkill_0Discrible;
        Skill_1EventHandler.PointerEnterHandleDelegate += ShowSkill_1Discrible;

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FirestoreDataManagement.Instance.GameTempData.TempAccountCoinChangeDelegate += TempAccountCoinChange;
            FirestoreDataManagement.Instance.GameTempData.IsSkill_AutoCloseDelegate += Skill_AutoClose;
            FirestoreDataManagement.Instance.GameTempData.TempAccountFreeBulletChangeDelegate += TempAccountFreeBulletDataChange;
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
        UpdateEnergySkill(currEnergy: 0);

        Skill_0DescribeArea.gameObject.SetActive(false);
        Skill_0DescribeArea.DOKill();
        Skill_0DescribeArea.DOLocalMoveY(Skill_0DescribeArea.anchoredPosition.y + 5, 1.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(Skill_0DescribeArea.gameObject);

        Skill_1DescribeArea.gameObject.SetActive(false);
        Skill_1DescribeArea.DOKill();
        Skill_1DescribeArea.DOLocalMoveY(Skill_1DescribeArea.anchoredPosition.y + 5, 1.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(Skill_1DescribeArea.gameObject);

        string tableName = LocalizationManagement.Instance.TableName;
        LocalizedString Skill_0DescribeLocalized = new();
        Skill_0DescribeLocalized.SetReference(tableName, "Skill-0_Describle");
        Skill_0DescribeLocalized.Arguments = new object[] 
        {
            FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.DefaultCost,
            LocalData.Skill_0EffectDuration + 2,
            LocalData.Skill_0MaxHitFish 
        };
        // 以{0}分數在{1}秒內隨機嘗試捕獲最多{2}條魚1次
        Skill_0DescribeText.text = Skill_0DescribeLocalized.GetLocalizedString();

        LocalizedString Skill_1DescribeLocalized = new();
        Skill_1DescribeLocalized.SetReference(tableName, "Skill-1_Describle");
        Skill_1DescribeLocalized.Arguments = new object[]
        {
            FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.DefaultCost,
        };
        // 以{0}分數對全屏魚嘗試捕獲1次
        Skill_1DescribeText.text = Skill_1DescribeLocalized.GetLocalizedString();
    }

    public void SetData(int localSeat, bool isMirror, Action closeAction)
    {
        IsLocalMirror = isMirror;
        CloseAction = closeAction;

        Initialize();

        // 座位區域
        SeatArea.anchoredPosition =
            localSeat == 0 || localSeat  == 3?
            LeftSeatPosision :
            RightSeatPosision;

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            AccountData accountData = FirestoreDataManagement.Instance.GameTempData.TempAccountData;
            AvatatUnit.SetData(
                avatarImg: TextureManagement.Instance.GetAvatar(accountData.Avatar),
                avatarFrameImg: TextureManagement.Instance.GetAvatarFrame(accountData.AvatarFrame));
            AccountText.text = accountData.Account;
            AccountCoinText.text = StringUtility.CurrencyFormat(accountData.Coins);
            TempAccountFreeBulletDataChange(accountData.FreeBullet);
        }

        SetCurrLevelInfo();
        CreateProps();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 設置關卡訊息
    /// </summary>
    private void SetCurrLevelInfo()
    {
        LevelEnum levelType = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.LevelType;
        LevelInfoEntry levelInfo = TextureManagement.Instance.GetLevelInfo(levelType);

        CurrLevelIconImage.sprite = levelInfo.LevelIcon;
        CurrLevelNameText.text = LocalizationManagement.Instance.GetLocalizedString(levelInfo.LevelNameKey);
        CurrLevelNameText.colorGradient = levelInfo.LevelNameColors;
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

    #region 道具

    /// <summary>
    /// 創建道具列表
    /// </summary>
    private void CreateProps()
    {
        PropsBtnUnit.gameObject.SetActive(false);
        foreach (PropsEnum propsType in Enum.GetValues(typeof(PropsEnum)))
        {
            if (propsType == PropsEnum.None)
                continue;

            PropsEnum type = propsType;

            GameObject obj = Instantiate(PropsBtnUnit.gameObject, PropsContent);
            obj.SetActive(true);
            GamePropsBtnUnit gamePropsBtnUnit = obj.GetComponent<GamePropsBtnUnit>();
            if (gamePropsBtnUnit != null)
            {
                gamePropsBtnUnit.SetData(propsType: propsType, clickAction: () => { UseProps(type); });
            }
        }
    }

    /// <summary>
    /// 使用道具
    /// </summary>
    private void UseProps(PropsEnum propsType)
    {
        int newFreezeCount = 0;

        switch (propsType)
        {
            // 冰凍道具
            case PropsEnum.Freeze:
                if (FreezeMask.gameObject.activeInHierarchy)
                {
                    // 冰凍道具使用中!
                    AddressableManagement.Instance.ShowToast("Freezing item in use");
                    return;
                }

                int currFreezeCount = FirestoreDataManagement.Instance.CurrAccountData.FreezeProps;
                newFreezeCount = currFreezeCount - 1;
                if(newFreezeCount < 0)
                {
                    // 道具數量不足!
                    AddressableManagement.Instance.ShowToast("Not Props");
                    AddressableManagement.Instance.OpenPropsStoreView();
                    return;
                }

                // 顯示與發送RPC冰凍效果
                if (GameTerrain == null)
                    GameTerrain = FindFirstObjectByType<GameTerrain>();
                if (GameTerrain != null)
                    GameTerrain.AddFreezeTime();

                break;
        }

        // 更新帳戶道具數量
        var updates = new Dictionary<string, object>
        {
            { $"{propsType}Props", newFreezeCount}
        };

        if (FirestoreManagement.Instance != null && FirestoreDataManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                if (!res.IsSuccess) Debug.LogError($"更新Firestore帳戶{propsType}道具資料失敗");
            });
        }
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

    #endregion

    #region 技能

    /// <summary>
    /// 技能_鎖定
    /// </summary>
    private void Skill_Lock(bool isOn)
    {
        LockingSelectFrame.SetActive(isOn);

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FirestoreDataManagement.Instance.GameTempData.IsSkill_Locking = isOn;
        }        
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

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            FirestoreDataManagement.Instance.GameTempData.IsSkill_Auto = isOn;
        }
    }

    /// <summary>
    /// 點擊能量技能0_流星雨
    /// </summary>
    private void CkickSkill_0()
    {
        if(FirestoreDataManagement.Instance.GameTempData.CurrEnergy < LocalData.Skill_0NeedEnergy)
        {
            AddressableManagement.Instance.ShowToast(messageKey: "Insufficient energy");
            return;
        }

        // CD倒數
        EnergySkillCd();

        FirestoreDataManagement.Instance.GameTempData.CurrEnergy -= LocalData.Skill_0NeedEnergy;

        if (GameTerrain == null)
            GameTerrain = FindFirstObjectByType<GameTerrain>();

        if (SpecialEffectController == null)
            SpecialEffectController = UnityEngine.Object.FindFirstObjectByType<SpecialEffectController>();
        if (SpecialEffectController != null)
        {
            EenergSkillData data = new()
            {
                PlayerRef = GameTerrain.Runner.LocalPlayer,
                DefaultCost = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.MinCost,
                SeatIndex = FirestoreDataManagement.Instance.GameTempData.LocalSeatIndex,
            };
            SpecialEffectController.MeteorRain(data);
        }
    }

    /// <summary>
    /// 點擊能量技能1_冰之爆裂
    /// </summary>
    private void CkickSkill_1()
    {
        if (FirestoreDataManagement.Instance.GameTempData.CurrEnergy < LocalData.Skill_1NeedEnergy)
        {
            AddressableManagement.Instance.ShowToast(messageKey: "Insufficient energy");
            return;
        }

        // CD倒數
        EnergySkillCd();

        FirestoreDataManagement.Instance.GameTempData.CurrEnergy -= LocalData.Skill_1NeedEnergy;

        if (GameTerrain == null)
            GameTerrain = FindFirstObjectByType<GameTerrain>();

        if (SpecialEffectController == null)
            SpecialEffectController = UnityEngine.Object.FindFirstObjectByType<SpecialEffectController>();
        if (SpecialEffectController != null)
        {
            EenergSkillData data = new()
            {
                PlayerRef = GameTerrain.Runner.LocalPlayer,
                DefaultCost = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.MinCost,
                SeatIndex = FirestoreDataManagement.Instance.GameTempData.LocalSeatIndex,
            };
            SpecialEffectController.CrystalsCrossfade(data);
        }
    }

    /// <summary>
    /// 更新能量技能
    /// </summary>
    public void UpdateEnergySkill(int currEnergy)
    {
        if (IsEnergySkillCd)
            return;

        // 技能_0
        int skill_0NeedEnergy = LocalData.Skill_0NeedEnergy;
        float skill_0Percentage = ((float)currEnergy / skill_0NeedEnergy) * 100;
        Skill_0Progress.text = $"{skill_0Percentage.ToString("F1")}%";
        float target_0 = 1 - ((float)currEnergy / skill_0NeedEnergy);
        Skill_0Mask.DOKill();
        Skill_0Mask.DOFillAmount(target_0, 0.5f).SetEase(Ease.OutQuad).SetLink(Skill_0Mask.gameObject);

        // 技能_1
        int skill_1NeedEnergy = LocalData.Skill_1NeedEnergy;
        float skill_1Percentage = ((float)currEnergy / skill_1NeedEnergy) * 100;
        Skill_1Progress.text = $"{skill_1Percentage.ToString("F1")}%";
        float target_1 = 1 - ((float)currEnergy / skill_1NeedEnergy);
        Skill_1Mask.DOKill();
        Skill_1Mask.DOFillAmount(target_1, 0.5f).SetEase(Ease.OutQuad).SetLink(Skill_1Mask.gameObject);
    }

    /// <summary>
    /// 能量技能CD
    /// </summary>
    private void EnergySkillCd()
    {
        IsEnergySkillCd = true;
        Skill_0Mask.DOKill();
        Skill_1Mask.DOKill();

        float cdTime = LocalData.EnergySkillCd;
        float timer = cdTime;

        // 初始化狀態：遮罩填滿，按鈕關閉
        Skill_0Mask.fillAmount = 1f;
        Skill_1Mask.fillAmount = 1f;
        if (Skill_0Btn != null) Skill_0Btn.interactable = false;
        if (Skill_1Btn != null) Skill_1Btn.interactable = false;

        Sequence cdSequence = DOTween.Sequence();

        // 遮罩動畫
        cdSequence.Join(Skill_0Mask.DOFillAmount(0f, cdTime).SetEase(Ease.Linear));
        cdSequence.Join(Skill_1Mask.DOFillAmount(0f, cdTime).SetEase(Ease.Linear));

        // 文字倒數
        cdSequence.Join(DOTween.To(() => timer, x => timer = x, 0f, cdTime)
            .SetEase(Ease.Linear)
            .OnUpdate(() => 
            {
                string timeStr = $"{timer.ToString("F1")}s";
                if (Skill_0Progress != null) Skill_0Progress.text = timeStr;
                if (Skill_1Progress != null) Skill_1Progress.text = timeStr;
            }));

        // CD 完成
        cdSequence.OnComplete(() => 
        {
            IsEnergySkillCd = false;

            if (Skill_0Btn != null) Skill_0Btn.interactable = true;
            if (Skill_1Btn != null) Skill_1Btn.interactable = true;

            UpdateEnergySkill(FirestoreDataManagement.Instance.GameTempData.CurrEnergy);
        });

        cdSequence.SetLink(gameObject);
    }

    /// <summary>
    /// 顯示技能0描述
    /// </summary>
    public void ShowSkill_0Discrible(PointerEventData eventData, bool isEnter)
    {
        Skill_0DescribeArea.gameObject.SetActive(isEnter);
    }

    /// <summary>
    /// 顯示技能1描述
    /// </summary>
    public void ShowSkill_1Discrible(PointerEventData eventData, bool isEnter)
    {
        Skill_1DescribeArea.gameObject.SetActive(isEnter);
    }

    #endregion

    #region 帳戶資料變更

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
    public void PlayerCostChange(bool hasStateAuthority, int seatIndex, double cost)
    {
        int seat = seatIndex;

        // 反向
        if (IsLocalMirror)
        {
            seat = 3 - seatIndex;
        }

        if (seat >= PlayerCostTexts.Count || seat < 0)
        {
            Debug.LogError($"玩家子彈花費變更錯誤: index = {seat}");
            return;
        }

        if (cost < 0)
        {
            PlayerCostPanels[seat].SetActive(false);
            return;
        }

        if (!PlayerCostPanels[seat].activeSelf)
            PlayerCostPanels[seat].SetActive(true);

        PlayerCostTexts[seat].text = StringUtility.CurrencyFormat(cost);

        Color myColor;
        if (!hasStateAuthority)
        {
            // 背景顏色
            if (ColorUtility.TryParseHtmlString("#F8F4C1", out myColor))
                PlayerCostBgImageGradients[seat].color1 = myColor;
            if (ColorUtility.TryParseHtmlString("#07FFCB", out myColor))
                PlayerCostBgImageGradients[seat].color2 = myColor;

            // 框顏色
            if (ColorUtility.TryParseHtmlString("#6EDBC0", out myColor))
                PlayerCostFrameImageGradients[seat].color = myColor;
        }
        else
        {
            // 背景顏色
            if (ColorUtility.TryParseHtmlString("#F8F4C1", out myColor))
                PlayerCostBgImageGradients[seat].color1 = myColor;
            if (ColorUtility.TryParseHtmlString("#F8E1C1", out myColor))
                PlayerCostBgImageGradients[seat].color2 = myColor;

            // 框顏色
            if (ColorUtility.TryParseHtmlString("#FFD37E", out myColor))
                PlayerCostFrameImageGradients[seat].color = myColor;
        }
    }

    /// <summary>
    /// 帳戶免費子彈資料變更
    /// </summary>
    private void TempAccountFreeBulletDataChange(int newFreeBullet)
    {
        FreeBulletBlock.SetActive(newFreeBullet > 0);

        if(newFreeBullet > 0 && TempFreeBullet != newFreeBullet)
        {
            TempFreeBullet = newFreeBullet;

            FreeBulletText.text = $"Free : {StringUtility.CurrencyFormat(newFreeBullet)}";

            // 縮放效果
            FreeBulletText.rectTransform.DOKill();
            Sequence freeBulletSequence = DOTween.Sequence();
            freeBulletSequence
                .Append(FreeBulletText.rectTransform.DOScale(1.3f, 0.2f).SetEase(Ease.OutQuad))
                .Append(FreeBulletText.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.InQuad))
                .SetLink(FreeBulletText.gameObject);
        }
    }

    #endregion
}
