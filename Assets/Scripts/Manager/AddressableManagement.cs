using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManagement : SingletonMonoBehaviour<AddressableManagement>
{
    //原始解析度比例
    public Vector2 TargetResolution { get; private set; } = new(1920, 1080);

    //避免重複加載資源
    HashSet<ViewEnum> LoadViewAsyncSet = new();
    HashSet<GamePrefabEnum> LoadGamePrefabAsyncSet = new();

    // 紀錄場景已開啟介面
    private Dictionary<ViewEnum, List<PrefabInstance>> SceneViewDic = new();

    // 紀錄全域已開啟介面
    private Dictionary<ViewEnum, List<PrefabInstance>> GlobalViewDic = new();

    // 紀錄已開啟遊戲預製物
    private Dictionary<GamePrefabEnum, List<PrefabInstance>> GamePrefabDic = new();

    private RectTransform SafeArea_Scene;
    private RectTransform SafeArea_Global;

    // 防止Loading物件創建多個
    private GameObject CurrLoadingObj;

    private class PrefabInstance
    {
        public GameObject Go;
        public AsyncOperationHandle<GameObject> Handle;
    }

    /// <summary>
    /// 設置當前場景Canvas
    /// </summary>
    public void SetCanvase()
    {
        try
        {
            SetSafeArea(canvasObj: GameObject.Find("Canvas_Scene"), safeArea: ref SafeArea_Scene);
            SetSafeArea(canvasObj: GameObject.Find("Canvas_Global"), safeArea: ref SafeArea_Global);
        }
        catch (Exception e)
        {
            Debug.LogError($"設置當前場景Canvas錯誤: {e}");
        }
    }

    /// <summary>
    /// 設置介面產生父物件
    /// </summary>
    private void SetSafeArea(GameObject canvasObj, ref RectTransform safeArea)
    {
        if (canvasObj == null) return;

        Transform found = canvasObj.transform.Find("SafeArea");

        if (found != null)
        {
            safeArea = found as RectTransform;
        }
        else
        {
            GameObject newObj = new GameObject("SafeArea", typeof(RectTransform));
            newObj.transform.SetParent(canvasObj.transform, false);
            safeArea = newObj.GetComponent<RectTransform>();
        }

        if (safeArea != null)
        {
            safeArea.anchorMin = new Vector2(0.5f, 0.5f);
            safeArea.anchorMax = new Vector2(0.5f, 0.5f);
            safeArea.sizeDelta = TargetResolution;
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
    private async Task OpenView(ViewEnum viewType, Action<GameObject> callback = null, bool IsCanStack = false, CanvasEnum canvasEnum = CanvasEnum.Canvas_Scene)
    {
        // 避免重複加載資源
        if (LoadViewAsyncSet.Contains(viewType))
            return;

        // 不可重複開啟
        if (!IsCanStack && SceneViewDic.ContainsKey(viewType))
            return;

        LoadViewAsyncSet.Add(viewType);

        try
        {
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(viewType.ToString());
            await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Transform parent = null;
                switch (canvasEnum)
                {
                    case CanvasEnum.Canvas_Scene:
                        parent = SafeArea_Scene;
                        break;

                    case CanvasEnum.Canvas_Global:
                        parent = SafeArea_Global;
                        break;

                    default:
                        parent = SafeArea_Scene;
                        break;
                }

                GameObject prefab = loadHandle.Result;
                GameObject go = Instantiate(prefab, parent);
                go.transform.SetSiblingIndex(parent.childCount + 1);

                var newInstance = new PrefabInstance { Go = go, Handle = loadHandle };

                switch (canvasEnum)
                {
                    case CanvasEnum.Canvas_Scene:
                        if (!SceneViewDic.ContainsKey(viewType))
                            SceneViewDic[viewType] = new List<PrefabInstance>();

                        SceneViewDic[viewType].Add(newInstance);
                        break;

                    case CanvasEnum.Canvas_Global:
                        if (!GlobalViewDic.ContainsKey(viewType))
                            GlobalViewDic[viewType] = new List<PrefabInstance>();

                        GlobalViewDic[viewType].Add(newInstance);
                        break;

                    default:
                        if (!SceneViewDic.ContainsKey(viewType))
                            SceneViewDic[viewType] = new List<PrefabInstance>();

                        SceneViewDic[viewType].Add(newInstance);
                        break;
                }

                callback?.Invoke(go);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"開啟介面錯誤: {e}");
        }
        finally
        {
            LoadViewAsyncSet.Remove(viewType);
        }
    }

    /// <summary>
    /// 移除場景介面
    /// </summary>
    /// <param name="viewEnum">介面</param>
    /// <param name="isFirstRemove">true = 移除先產生, false = 最後產生的移除</param>
    private void RemoveSceneView(ViewEnum viewEnum, bool isFirstRemove = false)
    {
        try
        {
            if (SceneViewDic.TryGetValue(viewEnum, out var list) && list.Count > 0)
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
                    SceneViewDic.Remove(viewEnum);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"移除場景介面錯誤: {e}");
        }
    }

    /// <summary>
    /// 移除全域介面
    /// </summary>
    /// <param name="viewEnum">介面</param>
    /// <param name="isFirstRemove">true = 移除先產生, false = 最後產生的移除</param>
    private void RemoveGlobalView(ViewEnum viewEnum, bool isFirstRemove = false)
    {
        try
        {
            if (GlobalViewDic.TryGetValue(viewEnum, out var list) && list.Count > 0)
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
                    GlobalViewDic.Remove(viewEnum);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"移除全域介面錯誤: {e}");
        }
    }

    /// <summary>
    /// 清除所有場景介面
    /// </summary>
    public void ClearAllSceneViews()
    {
        foreach (var viewList in SceneViewDic.Values)
        {
            foreach (var item in viewList)
            {
                if (item.Go != null) Destroy(item.Go);
                if (item.Handle.IsValid()) Addressables.Release(item.Handle);
            }
        }
        SceneViewDic.Clear();
        LoadViewAsyncSet.Clear();
    }

    #endregion

    #region 介面(Canvas_Scene)

    /// <summary>
    /// 開啟登入介面
    /// </summary>
    public async Task OpenLoginView(bool isLogout = false, Action closeAction = null)
    {
        ViewEnum view = ViewEnum.LoginView;

        Action viewCloseAction = () =>
        {
            closeAction?.Invoke();
            RemoveSceneView(view);
        };

        await OpenView(
            viewType: view,
            callback: (viewObj) =>
            {
                if(viewObj != null)
                {
                    viewObj.GetComponent<LoginView>().SetData(
                        isLogout: isLogout, 
                        closeAction: viewCloseAction);
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
            RemoveSceneView(view);
        };

        await OpenView(
            viewType: view,
            callback: (viewObj) =>
            {
                if (viewObj != null)
                {
                    viewObj.GetComponent<LobbyView>().SetData(closeAction: viewCloseAction);
                }
            });
    }

    /// <summary>
    /// 開啟遊戲介面
    /// </summary>
    public async Task OpenGameView(Action closeAction = null)
    {
        ViewEnum view = ViewEnum.GameView;

        Action viewCloseAction = () =>
        {
            closeAction?.Invoke();
            RemoveSceneView(view);
        };

        await OpenView(
            viewType: view,
            callback: (viewObj) =>
            {
                if (viewObj != null)
                {
                    viewObj.GetComponent<GameView>().SetData(closeAction: viewCloseAction);
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
            RemoveGlobalView(viewEnum: view);
        };

        await OpenView(
            viewType: view,
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
    public async void CloseLoading()
    {
        await Task.Yield();

        if (CurrLoadingObj != null)
        {
            CurrLoadingObj = null;
            RemoveGlobalView(viewEnum: ViewEnum.Loading);
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
            RemoveGlobalView(viewEnum: view, isFirstRemove: true);
        };

        await OpenView(
            viewType: view,
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

    #region 遊戲預製物

    /// <summary>
    /// 清空遊戲預置物
    /// </summary>
    public void ClearGamePrefab()
    {
        foreach (var prefab in GamePrefabDic.Values)
        {
            foreach (var item in prefab)
            {
                if (item.Go != null) Destroy(item.Go);
                if (item.Handle.IsValid()) Addressables.Release(item.Handle);
            }
        }
        GamePrefabDic.Clear();
        LoadGamePrefabAsyncSet.Clear();
    }

    /// <summary>
    /// 創建遊戲預製物
    /// </summary>
    public  async Task CreateGamePrefab(GamePrefabEnum prefabType, Action<GameObject> callback = null)
    {
        // 避免重複加載資源
        if (LoadGamePrefabAsyncSet.Contains(prefabType))
            return;

        LoadGamePrefabAsyncSet.Add(prefabType);

        try
        {
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(prefabType.ToString());
            await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {               
                GameObject prefab = loadHandle.Result;
                GameObject go = Instantiate(prefab);

                var newInstance = new PrefabInstance { Go = go, Handle = loadHandle };

                if (!GamePrefabDic.ContainsKey(prefabType))
                    GamePrefabDic[prefabType] = new List<PrefabInstance>();

                GamePrefabDic[prefabType].Add(newInstance);

                callback?.Invoke(go);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"創建遊戲預製物{prefabType}錯誤: {e}");
        }
        finally
        {
            LoadGamePrefabAsyncSet.Remove(prefabType);
        }
    }

    #endregion
}
