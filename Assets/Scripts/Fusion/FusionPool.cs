using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FusionPool : NetworkObjectProviderDefault
{
    private Dictionary<NetworkPrefabId, Stack<NetworkObject>> Pool = new();

    private void OnDestroy()
    {
        ClearPool();
    }

    /// <summary>
    /// 獲取物件
    /// </summary>
    public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject instance)
    {
        instance = null;

        // 檢查場景管理器是否忙碌
        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy)
        {
            return NetworkObjectAcquireResult.Retry;
        }

        // 從池中嘗試取得物件
        if (Pool.TryGetValue(context.PrefabId, out var stack) && stack.Count > 0)
        {
            instance = stack.Pop();
            instance.gameObject.SetActive(true);
        }
        else
        {
            // 池中沒有物件，則載入 Prefab 並生成
            NetworkObject prefab;
            try
            {
                // 使用 context 提供的 PrefabId
                prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
            }
            catch (Exception ex)
            {
                Log.Error($"物件池載入失敗: {ex}");
                return NetworkObjectAcquireResult.Failed;
            }

            if (!prefab)
            {
                return NetworkObjectAcquireResult.Retry;
            }

            instance = Instantiate(prefab);
        }

        // 設定物件的場景歸屬 (DontDestroyOnLoad 或 Runner Scene)
        if (context.DontDestroyOnLoad)
        {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        }
        else
        {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        // 必須告知 Fusion 內部 Prefab 管理器新增了一個實例
        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    /// <summary>
    /// 回收物件
    /// </summary>
    public override void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // 如果物件正在被銷毀（例如 Runner 正在關閉），則不進池
        if (context.IsBeingDestroyed)
        {
            if (context.TypeId.IsPrefab)
            {
                runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
            }
            return;
        }

        // 不進物件池直接銷毀
        if(IsNotPoolObject(runner, context, instance))
        {
            Destroy(instance.gameObject);
            return;
        }

        // 只有 Prefab 類型的物件才進池
        if (context.TypeId.IsPrefab)
        {
            var prefabId = context.TypeId.AsPrefabId;

            if (!Pool.TryGetValue(prefabId, out var stack))
            {
                stack = new Stack<NetworkObject>();
                Pool.Add(prefabId, stack);
            }

            Transform parent = instance.gameObject.transform.parent;

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(parent);
            stack.Push(instance);

            // 告知 Fusion 實例已減少
            runner.Prefabs.RemoveInstance(prefabId);
        }
        else
        {
            // 場景物件或其他特殊物件直接銷毀
            Destroy(instance.gameObject);
        }
    }

    /// <summary>
    /// 是否不進物件池
    /// </summary>
    private bool IsNotPoolObject(NetworkRunner runner, NetworkObjectReleaseContext context, NetworkObject instance)
    {
        if (instance.GetComponent<Player>() != null)
        {
            if (context.TypeId.IsPrefab)
            {
                runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 清空物件池
    /// </summary>
    public void ClearPool()
    {
        foreach (var stack in Pool.Values)
        {
            while (stack.Count > 0)
            {
                var instance = stack.Pop();
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
            }
        }
        Pool.Clear();
        Debug.Log("Fusion 物件池已清理");
    }
}
