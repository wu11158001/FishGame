using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using System.Collections.Generic;
using System;

public class TurretStoreUnit : MonoBehaviour
{
    [Header("TurretStoreUnit")]
    [SerializeField] float MaxCoverSize = 256f;
    [SerializeField] Toggle MainTog;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;
    [SerializeField] LocalizeStringEvent BuyBtnTextLocalize;
    [SerializeField] GameObject IconBlock;
    [SerializeField] Image CoverImage;

    TurretData TurretData;
    Sprite CoverSprite;
    Transform Model3D;
    Action<TurretData, RectTransform> SelectAction;
    bool IsOwn;

    private void Start()
    {
        BuyBtn.onClick.AddListener(BuyBtnClick);
        MainTog.onValueChanged.AddListener(SetModel3D);
    }

    public void SetData(TurretEnum turretType, Sprite coverSprite, Transform model3D, Action<TurretData, RectTransform> selectAction)
    {
        CoverSprite = coverSprite;
        Model3D = model3D;
        SelectAction = selectAction;
        TurretData = FirestoreDataManagement.Instance?.GetTurrethData(turretType);

        if (TurretData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 設置砲台圖片
        CoverImage.sprite = CoverSprite;
        CoverImage.SetNativeSize();
        UIUtility.SetMaxUISize(targetRt: CoverImage.rectTransform, maxSize: MaxCoverSize);
        CoverImage.rectTransform.anchoredPosition = Vector2.zero;

        CheckTurret(FirestoreDataManagement.Instance.CurrAccountData);
    }

    /// <summary>
    /// 檢測砲台狀態
    /// </summary>
    public void CheckTurret(AccountData accountData)
    {
        bool isSelect = false;

        bool isOwn = false;
        List<int> ownTurrets = accountData.GetOwnTurretList();
        for (int i = 0; i < ownTurrets.Count; i++)
        {
            if (ownTurrets[i] == (int)TurretData.TurretType)
            {
                isOwn = true;
                break;
            }
        }

        if (isOwn)
        {
            if (accountData.DefaultTurret == (int)TurretData.TurretType)
            {
                MainTog.isOn = true;
                isSelect = true;

                // 使用中
                LocalizationManagement.Instance.UpdateKey(BuyBtnTextLocalize, "In Use");
            }
            else
            {
                // 更換
                LocalizationManagement.Instance.UpdateKey(BuyBtnTextLocalize, "Change");
            }
        }
        else
        {
            BuyBtnText.text = StringUtility.CurrencyFormat(TurretData.Price);
        }

        IconBlock.SetActive(!isOwn);
        IsOwn = isOwn;
        SetModel3D(isSelect);
    }

    /// <summary>
    /// 設置3D模型
    /// </summary>
    private void SetModel3D(bool isOn)
    {
        if (Model3D != null)
            Model3D.gameObject.SetActive(isOn);

        if (isOn)
            SelectAction?.Invoke(TurretData, MainRect);
    }

    /// <summary>
    /// 點擊購買/選擇按鈕
    /// </summary>
    private void BuyBtnClick()
    {
        // 暫存購買前擁有狀態
        bool isOwn = IsOwn;

        // 判斷帳戶金錢
        if (!isOwn && FirestoreDataManagement.Instance.CurrAccountData.Coins - TurretData.Price < 0)
        {
            // 金幣不足!
            AddressableManagement.Instance.ShowToast("Insufficient Coin");
            _ = AddressableManagement.Instance.OpenCoinStoreView();            
            return;
        }

        Canvas_Global.Instance.ShowLoading();
        MainTog.isOn = true;

        // 扣除金幣
        double newCoin = FirestoreDataManagement.Instance.CurrAccountData.Coins;
        int defaultTurret = (int)TurretData.TurretType;
        SortedSet<int> owns = new(FirestoreDataManagement.Instance.CurrAccountData.GetOwnTurretList());

        // 未購買
        if (!isOwn)
            newCoin -= TurretData.Price;

        owns.Add((int)TurretData.TurretType);
        string result = string.Join(",", owns);

        var updates = new Dictionary<string, object>
        {
            { "Coins", newCoin },
            { "DefaultTurret", (int)TurretData.TurretType },
            { "OwnTurret", result }
        };

        if (FirestoreManagement.Instance != null && FirestoreDataManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                Canvas_Global.Instance.CloseLoading();

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣資料失敗");
                else
                {
                    if(!isOwn)
                    {
                        // 顯示獲得物品
                        AddressableManagement.Instance.ShowGetItemView(
                            iconSprite: CoverSprite);
                    }
                }
            });
        }
    }
}
