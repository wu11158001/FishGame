using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class LobbyView : BasicView
{
    [Header("LobbyView")]
    [SerializeField] RectTransform LevelBtnRect;
    [SerializeField] Button LevelBtn;

    protected override void Start()
    {
        base.Start();

        // 關卡按鈕上下移動
        LevelBtnRect.DOLocalMoveY(LevelBtnRect.anchoredPosition.y + 10, 1.5f)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo);

        LevelBtn.onClick.AddListener(() =>
        {
            AddressableManagement.Instance.OpenLevelView();
        });
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 登出
    /// </summary>
    private void Logout()
    {
        Canvas_Global.Instance.ShowLoading();
        FirestoreDataManagement.Instance.StopHeartbeat();

        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Login,
            callback: async () =>
            {
                await AddressableManagement.Instance.OpenLoginView(isLogout: true);
            });

        CloseAction?.Invoke();
    }
}
