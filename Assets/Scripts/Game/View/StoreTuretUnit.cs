using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StoreTuretUnit : MonoBehaviour
{
    [SerializeField] Toggle MainTog;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;
    [SerializeField] Image TurretImage;

    AccountData AccountData;
    TurretData TurretData;

    private void Start()
    {
        BuyBtn.onClick.AddListener(BuyBtnClick);
    }

    public void SetData(TurretEnum turretType, int turretIndex, Sprite turretSprite)
    {
        TurretData = TempDataManagement.Instance.GetTurrethData(turretType);
        AccountData = TempDataManagement.Instance.TempAccountData;

        if(TurretData == null || AccountData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        TurretImage.sprite = turretSprite;
        TurretImage.SetNativeSize();
        TurretImage.rectTransform.anchoredPosition = Vector2.zero;

        BuyBtnText.text = StringUtility.CurrencyFormat(TurretData.Price);
    }

    /// <summary>
    /// 點擊購買按鈕
    /// </summary>
    private void BuyBtnClick()
    {
        if (TurretData == null || AccountData == null)
        {
            Debug.LogError($"砲台商店單位獲取資料錯誤!");
            return;
        }

        // 判斷帳戶金錢
        if (AccountData.Coins - TurretData.Price < 0)
        {
            // 金幣不足!
            AddressableManagement.Instance.ShowToast("Insufficient Coin");
            return;
        }

        MainTog.isOn = true;

        TempDataManagement.Instance.ChangeTempAccountCoin(changeValue: -TurretData.Price);
        TempDataManagement.Instance.SendUpdateAccountCoinData();
        TempDataManagement.Instance.SendUpdateAccountDefaultTurretData(TurretData.TurretType);
    }
}
