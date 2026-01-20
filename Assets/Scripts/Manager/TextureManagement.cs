using System;
using System.Collections.Generic;
using UnityEngine;

public class TextureManagement : SingletonMonoBehaviour<TextureManagement>
{
    [Serializable]
    public struct FishTextureEntry
    {
        public NetworkPrefabEnum FishType;
        public Sprite Sp;
    }

    /// <summary>
    /// 魚圖集
    /// </summary>
    [SerializeField]
    private List<FishTextureEntry> FishTextureEntryList = new();

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
}
