using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using System;

public class GuideSpecialUnit : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TopMessageText;
    [SerializeField] TextMeshProUGUI MessageText;
    [SerializeField] Image CoverImage;

    NetworkPrefabEnum FishType;
    FishData FishData;
    GameTerrain GameTerrain;

    public void SetData(NetworkPrefabEnum fishType, FishData fishData)
    {
        FishType = fishType;
        FishData = fishData;

        Sprite sprite = TextureManagement.Instance.GetFishTexture(fishType);
        CoverImage.sprite = sprite;

        LocalizedString MessageLocalized = new();
        string tableName = LocalizationManagement.Instance.TableName;

        string topMessage = "";
        switch (fishType)
        {
            // 魟魚
            case NetworkPrefabEnum.StingrayFish:
                MessageLocalized.SetReference(tableName, "Stingray Fish Message");
                // 隨機給予{0}-{1}倍獎勵\n最高<size=48><color=#FAFF51> {2}X </color></size>!
                MessageLocalized.Arguments = new object[] { fishData.MinMagnification, fishData.MaxMagnification, fishData.MaxMagnification };
                topMessage = $"{fishData.MinMagnification}X - {fishData.MaxMagnification}X";
                break;

            // 鯊魚
            case NetworkPrefabEnum.SharkFish:
                MessageLocalized.SetReference(tableName, "Shark Fish Message");
                // 可獲得一次轉輪遊戲，結束獲得對應的倍率\n最高<size=48><color=#FAFF51> {0}X </color></size>!
                MessageLocalized.Arguments = new object[] { fishData.MaxMagnification };
                topMessage = $"{fishData.MinMagnification}X - {fishData.MaxMagnification}X";
                break;

            // 金龍
            case NetworkPrefabEnum.DragonFish:
                MessageLocalized.SetReference(tableName, "Dragon Fish Message");
                // 最高倍率 = 金龍倍率 * 最高倍率 + 魚群(最大預設30之3倍率)
                int maxOdds = (int)((fishData.Magnification * fishData.MaxMagnification) + (30 * 3));
                // 固定獲得{0}倍獎勵，並捕獲全屏魚群，獎勵再翻倍，最高翻倍X{1}\n最高<size=48><color=#FAFF51> {2}X </color></size>!
                MessageLocalized.Arguments = new object[] { fishData.Magnification, fishData.MaxMagnification, maxOdds };
                topMessage = $"{fishData.Magnification}X - {maxOdds}X";
                break;

            // 流水魚_0
            case NetworkPrefabEnum.TurnoverFish_0:
                MessageLocalized.SetReference(tableName, "Turnover fish message");
                // 固定獲得{0}倍獎勵，並獲得免費{1}發子彈，子彈捕獲倍率增加{2}倍!
                MessageLocalized.Arguments = new object[] { fishData.Magnification, fishData.FreeBullet, LocalData.FreeBulletAddOdds };
                topMessage = UpdateTurnoverProgress();
                break;
        }

        TopMessageText.text = topMessage;
        MessageText.text = MessageLocalized.GetLocalizedString();
    }

    /// <summary>
    /// 更新流水進度
    /// </summary>
    private string UpdateTurnoverProgress()
    {
        LocalizedString topMessageLocalized = new();

        if (GameTerrain == null)
            GameTerrain = UnityEngine.Object.FindFirstObjectByType<GameTerrain>();
        if(GameTerrain != null)
        {
            LevelData levelData = FirestoreDataManagement.Instance.GameTempData.CurrentLevelData;
            // 已生成魚數量
            int createCount = GameTerrain.TurnoverFishDic[FishType];

            // 計算魚「開始時」的基礎流水
            double startValue = (FishData.NeedTurnoverOdds * levelData.MaxCost) * createCount;

            // 計算目標流水
            double targetValue = (FishData.NeedTurnoverOdds * levelData.MaxCost) * (createCount + 1);

            // 計算區間
            double currentProgress = GameTerrain.TotalTurnover - startValue;    // 當前跑了多少
            double totalDistance = targetValue - startValue;                    // 總共要跑多少

            // 計算百分比
            double percentage = 0;
            if (totalDistance > 0)
            {
                percentage = (currentProgress / totalDistance) * 100;
                percentage = Math.Clamp(percentage, 0, 100);
            }

            string tableName = LocalizationManagement.Instance.TableName;
            topMessageLocalized.SetReference(tableName, "Turnover progress");
            // 流水進度 : {0}%
            topMessageLocalized.Arguments = new object[] { percentage.ToString("F0") };
            TopMessageText.text = topMessageLocalized.GetLocalizedString();
        }
        
        return topMessageLocalized.GetLocalizedString();
    }
}
