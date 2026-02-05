using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine.Localization;

public class GuideView : BasicView
{
    [Header("Switch")]
    [SerializeField] Button LeftBtn;
    [SerializeField] Button RightBtn;
    [SerializeField] RectTransform ViewRect;
    [SerializeField] HorizontalLayoutGroup MoveLayout;
    [SerializeField] RectTransform MoveRect;
    [SerializeField] ScrollRect MoveScrollRect;

    [Header("Guide Normal Unit")]
    [SerializeField] GuideNormalUnit GuideNormalUnit;
    [SerializeField] RectTransform NormalContentRect;

    [Header("Guide Special Unit")]
    [SerializeField] GuideSpecialUnit GuideSpecialUnit;

    Dictionary<NetworkPrefabEnum, FishData> FishDataDic = new();
    List<GuideNormalUnit> NormalUnitDatas = new();
    List<GuideSpecialUnit> TurnoverUnitDatas = new();
    GuideSpecialUnit SpecialData = null;
    GameTerrain GameTerrain;

    int CurrPage = 0;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        MoveRect.DOKill();
        LeftBtn.GetComponent<RectTransform>().DOKill();
        RightBtn.GetComponent<RectTransform>().DOKill();

        GameTerrain.TotalTurnoverChangeDelegate -= UpdateTurnoverProgress;
    }

    protected override void Start()
    {
        base.Start();

        LeftBtn.onClick.AddListener(() => { SwitchPanel(false); });
        RightBtn.onClick.AddListener(() => { SwitchPanel(true); });

        if (GameTerrain == null)
            GameTerrain = UnityEngine.Object.FindFirstObjectByType<GameTerrain>();
        if (GameTerrain != null)
        {
            GameTerrain.TotalTurnoverChangeDelegate += UpdateTurnoverProgress;
        }
    }

    private void Initialize()
    {
        MoveRect.anchoredPosition = Vector2.zero;
        LeftBtn.gameObject.SetActive(false);

        MoveScrollRect.enabled = true;

        LeftBtn.GetComponent<RectTransform>().DOAnchorPosX(5, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        RightBtn.GetComponent<RectTransform>().DOAnchorPosX(-5, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        Initialize();
        GetFishData();
    }

    /// <summary>
    /// 切換面板
    /// </summary>
    private void SwitchPanel(bool isToRight)
    {
        CurrPage = isToRight ? CurrPage += 1 : CurrPage -= 1;

        if(CurrPage <= 0)
            CurrPage = 0;

        // 最大面板數(一般魚 + 流水魚 + 關卡特殊魚)
        int maxPage = 1 + TurnoverUnitDatas.Count + 1;
        if (CurrPage >= maxPage - 1)
            CurrPage = maxPage - 1;

        LeftBtn.gameObject.SetActive(CurrPage > 0);
        RightBtn.gameObject.SetActive(CurrPage < maxPage - 1);

        float targetPos = -(ViewRect.rect.size.x + MoveLayout.spacing) * CurrPage;

        MoveRect.DOKill();
        MoveRect.DOAnchorPos(new Vector2(targetPos, 0), 0.5f).SetEase(Ease.OutQuad)
            .OnComplete(() => 
            {
                MoveScrollRect.enabled = !isToRight;
            });
    }

    /// <summary>
    /// 獲取魚群資料
    /// </summary>
    private void GetFishData()
    {
        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.FishData,
                callback: GetFishDataCallback);
        }
    }

    /// <summary>
    /// 獲取魚群資料Callback
    /// </summary>
    private void GetFishDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                FishDataDic.Clear();
                List<FishData> fishList = JsonConvert.DeserializeObject<List<FishData>>(response.JsonData);

                foreach (var data in fishList)
                {
                    FishDataDic.Add(data.FishType, data);
                }

                ReciveAllDataComplete();
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取獲取魚群資料錯誤: {e}");
            }
        }
    }

    /// <summary>
    /// 接收資料完成
    /// </summary>
    private void ReciveAllDataComplete()
    {
        CreateGuideContent();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建分配表內容
    /// </summary>
    private void CreateGuideContent()
    {
        for (int i = 0; i < NormalUnitDatas.Count; i++)
        {
            Destroy(NormalUnitDatas[i].gameObject);
        }
        NormalUnitDatas.Clear();

        for (int i = 0; i < TurnoverUnitDatas.Count; i++)
        {
            Destroy(TurnoverUnitDatas[i].gameObject);
        }
        TurnoverUnitDatas.Clear();

        if(SpecialData != null && SpecialData.gameObject != null)
        {
            Destroy(SpecialData.gameObject);
        }

        // 排序
        var sortedByMag = FishDataDic.OrderBy(x => x.Value.Magnification).ToList();

        GuideNormalUnit.gameObject.SetActive(false);
        GuideSpecialUnit.gameObject.SetActive(false);
        foreach (var fish in sortedByMag)
        {
            // 一般魚
            if (fish.Key.ToString().StartsWith("NormalFish"))
            {
                GameObject obj = Instantiate(GuideNormalUnit.gameObject, NormalContentRect);
                obj.SetActive(true);
                GuideNormalUnit guideNormalUnit = obj.GetComponent<GuideNormalUnit>();
                if (guideNormalUnit != null)
                {
                    Sprite sprite = TextureManagement.Instance.GetFishTexture(fish.Key);
                    double magnification = fish.Value.Magnification;

                    guideNormalUnit.SetData(sprite: sprite, magnification: magnification);
                    NormalUnitDatas.Add(guideNormalUnit);
                }
            }
            else if(fish.Key.ToString().StartsWith("TurnoverFish"))
            {
                // 流水魚
                GameObject obj = Instantiate(GuideSpecialUnit.gameObject, MoveRect);
                obj.SetActive(true);
                GuideSpecialUnit turnoverUnit = obj.GetComponent<GuideSpecialUnit>();
                if(turnoverUnit != null)
                {
                    turnoverUnit.SetData(fishType: fish.Key, fishData: fish.Value);
                    TurnoverUnitDatas.Add(turnoverUnit);
                }
            }
        }

        // 關卡特殊魚
        if(FirestoreDataManagement.Instance != null && FirestoreDataManagement.Instance.GameTempData != null)
        {
            GameObject obj = Instantiate(GuideSpecialUnit.gameObject, MoveRect);
            obj.SetActive(true);
            GuideSpecialUnit specialUnit = obj.GetComponent<GuideSpecialUnit>();
            FishData fishData = null;
            NetworkPrefabEnum fushType = NetworkPrefabEnum.None;
            if (specialUnit != null)
            {
                switch (FirestoreDataManagement.Instance.GameTempData.CurrentLevelData.LevelType)
                {
                    // 經典關卡
                    case LevelEnum.ClassicLevel:
                        fushType = NetworkPrefabEnum.StingrayFish;
                        fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fushType);
                        specialUnit.SetData(fishType: fushType, fishData: fishData);
                        break;

                    // 鯊魚關卡
                    case LevelEnum.SharkLevel:
                        fushType = NetworkPrefabEnum.SharkFish;
                        fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fushType);
                        specialUnit.SetData(fishType: fushType, fishData: fishData);
                        break;

                    // 金龍關卡
                    case LevelEnum.DragonLevel:
                        fushType = NetworkPrefabEnum.DragonFish;
                        fishData = FirestoreDataManagement.Instance.GameTempData.GetFishData(fushType);
                        specialUnit.SetData(fishType: fushType, fishData: fishData);
                        break;
                }

                SpecialData = specialUnit;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(MoveRect);
    }

    /// <summary>
    /// 更新流水進度
    /// </summary>
    private void UpdateTurnoverProgress()
    {
        foreach (var turnover in TurnoverUnitDatas)
        {
            turnover.UpdateTurnoverProgress();
        }
    }
}
