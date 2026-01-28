using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ShopView : BasicView
{
    [Header("ShopView")]
    [SerializeField] GameObject MaskBg;

    [Header("Top Area")]
    [SerializeField] TextMeshProUGUI CoinText;

    [Header("Left Area")]
    [SerializeField] Toggle CoinTog;
    [SerializeField] Toggle TurretTog;
    [SerializeField] Toggle PropsTog;

    [Header("ShopContent")]
    [SerializeField] CoinStoreView CoinStoreView;
    [SerializeField] TurretStoreView TurretStoreView;
    [SerializeField] PropsStoreView PropsStoreView;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AccountDataChangeDelegate -= AccountDataChange;
        }
    }

    protected override void Start()
    {
        base.Start();

        // 金幣商店標籤
        CoinTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) OnTagSwitch(shopType: ShopSwitchEnum.CoinTag);
        });

        // 砲台商店標籤
        TurretTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) OnTagSwitch(shopType: ShopSwitchEnum.TurretTag);
        });

        // 道具商店標籤
        PropsTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) OnTagSwitch(shopType: ShopSwitchEnum.PropsTag);
        });

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AccountDataChangeDelegate += AccountDataChange;
        }
    }

    public void SetData(ShopSwitchEnum defaultShopType, Action closeAction)
    {
        CloseAction = closeAction;

        if (FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.CurrAccountData != null)
        {
            AccountData accountData = FirestoreDataManagement.Instance.CurrAccountData;
            AccountDataChange(accountData);
        }

        CoinStoreView.SetData(null);
        TurretStoreView.SetData(null);
        PropsStoreView.SetData(null);

        CoinStoreView.CanvasGroupShow(false);
        TurretStoreView.CanvasGroupShow(false);
        PropsStoreView.CanvasGroupShow(false);

        CoinTog.isOn = defaultShopType == ShopSwitchEnum.CoinTag;
        TurretTog.isOn = defaultShopType == ShopSwitchEnum.TurretTag;
        PropsTog.isOn = defaultShopType == ShopSwitchEnum.PropsTag;

        StartCoroutine(IYieldSetData(defaultShopType));
    }

    /// <summary>
    /// 等待各商店資料完成
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldSetData(ShopSwitchEnum defaultShopType)
    {
        Canvas_Global.Instance.ShowLoading();
        MaskBg.SetActive(true);

        yield return new WaitForSeconds(0.5f);
        OnTagSwitch(shopType: defaultShopType);

        MaskBg.SetActive(false);
        Canvas_Global.Instance.CloseLoading();
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        if (accountData == null)
            return;

        CoinText.text = StringUtility.CurrencyFormat(accountData.Coins);
    }

    /// <summary>
    /// 商店標籤更換
    /// </summary>
    private void OnTagSwitch(ShopSwitchEnum shopType)
    {
        CoinStoreView.CanvasGroupShow(shopType == ShopSwitchEnum.CoinTag);
        TurretStoreView.CanvasGroupShow(shopType == ShopSwitchEnum.TurretTag);
        PropsStoreView.CanvasGroupShow(shopType == ShopSwitchEnum.PropsTag);

        CoinStoreView.gameObject.SetActive(shopType == ShopSwitchEnum.CoinTag);
        TurretStoreView.gameObject.SetActive(shopType == ShopSwitchEnum.TurretTag);
        PropsStoreView.gameObject.SetActive(shopType == ShopSwitchEnum.PropsTag);
    }
}
