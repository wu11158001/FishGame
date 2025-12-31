using UnityEngine;
using UnityEngine.UI;
using System;
using Fusion;

public class GameView : BasicView
{
    [SerializeField] Button ShutdownBtn;

    private void Start()
    {
        ShutdownBtn.onClick.AddListener(Shutdown);
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 斷開連接離開
    /// </summary>
    private void Shutdown()
    {
        AddressableManagement.Instance.ShowLoading();
        NetworkRunnerManagement.Instance.Shutdown();
    }
}
