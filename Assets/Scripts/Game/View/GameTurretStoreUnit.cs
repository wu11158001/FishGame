using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using System.Collections.Generic;
using System;

public class GameTurretStoreUnit : MonoBehaviour
{
    [SerializeField] Toggle MainTog;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;
    [SerializeField] LocalizeStringEvent BuyBtnTextLocalize;
    [SerializeField] GameObject IconBlock;
    [SerializeField] Image TurretImage;

    AccountData AccountData;
    TurretData TurretData;

    Transform TargetModel3D;
    int TurretIndex;
    bool IsOwn;

    Action<TurretData, RectTransform> SelectAction;

    private void OnDestroy()
    {
        if (TempDataManagement.Instance != null)
            TempDataManagement.Instance.TempAccountOwnTurretChangeDelegate -= CheckTurret;
    }

    private void Start()
    {
        BuyBtn.onClick.AddListener(BuyBtnClick);
        MainTog.onValueChanged.AddListener(SetModel3D);

        TempDataManagement.Instance.TempAccountOwnTurretChangeDelegate += CheckTurret;
    }

    public void SetData(TurretEnum turretType, int turretIndex, Sprite turretSprite, Transform targetModel3D, Action<TurretData, RectTransform> selectAction)
    {
        TurretData = TempDataManagement.Instance.GetTurrethData(turretType);
        AccountData = TempDataManagement.Instance.TempAccountData;
        TurretIndex = turretIndex;
        TargetModel3D = targetModel3D;
        SelectAction = selectAction;

        if (TurretData == null || AccountData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 設置砲台圖片
        TurretImage.sprite = turretSprite;
        TurretImage.SetNativeSize();
        TurretImage.rectTransform.anchoredPosition = Vector2.zero;

        CheckTurret();
    }

    /// <summary>
    /// 檢測砲台擁有狀態
    /// </summary>
    private void CheckTurret()
    {
        bool isSelect = false;

        bool isOwn = false;
        List<int> ownTurrets = AccountData.GetOwnTurretList();
        for (int i = 0; i < ownTurrets.Count; i++)
        {
            if (ownTurrets[i] == TurretIndex)
            {
                isOwn = true;
                break;
            }
        }

        if (isOwn)
        {
            if (AccountData.DefaultTurret == TurretIndex)
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
        if (TargetModel3D != null)
            TargetModel3D.gameObject.SetActive(isOn);

        if (isOn)
            SelectAction?.Invoke(TurretData, MainRect);
    }

    /// <summary>
    /// 點擊購買/選擇按鈕
    /// </summary>
    private void BuyBtnClick()
    {
        if (TurretData == null || AccountData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 判斷帳戶金錢
        if (!IsOwn && AccountData.Coins - TurretData.Price < 0)
        {
            // 金幣不足!
            AddressableManagement.Instance.ShowToast("Insufficient Coin");
            return;
        }

        MainTog.isOn = true;

        // 購買
        if(!IsOwn)
        {
            TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -TurretData.Price);
            TempDataManagement.Instance.SendUpdateAccountCoinData();
        }
        
        TempDataManagement.Instance.SendUpdateAccountDefaultTurretData(TurretData.TurretType);
        TempDataManagement.Instance.SendUpdateAccountOwnTurretData(TurretData.TurretType);
    }
}
