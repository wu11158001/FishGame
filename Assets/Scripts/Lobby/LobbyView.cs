using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using TMPro;

public class LobbyView : BasicView
{
    [Header("Top Area")]
    [SerializeField] Button AvatarBtn;
    [SerializeField] AvatatUnit AvatatUnit;
    [SerializeField] TextMeshProUGUI NickNameText;
    [SerializeField] Button NicknameEditBtn;
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Button CoinStoreBtn;
    [SerializeField] Button SettingBtn;

    [Header("Bottom Area")]
    [SerializeField] RectTransform BottomAreaRect;
    [SerializeField] Button ShopBtn;

    [Header("Left Area")]
    [SerializeField] Button SevenDayBtn;
    [SerializeField] RectTransform SevenDayRect;

    [Header("Level")]
    [SerializeField] RectTransform LevelBtnRect;
    [SerializeField] Button LevelBtn;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        LevelBtnRect.DOKill();
        BottomAreaRect.DOKill();
        SevenDayRect.DOKill();

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AccountDataChangeDelegate -= AccountDataChange;
        }
    }

    protected override void Start()
    {
        base.Start();

        // 頭像按鈕
        AvatarBtn.onClick.AddListener(() => { AddressableManagement.Instance.OpenEditAvatarView(); });

        // 編輯暱稱按鈕
        NicknameEditBtn.onClick.AddListener(() => { AddressableManagement.Instance.OpenEditNicknameView(); });

        // 設置按鈕
        SettingBtn.onClick.AddListener(() => { AddressableManagement.Instance.OpenSettingView(); });

        CoinStoreBtn.onClick.AddListener(() => { AddressableManagement.Instance.OpenShopView(defaultShopType: ShopSwitchEnum.CoinTag); });

        // 商店按鈕
        ShopBtn.onClick.AddListener(() => { AddressableManagement.Instance.OpenShopView(); });

        // 關卡按鈕
        LevelBtn.onClick.AddListener(() =>
        {
            HideBottomArea();
            AddressableManagement.Instance.OpenLevelView(ShowBottomArea);
        });

        // 7日簽到按鈕
        SevenDayBtn.onClick.AddListener(() =>
        {
            AddressableManagement.Instance.OpenSevenDayView();
        });

        // 7日按鈕上下移動
        SevenDayRect.DOKill();
        SevenDayRect.DOLocalMoveY(SevenDayRect.anchoredPosition.y + 10, 3.1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 關卡按鈕上下移動
        LevelBtnRect.DOKill();
        LevelBtnRect.DOLocalMoveY(LevelBtnRect.anchoredPosition.y + 10, 1.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AccountDataChangeDelegate += AccountDataChange;
        }
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        if(FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.CurrAccountData!= null)
        {
            AccountData accountData = FirestoreDataManagement.Instance.CurrAccountData;
            AccountDataChange(accountData);
        }  
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        if (accountData == null)
            return;

        // 頭像
        AvatatUnit.SetData(
               avatarImg: TextureManagement.Instance.GetAvatar(accountData.Avatar),
               avatarFrameImg: TextureManagement.Instance.GetAvatarFrame(accountData.AvatarFrame));

        // 金幣
        CoinText.text = StringUtility.CurrencyFormat(accountData.Coins);

        // 暱稱
        NickNameText.text = accountData.Nickname;
        RectTransform rt = NicknameEditBtn.GetComponent<RectTransform>();
        StringUtility.RectFollowTextBehind(
            rt: rt, 
            tmpText: NickNameText, 
            offset: new Vector2(50, -7));

        // 判斷7日簽到按鈕是否顯示
        CheckSevenDay();
    }

    /// <summary>
    /// 影藏底部區域
    /// </summary>
    private void HideBottomArea()
    {
        BottomAreaRect.DOKill();
        BottomAreaRect.anchoredPosition = new(0, 0);
        BottomAreaRect.DOAnchorPos(new Vector2(BottomAreaRect.anchoredPosition.x, -BottomAreaRect.sizeDelta.y), PopUpTime)
            .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 顯示底部區域
    /// </summary>
    private void ShowBottomArea()
    {
        BottomAreaRect.DOKill();
        BottomAreaRect.anchoredPosition = new(0, -BottomAreaRect.sizeDelta.y);
        BottomAreaRect.DOAnchorPos(new Vector2(0, 0), PopUpTime)
            .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 判斷7日簽到按鈕是否顯示
    /// </summary>
    private void CheckSevenDay()
    {
        // 獲取當前時間與註冊時間相差天數
        string registerDay = FirestoreDataManagement.Instance.CurrAccountData.RegisterTime;
        DateTime registerDate = DateTime.Parse(registerDay);
        DateTime now = DateTime.UtcNow.AddHours(8);
        TimeSpan diff = now.Date - registerDate.Date;
        int registerDays = diff.Days;

        SevenDayBtn.gameObject.SetActive(registerDays < 7);
    }
}
