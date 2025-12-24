using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    private Canvas Canvas_Camera;
    private Canvas Canvas_Overlay;

    #region 

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
        catch (Exception)
        {

            throw;
        }
    }

    #endregion

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

    /// <summary>
    /// 移除介面
    /// </summary>
    public void RemoveView(ViewEnum viewEnum)
    {
        try
        {
            if (ViewDic.TryGetValue(viewEnum, out var list) && list.Count > 0)
            {
                // 取出最後一個開啟的介面
                var lastIndex = list.Count - 1;
                var viewItem = list[lastIndex];

                // 摧毀場景上的物件
                if (viewItem.Go != null)
                    Destroy(viewItem.Go);

                // 釋放 Addressables 資源引用
                Addressables.Release(viewItem.Handle);

                // 從列表中移除
                list.RemoveAt(lastIndex);

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

    /// <summary>
    /// 開啟介面
    /// </summary>
    public async Task OpenView(ViewEnum viewEnum, Action<GameObject> callback, bool IsCanStack = false, CanvasEnum canvasEnum = CanvasEnum.Canvas_Overlay)
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
                Transform parent =
                    canvasEnum == CanvasEnum.Canvas_Overlay ?
                    Canvas_Overlay.transform :
                    Canvas_Camera.transform;

                GameObject prefab = loadHandle.Result;
                GameObject go = Instantiate(prefab, parent);

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

    #endregion

    #region 介面

    /// <summary>
    /// 開啟登入介面
    /// </summary>
    public async Task OpenLoginView(Action closeAction = null)
    {
        await OpenView(
            viewEnum: ViewEnum.LoginView,
            callback: (view) =>
            {
                view.GetComponent<LoginView>().SetData(
                    closeAction: () =>
                    {
                        closeAction?.Invoke();
                        RemoveView(ViewEnum.LoginView);
                    });
            });
    }

    #endregion

}
