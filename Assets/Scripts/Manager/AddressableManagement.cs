using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using Fusion;

public class AddressableManagement : SingletonMonoBehaviour<AddressableManagement>
{
    //避免重複加載資源
    HashSet<ViewEnum> LoadViewAsyncSet = new();

    // 紀錄已開啟介面
    private class ViewInstance
    {
        public GameObject Go;
        public AsyncOperationHandle<GameObject> Handle;
    }
    private Dictionary<ViewEnum, List<ViewInstance>> ViewDic = new();

    // 紀錄遊戲內物件
    private class NetworkObjectInstance
    {
        public GameObject Go;
        public AsyncOperationHandle<GameObject> Handle;
    }
    private Dictionary<GameNetworkObject, NetworkObjectInstance> NetworkObjectDic = new();

    private Canvas Canvas_Camera;
    private Canvas Canvas_Overlay;

    private GameObject CurrLoadingObj;

    /// <summary>
    /// 設置當前場景Canvas
    /// </summary>
    public void SetCanvase()
    {
        try
        {
            Canvas_Camera = GameObject.Find("Canvas_Camera").GetComponent<Canvas>();
            Canvas_Overlay = GameObject.Find("Canvas_Overlay").GetComponent<Canvas>();
        }
        catch (Exception e)
        {
            Debug.LogError($"設置當前場景Canvas錯誤: {e}");
        }
    }

    #region 資源加載

    /// <summary>
    /// 下載預載資源
    /// </summary>
    public async void DownloadPreAssets(Action<float> progressCallback, Action finishCallback)
    {
        try
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync("PreLoad");
            while (!downloadHandle.IsDone)
            {
                Debug.Log($"下載中: {downloadHandle.PercentComplete * 100}%");
                progressCallback?.Invoke(downloadHandle.PercentComplete);
                await Task.Yield();
            }
            Addressables.Release(downloadHandle);

            finishCallback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"下載預載資源錯誤: {e}");
        }        
    }

    #endregion

    #region 介面處理

    /// <summary>
    /// 開啟介面
    /// </summary>
    private async Task OpenView(ViewEnum viewEnum, Action<GameObject> callback = null, bool IsCanStack = false, CanvasEnum canvasEnum = CanvasEnum.Canvas_Overlay)
    {
        // 避免重複加載資源
        if (LoadViewAsyncSet.Contains(viewEnum))
            return;

        // 不可重複開啟
        if (!IsCanStack && ViewDic.ContainsKey(viewEnum))
            return;

        LoadViewAsyncSet.Add(viewEnum);

        try
        {
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(viewEnum.ToString());
            await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Transform parent = null;
                switch (canvasEnum)
                {
                    case CanvasEnum.Canvas_Overlay:
                        parent = Canvas_Overlay.transform;

                        break;
                    case CanvasEnum.Canvas_Camera:
                        parent = Canvas_Camera.transform;
                        break;

                    case CanvasEnum.Canvas_Global:
                        parent = Canvas_Global.Instance.GlobalCanvas.transform;
                        break;

                    default:
                        parent = Canvas_Overlay.transform;
                        break;
                }

                GameObject prefab = loadHandle.Result;
                GameObject go = Instantiate(prefab, parent);
                go.transform.SetSiblingIndex(parent.childCount + 1);

                var newInstance = new ViewInstance { Go = go, Handle = loadHandle };

                if (!ViewDic.ContainsKey(viewEnum))
                    ViewDic[viewEnum] = new List<ViewInstance>();

                ViewDic[viewEnum].Add(newInstance);

                callback?.Invoke(go);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"開啟介面錯誤: {e}");
        }
        finally
        {
            LoadViewAsyncSet.Remove(viewEnum);
        }
    }

    /// <summary>
    /// 移除介面
    /// </summary>
    /// <param name="viewEnum">介面</param>
    /// <param name="isFirstRemove">true = 移除先產生, false = 最後產生的移除</param>
    private void RemoveView(ViewEnum viewEnum, bool isFirstRemove = false)
    {
        try
        {
            if (ViewDic.TryGetValue(viewEnum, out var list) && list.Count > 0)
            {
                // 取出最後一個開啟的介面
                var takeIndex = isFirstRemove ? 0 : list.Count - 1;
                var viewItem = list[takeIndex];

                // 摧毀場景上的物件
                if (viewItem.Go != null)
                    Destroy(viewItem.Go);

                // 釋放 Addressables 資源引用
                Addressables.Release(viewItem.Handle);

                // 從列表中移除
                list.RemoveAt(takeIndex);

                // 如果該類型介面都關了，就把 List 也砍了
                if (list.Count == 0)
                    ViewDic.Remove(viewEnum);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"移除介面錯誤: {e}");
        }
    }

    /// <summary>
    /// 清除所有介面
    /// </summary>
    public void ClearAllViews()
    {
        foreach (var viewList in ViewDic.Values)
        {
            foreach (var item in viewList)
            {
                if (item.Go != null) Destroy(item.Go);
                if (item.Handle.IsValid()) Addressables.Release(item.Handle);
            }
        }
        ViewDic.Clear();
        LoadViewAsyncSet.Clear();
    }

    #endregion

    #region 介面(Canvas_Overlay)

    /// <summary>
    /// 開啟登入介面
    /// </summary>
    public async Task OpenLoginView(Action closeAction = null)
    {
        ViewEnum view = ViewEnum.LoginView;

        Action viewCloseAction = () =>
        {
            closeAction?.Invoke();
            RemoveView(view);
        };

        await OpenView(
            viewEnum: view,
            callback: (viewObj) =>
            {
                if(viewObj != null)
                {
                    viewObj.GetComponent<LoginView>().SetData(closeAction: viewCloseAction);
                }
            });
    }

    /// <summary>
    /// 開啟大廳介面
    /// </summary>
    public async Task OpenLobbyView(Action closeAction = null)
    {
        ViewEnum view = ViewEnum.LobbyView;

        Action viewCloseAction = () =>
        {
            closeAction?.Invoke();
            RemoveView(view);
        };

        await OpenView(
            viewEnum: view,
            callback: (viewObj) =>
            {
                if (viewObj != null)
                {
                    viewObj.GetComponent<LobbyView>().SetData(closeAction: viewCloseAction);
                }
            });
    }

    #endregion

    #region 介面(Canvas_Global)

    /// <summary>
    /// 顯示Loading
    /// </summary>
    public async void ShowLoading()
    {
        ViewEnum view = ViewEnum.Loading;

        Action viewCloseAction = () =>
        {
            CurrLoadingObj = null;
            RemoveView(viewEnum: view);
        };

        await OpenView(
            viewEnum: view,
            callback: (viewObj) =>
            {
                if(viewObj != null)
                {
                    CurrLoadingObj = viewObj;

                    if (viewObj != null)
                    {
                        viewObj.GetComponent<Loading>().SetData(
                            closeAction: viewCloseAction);
                    }
                }                
            },
            canvasEnum: CanvasEnum.Canvas_Global);
    }

    /// <summary>
    /// 關閉Loading
    /// </summary>
    public void CloseLoading()
    {
        if (CurrLoadingObj != null)
        {
            CurrLoadingObj = null;
            RemoveView(viewEnum: ViewEnum.Loading);
        }
    }

    /// <summary>
    /// 顯示吐司訊息
    /// </summary>
    public async void ShowToast(string messageKey)
    {
        ViewEnum view = ViewEnum.Toast;

        Action viewCloseAction = () =>
        {
            RemoveView(viewEnum: view, isFirstRemove: true);
        };

        await OpenView(
            viewEnum: view,
            callback: (viewObj) =>
            {
                if(viewObj != null)
                {
                    viewObj.GetComponent<Toast>().SetData(
                        messageKey: messageKey,
                        closeAction: viewCloseAction);
                }                
            },
            IsCanStack: true,
            canvasEnum: CanvasEnum.Canvas_Global);
    }

    #endregion

    #region 遊戲中物件(NetworkObject)

    /// <summary>
    /// 移除所有遊戲物件
    /// </summary>
    public void RemoveAllNetworkObject()
    {
        try
        {
            foreach (var obj in NetworkObjectDic)
            {
                if (obj.Value.Go != null)
                    Destroy(obj.Value.Go);

                Addressables.Release(obj.Value.Handle);
            }

            NetworkObjectDic.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"移除所有遊戲物件錯誤: {e}");
        }
    }

    /// <summary>
    /// 產生Network遊戲物件
    /// </summary>
    public async Task SapwnNetworkObject(GameNetworkObject gameNetworkObject, Vector3 Pos, PlayerRef player, Transform parent = null)
    {
        var NetworkRunner = NetworkRunnerManagement.Instance.NetworkRunner;

        NetworkRunner.OnBeforeSpawned onBeforeSpawned = (runner, obj) =>
        {
            if (parent != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
            }
        };

        if (NetworkObjectDic.ContainsKey(gameNetworkObject))
        {
            var obj = NetworkObjectDic[gameNetworkObject].Go;
            NetworkRunner.Spawn(obj, Pos, Quaternion.identity, player, onBeforeSpawned);
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(gameNetworkObject.ToString());
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;

            NetworkObjectInstance networkObjectInstance = new()
            {
                Go = prefab,
                Handle = handle,
            };

            NetworkObjectDic.Add(gameNetworkObject, networkObjectInstance);
            NetworkRunner.Spawn(prefab, Pos, Quaternion.identity, player, onBeforeSpawned);
        }
        else
        {
            Debug.LogError("產生遊戲物件失敗");
        }
    }

    #endregion
}
