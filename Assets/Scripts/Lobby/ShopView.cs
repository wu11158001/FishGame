using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

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

    [Header("Shop Content")]
    [SerializeField] CoinStoreView CoinStoreView;
    [SerializeField] TurretStoreView TurretStoreView;
    [SerializeField] PropsStoreView PropsStoreView;

    [Header("Start Icon Area")]
    [SerializeField] List<Image> Stars_Ahpha = new();
    [SerializeField] List<RectTransform> Stars_Size = new();

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

        // 星星效果_圖片Alpha
        foreach (var start_Alpha in Stars_Ahpha)
        {
            float duration = UnityEngine.Random.Range(0.5f, 2f);
            start_Alpha.DOFade(0f, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(UnityEngine.Random.Range(0f, 1f))
                .SetLink(start_Alpha.gameObject);
        }

        // 星星效果_大小縮放
        foreach (var star_Size in Stars_Size)
        {
            float size = UnityEngine.Random.Range(1.2f, 1.5f);
            float duration = UnityEngine.Random.Range(0.5f, 1.5f);
            float waitTime = UnityEngine.Random.Range(3f, 5f);

            DOTween.Sequence()
                .Append(star_Size.DOScale(size, duration)) // 放大
                .Append(star_Size.DOScale(1.0f, duration)) // 縮小
                .AppendInterval(waitTime)                  // 停留
                .SetLoops(-1)                              // 無限循環
                .SetLink(star_Size.gameObject);
        }

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
