using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class LobbyView : BasicView
{
    [Header("LobbyView")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Button StartBtn;
    [SerializeField] Button LogoutBtn;

    Dictionary<CheckJoinRoomDataEnum, bool> CheckJoinRoomDic = new();
    CancellationTokenSource matchmakingCTS;
    bool IsMatchmaking;

    private void OnDestroy()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AsccountDataChangeDelegate -= AccountDataChange;

        if(NetworkRunnerManagement.Instance != null)
            NetworkRunnerManagement.Instance.RoomListUpdatedEvent -= OnRoomListUpdatedUpdate;
    }

    protected override void Start()
    {
        base.Start();

        StartBtn.onClick.AddListener(() => { StartJoInGame(levelType: LevelEnum.DragonLevel); });
        LogoutBtn.onClick.AddListener(Logout);

        NetworkRunnerManagement.Instance.RoomListUpdatedEvent += OnRoomListUpdatedUpdate;

        FirestoreManagement.Instance.AsccountDataChangeDelegate += AccountDataChange;
        FirestoreManagement.Instance.StartListenAccountData();
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
        FirestoreManagement.Instance.StopHeartbeat();

        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Login,
            callback: async () =>
            {
                await AddressableManagement.Instance.OpenLoginView(isLogout: true);
            });

        CloseAction?.Invoke();
    }

    /// <summary>
    /// 加入房間錯誤關閉
    /// </summary>
    private void JoinErrorCancel()
    {
        IsMatchmaking = false;
        Canvas_Global.Instance.CloseLoading();
        Canvas_Global.Instance.CloseSceneLoadingView();
    }

    #region 資料變更監聽

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        if(accountData != null)
        {
            CoinText.text = StringUtility.CurrencyFormat(accountData.Coins);

            Canvas_Global.Instance.CloseLoading();
        }
    }

    #endregion

    #region 加入遊戲

    /// <summary>
    /// 開始加入遊戲
    /// </summary>
    private void StartJoInGame(LevelEnum levelType)
    {
        if (IsMatchmaking) return;

        IsMatchmaking = true;
        Canvas_Global.Instance.ShowLoading();
        CheckJoinRoomDic.Clear();

        foreach (CheckJoinRoomDataEnum item in Enum.GetValues(typeof(CheckJoinRoomDataEnum)))
        {
            CheckJoinRoomDic.Add(item, false);
        }

        TempDataManagement.Instance.Initialize();

        // 獲取所有魚資料
        TempDataManagement.Instance.GetAllFishData(callback: CheckJoinRoomData);

        // 獲取所有砲台資料
        TempDataManagement.Instance.GetAllTurretData(callback: CheckJoinRoomData);

        // 獲取關卡資料
        TempDataManagement.Instance.GetCurrentLevelData(levelType: levelType, callback: CheckJoinRoomData);

        // 獲取帳戶資料
        TempDataManagement.Instance.GetTempAccountData(callback: CheckJoinRoomData);

        // 開始監聽關卡資料
        FirestoreManagement.Instance.StartListenLevelData(levelType: levelType);
    }

    /// <summary>
    /// 檢查加入房間資料獲取狀態
    /// </summary>
    /// <param name="dataType"></param>
    private void CheckJoinRoomData(CheckJoinRoomDataEnum dataType)
    {
        if(!CheckJoinRoomDic.ContainsKey(dataType))
        {
            Debug.LogError($"檢查加入房間資料獲取狀態錯誤: {dataType}");
            JoinErrorCancel();
            return;
        }

        CheckJoinRoomDic[dataType] = true;

        if(CheckJoinRoomDic.All(x => x.Value == true))
        {
            Debug.Log("進入房間資料獲取完成");
            JoInLobby();
        }
    }

    /// <summary>
    /// 加入大廳
    /// </summary>
    private async void JoInLobby()
    {
        NetworkRunnerManagement.Instance.ResetRunner();
        Canvas_Global.Instance.ShowSceneLoadingView();

        var runner = NetworkRunnerManagement.Instance.NetworkRunner;
        runner.ProvideInput = true;

        var result = await runner.JoinSessionLobby(SessionLobby.Shared);

        if (result.Ok)
        {
            Debug.Log("成功加入大廳，開始等待列表同步...");

            // 重置取消令牌
            matchmakingCTS?.Cancel();
            matchmakingCTS = new CancellationTokenSource();

            try
            {
                // 等待 2 秒給予列表同步時間
                await Task.Delay(2000, matchmakingCTS.Token);

                // 如果 2 秒後 IsMatchmaking 還是 true，代表 OnRoomListUpdated 沒找到房
                if (IsMatchmaking)
                {
                    Debug.Log("等待超時，未發現現有房間，準備自行創建...");
                    JoinRoom(Guid.NewGuid().ToString());
                }
            }
            catch (TaskCanceledException )
            {
                Debug.Log("等待期間已經JoinRoom!");
            }
        }
        else
        {
            Debug.LogError("加入大廳失敗!");
            JoinErrorCancel();
        }
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    private async void JoinRoom(string sessionName)
    {
        // 先停止配對計時，避免重複進入此 function
        matchmakingCTS?.Cancel();

        // 如果已經不在配對狀態，就跳出（防止複數次觸發）
        if (!IsMatchmaking) return;
        IsMatchmaking = false;

        Debug.Log($"準備進入房間: {sessionName}");

        // 關閉在大廳中的 Runner
        if (NetworkRunnerManagement.Instance.NetworkRunner.IsCloudReady ||
            NetworkRunnerManagement.Instance.NetworkRunner.IsRunning)
        {
            await NetworkRunnerManagement.Instance.NetworkRunner.Shutdown();
        }

        // 透過 Management 重新取得/重置一個新的 Runner
        NetworkRunnerManagement.Instance.ResetRunner();

        // 執行 StartGame
        var result = await NetworkRunnerManagement.Instance.StartGame(sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入房間: {result.ShutdownReason}");
            JoinErrorCancel();
        }
    }

    /// <summary>
    /// 房間列表更新
    /// </summary>
    private void OnRoomListUpdatedUpdate(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!IsMatchmaking) return;

        // 尋找第一個還沒滿且開啟中的房間
        SessionInfo availableSession = sessionList.FirstOrDefault(s => s.IsOpen && s.PlayerCount < s.MaxPlayers);

        if (availableSession != null)
        {
            Debug.Log($"[列表更新] 找到可用房間: {availableSession.Name}");
            JoinRoom(availableSession.Name);
        }
    }

    #endregion
}
