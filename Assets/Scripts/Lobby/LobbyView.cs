using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Newtonsoft.Json;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class LobbyView : BasicView
{
    [Header("LobbyView")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Button StartBtn;
    [SerializeField] Button LogoutBtn;

    Dictionary<CheckJoinRoomDataEnum, bool> CheckJoinRoomDic = new();

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

        StartBtn.onClick.AddListener(() => { StartJoInGame(levelType: LevelEnum.ClassicLevel); });
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

    /// <summary>
    /// 房間列表更新
    /// </summary>
    private void OnRoomListUpdatedUpdate(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!IsMatchmaking)
            return;

        // 尋找第一個還沒滿的房間
        SessionInfo availableSession = sessionList.FirstOrDefault(s => s.IsOpen && s.PlayerCount < s.MaxPlayers);

        if (availableSession != null)
        {
            Debug.Log($"找到可用房間: {availableSession.Name}，準備加入...");
            JoinRoom(availableSession.Name);
        }
        else
        {
            Debug.Log("目前沒有空房，準備創建新房間...");
            JoinRoom(Guid.NewGuid().ToString());
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

        GameTempDataManagement.Instance.Initialize();

        // 獲取所有魚資料
        GameTempDataManagement.Instance.GetAllFishData(callback: CheckJoinRoomData);

        // 獲取所有砲台資料
        GameTempDataManagement.Instance.GetAllTurretData(callback: CheckJoinRoomData);

        // 獲取關卡資料
        GameTempDataManagement.Instance.GetCurrentLevelData(levelType: levelType, callback: CheckJoinRoomData);

        // 獲取帳戶資料
        GameTempDataManagement.Instance.GetTempAccountData(callback: CheckJoinRoomData);

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

        var runner = NetworkRunnerManagement.Instance.NetworkRunner;

        runner.ProvideInput = true;

        // 加入大廳
        var result = await runner.JoinSessionLobby(SessionLobby.Shared);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入大廳: {result.ShutdownReason}");
            IsMatchmaking = false;
        }
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    private async void JoinRoom(string sessionName)
    {
        var result = await NetworkRunnerManagement.Instance.StartGame(sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入房間: {result.ShutdownReason}");
            Canvas_Global.Instance.CloseLoading();
            IsMatchmaking = false;
        }
    }

    #endregion
}
