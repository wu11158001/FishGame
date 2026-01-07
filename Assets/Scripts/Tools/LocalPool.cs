using UnityEngine;
using System.Collections.Generic;
using System;

public class LocalPool : MonoBehaviour
{
    Dictionary<GamePrefabEnum, List<GameObject>> LocalPoolDic = new();

    /// <summary>
    /// 獲取物件
    /// </summary>
    public void AcquirePrefabInstance<T>(GamePrefabEnum prefabType, Vector3 pos, Transform parent, Action<T> callback)
    {
        if(LocalPoolDic.ContainsKey(prefabType))
        {
            GameObject takeObj = null;
            foreach (var obj in LocalPoolDic[prefabType])
            {
                if(!obj.activeSelf)
                {
                    takeObj = obj;
                    break;
                }
            }

            if(takeObj != null)
            {
                if (takeObj.TryGetComponent<T>(out T t))
                {
                    takeObj.transform.position = pos;
                    takeObj.transform.SetParent(parent);
                    takeObj.SetActive(true);
                    callback?.Invoke(t);
                }
                else
                {
                    Debug.LogError($"獲取本地物件池錯誤: {prefabType}");
                }
            }
            else
            {
                CreateNew(prefabType, pos, parent, callback);
            }
        }
        else
        {
            CreateNew(prefabType, pos, parent, callback);
        }
    }

    /// <summary>
    /// 創建新物件
    /// </summary>
    private async void CreateNew<T>(GamePrefabEnum prefabType, Vector3 pos, Transform parent, Action<T> callback)
    {
        await AddressableManagement.Instance.CreateGamePrefab(
                prefabType: prefabType,
                parent: parent,
                callback: (obj) =>
                {
                    if (obj.TryGetComponent<T>(out T t))
                    {
                        obj.transform.position = pos;

                        if (!LocalPoolDic.ContainsKey(prefabType))
                            LocalPoolDic[prefabType] = new List<GameObject>();

                        LocalPoolDic[prefabType].Add(obj);
                        callback?.Invoke(t);
                    }
                    else
                    {
                        Debug.LogError($"獲取本地物件池錯誤: {prefabType}");
                    }
                });
    }
}
