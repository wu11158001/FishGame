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
    [Serializable]
    public struct NetworkPrefabEntry
    {
        public NetworkPrefabEnum Key;
        public Sprite Value;
    }

    [Header("GameGuideView")]
    [SerializeField] List<NetworkPrefabEntry> FishCovers = new();

    [Header("Switch")]
    [SerializeField] Button LeftBtn;
    [SerializeField] Button RightBtn;
    [SerializeField] HorizontalLayoutGroup MoveLayout;
    [SerializeField] RectTransform MoveRect;

    [Header("NormalUnit")]
    [SerializeField] GuideUnit GuideUnit;
    [SerializeField] RectTransform NormalContentRect;

    [Header("SpecialContent")]
    [SerializeField] LocalizedString SpecialMessageLocalized;
    [SerializeField] TextMeshProUGUI SpecialMessageText;
    [SerializeField] Image SpecialCoverImage;

    Dictionary<NetworkPrefabEnum, FishData> FishDataDic = new();
    List<GuideUnit> GuideUnitDatas = new();

    private void OnDestroy()
    {
        MoveRect.DOKill();
        LeftBtn.GetComponent<RectTransform>().DOKill();
        RightBtn.GetComponent<RectTransform>().DOKill();
    }

    protected override void Start()
    {
        base.Start();

        LeftBtn.onClick.AddListener(() => { SwitchPanel(false); });
        RightBtn.onClick.AddListener(() => { SwitchPanel(true); });
    }

    private void Initialize()
    {
        MoveRect.anchoredPosition = Vector2.zero;
        LeftBtn.gameObject.SetActive(false);

        LeftBtn.GetComponent<RectTransform>().DOAnchorPosX(35, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        RightBtn.GetComponent<RectTransform>().DOAnchorPosX(-35, 1f)
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
        LeftBtn.gameObject.SetActive(isToRight);
        RightBtn.gameObject.SetActive(!isToRight);

        float targetPos =
            isToRight ?
            -(MoveRect.rect.size.x + MoveLayout .spacing):
            0;

        MoveRect.DOKill();
        MoveRect.DOAnchorPos(new Vector2(targetPos, MoveRect.anchoredPosition.y), 0.5f).SetEase(Ease.OutQuad);
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
        CreateGameNormalGuideUnit();
        SetSpecialGuideContent();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建一般分配表內容
    /// </summary>
    private void CreateGameNormalGuideUnit()
    {
        for (int i = 0; i < GuideUnitDatas.Count; i++)
        {
            Destroy(GuideUnitDatas[i].gameObject);
        }
        GuideUnitDatas.Clear();

        // 排序
        var sortedByMag = FishDataDic.OrderBy(x => x.Value.Magnification).ToList();

        GuideUnit.gameObject.SetActive(false);
        foreach (var fish in sortedByMag)
        {
            if (!fish.Key.ToString().StartsWith("NormalFish"))
                continue;

            GameObject obj = Instantiate(GuideUnit.gameObject, NormalContentRect);
            obj.SetActive(true);
            GuideUnit gameGuideUnit = obj.GetComponent<GuideUnit>();

            if (gameGuideUnit != null)
            {
                Sprite sprite = FishCovers.FirstOrDefault(x => x.Key == fish.Key).Value;
                double magnification = fish.Value.Magnification;

                gameGuideUnit.SetData(sprite: sprite, magnification: magnification);
                GuideUnitDatas.Add(gameGuideUnit);
            }
            else
            {
                Debug.LogError("創建金幣商品錯誤");
            }
        }
    }

    /// <summary>
    /// 設置特殊魚分配表內容
    /// </summary>
    private void SetSpecialGuideContent()
    {
        string tableName = LocalizationManagement.Instance.TableName;
        Sprite sprite = null;

        switch (TempDataManagement.Instance.CurrentLevelData.LevelType)
        {
            // 經典關卡
            case LevelEnum.ClassicLevel:
                SpecialMessageLocalized.SetReference(tableName, "Stingray Fish Message");

                // 隨機給予{0}-{1}倍獎勵\n最高<size=48><color=#FAFF51> {2}X </color></size>!
                SpecialMessageLocalized.Arguments = new object[] { 15, 30, 30 };

                sprite = FishCovers.FirstOrDefault(x => x.Key == NetworkPrefabEnum.StingrayFish).Value;
                break;
        }

        SpecialMessageText.text = SpecialMessageLocalized.GetLocalizedString();
        SpecialCoverImage.sprite = sprite;
    }
}
