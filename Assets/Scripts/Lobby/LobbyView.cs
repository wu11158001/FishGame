using UnityEngine;
using UnityEngine.UI;
using System;

public class LobbyView : BasicView
{
    [Header("LobbyView")]
    [SerializeField] Button StartBtn;
    [SerializeField] Button LogoutBtn;

    protected override void Start()
    {
        base.Start();

        StartBtn.onClick.AddListener(JoinGame);
        LogoutBtn.onClick.AddListener(Logout);
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 加入遊戲
    /// </summary>
    private void JoinGame()
    {
        Canvas_Global.Instance.ShowSceneLoadingView();

        // 進入遊戲場景
        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Game,
            callback: async () =>
            {
                await AddressableManagement.Instance.CreateGamePrefab(
                    prefabType: GamePrefabEnum.GameEntry,
                    callback: (obj) =>
                    {
                        GameEntry gameEntry = obj.GetComponent<GameEntry>();
                        if(gameEntry != null)
                        {
                            gameEntry.SetData(levelType: LevelEnum.DragonLevel);
                        }
                    });
            });
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
