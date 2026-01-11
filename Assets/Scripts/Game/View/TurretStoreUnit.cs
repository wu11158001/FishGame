using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using System.Collections.Generic;
using System;

public class TurretStoreUnit : MonoBehaviour
{
    [SerializeField] Toggle MainTog;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;
    [SerializeField] LocalizeStringEvent BuyBtnTextLocalize;
    [SerializeField] GameObject IconBlock;
    [SerializeField] Image TurretImage;

    TurretStoreUnitData TurretStoreUnitData;
    bool IsOwn;

    private void Start()
    {
        BuyBtn.onClick.AddListener(BuyBtnClick);
        MainTog.onValueChanged.AddListener(SetModel3D);
    }

    public void SetData(TurretStoreUnitData data)
    {
        TurretStoreUnitData = data;

        if (TurretStoreUnitData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 設置砲台圖片
        TurretImage.sprite = data.CoverSprite;
        TurretImage.SetNativeSize();
        TurretImage.rectTransform.anchoredPosition = Vector2.zero;

        CheckTurret(data.AccountData);
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
            if (ownTurrets[i] == (int)TurretStoreUnitData.TurretData.TurretType)
            {
                isOwn = true;
                break;
            }
        }

        if (isOwn)
        {
            if (accountData.DefaultTurret == (int)TurretStoreUnitData.TurretData.TurretType)
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
            BuyBtnText.text = StringUtility.CurrencyFormat(TurretStoreUnitData.TurretData.Price);
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
        if (TurretStoreUnitData.Model3D != null)
            TurretStoreUnitData.Model3D.gameObject.SetActive(isOn);

        if (isOn)
            TurretStoreUnitData.SelectCallback?.Invoke(TurretStoreUnitData.TurretData, MainRect);
    }

    /// <summary>
    /// 點擊購買/選擇按鈕
    /// </summary>
    private void BuyBtnClick()
    {
        Canvas_Global.Instance.ShowLoading();

        if (TurretStoreUnitData.TurretData == null || TurretStoreUnitData.AccountData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 判斷帳戶金錢
        if (!IsOwn && TurretStoreUnitData.AccountData.Coins - TurretStoreUnitData.TurretData.Price < 0)
        {
            // 金幣不足!
            AddressableManagement.Instance.ShowToast("Insufficient Coin");
            return;
        }

        MainTog.isOn = true;

        // 扣除金幣
        double newCoin = TurretStoreUnitData.AccountData.Coins;
        int defaultTurret = (int)TurretStoreUnitData.TurretData.TurretType;
        SortedSet<int> owns = new(TurretStoreUnitData.AccountData.GetOwnTurretList());

        // 未購買
        if (!IsOwn)
            newCoin -= TurretStoreUnitData.TurretData.Price;

        owns.Add((int)TurretStoreUnitData.TurretData.TurretType);
        string result = string.Join(",", owns);

        var updates = new Dictionary<string, object>
        {
            { "Coins", newCoin },
            { "DefaultTurret", (int)TurretStoreUnitData.TurretData.TurretType },
            { "OwnTurret", result }
        };

        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: PlayerPrefsManagement.GetLoginInfo().Account,
            updates: updates,
            callback: (res) =>
            {
                Canvas_Global.Instance.CloseLoading();

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣資料失敗");
            });
        }
    }
}
