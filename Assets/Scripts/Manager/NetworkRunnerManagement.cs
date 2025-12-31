using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class NetworkRunnerManagement : SingletonMonoBehaviour<NetworkRunnerManagement>, INetworkRunnerCallbacks
{
    // 房間列表更新
    public delegate void RoomListUpdatedDelegate(NetworkRunner runner, List<SessionInfo> sessionList);
    public event RoomListUpdatedDelegate RoomListUpdatedEvent;

    public delegate void PlayerLeftDelegate(NetworkRunner runner, PlayerRef player);
    public event PlayerLeftDelegate PlayerLeftEvent;

    public NetworkRunner NetworkRunner { get; private set; }
    public NetworkSceneManagerDefault NetworkSceneManagerDefault { get; set; }
    public FusionPoolManager FusionPoolManager { get; set; }

    private void Start()
    {
        NetworkRunner = GetComponent<NetworkRunner>();
        NetworkSceneManagerDefault = GetComponent<NetworkSceneManagerDefault>();
        FusionPoolManager = GetComponent<FusionPoolManager>();
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    public async Task<StartGameResult> StartGame(string sessionName)
    {
        return await NetworkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            ObjectProvider = FusionPoolManager,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex((int)SceneEnum.Game),
            SceneManager = NetworkSceneManagerDefault,
            PlayerCount = 4,
        });

    }

    /// <summary>
    /// 斷開連線
    /// </summary>
    public async Task Shutdown()
    {
        await NetworkRunner.Shutdown(false);

        if (NetworkRunner != null)
        {
            Destroy(NetworkRunner);
            NetworkRunner = null;
        }

        FusionPoolManager.ClearPool();

        await Task.Yield();

        NetworkRunner = gameObject.AddComponent<NetworkRunner>();
    }

    #region NetworkRunnerCallbacks

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData inputData = new();

        inputData.MousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current != null)
            inputData.IsFirePressed = Mouse.current.leftButton.isPressed;

        input.Set(inputData);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
      
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
  
    }

    /// <summary>
    /// 玩家加入
    /// </summary>
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"玩家加入: {player.PlayerId}");
    }

    /// <summary>
    /// 玩家離開
    /// </summary>
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"玩家離開: {player.PlayerId}");

        if (runner.IsSharedModeMasterClient)
        {
            // 尋找場景中所有屬於該斷線玩家的物件
            foreach (var no in runner.GetAllNetworkObjects())
            {
                // 如果該物件的輸入權限屬於該斷線玩家
                if (no.InputAuthority == player)
                {
                    runner.Despawn(no);
                }
            }
        }

        PlayerLeftEvent?.Invoke(runner, player);
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
   
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    /// <summary>
    /// 場景載入完成
    /// </summary>
    public async void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("場景載入完成");

        // 產生遊戲地形
        if(runner.IsSharedModeMasterClient)
        {
            NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                key: NetworkPrefabEnum.GameTerrain,
                Pos: Vector3.zero,
                rot: Quaternion.identity,
                parent: null,
                player: PlayerRef.None);
        }

        AddressableManagement.Instance.SetCanvase();
        await AddressableManagement.Instance.OpenGameView();
    }

    /// <summary>
    /// 場景開始載入
    /// </summary>
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("場景開始載入");
    }

    /// <summary>
    /// 房間列表更新
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        RoomListUpdatedEvent?.Invoke(runner, sessionList);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
      
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
      
    }

    #endregion
}
