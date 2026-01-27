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
    [SerializeField] Button SettingBtn;

    [Header("Bottom Area")]
    [SerializeField] RectTransform BottomAreaRect;
    [SerializeField] RectTransform LevelBtnRect;
    [SerializeField] Button LevelBtn;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        LevelBtnRect.DOKill();
        BottomAreaRect.DOKill();

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AsccountDataChangeDelegate -= AccountDataChange;
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

        // 關卡按鈕上下移動
        LevelBtnRect.DOKill();
        LevelBtnRect.DOLocalMoveY(LevelBtnRect.anchoredPosition.y + 10, 1.5f)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo);

        // 關卡按鈕
        LevelBtn.onClick.AddListener(() =>
        {
            HideBottomArea();
            AddressableManagement.Instance.OpenLevelView(ShowBottomArea);
        });

        if(FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AsccountDataChangeDelegate += AccountDataChange;
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

        AvatatUnit.SetData(
               avatarImg: TextureManagement.Instance.GetAvatar(accountData.Avatar),
               avatarFrameImg: TextureManagement.Instance.GetAvatarFrame(accountData.AvatarFrame));

        CoinText.text = StringUtility.CurrencyFormat(accountData.Coins);

        NickNameText.text = accountData.Nickname;

        RectTransform rt = NicknameEditBtn.GetComponent<RectTransform>();
        StringUtility.RectFollowTextBehind(
            rt: rt, 
            tmpText: NickNameText, 
            offset: new Vector2(50, -7));
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
}
