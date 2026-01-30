using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.Localization;

public class SevenDayView : BasicView
{
    [Header("SevenDayView")]
    [SerializeField] SevenDayUnit SevenDayUnit;
    [SerializeField] RectTransform ContentRect;
    [SerializeField] TextMeshProUGUI NextTimeText;

    List<GameObject> UnitObjs = new();

    // 紀錄介面開啟時間，判斷介面開著時已過隔日
    DateTime ViewStartTime;

    LobbyView LobbyView;

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

        if (FirestoreDataManagement.Instance != null)
        {
            FirestoreDataManagement.Instance.AccountDataChangeDelegate += AccountDataChange;
        }

        ViewStartTime = DateTime.UtcNow.AddHours(8);

        // 7天內未簽到顯示下次簽到時間倒數
        if(FirestoreDataManagement.Instance.GetDifferenceDays() < 7)
        {
            StartCoroutine(UpdateNextTimer());
        }
        else
        {
            NextTimeText.gameObject.SetActive(false);
        }
    }

    protected override void Close()
    {
        if (LobbyView != null)
            LobbyView.EffectObjectShowControl(true);

        base.Close();
    }

    public void SetData(LobbyView lobbyView, Action closeAction)
    {
        CloseAction = closeAction;
        LobbyView = lobbyView;

        MainCanvasGroup.alpha = 0;
        lobbyView.EffectObjectShowControl(false);

        CreateSignInDayUnit();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 更新下次領獎時間
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateNextTimer()
    {
        LocalizedString nextTimeLocalized = new();
        string tableName = LocalizationManagement.Instance.TableName;
        nextTimeLocalized.SetReference(tableName, "Update time");

        while (true)
        {
            DateTime nowTaiwan = DateTime.UtcNow.AddHours(8);
            DateTime tomorrowMidnight = nowTaiwan.Date.AddDays(1);
            TimeSpan remaining = tomorrowMidnight - nowTaiwan;
            string timeStr = "";

            if (remaining.TotalSeconds <= 0)
            {
                timeStr = "00:00:00";
                yield break;
            }

            timeStr = string.Format("{0:00}:{1:00}:{2:00}",
                (int)remaining.TotalHours,
                remaining.Minutes,
                remaining.Seconds);

            nextTimeLocalized.Arguments = new object[] { timeStr };

            NextTimeText.text = nextTimeLocalized.GetLocalizedString();

            yield return new WaitForSeconds(1.0f);
        }
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        DateTime now = DateTime.UtcNow.AddHours(8);
        if (now.Date > ViewStartTime.Date)
        {
            // 介面開啟狀態已過隔日
            CreateSignInDayUnit();
        }
    }

    /// <summary>
    /// 創建簽到單位
    /// </summary>
    private void CreateSignInDayUnit()
    {
        int registerDays = FirestoreDataManagement.Instance.GetDifferenceDays();

        if (registerDays > 7)
        {
            Close();
            return;
        }

        // 獲取已簽到天
        SortedSet<int> sigInDays = FirestoreDataManagement.Instance.GetSignInDays();

        // 移除舊的物件
        for (int i = 0; i < UnitObjs.Count; i++)
        {
            Destroy(UnitObjs[i]);
        }
        UnitObjs.Clear();

        // 創建新物件
        SevenDayUnit.gameObject.SetActive(false);
        for (int i = 0; i < 7; i++)
        {
            int index = i;

            GameObject obj = Instantiate(SevenDayUnit.gameObject, ContentRect);
            obj.SetActive(true);
            SevenDayUnit sevenDayUnit = obj.GetComponent<SevenDayUnit>();
            UnitObjs.Add(obj);

            SevenDayUnitData sevenDayUnitData = new();
            // 是否是簽到日
            sevenDayUnitData.IsSignInDay = registerDays == index;
            // 是否過期
            sevenDayUnitData.IsExpired = (index < registerDays) && !sigInDays.Contains(index);
            // 是否已領取
            sevenDayUnitData.IsReceived = sigInDays.Contains(index);
            // 數字圖片
            sevenDayUnitData.NumberSprite = TextureManagement.Instance.GetNumberSprite(index + 1);

            // 簽到獎勵設置
            switch (index)
            {
                case 0:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_0;
                    break;

                case 1:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_1;
                    break;

                case 2:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_2;
                    break;

                case 3:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_3;
                    break;

                case 4:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_4;
                    break;

                case 5:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_5;
                    break;

                case 6:
                    sevenDayUnitData.RewardValue = FirestoreDataManagement.Instance.SevenDayData.Day_6;
                    break;
            }

            // 圖片依照金額變化
            Sprite rewaedSprite = TextureManagement.Instance.GetCoinTexture(ShopCoinEnum.StoreCoin_0);
            if (sevenDayUnitData.RewardValue >= 100000)
                rewaedSprite = TextureManagement.Instance.GetCoinTexture(ShopCoinEnum.StoreCoin_4);
            else if(sevenDayUnitData.RewardValue >= 70000)
                rewaedSprite = TextureManagement.Instance.GetCoinTexture(ShopCoinEnum.StoreCoin_3);
            else if (sevenDayUnitData.RewardValue >= 50000)
                rewaedSprite = TextureManagement.Instance.GetCoinTexture(ShopCoinEnum.StoreCoin_2);
            else if (sevenDayUnitData.RewardValue >= 20000)
                rewaedSprite = TextureManagement.Instance.GetCoinTexture(ShopCoinEnum.StoreCoin_1);

            sevenDayUnitData.RewardSprite = rewaedSprite;

            // 簽到更新帳戶金幣
            sevenDayUnitData.SignInAction = () =>
            {
                // 已領取 || 已過期 || 非簽到日
                if (sevenDayUnitData.IsReceived || sevenDayUnitData.IsExpired || !sevenDayUnitData.IsSignInDay)
                    return;

                // 更新金幣
                double newCoin = FirestoreDataManagement.Instance.CurrAccountData.Coins + sevenDayUnitData.RewardValue;

                // 更新已簽到7日天
                string signInDaysStr = FirestoreDataManagement.Instance.CurrAccountData.SevenDays;
                List<int> dayList = new();
                SortedSet<int> signInDas = new();
                if (!string.IsNullOrEmpty(signInDaysStr))
                {
                    var parts = signInDaysStr.Trim().Split(',');
                    foreach (var p in parts)
                    {
                        if (int.TryParse(p, out int id)) dayList.Add(id);
                    }
                }
                signInDas = new(dayList);
                signInDas.Add(index);
                string newSevenDays = string.Join(",", signInDas);

                // 帳戶金幣與簽到日資料
                var updates = new Dictionary<string, object>
                {
                    { "Coins", newCoin },
                    { "SevenDays", newSevenDays}
                };

                if (FirestoreManagement.Instance != null && FirestoreDataManagement.Instance != null)
                {
                    Canvas_Global.Instance.ShowLoading();

                    FirestoreManagement.Instance.UpdateDataToFirestore(
                    path: FirestoreCollectionNameEnum.AccountData,
                    docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
                    updates: updates,
                    callback: (res) =>
                    {
                        Canvas_Global.Instance.CloseLoading();

                        if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣與簽到日資料失敗");
                        else
                        {
                            // 顯示獲得物品
                            AddressableManagement.Instance.ShowGetItemView(
                                iconSprite: sevenDayUnitData.RewardSprite,
                                value: sevenDayUnitData.RewardValue);

                            Close();
                        }
                    });
                }
            };

            // 簽到單位設置
            if (sevenDayUnit != null)
            {
                sevenDayUnit.SetData(sevenDayUnitData);
            }
        }
    }
}

/// <summary>
/// 簽到單位資料
/// </summary>
public struct SevenDayUnitData
{
    /// <summary> 數字圖片 </summary>
    public Sprite NumberSprite;

    /// <summary> 獎勵圖片 </summary>
    public Sprite RewardSprite;

    /// <summary> 獎勵值 </summary>
    public double RewardValue;

    /// <summary> 是否是簽到日 </summary>
    public bool IsSignInDay;

    /// <summary> 是否過期 </summary>
    public bool IsExpired;

    /// <summary> 是否已領取 </summary>
    public bool IsReceived;

    /// <summary> 簽到Action </summary>
    public Action SignInAction;
}
