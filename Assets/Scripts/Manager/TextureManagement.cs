using System;
using System.Collections.Generic;
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
}
