using UnityEngine;

public class Canvas_Global : SingletonMonoBehaviour<Canvas_Global>
{
    [SerializeField] GameObject LoadingView;

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
}
