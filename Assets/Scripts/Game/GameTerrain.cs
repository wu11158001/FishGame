using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameTerrain : NetworkBehaviour
{
    [SerializeField] List<GameObject> Seats;

    /// <summary> 紀錄座位上玩家ID </summary>
    [Networked, Capacity(4)]
    [OnChangedRender(nameof(OnSpawnLocalObject))]
    private NetworkArray<int> SeatPlayerIDs { get; }

    bool isLocalSpawn;

    private void OnDestroy()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent -= LeftRoom;
    }

    private void Start()
    {
        NetworkRunnerManagement.Instance.PlayerLeftEvent += LeftRoom;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // 初始化座位
            for(int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                SeatPlayerIDs.Set(i, -1);
            }
        }

        StartCoroutine(IJoinSeat());
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    /// <returns></returns>
    private IEnumerator IJoinSeat()
    {
        yield return null;

        if (Object != null && Object.IsValid)
        {
            JoinSeat();
        }

        yield return null;

        OnSpawnLocalObject();
    }

    /// <summary>
    /// 產生本地玩家物件
    /// </summary>
    private void OnSpawnLocalObject()
    {
        if (isLocalSpawn) 
            return;

        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == Runner.LocalPlayer.PlayerId)
            {
                isLocalSpawn = true;

                var pos = Vector3.zero;

                NetworkPrefabManagement.Instance.SpawnNetworkPrefab(
                    key: NetworkPrefabEnum.Player,
                    Pos: Seats[i].transform.position,
                    rot: Quaternion.identity,
                    parent: Seats[i].transform,
                    player: Runner.LocalPlayer);

                break;
            }
        }

        AddressableManagement.Instance.CloseLoading();
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    private void LeftRoom(NetworkRunner runner, PlayerRef player)
    {
        // 原房主離開後，Photon Cloud 會瞬間指派新的 Master Client
        if (Runner.IsSharedModeMasterClient)
        {
            // 請求地形權限
            if (!Object.HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }

            // 請求座位權限
            foreach (var seatGo in Seats)
            {
                if (seatGo != null && seatGo.TryGetComponent<NetworkObject>(out var seatNO))
                {
                    if (!seatNO.HasStateAuthority)
                    {
                        seatNO.RequestStateAuthority();
                    }
                }
            }

            StartCoroutine(IYieldResetSeat(player));
        }
    }

    /// <summary>
    /// 等待獲取權限重設離開玩家座位
    /// </summary>
    /// <param name="leftPlayer"></param>
    /// <returns></returns>
    private IEnumerator IYieldResetSeat(PlayerRef leftPlayer)
    {
        float timer = 0;
        while (!Object.HasStateAuthority && timer < 2.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (Object.HasStateAuthority)
        {
            // 既然我是權限者了，直接改 Networked Array
            for (int i = 0; i < SeatPlayerIDs.Length; i++)
            {
                if (SeatPlayerIDs[i] == leftPlayer.PlayerId)
                {
                    Debug.Log($"[新房主] 成功取得權限，清理座位 Index: {i}");
                    SeatPlayerIDs.Set(i, -1);
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("取得權限超時，無法清理座位");
        }
    }

    /// <summary>
    /// 加入座位
    /// </summary>
    private void JoinSeat()
    {
        RPC_JoinSeat(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_JoinSeat(PlayerRef player)
    {
        // 已經有位置
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == player.PlayerId) 
                return; 
        }

        // 設置座位
        for (int i = 0; i < SeatPlayerIDs.Length; i++)
        {
            if (SeatPlayerIDs[i] == -1)
            {
                SeatPlayerIDs.Set(i, player.PlayerId);
                return;
            }
        }
    }
}
