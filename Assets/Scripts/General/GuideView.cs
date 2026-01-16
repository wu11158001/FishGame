using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Linq;

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

    [SerializeField] GuideUnit GuideUnit;
    [SerializeField] RectTransform ContentRect;

    Dictionary<NetworkPrefabEnum, FishData> FishDataDic = new();
    List<GuideUnit> GuideUnitDatas = new();

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        GetFishData();
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
        CreateGameGuideUnit();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 創建分配表內容
    /// </summary>
    private void CreateGameGuideUnit()
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

            GameObject obj = Instantiate(GuideUnit.gameObject, ContentRect);
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
}
