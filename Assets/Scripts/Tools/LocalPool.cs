using UnityEngine;
using System.Collections.Generic;
using System;

public class LocalPool : MonoBehaviour
{
    Dictionary<GamePrefabEnum, List<GameObject>> LocalPoolDic = new();
    // 追蹤哪些 Prefab 正在載入中，避免重複觸發邏輯衝突
    HashSet<GamePrefabEnum> loadingKeys = new();

    /// <summary>
    /// 獲取物件
    /// </summary>
    public void AcquirePrefabInstance<T>(GamePrefabEnum prefabType, Vector3 pos, Transform parent, Action<T> callback)
    {
        // 尋找現有的閒置物件
        if (LocalPoolDic.TryGetValue(prefabType, out var list))
        {
            GameObject takeObj = list.Find(obj => obj != null && !obj.activeSelf);
            if (takeObj != null)
            {
                ApplyObject(takeObj, pos, parent, callback);
                return;
            }
        }

        // 如果沒有閒置物件，直接創建新的
        CreateNew(prefabType, pos, parent, callback);
    }

    /// <summary>
    /// 物件設置
    /// </summary>
    private void ApplyObject<T>(GameObject obj, Vector3 pos, Transform parent, Action<T> callback)
    {
        if (obj.TryGetComponent<T>(out T t))
        {
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.SetActive(true);
            callback?.Invoke(t);
        }
        else
        {
            Debug.LogError($"組件類型不符: {typeof(T)}");
        }
    }

    /// <summary>
    /// 創建新物件
    /// </summary>
    private async void CreateNew<T>(GamePrefabEnum prefabType, Vector3 pos, Transform parent, Action<T> callback)
    {
        // 確保 Dictionary 裡有該 List，避免非同步競爭
        if (!LocalPoolDic.ContainsKey(prefabType))
        {
            LocalPoolDic[prefabType] = new List<GameObject>();
        }

        await AddressableManagement.Instance.CreateGamePrefab(
            prefabType: prefabType,
            parent: parent,
            callback: (obj) =>
            {
                if (obj != null)
                {
                    LocalPoolDic[prefabType].Add(obj);
                    ApplyObject(obj, pos, parent, callback);
                }
            });
    }
}
