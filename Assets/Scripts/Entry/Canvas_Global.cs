using UnityEngine;

public class Canvas_Global : SingletonMonoBehaviour<Canvas_Global>
{
    [SerializeField] GameObject LoadingView;
    [SerializeField] GameObject SceneLoadingView;

    private void Start()
    {
        LoadingView.SetActive(false);
        SceneLoadingView.SetActive(false);
    }

    #region Loading

    /// <summary>
    /// 開啟Loading
    /// </summary>
    public void ShowLoading()
    {
        LoadingView.SetActive(true);
        LoadingView.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 關閉Loading
    /// </summary>
    public void CloseLoading()
    {
        LoadingView.SetActive(false);
    }

    #endregion

    #region SceneLoading

    /// <summary>
    /// 開啟場景轉換介面
    /// </summary>
    public void ShowSceneLoadingView()
    {
        SceneLoadingView.SetActive(true);
        SceneLoadingView.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 關閉場景轉換介面
    /// </summary>
    public void CloseSceneLoadingView()
    {
        SceneLoadingView.SetActive(false);
    }

    #endregion
}
