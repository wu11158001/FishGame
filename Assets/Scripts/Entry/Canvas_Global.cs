using UnityEngine;

public class Canvas_Global : SingletonMonoBehaviour<Canvas_Global>
{
    [SerializeField] GameObject Loading;

    /// <summary>
    /// 開啟Loading
    /// </summary>
    public void ShowLoading()
    {
        Loading.SetActive(true);
        Loading.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 關閉Loading
    /// </summary>
    public void CloseLoading()
    {
        Loading.SetActive(false);
    }
}
