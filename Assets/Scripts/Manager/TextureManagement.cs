using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextureManagement : SingletonMonoBehaviour<TextureManagement>
{
    #region 魚圖片

    /// <summary>
    /// 魚圖片資料
    /// </summary>
    [Serializable]
    public struct FishTextureEntry
    {
        public NetworkPrefabEnum FishType;
        public Sprite Sp;
    }

    /// <summary>
    /// 魚圖集
    /// </summary>
    [SerializeField] List<FishTextureEntry> FishTextureEntryList = new();

    /// <summary>
    /// 獲取魚圖片
    /// </summary>
    public Sprite GetFishTexture(NetworkPrefabEnum fishType)
    {
        var entry = FishTextureEntryList.Find(x => x.FishType == fishType);

        if (entry.Sp == null)
        {
            Debug.LogError($"無法在清單中找到 {fishType} 的圖片");
            return null;
        }

        return entry.Sp;
    }

    #endregion

    #region 頭像圖片

    /// <summary>
    /// 頭像圖片
    /// </summary>
    [field: SerializeField] public List<Sprite> AvatarList { get; private set; } = new();

    /// <summary>
    /// 獲取頭像圖片
    /// </summary>
    public Sprite GetAvatar(int index)
    {
        if (index <= 0) index = 0;
        else if (index >= AvatarList.Count) index = AvatarList.Count - 1;

        return AvatarList[index];
    }

    #endregion

    #region 頭像圖片

    /// <summary>
    /// 頭像框圖片
    /// </summary>
    [field: SerializeField] public List<Sprite> AvatarFrameList { get; private set; } = new();

    /// <summary>
    /// 獲取頭像圖片
    /// </summary>
    public Sprite GetAvatarFrame(int index)
    {
        if (index <= 0) index = 0;
        else if (index >= AvatarFrameList.Count) index = AvatarFrameList.Count - 1;

        return AvatarFrameList[index];
    }

    #endregion

    #region 道具圖片

    /// <summary>
    /// 道具圖片資料
    /// </summary>
    [Serializable]
    public struct PropsEntry
    {
        public PropsEnum PropsType;
        public Sprite Sp;
    }

    /// <summary>
    /// 道具圖集
    /// </summary>
    [SerializeField] List<PropsEntry> PropsTextureEntryList = new();

    /// <summary>
    /// 獲取道具圖片
    /// </summary>
    public Sprite GetPropsTexture(PropsEnum propsType)
    {
        var entry = PropsTextureEntryList.Find(x => x.PropsType == propsType);

        if (entry.Sp == null)
        {
            Debug.LogError($"無法在清單中找到 {propsType} 的圖片");
            return null;
        }

        return entry.Sp;
    }

    #endregion

    #region 金幣圖片

    /// <summary>
    /// 金幣圖片資料
    /// </summary>
    [Serializable]
    public struct CoinTextureEntry
    {
        public ShopCoinEnum ShopCoinType;
        public Sprite Sp;
    }

    /// <summary>
    /// 金幣圖集
    /// </summary>
    [SerializeField] List<CoinTextureEntry> ShopCoinTextureEntryList = new();

    /// <summary>
    /// 獲取金幣圖片
    /// </summary>
    public Sprite GetCoinTexture(ShopCoinEnum coinType)
    {
        var entry = ShopCoinTextureEntryList.Find(x => x.ShopCoinType == coinType);

        if (entry.Sp == null)
        {
            Debug.LogError($"無法在清單中找到 {coinType} 的圖片");
            return null;
        }

        return entry.Sp;
    }

    #endregion

    #region 砲台圖片

    /// <summary>
    /// 道具圖片資料
    /// </summary>
    [Serializable]
    public struct TurretEntry
    {
        public TurretEnum TurretType;
        public Sprite Sp;
    }

    /// <summary>
    /// 道具圖集
    /// </summary>
    [SerializeField] List<TurretEntry> TurretTextureEntryList = new();

    /// <summary>
    /// 獲取砲台圖片
    /// </summary>
    public Sprite GetTurretTexture(TurretEnum turretType)
    {
        var entry = TurretTextureEntryList.Find(x => x.TurretType == turretType);

        if (entry.Sp == null)
        {
            Debug.LogError($"無法在清單中找到 {turretType} 的圖片");
            return null;
        }

        return entry.Sp;
    }

    #endregion

    #region 數字圖片

    /// <summary>
    /// 數字圖片
    /// </summary>
    [field: SerializeField] public List<Sprite> NumberList { get; private set; } = new();

    /// <summary>
    /// 獲取數字圖片
    /// </summary>
    public Sprite GetNumberSprite(int index)
    {
        if (index <= 0) index = 0;
        else if (index >= NumberList.Count) index = NumberList.Count - 1;

        return NumberList[index];
    }

    #endregion

    #region 關卡訊息

    /// <summary>
    /// 關卡訊息集合
    /// </summary>
    [SerializeField] List<LevelInfoEntry> LevelInfoEntryList = new();

    /// <summary>
    /// 獲取關卡訊息
    /// </summary>
    public LevelInfoEntry GetLevelInfo(LevelEnum levelType)
    {
        var entry = LevelInfoEntryList.Find(x => x.LevelType == levelType);
        return entry;
    }

    #endregion
}

/// <summary>
/// 關卡訊息
/// </summary>
[Serializable]
public struct LevelInfoEntry
{
    /// <summary> 關卡類型 </summary>
    public LevelEnum LevelType;

    /// <summary> 關卡單位背景 </summary>
    public Sprite LevelBg;

    /// <summary> 關卡單位Icon </summary>
    public Sprite LevelIcon;

    /// <summary> 關卡名稱Key </summary>
    public string LevelNameKey;

    /// <summary> 關卡名稱顏色 </summary>
    public VertexGradient LevelNameColors;
}
