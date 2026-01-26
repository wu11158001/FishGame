using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class LobbyView : BasicView
{
    [Header("Bottom Area")]
    [SerializeField] RectTransform BottomAreaRect;
    [SerializeField] RectTransform LevelBtnRect;
    [SerializeField] Button LevelBtn;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        LevelBtnRect.DOKill();
        BottomAreaRect.DOKill();
    }

    protected override void Start()
    {
        base.Start();

        // 關卡按鈕上下移動
        LevelBtnRect.DOKill();
        LevelBtnRect.DOLocalMoveY(LevelBtnRect.anchoredPosition.y + 10, 1.5f)
            .SetEase(Ease.InOutSine) 
            .SetLoops(-1, LoopType.Yoyo);

        LevelBtn.onClick.AddListener(() =>
        {
            HideBottomArea();
            AddressableManagement.Instance.OpenLevelView(ShowBottomArea);
        });
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 影藏底部區域
    /// </summary>
    private void HideBottomArea()
    {
        BottomAreaRect.DOKill();
        BottomAreaRect.anchoredPosition = new(0, 0);
        BottomAreaRect.DOAnchorPos(new Vector2(BottomAreaRect.anchoredPosition.x, -BottomAreaRect.sizeDelta.y), PopUpTime)
            .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 顯示底部區域
    /// </summary>
    private void ShowBottomArea()
    {
        BottomAreaRect.DOKill();
        BottomAreaRect.anchoredPosition = new(0, -BottomAreaRect.sizeDelta.y);
        BottomAreaRect.DOAnchorPos(new Vector2(0, 0), PopUpTime)
            .SetEase(Ease.Linear);
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
